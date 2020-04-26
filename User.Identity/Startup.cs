using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using User.Identity.Authentication;
using User.Identity.Entities;
using User.Identity.Services;

namespace User.Identity
{
    public class Startup
    {
        private  ConsulService _consulService ;
        //服务注册后返回的状态码
        private  string statusCode=null;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentityServer()
                //添加自定义的认证处理
                    .AddExtensionGrantValidator<SmsAuthCodeValidator>()
                    .AddDeveloperSigningCredential()
                    .AddInMemoryClients(Config.GetClients())
                    .AddInMemoryIdentityResources(Config.GetIdentityResources())
                    .AddInMemoryApiResources(Config.GetApiResources());

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthCodeService, TestAuthCodeService>();
            services.AddSingleton(new HttpClient());
            
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //在程序完全启动后，做一些事
            lifetime.ApplicationStarted.Register(RegisterService);
            lifetime.ApplicationStopped.Register(DelRegisterService);

            app.UseIdentityServer();
            app.UseMvc();
        }

        /// <summary>
        /// 注册这个服务到Consul里
        /// </summary>
        private async void RegisterService()
        {
            try
            {
                //下面的内容可以放在一个配置文件里，然后转换json为对象
                _consulService = new ConsulService();
                _consulService.Id = "identity01";
                _consulService.Name = "identity01";
                _consulService.Tags = new List<string> { "identity01" };
                _consulService.Address = "127.0.0.1";
                _consulService.Port = 5000;
                _consulService.Enable_Tag_Override = false;
                _consulService.checks = new List<ConsulServiceCheck>
                {
                    new ConsulServiceCheck()
                    {
                        Name= "identity01",
                        Http= "http://127.0.0.1:5000/health",
                        Tls_Skip_Verify= true,
                        Method= "GET",
                        Interval= "5s",
                        Timeout= "30s",
                    }
                };
                HttpClient client = new HttpClient();
                var response = await client.PutAsJsonAsync<ConsulService>("http://127.0.0.1:8500/v1/agent/service/register", _consulService);
                statusCode = response.StatusCode.ToString();
            }
            catch(Exception e)
            {

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
                if (string.Equals(statusCode, "ok", StringComparison.CurrentCultureIgnoreCase)) ;
                {
                    HttpClient client = new HttpClient();
                    var response = await client.PutAsync("http://127.0.0.1:8500/v1/agent/service/deregister/" + $"{_consulService.Id}", null);

                }
            }
            catch (Exception)
            {

                
            }
          
        }
    }
}
