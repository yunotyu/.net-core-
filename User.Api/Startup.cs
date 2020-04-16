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

namespace User.Api
{
    public class Startup
    {
        public static ILoggerRepository LoggerRepository { get; set; }

        public static Dictionary<string, string> LogDic = new Dictionary<string, string>();

        public Startup(IConfiguration configuration)
        {
            LoggerRepository = LogManager.CreateRepository("NETCoreRepository");
            //当配置文件发生修改时，重新加载文件
            XmlConfigurator.ConfigureAndWatch(LoggerRepository, new FileInfo("Configs/log4net.config"));
            //GetLogger第一个参数是我们在Startup定义的LoggerRepository名字，
            //第二参数是我们在配置文件定义的logger节点的logger对象，可以定义多个logger（日志打印对象）
            ILog logger = LogManager.GetLogger(Startup.LoggerRepository.Name, "logger1");
        
            //线程扫描写入日志，高并发
            ThreadPool.QueueUserWorkItem((o) =>
            {
                while (true)
                {
                    if (LogDic.Count > 0)
                    {
                        //这里要先把字典缓存的值使用别的数据类型先转换，不然直接使用变量指向，会直接指向原来
                        //缓存字典的值，如果缓存字典清空了，那么该指向也没有值
                        HashSet<KeyValuePair<string,string>> datas = LogDic.ToHashSet();
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
            services.AddDbContext<UserContext>(options => {
                options.UseMySQL(Configuration.GetConnectionString("Mysql"));
            });

          
            services.AddMvc(options=> {
                //添加全局异常过滤器
                options.Filters.Add<GlobalExceptionFilter>();
                }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
           

            //处理全局异常,任何地方的异常都能捕获到
            app.UseExceptionHandler(builder=> {
                builder.Run(async context=> {
                    
                    //获取异常
                    var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                   
                    //?. 检查对象是否为null运算符，如果exceptionFeature为null，不获取后面Error属性的值
                    Exception error = exceptionFeature?.Error;
                   
                    //把异常添加到队列进行日志打印,使用Guid是防止键名重复
                    LogDic.Add(Guid.NewGuid() + "error", error.Message + "/r/n" + error.StackTrace);

                    //如果是开发环境
                    if (env.IsDevelopment())
                    {
                        app.UseDeveloperExceptionPage();
                    }
                    //生产环境
                    else
                    {
                        await context.Response.WriteAsync(new JsonResult("服务器内部未知错误").ToString());
                    }

                });
            });

            app.UseMvc();
            int a = 0;
            int b = 7 / a;
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
    }
}
