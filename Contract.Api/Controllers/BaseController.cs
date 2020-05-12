using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Contract.Api.Dtos;
using Microsoft.AspNetCore.Http;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Contract.Api.Controllers
{
    public class BaseController : ControllerBase
    {
        public UserIdentity UserIdentity {
            get;
        }

        public BaseController(IHttpContextAccessor httpContextAccessor)
        {
            var userIdentity = new UserIdentity();
            if (httpContextAccessor.HttpContext.User.Claims.Count() > 0)
            {
                //返回认证框架里获取jwt的token后的claim信息
                userIdentity.Id = Convert.ToInt32(httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "sub").Value);
                userIdentity.Name = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "name").Value;
                userIdentity.Avatar = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "avatar").Value;
                userIdentity.Title = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "title").Value;
                userIdentity.Company = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "company").Value;

                UserIdentity = userIdentity;
            }
        }
               
    }
}
