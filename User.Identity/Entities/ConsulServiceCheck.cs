using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace User.Identity.Entities
{
    /// <summary>
    /// consul服务健康监控的类
    /// </summary>
    public class ConsulServiceCheck
    {
        public string Name { get; set; }
        public string Http { get; set; }
        public bool Tls_Skip_Verify { get; set; }
        public string Method { get; set; }
        public string Interval { get; set; }
        public string Timeout { get; set; } 
    }
}
