using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace LiGet.Cache.Proxy
{
    public class NetHttpClient : IHttpClient
    {
        HttpClient client = new HttpClient();

        public NetHttpClient() {
            client.DefaultRequestHeaders.ConnectionClose = false;
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return this.client.SendAsync(request);
        }
    }
}