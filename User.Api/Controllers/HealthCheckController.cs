using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;


namespace User.Api.Controllers
{
    /// <summary>
    /// 专门用来做服务健康检查的
    /// </summary>
    [Route("/health")]
    public class HealthCheckController : ControllerBase
    {
        // GET: api/<controller>
        [HttpGet]
        [Route("")]
        public IActionResult Get()
        {
            return Ok();
        }

       
    }
}
