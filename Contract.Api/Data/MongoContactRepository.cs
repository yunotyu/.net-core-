using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contract.Api.Dtos;
using Contract.Api.Models;
using MongoDB.Driver;

namespace Contract.Api.Data
{
    public class MongoContactRepository : IContactRepository
    {

        private ContactContext _contactContext;

        public MongoContactRepository(ContactContext contactContext)
        {
            _contactContext = contactContext;
        }

        /// <summary>
        /// 添加到联系人列表
        /// </summary>
        /// <param name="baseUser"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> AddContactAsync(int userId,BaseUserInfo applierInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            //如果用户没有通讯录，先创建
            if((await _contactContext.ContactBooks.CountDocumentsAsync(c => c.UserId == userId)) == 0)
            {
                await _contactContext.ContactBooks.InsertOneAsync(new ContactBook() { UserId = userId });
            }


            //先找到当前用户，然后把这个好友的信息添加到MongoDB里该用户的记录里
            var filter = Builders<ContactBook>.Filter.Eq(c => c.UserId, userId);
            var update = Builders<ContactBook>.Update.AddToSet(c => c.Contacts, new Contact()
            {
                UserId = applierInfo.UserId,
                Avatar = applierInfo.Avatar,
                Company= applierInfo.Company,
                Name= applierInfo.Name,
                Title= applierInfo.Title,
            });
            var result = await _contactContext.ContactBooks.UpdateOneAsync(filter, update);

            return result.ModifiedCount == result.MatchedCount && result.MatchedCount == 1;
        }


        /// <summary>
        /// 获取对应用户的通讯录
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<Contact>> GetContactsAsync(int userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            //获取对应用户的通讯录
           var contactBook= await(await _contactContext.ContactBooks.FindAsync(c => c.UserId == userId)).FirstOrDefaultAsync();
           return contactBook.Contacts;
        }

       
        /// <summary>
        /// 给某个用户的好友打标签
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="friendId"></param>
        /// <param name="tags"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> TagContactAsync(int userId, int friendId, List<string> tags, CancellationToken cancellationToken = default(CancellationToken))
        {
            //c=>c.UserId
            //filter设置为选择用户ID和某个好友ID的筛选 
            var filter = Builders<ContactBook>.Filter.And( Builders<ContactBook>.Filter.Eq("ContactBook.UserId", userId), Builders<ContactBook>.Filter.Eq("Contacts.UserId", friendId));
            var update = Builders<ContactBook>.Update.Set("Contacts.$.Tags", tags);

            var result= await _contactContext.ContactBooks.UpdateOneAsync(filter, update);
            return result.MatchedCount == result.ModifiedCount;
        }

        public async Task<bool> UpdateContactInfoAsync(BaseUserInfo userInfo, CancellationToken cancellationToken)
        {
            //如果某个用户修改了资料，这里找出他的所有好友，然后修改这些好友里的他的资料
            var allFriend = (await _contactContext.ContactBooks.FindAsync(u => u.UserId == userInfo.UserId)).ToList();

            //如果没有好友，直接返回
            if (allFriend == null)
            {
                return true;
            }

            //然后要更新上面修改资料的用户里的所有好友里该用户的资料

            //找出所有拥有修改资料用户好友的id
            var ids = allFriend.Select(c => c.UserId);
            //创建一个filter用于选择某个用户里有上面修改资料用户的信息
            //And:可以传入多个用于选择的条件，参数类型都是FilterDefinition，所以都是使用Builders<ContactBook>.Filter.xxx的格式
            //In: 参数1：要被进行选择的字段， 参数2：参数1字段的值的集合
            //ElemMatch: 参数1：返回一个值的集合  参数2：对参数1的每个元素进行判断的条件，如果为 true，返回该元素
            FilterDefinition<ContactBook> filter = 
                Builders<ContactBook>.Filter.And(Builders<ContactBook>.Filter.In(c => c.UserId, ids), Builders<ContactBook>.Filter.ElemMatch(c => c.Contacts, contact => contact.UserId ==userInfo.UserId));

            //更新每个用户里该好友的数据
            //这里是更新ContactBook文档下的子文档，其实就是一个对象里包含着另外一个对象
            //"Contacts.$.Name"代表ContactBook类下的Contacts属性的Name属性，$：代表在使用filter进行筛选后，获取符合条件的第一个元素
            var update = Builders<ContactBook>.Update.Set("Contacts.$.Name", userInfo.Name)
                                                     .Set("Contacts.$.Avatar", userInfo.Avatar)
                                                     .Set("Contacts.$.Company", userInfo.Company)
                                                     .Set("Contacts.$.Title", userInfo.Title);

            //更新数据
            var result = _contactContext.ContactBooks.UpdateMany(filter, update);

            //判断匹配的元素个数和修改的元素个数是否相等
            return result.MatchedCount == result.ModifiedCount;
        }
    }
}
