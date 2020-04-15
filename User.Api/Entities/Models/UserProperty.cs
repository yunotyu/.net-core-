using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace User.Api.Models
{
    /// <summary>
    /// 用户的一些属性
    /// </summary>
    public class UserProperty
    {
        public int AppUserId { get; set; }

        /// <summary>
        /// 投资行业的分类
        /// </summary>
        public string Key { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }

    }
}
