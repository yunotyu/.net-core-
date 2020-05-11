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
using Contract.Api.Data;
using DnsClient;
using System.Net;
using Contract.Api.Infrastructure;
using log4net;
using log4net.Repository;
using log4net.Config;
using System.IO;

namespace Contract.Api
{
    public class Startup
    {
        public static ILog logger;
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
            ILoggerRepository _loggerRepository;
            _loggerRepository = LogManager.CreateRepository("NETCoreRepository");

            //当配置文件发生修改时，重新加载文件
            XmlConfigurator.ConfigureAndWatch(_loggerRepository, new FileInfo("Configs/log4net.config"));

            //GetLogger第一个参数是我们在Startup定义的LoggerRepository名字，
            //第二参数是我们在配置文件定义的logger节点的logger对象，可以定义多个logger（日志打印对象）
            logger = LogManager.GetLogger(_loggerRepository.Name, "logger1");

            services.AddScoped<IContactApplyRequestRepository, MongoContactApplyRequestRepository>();
            services.AddScoped<IContactRepository, MongoContactRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped(typeof(ContactContext));
            //将对应的AppSetting去获取配置文件的值，然后注入容器
            //在控制器里可以使用IOption<AppSetting>来获取该值
            services.Configure<AppSetting>(_configuration.GetSection("AppSettings"));

            //清除默认显示的claimtype，采用更加简洁的类型
            System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
            //这里加认证框架，只是为了拿到token里的claim信息，因为认证已经在ocelot实现了
            //认证框架会将jwt转换为正常的对象,所以可以拿到claim,这些claim是查询到用户信息
            //放入UserIdentity类里，以供使用
            services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        //options.MetadataAddress = "http://127.0.0.1:5000/.well-known/openid-configuration";
                        options.RequireHttpsMetadata = false;
                        options.Authority = "http://127.0.0.1:5000";
                        options.Audience = "contact_api";
                        //只有SaveToken才能在这个项目的代码里获取到token的值
                        options.SaveToken = true;
                    });

            services.AddHttpContextAccessor();

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
            services.AddSingleton<Resillience.IHttpClient>(serviceProvider =>
            {
                return serviceProvider.GetRequiredService<ResillienceClientFactory>().GetResillienceHttpClient();
            });

            //添加DSN解析对象的注入
            services.AddSingleton<IDnsQuery>(p =>
            {
                return new LookupClient(IPAddress.Parse("127.0.0.1"), 8600);
            });

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

            app.UseAuthentication();
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
                _consulService.Port = 8008;
                _consulService.Enable_Tag_Override = false;
                _consulService.checks = new List<ConsulServiceCheck>
                {
                    new ConsulServiceCheck()
                    {
                        Name= "contact01check",
                        Http= "http://127.0.0.1:8008/health",
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
