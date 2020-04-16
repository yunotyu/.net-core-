using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using User.Api.Data;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace User.Api.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : BaseController
    {
        private UserContext _userContext;

        public UsersController(UserContext userContext)
        {
           
            _userContext = userContext;
        }

        [Route("")]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            //加上Include(u => u.Properties)才能获取到导航属性的值
            var user=_userContext.AppUser.AsNoTracking().Include(u => u.Properties).SingleOrDefault(u=>u.Id == UserIdentity.UserId);
            if (user == null)
            {
                throw new UserOperationException($"用户不存在id：{UserIdentity.UserId}"); 
            }
            return Json(user);
        }

    }
}
