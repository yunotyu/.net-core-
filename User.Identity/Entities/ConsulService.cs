using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace User.Identity.Entities
{
    /// <summary>
    /// 注册consul服务的类
    /// </summary>
    public class ConsulService
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Tags { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }   
        public bool Enable_Tag_Override { get; set; }
        public List<ConsulServiceCheck> checks { get; set; }
    }
}
