using IdentityServer4.Models;
using IdentityServer4.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Identity.Services;

namespace User.Identity.Authentication
{
    /// <summary>
    /// 自定义的根据手机验证码来返回token的验证,使用identity server4来拓展
    /// </summary>
    public class SmsAuthCodeValidator : IExtensionGrantValidator
    {
        //检查用户信息是否正确的服务
        private readonly IUserService _userService;

        //检查验证码是否正确的服务
        private readonly IAuthCodeService _authCodeService;

        public SmsAuthCodeValidator(IUserService userService,IAuthCodeService authCodeService)
        {
            _userService = userService;
            _authCodeService = authCodeService;

        }

        /// <summary>
        /// 验证的类型，在请求里携带的,自己定义的
        /// </summary>
        public string GrantType => "sms_auth_code";

        /// <summary>
        /// 进行验证的主要方法
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            var phone = context.Request.Raw["phone"];
            var code = context.Request.Raw["auth_code"];
            //设一个错误结果，当认证失败时，返回该结果
            var errorValidationResult = new GrantValidationResult(TokenRequestErrors.InvalidGrant);

            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(code))
            {
                 context.Result = errorValidationResult;
                 return;
            }

            //检查验证码是否正确
            if (!_authCodeService.Validate(phone, code))
            {
                context.Result = errorValidationResult;
                return;
            }

            //检查用户是否存在，不存在创建用户
            int userId=await _userService.CheckOrCreate(phone);
            if (userId <= 0)
            {
                context.Result = errorValidationResult;
                return;
            }

            //返回认证成功，用户ID(subject)和认证类型
            context.Result = new GrantValidationResult(userId.ToString(), GrantType);
        }
    }
}
