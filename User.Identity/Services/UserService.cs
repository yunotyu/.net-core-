using DnsClient;
using log4net;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using User.Identity.Entities;
using Newtonsoft.Json;

namespace User.Identity.Services
{
    public class UserService : IUserService
    {
        private readonly Resillience.IHttpClient _client;
        private  readonly ILog _logger;
        /// <summary>
        /// 用户服务所在的服务器地址
        /// </summary>
        private readonly string _userServiceUrl;

        //存储全部User服务实例的全部地址
        private readonly List<string> _userUrls;
    

        public  UserService(Resillience.IHttpClient client,IDnsQuery dnsQuery)
        {
            _client = client;
            _logger = Startup.logger;
            _userUrls = new List<string>();
            try
            {
                //因为是查询consul的某个服务的服务实例，根据consul文档格式为：服务名.service.consul
                //所以第一个参数是：service.consul
                //第二参数是要查询的服务名
                var services= dnsQuery.ResolveServiceAsync("service.consul", "user").Result;
                foreach(var service in services)
                {
                    //返回查询到user服务下的第1个服务实例的ip和端口,给这个identity sever4去进行验证使用
                    var host = service.AddressList.First();
                    var port = service.Port;
                    var url = $"http://{host}:{port}";
                    //存储全部能用的user服务的实例地址
                    _userUrls.Add(url);
                }
                
            }
            catch (Exception e)
            {

            }
        }

        public async Task<UserInfo> CheckOrCreate(string phone)
        {
            var form = new Dictionary<string, string> { { "phone", phone } };
            //使用x-www-form-urlencoded在postman提交数据
            //var content = new FormUrlEncodedContent(form);
            try
            {
                var response = await _client.PostAsync(HttpMethod.Post, form, _userUrls[0] + "/api/users/check-or-create");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var userStr = await response.Content.ReadAsStringAsync();
                    var userInfo= JsonConvert.DeserializeObject<UserInfo>(userStr);
                    if (userInfo != null)
                    {
                        return userInfo;
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("CheckOrCreate函数在重试后失败," + ex.Message + "," + ex.StackTrace);
                throw ex;
            }
            //如果不成功，返回id为0
            return null;
        }
    }
}
