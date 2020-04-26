using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.AspNetCore.Http;
using Polly;
using Polly.Wrap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Resillience
{
    public class ResillienceHttpClient : IHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggerRepository _loggerRepository;
        private readonly ILog _logger;

        //根据某个URL，创建对象的policy
        private readonly Func<string, IEnumerable<IAsyncPolicy>> _policyCreator;

        //打包后的policy组合放入一个字典里
        private readonly ConcurrentDictionary<string, AsyncPolicyWrap> _policyWraps;

        //需要安装Microsoft.AspNetCore.Http
        private IHttpContextAccessor _httpContextAccessor;

        public ResillienceHttpClient(IHttpContextAccessor httpContextAccessor)
        {
            _loggerRepository = LogManager.CreateRepository("NETCoreRepository");
            //当配置文件发生修改时，重新加载文件
            XmlConfigurator.ConfigureAndWatch(_loggerRepository, new FileInfo("Configs/log4net.config"));
            //GetLogger第一个参数是我们在Startup定义的LoggerRepository名字，
            //第二参数是我们在配置文件定义的logger节点的logger对象，可以定义多个logger（日志打印对象）
            _logger = LogManager.GetLogger(_loggerRepository.Name, "logger1");
            _policyWraps = new ConcurrentDictionary<string, AsyncPolicyWrap>();
            _httpContextAccessor = httpContextAccessor;
            _httpClient = new HttpClient();
        }

        public Task<HttpResponseMessage> PostAsync<T>(HttpMethod method,string url, T item, string authorizationToken, string requestId = null, string authorizationMethod = "Bearer")
        {
            if (method != HttpMethod.Post && method != HttpMethod.Put)
            {
                throw new ArgumentException("not put or post method");
            }

            var origin = GetOriginFromUri(url);
            return HttpInvoker<HttpResponseMessage>(origin, async (context) =>
            {
                var requestMessage = new HttpRequestMessage(method, url);
                SetAuthorizationHeader(requestMessage);
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(item), System.Text.Encoding.UTF8, "application/json");
                if (authorizationToken != null)
                {
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(authorizationMethod, authorizationToken);
                }
                if (requestId != null)
                {
                    requestMessage.Headers.Add("x-requestid", requestId);
                }
                var response = await _httpClient.SendAsync(requestMessage);
                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    throw new HttpRequestException();
                }
                return response;
            });
        }

        private async Task<T> HttpInvoker<T>(string origin, Func<Context,Task<T>> action)
        {
            var normallizeOrigin = NormalizeOrigin(origin);
            if(!_policyWraps.TryGetValue(normallizeOrigin,out AsyncPolicyWrap policyWrap))
            {
                policyWrap = Policy.WrapAsync(_policyCreator(normallizeOrigin).ToArray());
                _policyWraps.TryAdd(normallizeOrigin, policyWrap);
            }
            return await policyWrap.ExecuteAsync(action, new Context(normallizeOrigin));
        }

        private string NormalizeOrigin(string origin)
        {
            return origin?.Trim()?.ToLower();
        }

        private static string GetOriginFromUri(string uri)
        {
            var url = new Uri(uri);
            var origin = $"{url.Scheme}://{url.DnsSafeHost}:{url.Port}";
            return origin;
        }

        private void SetAuthorizationHeader(HttpRequestMessage requestMessage)
        {
            var authorizationHeader = _httpContextAccessor.HttpContext.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                requestMessage.Headers.Add("Authorization", new List<string>() { authorizationHeader });
            }
        }
    }
}
