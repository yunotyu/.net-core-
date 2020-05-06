using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contract.Api
{
    /// <summary>
    /// 获取appsettings.json配置文件的内容
    /// </summary>
    public class AppSetting
    {
        public string MongoConectionString { get; set; }

        public string MongoDataBaseName { get; set; }

    }
}
