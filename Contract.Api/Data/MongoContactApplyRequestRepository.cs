using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contract.Api.Models;
using MongoDB.Driver;

namespace Contract.Api.Data
{
    public class MongoContactApplyRequestRepository : IContactApplyRequestRepository
    {
        private ContactContext _contactContext;

        public MongoContactApplyRequestRepository(ContactContext contactContext, CancellationToken cancellationToken)
        {
            _contactContext = contactContext;
        }

        public Task<bool> AddRequestAsync(ContactApplyRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        public Task<bool> ApprovalAsync(int applierId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取用户的添加好友列表
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<List<ContactApplyRequest>> GetRequestListAsync(int userId,CancellationToken cancellationToken)
        {
            //需要引入using MongoDB.Driver;
            return (await _contactContext.ContactApplyRequests.FindAsync(u => u.UserId == userId)).ToList(cancellationToken);
        }
    }
}
