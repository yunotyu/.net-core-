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
    /// ��Ԫ������
    /// </summary>
    public class UserControllerTest
    {
        //�����Ǵ���һ��ģ�����ݿ⣬���ڲ���
        private UserContext GetUserContext()
        {
            //��Ҫ���EFcore
            var options = new DbContextOptionsBuilder<UserContext>()
            //��Ҫ��װMicrosoft.EntityFrameworkCore.InMemory
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

        //���Է���������[Fact]��������ʽ���԰�װ  ����ʽ_���󷵻�ֵ_���� ����
        [Fact]
        public async Task Get_ReturnRegisterUser_WithExpectedParameters()
        {
            var context = GetUserContext();
            //������Ҫ��װMoq��ʹ��Moq��ģ��ĳ����Ķ���
            //������ģ��log4net����־ ��ӡ�������ĳ���������Ĺ��췽������Ҫ����
            //�ö��󣬾Ϳ���ʹ��
            var loggerMoq = new Mock<ILog>();
            var logger = loggerMoq.Object;
            var controller = new UsersController(context);

            var response = await controller.Get();
            //�ж����Ľ���ǲ���JSON������ǣ����Գɹ�
            Assert.IsType<JsonResult>(response);
        }
    }
}
