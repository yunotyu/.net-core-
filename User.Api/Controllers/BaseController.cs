using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using User.Api.Entities.Dtos;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace User.Api.Controllers
{
    public class BaseController : ControllerBase
    {
        public UserIdentity UserIdentity
        {
            get
            {
                var userIdentity = new UserIdentity()
                {
                    //返回认证框架里获取jwt的token后的claim信息
                    UserId = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "sub").Value),
                    Name = User.Claims.FirstOrDefault(c => c.Type == "name").Value,
                    Avatar = User.Claims.FirstOrDefault(c => c.Type == "avatar").Value,
                    Title = User.Claims.FirstOrDefault(c => c.Type == "title").Value,
                    Company = User.Claims.FirstOrDefault(c => c.Type == "company").Value
                };
                return userIdentity;
            }
        }

        public BaseController()
        {
        
        }
    }
}
