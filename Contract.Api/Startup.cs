using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Contract.Api.Service;

namespace Contract.Api
{
    public class Startup
    {
        private IConfiguration _configuration;
        private ConsulService _consulService;
        //服务注册后返回的状态码
        private string statusCode = null;

        public Startup(IConfiguration configuration)
        {
            _configuration=configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            //将对应的AppSetting去获取配置文件的值，然后注入容器
            //在控制器里可以使用IOption<AppSetting>来获取该值
            services.Configure<AppSetting>(_configuration.GetSection("AppSettings"));
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //在程序完全启动后，做一些事
            lifetime.ApplicationStarted.Register(RegisterService);
            lifetime.ApplicationStopped.Register(DelRegisterService);

            app.UseMvc();
        }


        /// <summary>
        /// 注册这个服务到Consul里
        /// </summary>
        private async void RegisterService()
        {
            try
            {
                _consulService = new ConsulService();
                _consulService.Id = "contact01";
                _consulService.Name = "contacts";
                _consulService.Tags = new List<string> { "contact01" };
                _consulService.Address = "127.0.0.1";
                _consulService.Port = 8005;
                _consulService.Enable_Tag_Override = false;
                _consulService.checks = new List<ConsulServiceCheck>
                {
                    new ConsulServiceCheck()
                    {
                        Name= "contact01check",
                        Http= "http://127.0.0.1:8005/health",
                        Tls_Skip_Verify= true,
                        Method= "GET",
                        Interval= "5s",
                        Timeout= "30s",
                    }
                };
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.PutAsJsonAsync<ConsulService>("http://127.0.0.1:8500/v1/agent/service/register", _consulService);
                    statusCode = response.StatusCode.ToString();
                }

            }
            catch (Exception e)
            {
                ////把异常添加到队列进行日志打印,使用Guid是防止键名重复
                //LogDic.Add(Guid.NewGuid() + "error", e.Message + "\n\r" + e.StackTrace);
            }
        }

        /// <summary>
        /// 在程序结束时，删除服务到Consul
        /// </summary>
        private async void DelRegisterService()
        {
            try
            {
                //如果前面的注册成功
                if (string.Equals(statusCode, "ok", StringComparison.CurrentCultureIgnoreCase))
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var response = await client.PutAsync("http://127.0.0.1:8500/v1/agent/service/deregister/" + $"{_consulService.Id}", null);
                    }

                }

            }
            catch (Exception e)
            {
              
            }


        }
    }
}
