using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using User.Api.Data;
using User.Api.Filters;
using MySql.Data;
using User.Api.Entities;
using System.Net.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using IdentityModel;
using zipkin4net;
using zipkin4net.Tracers.Zipkin;
using zipkin4net.Transport;
using zipkin4net.Transport.Http;
using zipkin4net.Tracers;
using Microsoft.Extensions.Logging.Console;
using zipkin4net.Middleware;
using Nest;

namespace User.Api
{
    public class Startup
    {
        public static ILoggerRepository LoggerRepository { get; set; }
        private ILog logger;
        //异常日志缓存字典
        public static Dictionary<string, string> LogDic = new Dictionary<string, string>();
        private ConsulService _consulService;
        //服务注册后返回的状态码
        private string statusCode = null;

        public Startup(IConfiguration configuration)
        {
            LoggerRepository = LogManager.CreateRepository("NETCoreRepository");
            //当配置文件发生修改时，重新加载文件
            XmlConfigurator.ConfigureAndWatch(LoggerRepository, new FileInfo("Configs/log4net.config"));
            //GetLogger第一个参数是我们在Startup定义的LoggerRepository名字，
            //第二参数是我们在配置文件定义的logger节点的logger对象，可以定义多个logger（日志打印对象）
            logger = LogManager.GetLogger(Startup.LoggerRepository.Name, "logger1");

            //线程扫描写入日志，高并发
            ThreadPool.QueueUserWorkItem((o) =>
            {
                while (true)
                {
                    if (LogDic.Count > 0)
                    {
                        //这里要先把字典缓存的值使用别的数据类型先转换，不然直接使用变量指向，会直接指向原来
                        //缓存字典的值，如果缓存字典清空了，那么该指向也没有值
                        HashSet<KeyValuePair<string, string>> datas = LogDic.ToHashSet();
                        LogDic.Clear();

                        foreach (var data in datas)
                        {
                            var key = data.Key;
                            var msg = data.Value;
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(msg))
                            {
                                if (key.Contains("exception"))
                                {
                                    logger.Error(msg);
                                }
                                if (key.Contains("error"))
                                {
                                    logger.Error(msg);
                                }
                            }
                        }
                    }
                    Thread.Sleep(5000);
                }
            });

            Configuration = configuration;

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //注入NEST的ES连接对象
            services.AddScoped<IElasticClient, ElasticClient>(sp =>
            {
                ElasticClient client;
                try
                {
                    var node = new Uri("http://127.0.0.1:9200");
                    var settings = new ConnectionSettings(node);
                    client = new ElasticClient(settings);
                }
                catch (Exception ex)
                {

                    throw;
                }
                return client;
            });

            services.AddDbContext<UserContext>(options =>
            {
                options.UseMySQL(Configuration.GetConnectionString("Mysql"));
            });

            
            //可以在后面的控制器的构造函数中使用IHttpContextAccessor来使用HttpContext,不能直接在控制器的构造函数里使用HttpContext属性
            //在控制器构造函数中时HttpContext属性的值是null
            services.AddHttpContextAccessor();

            //这里加认证框架，只是为了拿到token里的claim信息，因为认证已经在ocelot实现了
            //认证框架会将jwt转换为正常的对象,所以可以拿到claim,这些claim是查询到用户信息
            //放入UserIdentity类里，以供使用
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                       .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                       {
                           options.Audience = "user_api";
                           //这里的认证地址可以不用写identity server4的地址
                           //因为认证已经在Ocelot项目里配置过了，所以Ocelot会去认证
                           options.Authority = "http://127.0.0.1:5000";
                           options.RequireHttpsMetadata = false;
                           options.SaveToken = true;
                           //options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                           //{
                           //    NameClaimType = JwtClaimTypes.Name,
                           //    RoleClaimType = JwtClaimTypes.Role,
                           //};
                       });


            services.AddMvc(options=>options.Filters.Add(typeof(GlobalExceptionFilter))).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddCap(options =>
            {
                //使用这个后，会在这个DBContext对应的数据库生成cap.published和cap.received这2个表
                //来记录消息发送和接收的状态
                options.UseEntityFramework<UserContext>();

                options.UseRabbitMQ("127.0.0.1");

                //使用一个在网页的消息监控面板
                options.UseDashboard();

                //CAP支持将当前的CAP服务注册到COSNUL里，由consul服务发现
                //这个功能要结合上面的UseDashboard使用
                options.UseDiscovery(o =>
                {
                    //consul的主机名和端口号
                    o.DiscoveryServerHostName = "127.0.0.1";
                    o.DiscoveryServerPort = 8500;

                    //CAP服务的主机名和地址,端口是这个CAP服务的端口号，可以随意写
                    o.CurrentNodeHostName = "127.0.0.1";
                    o.CurrentNodePort = 5800;

                    //CAP注册时的一些信息，注册多个时不能重复,
                    //这里的名字也是注册到consul里的，最好只用字母和数字
                    o.NodeId = 1;
                    o.NodeName = "CAP NO1 Node";

                    //最后访问这个http://本项目地址:本项目端口号/cap/nodes，可以看到面板
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,IApplicationLifetime lifetime,ILoggerFactory loggerFactory)
        {
            //如果是开发环境
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //生产环境
            else
            {
                //能捕获中间件里的异常，无法捕获到直接写再Config()里的代码的异常
                //自定义异常处理中间
                app.UseExceptionHandler((builder) => {
                    builder.Run(HandlerException);
                });

            }

            async Task HandlerException(HttpContext context)
            {
                //获取异常
                var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();

                //?. 检查对象是否为null运算符，如果exceptionFeature为null，不获取后面Error属性的值
                Exception error = exceptionFeature?.Error;

                //把异常添加到队列进行日志打印,使用Guid是防止键名重复
                LogDic.Add(Guid.NewGuid() + "error", error.Message + "\n\r" + error.StackTrace);

                context.Response.ContentType = "text/plain;charset=utf-8";
                await context.Response.WriteAsync("服务器内部未知错误");
            }

            //在程序完全启动后，做一些事
            lifetime.ApplicationStarted.Register(RegisterService);
            lifetime.ApplicationStopped.Register(DelRegisterService);

            //注册到zipkin
            //RegisterZipkinTrace(app,lifetime,loggerFactory);

            app.UseAuthentication();

            app.UseMvc();
            InitUserData(app);
        }

       

        private async Task InitUserData(IApplicationBuilder app, int? retry = 0)
        {
            try
            {
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var userContext = scope.ServiceProvider.GetRequiredService<UserContext>();
                    userContext.Database.Migrate();

                    if (userContext.AppUser.FirstOrDefault() == null)
                    {
                        userContext.AppUser.Add(new Models.AppUser() { Name = "yfr" });
                        userContext.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                //当数据库没初始化完成时，使用多次重试的方法来解决，插入初始化数据失败
                if (retry < 10)
                {
                    await InitUserData(app, retry);
                }
            }

        }

        //需要在每个服务都注册到zipkin，不然某个服务如果用到，没有注册到zipkin，那么无法看到
        public void RegisterZipkinTrace(IApplicationBuilder app,IApplicationLifetime lifetime, ILoggerFactory loggerFactory)
        {
            lifetime.ApplicationStarted.Register(() =>
            {
                zipkin4net.TraceManager.SamplingRate = 1.0f;
                //注册这个服务到zipskin
                var httpSender = new HttpZipkinSender("http://127.0.0.1:9411", "application/json");
                //需要安装zipkin4net.middleware.aspnetcore
                var log = new TracingLogger(loggerFactory, "zipkin4net");
                var tracer = new ZipkinTracer(httpSender, new JSONSpanSerializer(),new Statistics());
                TraceManager.RegisterTracer(tracer);
                TraceManager.Start(log);
            });

            lifetime.ApplicationStopped.Register(() =>
            {
                //停止zipkin对这个服务的监控
                //TraceManager.Stop();
            });

            app.UseTracing("user_api01");
        }


        /// <summary>
        /// 注册这个服务到Consul里
        /// </summary>
        private async void RegisterService()
        {
            try
            {
                _consulService = new ConsulService();
                _consulService.Id = "user01";
                _consulService.Name = "user";
                _consulService.Tags = new List<string> { "user01" };
                _consulService.Address = "127.0.0.1";
                _consulService.Port = 8888;
                _consulService.Enable_Tag_Override = false;
                _consulService.checks = new List<ConsulServiceCheck>
                {
                    new ConsulServiceCheck()
                    {
                        Name= "user01check",
                        Http= "http://127.0.0.1:8888/health",
                        Tls_Skip_Verify= true,
                        Method= "GET",
                        Interval= "5s",
                        Timeout= "30s",
                    }
                };
                using(HttpClient client = new HttpClient())
                {
                    var response = await client.PutAsJsonAsync<ConsulService>("http://127.0.0.1:8500/v1/agent/service/register", _consulService);
                    statusCode = response.StatusCode.ToString();
                }
                                
            }
           catch(Exception e)
            {
                //把异常添加到队列进行日志打印,使用Guid是防止键名重复
                LogDic.Add(Guid.NewGuid() + "error", e.Message + "\n\r" + e.StackTrace);
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
                //把异常添加到队列进行日志打印,使用Guid是防止键名重复
                LogDic.Add(Guid.NewGuid() + "error", e.Message + "\n\r" + e.StackTrace);
            }


        }
    }
}
