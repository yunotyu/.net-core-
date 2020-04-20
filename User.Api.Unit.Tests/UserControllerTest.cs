using FluentAssertions;
using log4net;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading.Tasks;
using User.Api.Controllers;
using User.Api.Data;
using User.Api.Models;
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
            ////�ж����Ľ���ǲ���JSON������ǣ����Գɹ�
            //Assert.IsType<JsonResult>(response);

            var result = response.Should().BeOfType<JsonResult>().Subject;
            var appUser = result.Value.Should().BeAssignableTo<AppUser>().Subject;
            appUser.Id.Should().Be(1);
            appUser.Name.Should().Be("yfr");
        }

        [Fact]
        public async Task Patch_ReturnNewName_WithExpectedNewPara()
        {
            var context = GetUserContext();
            UsersController controller = new UsersController(context);
            //ʹ��jsonpatch��ԭ����ֵ�����滻
            var data = new JsonPatchDocument<AppUser>();
            data.Replace(u => u.Name, "yu");
            var response =await controller.Patch(data);

            var result = response.Should().BeOfType<JsonResult>().Subject;
            var appUser = result.Value.Should().BeAssignableTo<AppUser>().Subject;
            appUser.Name.Should().Be("yu");

            var userModel =await context.AppUser.SingleOrDefaultAsync(u => u.Name == "yu");
            userModel.Should().NotBeNull();
            userModel.Name.Should().Be("yu");
        }
    }
}
