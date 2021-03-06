﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using User.Api.Entities.Dtos;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace User.Api.Controllers
{
    public class BaseController : ControllerBase
    {
        public UserIdentity UserIdentity { get; set; }

        public BaseController()
        {
            UserIdentity = new UserIdentity()
            {
                UserId = 1,
                Name = "yfr"
            };
        }
    }
}
