using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using User.Api.Data;

namespace User.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<UserContext>(options => {
                options.UseMySQL(Configuration.GetConnectionString("Mysql"));
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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
    }
}
