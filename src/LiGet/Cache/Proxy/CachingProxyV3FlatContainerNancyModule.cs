using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Nancy;

namespace LiGet.Cache.Proxy
{
    public class CachingProxyV3FlatContainerNancyModule : NancyModule
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(CachingProxyV3FlatContainerNancyModule));

        static readonly string basePath = "/api/cache/v3-flatcontainer";
        private IHttpClient client;
        private readonly IUrlReplacementsProvider urlsProvider;

        public CachingProxyV3FlatContainerNancyModule(IHttpClient client, IUrlReplacementsProvider urlsProvider)
            : base(basePath)
        {
            this.urlsProvider = urlsProvider;
            this.client = client;

            base.Get<Response>("/{path*}", args =>
            {
                string pathAndQuery = args.path + this.Request.Url.Query;
                var request = new HttpRequestMessage()
                {
                    RequestUri = urlsProvider.GetOriginNupkgUri(pathAndQuery),
                    Method = HttpMethod.Get,
                };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                _log.DebugFormat("Proxying {0} to {1}", this.Request.Url, request.RequestUri);
                var originalResponse = client.SendAsync(request).Result;
                return new Response()
                {
                    ContentType = originalResponse.Content.Headers.ContentType.MediaType,
                    Contents = netStream =>
                    {
                        try
                        {
                            Stream originalStream = originalResponse.Content.ReadAsStreamAsync().Result;
                            originalStream.CopyTo(netStream);
                        }
                        catch (Exception ex)
                        {
                            _log.Error("Something went wrong when serving nupkg", ex);
                            throw new Exception("Serving nupkg failed",ex);
                        }
                    }
                };
            });
        }
    }
}