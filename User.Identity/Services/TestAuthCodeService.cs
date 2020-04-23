using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace User.Identity.Services
{
    public class TestAuthCodeService : IAuthCodeService
    {
        //实际上要到真正验证手机验证码，这是只是测试
        public bool Validate(string phone, string code)
        {
            return true;
        }
    }
}
