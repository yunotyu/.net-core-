using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contract.Api.ViewMpdel
{
    public class TagContactInputViewModel
    {
        /// <summary>
        /// 要打标签的好友id
        /// </summary>
        public int FriendId { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public List<string> Tags { get; set; }
    }
}
