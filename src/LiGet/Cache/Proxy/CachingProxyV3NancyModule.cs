using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Nancy;
using Newtonsoft.Json;

namespace LiGet.Cache.Proxy
{
    public class CachingProxyV3NancyModule : NancyModule
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(CachingProxyV3NancyModule));

        static readonly string basePath = "/api/cache/v3";
        private IHttpClient client;
        private IV3JsonInterceptor interceptor;
        private readonly IUrlReplacementsProvider replacementsProvider;

        public CachingProxyV3NancyModule(IHttpClient client, IV3JsonInterceptor interceptor, IUrlReplacementsProvider replacementsProvider)
            :base(basePath)
        {
            this.client = client;
            this.interceptor = interceptor;
            this.replacementsProvider = replacementsProvider;

            this.OnError.AddItemToEndOfPipeline(HandleError);

            base.Get<Response>("/{path*}", args => {
                string myV3Url = this.GetServiceUrl().AbsoluteUri;
                Dictionary<string, string> replacements = replacementsProvider.GetReplacements(myV3Url);
                string pathAndQuery = args.path + this.Request.Url.Query;
                var request = new HttpRequestMessage() {
                                            RequestUri = replacementsProvider.GetOriginUri(pathAndQuery),                                            
                                            Method = HttpMethod.Get,
                                        };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _log.DebugFormat("Proxying {0} to {1}", this.Request.Url, request.RequestUri);
                var originalResponse = client.SendAsync(request).Result;
                return new Response() {
                    StatusCode = (Nancy.HttpStatusCode)(int)originalResponse.StatusCode,
                    ContentType = originalResponse.Content.Headers.ContentType.MediaType,
                    Contents = netStream => {      
                        try {
                            Stream originalStream = originalResponse.Content.ReadAsStreamAsync().Result;
                            if(originalResponse.Content.Headers.ContentEncoding.Contains("gzip"))
                                originalStream = new GZipStream(originalStream,CompressionMode.Decompress);
                            interceptor.Intercept(replacements, originalStream, netStream);
                        }
                        catch(Exception ex) {
                            _log.Error("Something went wrong when intercepting origin response", ex);
                            throw new Exception("Intercepting origins response failed",ex);
                        }
                    }
                };
            });

            base.Head<object>(@"^(.*)$", new Func<dynamic,object>(ThrowNotSupported));
            base.Post<object>(@"^(.*)$", new Func<dynamic,object>(ThrowNotSupported));
            base.Put<object>(@"^(.*)$", new Func<dynamic,object>(ThrowNotSupported));
            base.Delete<object>(@"^(.*)$", new Func<dynamic,object>(ThrowNotSupported));
        }

        private object ThrowNotSupported(dynamic args) {
            throw new NotSupportedException(string.Format("Not supported endpoint path {0}",base.Request.Path));
        }

        public dynamic HandleError(NancyContext ctx, Exception ex)
        {
            string path = ctx.Request.Path;
            _log.ErrorFormat("Error handling proxy request {0}\n{1}", path, ex);
            var notSupported = ex as NotSupportedException;
            if(notSupported != null){
                if(_log.IsDebugEnabled) {
                    string method = ctx.Request.Method;
                    string body = "";
                    if(ctx.Request.Body != null) {
                        body = new StreamReader(ctx.Request.Body).ReadToEnd();
                    }
                    StringBuilder headers = new StringBuilder();
                    foreach(var h in ctx.Request.Headers) {
                        headers.Append(h.Key).Append(": ").Append(string.Join(" ", h.Value)).AppendLine();
                    }
                    _log.DebugFormat("Not supported proxy request content: {0} {1}\nHeaders:\n{2}\nBody:\n{3}",method,ctx.Request.Url,headers,body);
                }
                return HttpStatusCode.NotImplemented;
            }
            return null;
        }

        public Uri GetServiceUrl()
        {
            return new Uri(new Uri(base.Request.Url).GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped) + basePath);
        }
    }
}