﻿using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contract.Api.Models
{
    /// <summary>
    /// 发送好友请求的用户的信息
    /// </summary>
    [BsonIgnoreExtraElements]//在获取MongoDB的文档转换为对象时，忽略_id属性
    public class ContactApplyRequest
    {

        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 名字
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 公司
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 岗位
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// 申请人ID
        /// </summary>
        public int ApplierId { get; set; }

        /// <summary>
        /// 是否通过，0为通过，1为拒绝
        /// </summary>
        public int Approvaled { get; set; }

        /// <summary>
        /// 处理时间
        /// </summary>
        public DateTime HandledTime { get; set; }

        /// <summary>
        /// 发送好友申请时间
        /// </summary>
        public DateTime ApplyTime { get; set; }
    }
}
