using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace User.Api.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }

        /// <summary>
        /// 职位
        /// </summary>
        public string Title { get; set; }
        public string Phone { get; set; }
        
        /// <summary>
        /// 头像地址
        /// </summary>
        public string Avatar { get; set; }

        public byte Gender { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Tel { get; set; }

        /// <summary>
        /// 省ID
        /// </summary>
        public int ProvincedId { get; set; }

        public string Province  { get; set; }

        public int CityId { get; set; }
        public string City { get; set; }

        /// <summary>
        /// 名片地址
        /// </summary>
        public string NameCard { get; set; }

        /// <summary>
        /// 用户属性列表
        /// </summary>
        public List<UserProperty> Properties{ get; set; }




    }
}
