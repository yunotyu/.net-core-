using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Contract.Api.Data;
using Contract.Api.Service;
using Contract.Api.Dtos;

namespace Contract.Api.Controllers
{
    [Route("api/contact")]
    public class ContactController : BaseController
    {
        private readonly IContactApplyRequestRepository _contactApplyRequestRepository;
        private readonly IUserService _userService;

        public ContactController(IContactApplyRequestRepository contactApplyRequestRepository, IUserService userService)
        {
            _contactApplyRequestRepository = contactApplyRequestRepository;
            _userService = userService;
        }

        /// <summary>
        /// 获取好友请求列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetApplyRequests()
        {
            var requests = await _contactApplyRequestRepository.GetRequestListAsync(UserIdentity.UserId);
            return Ok();
        }

        /// <summary>
        /// 添加好友请求
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("")]
        public async Task<IActionResult> AddApplyRequest(int userId)
        {
           BaseUserInfo userInfo= _userService.GetBaseUserinfo(userId);
            if (userInfo == null)
            {
                throw new Exception("用户参数错误");
            }
            bool result =await _contactApplyRequestRepository.AddRequestAsync(new Models.ContactApplyRequest()
            {
                UserId=userId,
                ApplierId=UserIdentity.UserId,
                Name=userInfo.Name,
                Company=userInfo.Company,
                Title=userInfo.Title,
                Avatar=userInfo.Avatar
            });

            if (!result)
            {
                return BadRequest();
            }

            return Ok();
        }

        /// <summary>
        /// 通过好友请求
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("apply-request")]
        public async Task<IActionResult> ApprovalApplyRequests(int applierId)
        {
            var result = await _contactApplyRequestRepository.ApprovalAsync(applierId);
            if (!result)
            {
                //日志记录
                return BadRequest();
            }
            return Ok();
        }
    }
}
