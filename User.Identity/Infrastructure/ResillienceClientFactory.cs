using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.AspNetCore.Http;
using Polly;
using Resillience;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace User.Identity.Infrastructure
{
    /// <summary>
    /// 自定义的HttpClient工厂类
    /// </summary>
    public class ResillienceClientFactory
    {
        private IHttpContextAccessor _httpContextAccessor;

        private readonly ILog _logger;

        //重试次数
        private int _retryCount;

        //允许重试多少次,如果超过这个次数，就熔断
        private int _exceptionCountAllowedBeforeBreaking;

        public ResillienceClientFactory(ILog logger,int retryCount, int exceptionCountAllowedBeforeBreaking, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _retryCount = retryCount;
            _exceptionCountAllowedBeforeBreaking = exceptionCountAllowedBeforeBreaking;
            _httpContextAccessor = httpContextAccessor;
        }
        public ResillienceHttpClient GetResillienceHttpClient()
        {
            //将Policy传递给组合策略
            return new ResillienceHttpClient(_logger,url=>  CreatePolicy(url),_httpContextAccessor);
        }

        /// <summary>
        /// 创建一些策略Policy去处理发生的对应异常
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private IAsyncPolicy[] CreatePolicy(string url)
        {
            return new IAsyncPolicy[]
            {       
                //处理发生HttpRequestException异常时的策略
                    Policy.Handle<HttpRequestException>()
                    //发生这个异常时，要进行的处理，这里是重试
                    //参数1是重试次数，参数2是重试过程等待的时间，参数3是每次重试都会执行的函数
                    .WaitAndRetryAsync(_retryCount,retryAttempt=>TimeSpan.FromSeconds(Math.Pow(2,retryAttempt)),
                    (exception,timespan,retryCount,context)=>{
                        var msg=$"第{retryCount}次重试"+
                        $"of {context.PolicyKey}"+$"due to {exception}";
                        _logger.Warn(msg);
                    }),
                     //处理发生HttpRequestException异常时的策略
                     //因为一个异常可以使用多个策略，给组合策略使用
                    Policy.Handle<HttpRequestException>()
                    //采用熔断器策略，
                    //参数1是运行上面的异常发生多少次，如果多个这个次数，就会熔断
                    //参数2是熔断多长时间，然后重新连接
                    //参数3是一个委托，当熔断器从关闭到熔断（断开）时执行
                    //参数4是一个委托，当熔断器从熔断（断开）到正常闭合时执行的函数
                    .CircuitBreakerAsync(_exceptionCountAllowedBeforeBreaking,TimeSpan.FromSeconds(5),
                    (exception,duration)=>
                    {
                        _logger.Warn("熔断器打开");
                    },()=>
                    {
                        _logger.Warn("熔断器关闭");
                    },()=>
                    {
                        _logger.Warn("熔断时间到");
                    })
            };
        }
    }
}
