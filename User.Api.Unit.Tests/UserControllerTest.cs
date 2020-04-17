using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading.Tasks;
using User.Api.Controllers;
using User.Api.Data;
using Xunit;

namespace User.Api.Unit.Tests
{
    /// <summary>
    /// 单元测试类
    /// </summary>
    public class UserControllerTest
    {
        //这里是创建一个模拟数据库，用于测试
        private UserContext GetUserContext()
        {
            //需要添加EFcore
            var options = new DbContextOptionsBuilder<UserContext>()
            //需要安装Microsoft.EntityFrameworkCore.InMemory
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

            var userContext = new UserContext(options);

            userContext.AppUser.Add(new Models.AppUser
            {
                Id = 1,
                Name = "yfr"
            });

            userContext.SaveChanges();
            return userContext;
               
        }

        //测试方法，加上[Fact]，命名方式可以安装  请求方式_请求返回值_参数 命名
        [Fact]
        public async Task Get_ReturnRegisterUser_WithExpectedParameters()
        {
            var context = GetUserContext();
            //这里需要安装Moq，使用Moq来模拟某个类的对象
            //这里是模拟log4net的日志 打印对象，如果某个控制器的构造方法，需要传入
            //该对象，就可以使用
            var loggerMoq = new Mock<ILog>();
            var logger = loggerMoq.Object;
            var controller = new UsersController(context);

            var response = await controller.Get();
            //判断最后的结果是不是JSON，如果是，测试成功
            Assert.IsType<JsonResult>(response);
        }
    }
}
