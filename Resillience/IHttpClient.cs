using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Resillience
{
    /// <summary>
    /// 自定义的HttpClient
    /// </summary>
    public interface IHttpClient
    {
        Task<HttpResponseMessage>PostAsync(HttpMethod method, Dictionary<string, string> form, string url, string authorizationToken=null, string requestId = null, string authorizationMethod = "Bearer");

        Task<HttpResponseMessage> PostAsync<T>(HttpMethod method, T item, string url, string authorizationToken = null, string requestId = null, string authorizationMethod = "Bearer");
    }
}
