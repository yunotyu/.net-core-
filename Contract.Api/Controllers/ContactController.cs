using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Contract.Api.Data;
using Contract.Api.Service;
using Contract.Api.Dtos;
using System.Threading;
using Contract.Api.ViewMpdel;
using Microsoft.AspNetCore.Authorization;

namespace Contract.Api.Controllers
{
    [Route("api/contacts")]
    public class ContactController : BaseController
    {
        private readonly IContactApplyRequestRepository _contactApplyRequestRepository;
        private readonly IContactRepository _contactRepository;
        private readonly IUserService _userService;

        public ContactController(IContactApplyRequestRepository contactApplyRequestRepository, IUserService userService, IContactRepository contactRepository)
        {
            _contactApplyRequestRepository = contactApplyRequestRepository;
            _contactRepository =contactRepository;
            _userService = userService;
        }


        /// <summary>
        /// 获取用户通讯录列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Get(int userId)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            var data = await _contactRepository.GetContactsAsync(userId, tokenSource.Token);
            if (data == null)
            {
                return Ok("");
            }
            return Ok(data );
        }

        /// <summary>
        /// 给用户好友打标签
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("tag")]
        public async Task<IActionResult> TagContact(TagContactInputViewModel viewModel)
        {
            var result=await _contactRepository.TagContactAsync(UserIdentity.UserId, viewModel.FriendId, viewModel.Tags);
            if (result)
            {
                return Ok();
            }
            //日志
            return BadRequest();
        }

        /// <summary>
        /// 获取好友请求列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("apply-requests")]
        public async Task<IActionResult> GetApplyRequests()
        {
            var requests = await _contactApplyRequestRepository.GetRequestListAsync(UserIdentity.UserId);
            return Ok();
        }

        /// <summary>
        /// 添加好友请求，userId是要添加的好友的Id
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("apply-requests/{userId}")]
        public async Task<IActionResult> AddApplyRequest(int userId)
        {
           //BaseUserInfo userInfo= _userService.GetBaseUserinfo(userId);
            if (UserIdentity == null)
            {
                throw new Exception("用户参数错误");
            }
            bool result =await _contactApplyRequestRepository.AddRequestAsync(new Models.ContactApplyRequest()
            {
                UserId=userId,
                ApplierId=UserIdentity.UserId,
                Name= UserIdentity.Name,
                Company= UserIdentity.Company,
                Title= UserIdentity.Title,
                Avatar= UserIdentity.Avatar,
                ApplyTime=DateTime.Now
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
        [Route("apply-requests")]
        public async Task<IActionResult> ApprovalApplyRequests(int applierId)
        {
            var result = await _contactApplyRequestRepository.ApprovalAsync(UserIdentity.UserId, applierId);
            if (!result)
            {
                //日志记录
                return BadRequest();
            }

            //申请人的信息
            var applierMsg =await _userService.GetBaseUserinfo(applierId);
            //当前用户的信息
            var userMsg = await _userService.GetBaseUserinfo(UserIdentity.UserId);

            //因为添加好友是双向的，各自的通讯录都有对方
            //往当前用户的MongoDB通讯录添加好友
            await _contactRepository.AddContactAsync(UserIdentity.UserId, applierMsg);
            //往申请人的MongoDB通讯录添加好友
            await _contactRepository.AddContactAsync(applierMsg.UserId, userMsg);

            return Ok();
        }
    }
}
