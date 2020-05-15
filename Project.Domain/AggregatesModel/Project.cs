using System;
using System.Collections.Generic;
using System.Text;

namespace Project.Domain.AggregatesModel
{
    public class Project
    {
        public int UserId { get; set; }
        public string Avatar { get; set; }
        public string Company { get; set; }

        /// <summary>
        /// 原BP文件地址
        /// </summary>
        public string OriginBPFile { get; set; }

        /// <summary>
        /// 转换后的BP文件地址
        /// </summary>
        public string FormatBPFile { get; set; }

        /// <summary>
        /// 是否显示敏感信息
        /// </summary>
        public bool ShowSecurityInfo { get; set; }

        /// <summary>
        /// 公司所在省ID
        /// </summary>
        public int ProvinceId { get; set; }

        /// <summary>
        /// 公司所在城市ID
        /// </summary>
        public int CityID { get; set; }

        /// <summary>
        /// 公司所在城市名称
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// 区域ID
        /// </summary>
        public int AreaId { get; set; }

        /// <summary>
        ///区域名称 
        /// </summary>
        public string AreaName { get; set; }

        /// <summary>
        /// 公司注册时间
        /// </summary>
        public DateTime RegisterTime { get; set; }

        /// <summary>
        /// 项目基本信息
        /// </summary>
        public string Introduction { get; set; }

        /// <summary>
        /// 让出股票比例
        /// </summary>
        public string FinPercentage { get; set; }

        /// <summary>
        /// 融资阶段
        /// </summary>
        public string FinStage { get; set; }

        /// <summary>
        /// 融资金额 单位（万）
        /// </summary>
        public int FinMoney { get; set; }

        /// <summary>
        /// 收入  单位（万）
        /// </summary>
        public int Income { get; set; }

        /// <summary>
        /// 利润 单位（万）
        /// </summary>
        public int Revenue { get; set; }

        /// <summary>
        /// 估值 单位（万）
        /// </summary>
        public int Valuation { get; set; }

        /// <summary>
        /// 是否委托给平台进行客户寻找
        /// </summary>
        public bool OnPlatform { get; set; }

        /// <summary>
        /// 佣金分配方式
        /// </summary>
        public int BrokerageOptions { get; set; }

        /// <summary>
        /// 可见范围的设置
        /// </summary>
        public ProjectVisibleRule Visible { get; set; }

        /// <summary>
        /// 根引用项目ID
        /// </summary>
        public int SourceId { get; set; }

        /// <summary>
        /// 上级引用项目ID
        /// </summary>
        public int ReferenceId { get; set; }

        /// <summary>
        /// 项目标签
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// 项目属性：行业领域 融资币种
        /// </summary>
        public List<ProjectProperty> Properties { get; set; }

        /// <summary>
        /// 贡献者列表
        /// </summary>
        public List<ProjectContributor> Contributor { get; set; }

        /// <summary>
        /// 查看者
        /// </summary>
        public List<ProjectViewer> Viewers { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        public  Project CloneProject(Project source=null)
        {
            if (source == null)
            {
                source = this;
            }
            var newProject = new Project
            {
                AreaId = source.AreaId,
                AreaName = source.AreaName,
                Avatar = source.Avatar,
                City = source.City,
                CityID = source.CityID,
                Company = source.Company,
                Contributor = source.Contributor,
                Viewers = source.Viewers,
                CreateTime = source.CreateTime,
                FinMoney = source.FinMoney,
                BrokerageOptions = source.BrokerageOptions,
                FinPercentage = source.FinPercentage,
                FinStage = source.FinStage,
                Income = source.Income,
                Introduction = source.Introduction,
                FormatBPFile = source.FormatBPFile,
                OnPlatform = source.OnPlatform,
                OriginBPFile = source.OriginBPFile,
                ProvinceId = source.ProvinceId,
                ReferenceId = source.ReferenceId,
                RegisterTime = source.RegisterTime,
                Revenue = source.Revenue,
                ShowSecurityInfo = source.ShowSecurityInfo,
                SourceId = source.SourceId,
                Tags = source.Tags,
                UpdateTime = source.UpdateTime,
                UserId = source.UserId,
                Valuation = source.Valuation,
                Visible = source.Visible,
            };
            newProject.Properties = new List<ProjectProperty> { };
            foreach(var item in source.Properties)
            {
                newProject.Properties.Add(new ProjectProperty(item.Key, item.Value, item.Text));

            }
            return newProject;
        }

    }
}
