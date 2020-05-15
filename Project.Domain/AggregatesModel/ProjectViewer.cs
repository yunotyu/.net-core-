﻿using System;

namespace Project.Domain.AggregatesModel
{
    public class ProjectViewer
    {
        public int ProjectId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Avatar { get; set; }
        public DateTime CreateTime { get; set; }
    }
}