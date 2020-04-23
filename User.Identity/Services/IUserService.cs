using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace User.Identity.Services
{
    public interface IUserService
    {
        /// <summary>
        /// 根据手机号检查用户是否存在，不存在就创建
        /// </summary>
        /// <param name="phone">手机号</param>
        Task<int> CheckOrCreate(string phone);
    }
}
