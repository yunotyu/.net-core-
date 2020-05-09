using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DnsClient;
using IdentityServer4.Services;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Resillience;
using User.Identity.Authentication;
using User.Identity.Entities;
using User.Identity.Infrastructure;
using User.Identity.Services;

namespace User.Identity
{
    public class Startup
    {
        public static ILog logger;
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

            //替换默认的IProfileService，返回自己定义的claim
            services.AddTransient<IProfileService, ProfileService>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthCodeService, TestAuthCodeService>();

        
            ILoggerRepository _loggerRepository;
            _loggerRepository = LogManager.CreateRepository("NETCoreRepository");

                //当配置文件发生修改时，重新加载文件
             XmlConfigurator.ConfigureAndWatch(_loggerRepository, new FileInfo("Configs/log4net.config"));

            //GetLogger第一个参数是我们在Startup定义的LoggerRepository名字，
            //第二参数是我们在配置文件定义的logger节点的logger对象，可以定义多个logger（日志打印对象）
             logger = LogManager.GetLogger(_loggerRepository.Name, "logger1");
            

            //注入自定义HttpClient对象的工厂类
            services.AddSingleton(typeof(ResillienceClientFactory), sp =>
             {
                 IHttpContextAccessor contextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                
                 //这些可以配置在配置文件里
                 //重试次数
                 int retryCount = 3;
                 //允许重试多少次,如果超过这个次数，就熔断
                 int exceptionCountAllowedBeforeBreaking = 3;

                 return new ResillienceClientFactory(logger, retryCount, exceptionCountAllowedBeforeBreaking, contextAccessor);

             });

            //注入自定义的HttpClient对象
            services.AddSingleton<Resillience.IHttpClient>(serviceProvider=> 
            {
                return serviceProvider.GetRequiredService<ResillienceClientFactory>().GetResillienceHttpClient();
            });

            //添加DSN解析对象的注入
            services.AddSingleton<IDnsQuery>(p=>
            {
                return new LookupClient(IPAddress.Parse("127.0.0.1"), 8600);
            });
        
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
                _consulService.Name = "identity";
                _consulService.Tags = new List<string> { "identity01" };
                _consulService.Address = "127.0.0.1";
                _consulService.Port = 5000;
                _consulService.Enable_Tag_Override = false;
                _consulService.checks = new List<ConsulServiceCheck>
                {
                    new ConsulServiceCheck()
                    {
                        Name= "identitycheck",
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
