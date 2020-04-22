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
        private readonly string _userServiceUrl = "http://localhost:8888";

        public UserService(HttpClient client)
        {
            _client = client;
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
