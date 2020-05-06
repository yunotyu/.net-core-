using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contract.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Contract.Api.Data
{
    public class ContactContext
    {
        //mongodb的数据库
        private IMongoDatabase _database;

        //mongodb的集合
        private IMongoCollection<ContactBook> _collection;

        //获取json配置文件的类
        private AppSetting _appSetting;

        //因为在Startup使用了services.Configure<AppSetting>(_configuration.GetSection("AppSettings"))添加配置到IOption里;
        //所以IOptionsSnapshot来获取这个对象，IOptionsSnapshot是会随着配置文件的改变而实时改变的
        public ContactContext(IOptionsSnapshot<AppSetting> settings)
        {
            _appSetting = settings.Value;
            var client = new MongoClient(_appSetting.MongoConectionString);
            if (client != null)
            {
                _database = client.GetDatabase(_appSetting.MongoDataBaseName);
            }
        }

        /// <summary>
        /// 用户通讯录
        /// </summary>
        public IMongoCollection<ContactBook> ContactBooks
        {
            get
            {
                return _database.GetCollection<ContactBook>("ContactBooks");
            }
        }


        /// <summary>
        /// 好友申请记录
        /// </summary>
        public IMongoCollection<ContactApplyRequest> ContactApplyRequests
        {
            get
            {
                return _database.GetCollection<ContactApplyRequest>("ContactApplyRequest");
            }
        }


        /// <summary>
        /// 因为MongoDB不会创建不存在的集合，所以需要检查是否创建
        /// </summary>
        /// <param name="collectionName"></param>
        private async void CheckAndCreateCollection(string collectionName)
        {
            var collectionList = _database.ListCollections().ToList();
            var collectionNames = new List<string>();
            collectionList.ForEach(b => collectionNames.Add(b["name"].AsString));
            if (!collectionNames.Contains(collectionName))
            {
                await _database.CreateCollectionAsync(collectionName);
            }
        }
    }

}
