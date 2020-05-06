using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using User.Api.Data;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.JsonPatch;
using User.Api.Models;

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

        /// <summary>
        /// 获取用户数据 
        /// </summary>
        /// <returns>返回一个JSON对象给前端,因为是ControllerBase,是WEBAPI的风格，所以会帮我们转换为JSON对象</returns>
        [Route("")]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            //加上Include(u => u.Properties)才能获取到导航属性的值
            var user= await _userContext.AppUser.AsNoTracking().Include(u => u.Properties).SingleOrDefaultAsync(u=>u.Id == UserIdentity.UserId);
            if (user == null)
            {
                throw new UserOperationException($"用户不存在id：{UserIdentity.UserId}"); 
            }
            return Ok(user);
        }

        
        /// <summary>
        /// 做负载均衡测试
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("port")]
        [HttpGet]
        public IActionResult GetPort(int id)
        {
            return Ok(Request.HttpContext.Connection.LocalPort);
        }

        /// <summary>
        /// 更新用户数据
        /// </summary>
        /// <param name="patch"></param>
        /// <returns></returns>
        [Route("")]
        [HttpPatch]
        public async Task<IEnumerable<object>> Patch([FromBody]JsonPatchDocument<AppUser>patch)
        {
            var user = await _userContext.AppUser.Include(u => u.Properties).SingleOrDefaultAsync(u => u.Id == UserIdentity.UserId);

            using (var tra = _userContext.Database.BeginTransaction())
            {
                try
                {
                    //清除所有元素，再次添加新的属性
                    user.Properties.Clear();

                    //将JsonPatch里面的值赋给user
                    patch.ApplyTo(user);

                    _userContext.Update(user);
                    _userContext.SaveChanges();
                    tra.Commit();
                }
                catch (Exception ex)
                {
                    tra.Rollback();
                }
            }

            return new object[] { user };
        }

        /// <summary>
        /// 根据手机检查用户是否存在
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        [Route("check-or-create")]
        [HttpPost]
        public async Task<int> CheckOrCreateUser([FromForm]string phone)
        {
            //throw new Exception("熔断器错误");
            var user = await _userContext.AppUser.SingleOrDefaultAsync(u => u.Phone == phone);
            //TODO:要对手机号码进行验证
            if (user == null)
            {
                _userContext.AppUser.Add(new AppUser { Phone = phone });
                _userContext.SaveChanges();
            }
            return user.Id;
        }


        /// <summary>
        /// 获取用户标签
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("tags")]
        public async Task<IActionResult> GetUserTags()
        {
            return new JsonResult(_userContext.UserTag.Where(u=>u.UserId==UserIdentity.UserId).ToList());
        }

        /// <summary>
        /// 根据手机号查询用户资料
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("search")]
        public async Task<IActionResult> Search(string phone)
        {
           return Ok( await _userContext.AppUser.Include(u => u.Properties).SingleOrDefaultAsync(u=>u.Phone==phone));
        }

        /// <summary>
        /// 更新用户标签
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("update/tags")]
        public async Task<IActionResult> UpdateUserTags(List<string> tags)
        {

                var userTags = await _userContext.UserTag.Where(u => u.UserId == UserIdentity.UserId).ToListAsync();
                //Except比较两个序列的每一个值，然后得到不到的值的集合
                var newTags = tags.Except(userTags.Select(t => t.Tag));
                //使用AddRangeAsync一次插入多条数据
                await _userContext.AddRangeAsync(newTags.Select(tag => new UserTag
                {
                    UserId = UserIdentity.UserId,
                    Tag = tag
                }));

            await _userContext.SaveChangesAsync();
            return Ok();
        }
    }

        
}
