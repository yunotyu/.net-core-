using Contract.Api.Data;
using Contract.Api.integrationEvent.Events;
using DotNetCore.CAP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contract.Api.integrationEvent.EventHandling
{
    /// <summary>
    /// 使用CAP订阅ribbitmq里的某个消息,当有这个消息时，就执行对应的函数
    /// 需要将这个类注入到容器里services.AddScoped<UserProfileChangedEventHandler>();
    /// </summary>
    public class UserProfileChangedEventHandler:ICapSubscribe
    {
        private IContactRepository _contactRepository;

        public UserProfileChangedEventHandler(IContactRepository contactRepository)
        {
            _contactRepository = contactRepository;
        }

        /// <summary>
        /// 当发生订阅的事件时，执行的函数
        /// </summary>
        /// UserProfileChangedEvent是消息传过来的值，如一个对象
        /// <returns></returns>
        //CapSubscribe订阅这个消息，要和发布时的值一样
        [CapSubscribe("finbook.userapi.userprofilechange")]
        public async Task TaskUpdateContactInfo(UserProfileChangedEvent @event)
        {
            //如果某个用户修改了资料，会被发送消息到rabbitmq里，
            //然后这个方法接订阅了这个消息，就找出该用户的所有好友，然后修改这些好友里该用户的资料
            await _contactRepository.UpdateContactInfoAsync(new Dtos.UserIdentity
            {
                Id = @event.UserId,
                Name = @event.Name,
                Avatar = @event.Avatar,
                Company = @event.Company,
                Title = @event.Title,

            });
        }
    }
}
