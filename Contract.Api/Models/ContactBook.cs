using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contract.Api.Models
{
    [BsonIgnoreExtraElements]//在获取MongoDB的文档转换为对象时，忽略_id属性
    public class ContactBook
    {
        public ContactBook()
        {
            Contacts = new List<Contact>();
        }

        public int UserId { get; set; }

        /// <summary>
        /// 联系人列表
        /// </summary>
        public List<Contact> Contacts { get; set; }

    }
}
