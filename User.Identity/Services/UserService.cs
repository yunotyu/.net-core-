using DnsClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace User.Identity.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _client;

        /// <summary>
        /// 用户服务所在的服务器地址
        /// </summary>
        private readonly string _userServiceUrl;

        public  UserService(HttpClient client,IDnsQuery dnsQuery)
        {
            _client = client;
            try
            {
                //因为是查询consul的某个服务的服务实例，根据consul文档格式为：服务名.service.consul
                //所以第一个参数是：service.consul
                //第二参数是要查询的服务名
                var services= dnsQuery.ResolveServiceAsync("service.consul", "user").Result;
                //返回查询到user服务下的第1个服务实例的ip和端口,给这个identity sever4去进行验证使用
                var host = services.First().AddressList.First();
                var port = services.First().Port;
                
                _userServiceUrl = $"http://{host}:{port}";
            }
            catch (Exception e)
            {

            }
        }

        public async Task<int> CheckOrCreate(string phone)
        {
            var form = new Dictionary<string, string> { { "phone", phone } };
            //使用x-www-form-urlencoded在postman提交数据
            var content = new FormUrlEncodedContent(form);
            var response = await _client.PostAsync(_userServiceUrl + "/api/users/check-or-create", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var userId =await  response.Content.ReadAsStringAsync();
                int.TryParse(userId, out int intUserId);
                return intUserId;
            }
            //如果不成功，返回id为0
            return 0;
        }
    }
}
