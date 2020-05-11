using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Contract.Api.Dtos;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Contract.Api.Controllers
{
    public class BaseController : ControllerBase
    {
        public UserIdentity UserIdentity {
            get;
        }

        public BaseController()
        {
            var userIdentity = new UserIdentity();
            //返回认证框架里获取jwt的token后的claim信息
            userIdentity.UserId = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "sub").Value);
            userIdentity.Name = User.Claims.FirstOrDefault(c => c.Type == "name").Value;
            userIdentity.Avatar = User.Claims.FirstOrDefault(c => c.Type == "avatar").Value;
            userIdentity.Title = User.Claims.FirstOrDefault(c => c.Type == "title").Value;
            userIdentity.Company = User.Claims.FirstOrDefault(c => c.Type == "company").Value;
                
            UserIdentity = userIdentity;
        }
    }
}
