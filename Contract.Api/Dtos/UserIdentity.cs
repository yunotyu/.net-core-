﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contract.Api.Dtos
{
    public class UserIdentity
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public string Title { get; set; }
        public string Avatar { get; set; }

    }
}
