using IdentityServer4.Models;
using IdentityServer4.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace User.Identity.Authentication
{
    /// <summary>
    /// 使用这个类，将自定义的claim放在token返回,需要在startup注册
    /// </summary>
    public class ProfileService : IProfileService
    {
        /// <summary>
        /// 操作这个用户请求里包含的claim
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var subject=context.Subject==null?throw new ArgumentException(nameof(context.Subject)):context.Subject;
            var subjectId = subject.Claims.Where(c => c.Type == "sub").FirstOrDefault().Value;
            if(!int.TryParse(subjectId,out int userId))
            {
                throw new ArgumentException("invalid subjectId identifier");
            }
            //将自定义的全部claim运行返回给客户端
            context.IssuedClaims = context.Subject.Claims.ToList();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 判断这个请求的用户是否有效，如用户已经注销，那么就是无效的
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task IsActiveAsync(IsActiveContext context)
        {
            var subject = context.Subject == null ? throw new ArgumentException(nameof(context.Subject)) : context.Subject;
            var subjectId = subject.Claims.Where(c => c.Type == "sub").FirstOrDefault().Value;
            //用户有效
            context.IsActive = int.TryParse(subjectId, out int userId);
            return Task.CompletedTask;
        }
    }
}
