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
using DnsClient;

namespace Resillience
{
    /// <summary>
    /// 自定义的HttpClient类，用来发送自己的请求
    /// </summary>
    public class ResillienceHttpClient : IHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILog _logger;

        //根据某个URL，创建对象的policy
        private readonly Func<string, IEnumerable<IAsyncPolicy>> _policyCreator;

        //打包后的policy组合放入一个字典里
        private readonly ConcurrentDictionary<string, AsyncPolicyWrap> _policyWrapsDic;

        //需要安装Microsoft.AspNetCore.Http
        private IHttpContextAccessor _httpContextAccessor;


        public ResillienceHttpClient(ILog logger , Func<string,IEnumerable<IAsyncPolicy>> policyCreator, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            //组合策略PolicyWrap
            _policyWrapsDic = new ConcurrentDictionary<string, AsyncPolicyWrap>();
            _policyCreator = policyCreator;
            _httpContextAccessor = httpContextAccessor;
            _httpClient = new HttpClient();
        }

        public Task<HttpResponseMessage> PostAsync(HttpMethod method, Dictionary<string, string> form, string url, string authorizationToken, string requestId = null, string authorizationMethod = "Bearer")
        {
             Func<HttpRequestMessage> func = () => CeateHttpRequestMessage(method, url, form);
                //发送Http post请求, default(T)某个类型时，会产生默认值，例如 int类型为0，自定义类型为null
             return DoPostAsync(method, url, func, authorizationToken, requestId, authorizationMethod);
        
        }

        public Task<HttpResponseMessage> PostAsync<T>(HttpMethod method,  T item, string url, string authorizationToken, string requestId = null, string authorizationMethod = "Bearer")
        {
            Func<HttpRequestMessage> func = () => CeateHttpRequestMessage(method, url, item);
            //发送Http post请求
            return DoPostAsync(method,url, func, authorizationToken, requestId, authorizationMethod);
        }


        /// <summary>
        /// 发送HTTP POST请求
        /// </summary>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="requestMessage"></param>
        /// <param name="authorizationToken"></param>
        /// <param name="requestId"></param>
        /// <param name="authorizationMethod"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> DoPostAsync(HttpMethod method,string url, Func<HttpRequestMessage> func,string authorizationToken, string requestId = null, string authorizationMethod = "Bearer")
        {
            if (method != HttpMethod.Post && method != HttpMethod.Put)
            {
                throw new ArgumentException("not put or post method");
            }
            //获取一部分URL
            var baseUrl = GetOriginFromUri(url);
            
            //将要发送请求的代码传递给这个函数，这个函数会执行对应的代码
            //并使用上面组合策略对这些代码进行监控，如果发生对应的异常，就会使用策略
            return HttpInvoker<HttpResponseMessage>(baseUrl,  async() =>
            {
                //注意HttpRequestMessage要每次发送请求都是不一样的对象，不能发送多个请求采用一样的对象
                //所以要定义在这个函数里，每次重试发送请求都是不一样的对象
                HttpRequestMessage requestMessage = func();

                //下面都是HTTP请求内容的设置
                SetAuthorizationHeader(requestMessage);
                if (authorizationToken != null)
                {
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(authorizationMethod, authorizationToken);
                }
                if (requestId != null)
                {
                    requestMessage.Headers.Add("x-requestid", requestId);
                }
                var response = await _httpClient.SendAsync(requestMessage);
                //抛出异常，让策略Policy进行处理
                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    throw new HttpRequestException();
                }
           
                return response;
            });
        }
        
        /// <summary>
        /// 创建HTTP请求的请求体，参数部分，这里使用JSON形式提交
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private HttpRequestMessage CeateHttpRequestMessage<T>(HttpMethod method,string url,T item)
        {
            return new HttpRequestMessage(method, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(item), System.Text.Encoding.UTF8, "application/json")
            };
        }


        /// <summary>
        /// 创建HTTP请求的请求体，参数部分，这里使用x-www-form-urlencoded参数方式提交
        /// </summary>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="form"></param>
        /// <returns></returns>
        private HttpRequestMessage CeateHttpRequestMessage(HttpMethod method, string url, Dictionary<string,string> form)
        {
            return new HttpRequestMessage(method, url)
            {
                Content = new FormUrlEncodedContent(form)
            };
        }

        /// <summary>
        /// 将多个Policy打包成组合策略,并执行要使用这些策略监控的函数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        private  Task<T> HttpInvoker<T>(string url, Func<Task<T>> action)
        {
            var normalUrl = NormalizeOrigin(url);
            if(!_policyWrapsDic.TryGetValue(normalUrl, out AsyncPolicyWrap policyWrap))
            {
                //将多个Policy打包成组合策略
                //这里的normalUrl的作用是，给每个identity server4要访问的URL单独一个组合策略，然后放到字典里
                policyWrap = Policy.WrapAsync(_policyCreator(normalUrl).ToArray());
                _policyWrapsDic.TryAdd(normalUrl, policyWrap);
            }
            //在执行action这个委托的函数时，使用上面的组合策略去监控这个代码执行过程
            //new Context(normalUrl) 给每个策略的上下文使用一个key，从而能分别出来

            return policyWrap.ExecuteAsync(action);
        }

        private string NormalizeOrigin(string url)
        {
            return url?.Trim()?.ToLower();
        }

        /// <summary>
        /// 获取要访问的前面部分的URL:协议+主机+端口号
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static string GetOriginFromUri(string uri)
        {
            var url = new Uri(uri);
            var originurl = $"{url.Scheme}://{url.DnsSafeHost}:{url.Port}";
            return originurl;
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
