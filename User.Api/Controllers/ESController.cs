using Microsoft.AspNetCore.Mvc;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Api.Models;

namespace User.Api.Controllers
{
    [Route("/api/es")]
    public class ESController:ControllerBase
    {
        private readonly IElasticClient _esClient;

        public ESController(IElasticClient client)
        {
            _esClient = client;
        }

        /// <summary>
        /// 创建ES的索引
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("")]
        public async Task<IActionResult> CreateEsIndex()
        {
            try
            {
                var response = await _esClient.CreateIndexAsync("users", c =>
                {
                    return c.Mappings(ms =>
                    {
                        return ms.Map<AppUser>(m =>
                        {
                            return m.AutoMap<UserProperty>().AutoMap<AppUser>();
                        });
                    });
                });
                //写入成功
                if (response.IsValid)
                {
                    return Ok();
                }
            }
            catch (Exception e)
            {

                throw;
            }
            return BadRequest();
        }

        /// <summary>
        /// 添加多个测试数据到es
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("insert/many")]
        public async Task<IActionResult> InsertMany()
        {
            List<AppUser> users = new List<AppUser>();
            for(int i = 100; i < 200; i++)
            {
                users.Add(new AppUser
                {
                    Id = i,
                    Name = "testName" + i
                });
            }
            var res =await _esClient.IndexManyAsync(users, "users", typeof(AppUser));
            if (res.IsValid)
            {
                return Ok();
            }
            return BadRequest();
        }

        /// <summary>
        /// 插入文档数据
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("insert")]
        public async Task<IActionResult> InsertDoc([FromBody]AppUser u)
        {
            //判断索引是否存在
            var indexExist= await _esClient.IndexExistsAsync("users");
            //判断文档是否存在，这里是根据索引名字（数据库），索引type（表），和该文档的id来判断的（主键）
            IExistsResponse existsResponse = await _esClient.DocumentExistsAsync(new DocumentExistsRequest("users", "appuser", u.Id));

            if (indexExist.Exists && !existsResponse.Exists)
            {
                ////创建索引users，如果索引不存在会自动创建，并将上面user的值写入该索引的文档
                var response = await _esClient.IndexAsync(u, index =>
                {
                    return index.Index("users");
                });

                //写入成功
                if (response.IsValid)
                {
                    return Ok();
                }
            }
            return BadRequest();
        }

        /// <summary>
        /// 删除索引
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("del")]
        public async Task<IActionResult> DelIndex([FromForm] string indexName)
        {
            await _esClient.DeleteIndexAsync(new DeleteIndexRequest(Indices.Index(indexName)) );
            return Ok();
        }

        /// <summary>
        /// 删除文档
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("del/doc")]
        public async Task<IActionResult> DelDoc([FromForm]int id)
        {
            //判断文档是否存在，这里是根据索引名字（数据库），索引type（表），和该文档的id来判断的（主键）
            IExistsResponse existsResponse = await _esClient.DocumentExistsAsync(new DocumentExistsRequest("users", "appuser", id));
            if (existsResponse.Exists)
            {
                //删除对应id的文档
                var res= await _esClient.DeleteByQueryAsync<AppUser>(d =>
                {
                    return d.Index("users").Type("appuser").Query(q => q.Term(tm => tm.Field(new Field("id")).Value(id)));
                });
                if (res.IsValid)
                {
                    return Ok();
                }
                return BadRequest();
            }
            return NoContent();
        }

        /// <summary>
        /// 添加新字段到索引,这里其实用不到，因为如果你的实体类添加了一个新的属性
        /// 那么在新添加文档到该索引时，会自动映射到新属性到该文档
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("add/field")]
        public async Task<IActionResult> AddIndexField()
        {
            var res= _esClient.Map<AppUser>(m =>
            m.Index("users")
            //这里的Text是该字段的类型
            .Properties(p=>p.Text(s=>s.Name("area").Index(true)))
            );
            if (res.IsValid)
            {
                return Ok();
            }
            return BadRequest();
        }


        /// <summary>
        /// 查询索引的文档
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpPost]
        public async Task<IActionResult> QueryEsIndex()
        {
            try
            {
                var res = await _esClient.SearchAsync<AppUser>(s =>
                     {
                         s.Index(Indices.Index("users"));
                         return s.MatchAll();
                     });
                return Ok(res.Documents.ToList());
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
        /// <summary>
        /// 根据姓名进行查询
        /// 中文名查询，可使用模糊查询，但是英文的必须是一个完整单词，这跟分词器有关
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("query")]
        public async Task<IActionResult> QueryEsByName([FromQuery]string name)
        {
            try
            {
                if (!string.IsNullOrEmpty(name))
                {
                    //查询users索引里名为name的字段的值为传递进行的参数的文档
                    //注意这里的字段Field的值都为小写
                    var res = await _esClient.SearchAsync<AppUser>(s =>
                                  {
                                      s.Index(Indices.Index("users"));
                                      return s.Query(q =>
                                      {
                                          return q.Match(m =>
                                          {
                                              return m.Field("name")
                                                      .Query(name);
                                          });
                                      });
                                  });
                    if (res.Documents.Count > 0)
                    {
                        return Ok(res.Documents.ToList());
                    } 
                }
                return NoContent();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// 更新文档
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("update/doc")]
        public async Task<IActionResult> UpdateDoc([FromForm]int id)
        {
            //获取到对应的id的文档
            var path = new DocumentPath<AppUser>(id);
            //对该文档进行更新
            var res= await _esClient.UpdateAsync(path,d=>
            {
                return d.Index("users").Type(typeof(AppUser)).Doc(new AppUser() { Name = "余", Company = "电信", Address = "广东" });
            });
            if (res.IsValid)
            {
                return Ok();
            }
            return BadRequest();
        }

        /// <summary>
        /// 按id进行分页和排序
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("page")]
        public async Task<IActionResult> PageAndSortById([FromQuery]int page=1, [FromQuery]int pageSize=10)
        {
            //找出users索引的全部值，然后按照id进行降序排序
            //然后进行分页，From是跳过当前值向后多少个位置,Size是获取多少条数据
            var res = await _esClient.SearchAsync<AppUser>(s => {
               return s.Index("users").MatchAll().Sort(d =>d.Descending("id")).From((page-1)*pageSize).Size(pageSize);
            });
            if(res.IsValid && res.Documents.Count > 0)
            {
                return Ok(res.Documents.ToList());
            }
            return NoContent();
        }

        /// <summary>
        /// 使用聚合，求id的最大值，最小值，平均值
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("aggregations")]
        public async Task<IActionResult> Max_Min_Avg_Id()
        {
            var res=await _esClient.SearchAsync<AppUser>(s =>
            {
                return s.Index("users").Aggregations(a =>
                {
                    return a.Min("minId", m => m.Field("id"))
                            .Max("maxId",m=>m.Field("id"))
                            .Average("avgId",m=>m.Field("id"));
                });
            });
            if (res.IsValid)
            {
                var min = res.Aggregations.Min("minId").Value;
                var max = res.Aggregations.Max("maxId").Value;
                var avg = res.Aggregations.Average("avgId").Value;
                return Ok(new { Min = min, Max = max, Avg = avg });
            }
            return NoContent();
        }
    }
}
