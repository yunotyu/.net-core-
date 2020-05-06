using Contract.Api.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contract.Api.Data
{
    interface IContactRepository
    {
        Task<bool> UpdateContactInfo(BaseUserInfo userInfo);
    }
}
