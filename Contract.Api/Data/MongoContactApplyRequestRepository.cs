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

        public MongoContactApplyRequestRepository(ContactContext contactContext)
        {
            _contactContext = contactContext;
        }

        /// <summary>
        /// 添加好友请求到MongoDB里
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> AddRequestAsync(ContactApplyRequest request, CancellationToken cancellationToken)
        {
            //获取这个好友请求是否在MongoDB有记录，如果有，计算个数
           var count= await _contactContext.ContactApplyRequests.CountDocumentsAsync<ContactApplyRequest>(r => r.ApplierId == request.ApplierId);
           
            //已经发送过好友申请，更新申请时间
            if (count > 0)
            {
                var filter = Builders<ContactApplyRequest>.Filter.Where(a => a.ApplierId == request.ApplierId && a.UserId == request.UserId);
                var update= Builders<ContactApplyRequest>.Update.Set(a => a.ApplyTime, DateTime.Now);

                //UpdateOptions里的IsUpsert，可以用在下面的UpdateOneAsync中，
                //如果值为没有存在这条记录就会创建(该条记录里的所有值都要一样)，如果存在就更新
                //var option = new UpdateOptions { IsUpsert = true };
                var result= await _contactContext.ContactApplyRequests.UpdateOneAsync(filter, update);
                
                //修改的元素和匹配的元素对应，并且MongoDB里只能有一条该用户申请好友的记录
                return result.MatchedCount == result.ModifiedCount && result.MatchedCount == 1;

            }
            //创建一个好友申请记录在MongoDB
            await _contactContext.ContactApplyRequests.InsertOneAsync(request);
            return true;
        }

        /// <summary>
        /// 通过好友请求
        /// </summary>
        /// <param name="applierId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> ApprovalAsync(int userId,int applierId, CancellationToken cancellationToken)
        {
            var filter = Builders<ContactApplyRequest>.Filter.Where(a => a.UserId == userId && a.ApplierId == applierId);

            //将MongoDB里的某个用户的好友请求设置通过
            var update= Builders<ContactApplyRequest>.Update.Set(a => a.Approvaled, 1)
                                                 .Set(a=>a.HandledTime,DateTime.Now);
            var result=await _contactContext.ContactApplyRequests.UpdateOneAsync(filter, update);

            return result.MatchedCount == result.ModifiedCount && result.MatchedCount == 1;
        }

        /// <summary>
        /// 从MongoDB获取用户的添加好友列表
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
