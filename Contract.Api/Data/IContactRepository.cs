using Contract.Api.Dtos;
using Contract.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Contract.Api.Data
{
    public interface IContactRepository
    {
        /// <summary>
        /// 更新联系人信息
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        Task<bool> UpdateContactInfoAsync(BaseUserInfo userInfo,CancellationToken cancellationToken = default(CancellationToken));
        
        /// <summary>
        /// 添加联系人信息
        /// </summary>
        /// <param name="baseUser"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> AddContactAsync(int userId, BaseUserInfo applierInfo, CancellationToken cancellationToken=default(CancellationToken));

        /// <summary>
        /// 获取联系人列表
        /// </summary>
        /// <returns></returns>
        Task<List<Contact>> GetContactsAsync(int userId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// 给好友打标签
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        Task<bool> TagContactAsync(int userId,int friendId,List<string> tags, CancellationToken cancellationToken = default(CancellationToken));
    }
}
