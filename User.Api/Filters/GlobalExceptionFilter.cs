using log4net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace User.Api.Filters
{
    /// <summary>
    /// 异常全局过滤器
    /// </summary>
    public class GlobalExceptionFilter : IExceptionFilter
    {
        

        //获取当前运行的服务器环境
        private readonly IHostingEnvironment _env;

        public GlobalExceptionFilter(IHostingEnvironment env)
        {
            _env = env;
        }

        void IExceptionFilter.OnException(ExceptionContext context)
        {
            //发生知道的错误
            if (context.Exception.GetType() == typeof(UserOperationException))
            {
                context.Result = new JsonResult("用户信息异常");
                //将异常信息放入缓存
                Startup.LogDic.Add(Guid.NewGuid().ToString()+"exception", context.Exception.Message);
            }
            //发生未知错误
            else
            {
                //打印错误和堆栈信息
                Startup.LogDic.Add(Guid.NewGuid()+"error", context.Exception.Message + ":  " + context.Exception.StackTrace);
               
                //如果是开发环境
                if (_env.IsDevelopment())
                {
                    //返回详细信息
                    context.Result = new JsonResult(context.Exception.Message + ":  " + context.Exception.StackTrace);
                }
                //生产环境，返回信息
                context.Result = new JsonResult("未知内部错误");
            }
            //表示异常已经被处理完成
            context.ExceptionHandled = true;
        }
    }
}
