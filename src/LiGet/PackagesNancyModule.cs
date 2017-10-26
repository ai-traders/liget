using System;
using System.IO;
using System.Linq;
using System.Text;
using LiGet.OData;
using Microsoft.OData.UriParser;
using Nancy;

namespace LiGet
{
    public class PackagesNancyModule : NancyModule
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(PackagesNancyModule));

        static readonly string basePath = "/api/v2";

        public PackagesNancyModule()
            :base(basePath)
        {
            this.OnError.AddItemToEndOfPipeline(HandleError);

            //FIXME move to DI
            var odataModelBuilder = new NuGetWebApiODataModelBuilder();
            odataModelBuilder.Build();
            var odataModel = odataModelBuilder.Model;

            base.Get("FindPackagesById()", args => {
                string query = base.Request.Url.Query;
                var serviceUrl = new Uri(new Uri(base.Request.Url).GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped) + basePath);//FIXME actual url
                var uriParser = new ODataUriParser(odataModel,serviceUrl,base.Request.Url);
                var path = uriParser.ParsePath();
                //path.FirstSegment.Identifier=="FindPackagesById"
                var idOrNull = uriParser.CustomQueryOptions.FirstOrDefault(o => o.Key.ToLowerInvariant() == "id").Value;
                if(idOrNull == null)
                    throw new ArgumentException();//TODO nice bad request
                else
                {
                    string id = idOrNull;
                    return "find packages " + id;
                }
            });

            base.Get<object>(@"^(.*)$", new Func<dynamic,object>(ThrowNotSupported));
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
            _log.ErrorFormat("Error handling request {0}\n{1}", path, ex);
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
                    _log.DebugFormat("Not supported request content: {0} {1}\nHeaders:\n{2}\nBody:\n{3}",method,ctx.Request.Url,headers,body);
                }
                return HttpStatusCode.NotImplemented;
            }
            return null;
        }
    }
}