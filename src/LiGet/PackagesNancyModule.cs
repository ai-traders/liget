using System;
using System.IO;
using System.Text;
using Nancy;

namespace LiGet
{
    public class PackagesNancyModule : NancyModule
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(PackagesNancyModule));

        public PackagesNancyModule()
            :base("/api/v2")
        {
            this.OnError.AddItemToEndOfPipeline(HandleError);

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