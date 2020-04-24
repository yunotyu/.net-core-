using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace User.Api.Models
{
    /// <summary>
    /// 对某个用户的备注
    /// </summary>
    public class UserTag
    {
        public int UserId { get; set; }
        public string Tag { get; set; }

    }
}
