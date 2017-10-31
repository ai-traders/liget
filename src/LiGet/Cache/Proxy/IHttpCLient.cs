using System.Net.Http;
using System.Threading.Tasks;

namespace LiGet.Cache.Proxy
{
    public interface IHttpClient
    {
         Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
    }
}