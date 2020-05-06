using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contract.Api.Models;

namespace Contract.Api.Data
{
    public interface IContactApplyRequestRepository
    {
        /// <summary>
        /// 添加申请好友的请求
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<bool> AddRequestAsync(ContactApplyRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// 通过好友请求 
        /// </summary>
        /// <param name="applierId"></param>
        /// <returns></returns>
        Task<bool> ApprovalAsync(int applierId,CancellationToken cancellationToken);

        /// <summary>
        /// 获取好友申请列表
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<List<ContactApplyRequest>> GetRequestListAsync(int userId, CancellationToken cancellationToken);
    }
}
