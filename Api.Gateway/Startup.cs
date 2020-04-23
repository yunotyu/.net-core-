using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Consul;

namespace Gateway.Api
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //设置这个认证的Schem的名字
            string authenticationProviderSchem = "finbook";

            //在这里添加identity server4的认证，当要访问这个网关的某个下游服务时，需要使用identity server4提供的token
            //所以需要单独发请求先去获取identity server4提供的token，然后利用这个token来访问
            //需要在Ocelot的配置文件配置哪个下游服务需要认证
            services.AddAuthentication()
                .AddIdentityServerAuthentication(authenticationProviderSchem,options=>
                {
                    //去哪个地址验证这个token是否正确
                    options.Authority = "http://localhost:5000";
                    //来请求访问的token的api的名字
                    options.ApiName = "gateway_api";
                    options.SupportedTokens = IdentityServer4.AccessTokenValidation.SupportedTokens.Both;
                    //来请求访问的token的秘钥
                    options.ApiSecret = "secret";
                    //可以不用HTTPS请求访问
                    options.RequireHttpsMetadata = false;
                });

            //Ocelot.DependencyInjection
            services.AddOcelot()
                //添加Ocelot支持Consul
                 .AddConsul()
                .AddConfigStoredInConsul();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //Ocelot.DependencyInjection;
            app.UseOcelot().Wait();
        }
    }
}
