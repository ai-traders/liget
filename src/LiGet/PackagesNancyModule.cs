using System;
using System.IO;
using System.Linq;
using System.Text;
using LiGet.NuGet.Server.Infrastructure;
using LiGet.OData;
using LiGet.Util;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Nancy;
using Nancy.Responses;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace LiGet
{
    public class PackagesNancyModule : NancyModule
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(PackagesNancyModule));

        static readonly string basePath = "/api/v2";

        public PackagesNancyModule(IPackageService repository, IEdmModel odataModel)
            :base(basePath)
        {
            this.OnError.AddItemToEndOfPipeline(HandleError);

            base.Get("/contents/{id}/{version}", args => {
                string id = args.id;
                string v = args.version;
                NuGetVersion version;
                if(!NuGetVersion.TryParse(v, out version))
                {
                    _log.ErrorFormat("Bad version format {0}", v);  
                    return HttpStatusCode.BadRequest;
                }
                var nupkg = repository.GetStream(new PackageIdentity(id,version));
                if(nupkg == null) {
                    _log.WarnFormat("Package contents not found {0}", base.Request.Path);
                    return HttpStatusCode.NotFound;
                }                
                return new StreamResponse(nupkg, "application/zip");
            });

            base.Get<Response>("/", args => {
                var serviceUrl = GetServiceUrl();
                return new Response() {
                    StatusCode = HttpStatusCode.OK,
                    ContentType = "application/xml; charset=utf-8",
                    Contents = stream => {
                        string text = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<service xml:base=""{serviceUrl}"" xmlns=""http://www.w3.org/2007/app"" xmlns:atom=""http://www.w3.org/2005/Atom""><workspace>
   <atom:title type=""text"">Default</atom:title><collection href=""Packages""><atom:title type=""text"">Packages</atom:title></collection></workspace>
</service>";
                        var writer = new StreamWriter(stream, new UTF8Encoding(false));                        
                        writer.Write(text);
                        writer.Flush();
                    }
                };
            });
            base.Put<Response>("/", args => {
                // client for reference - https://github.com/NuGet/NuGet.Client/blob/4eed67e7e159796ae486d2cca406b283e23b6ac8/src/NuGet.Core/NuGet.Protocol/Resources/PackageUpdateResource.cs#L273
                if(this.Request.Files.Count() != 1) {
                    _log.ErrorFormat("Bad request, tried to push {0} files {1}",
                        this.Request.Files.Count(), string.Join(",",this.Request.Files.Select(f => f.Key)));
                    return HttpStatusCode.BadRequest;
                }                    
                var file = this.Request.Files.FirstOrDefault();
                try {
                    repository.PushPackage(file.Value);
                    return HttpStatusCode.Created;
                }
                catch(PackageDuplicateException dup) {
                    _log.Error("Tried to push a package which already exists", dup);
                    return HttpStatusCode.Conflict;
                }
            });

            base.Get<object>(@"^(.*)$", args => {
                try {
                    var serviceUrl = GetServiceUrl();
                    var uriParser = new ODataUriParser(odataModel,serviceUrl,base.Request.Url);
                    var path = uriParser.ParsePath();
                    if(path.FirstSegment.Identifier=="FindPackagesById") {
                        var idOrNull = uriParser.CustomQueryOptions.FirstOrDefault(o => o.Key.ToLowerInvariant() == "id").Value;
                        //TODO semVer
                        var semVer = ClientCompatibility.Max;
                        if(idOrNull == null)
                            throw new ArgumentException();//TODO nice bad request
                        else
                        {
                            string id = idOrNull;
                            _log.DebugFormat("Request to FindPackagesById id={0}",id);
                            var found = repository.FindPackagesById(id,semVer);
                            return found;
                        }
                    }
                    else if(path.FirstSegment.Identifier == "Packages") {
                        if(path.Count == 2 && path.LastSegment is KeySegment) {
                            KeySegment queryParams = (KeySegment)path.LastSegment;
                            string id = queryParams.Keys.First(k => k.Key == "Id").Value as string;
                            string version = queryParams.Keys.First(k => k.Key == "Version").Value as string;
                            _log.DebugFormat("Request to find package by id={0} and version={1}", id, version);
                            var found = repository.FindPackage(id, NuGetVersion.Parse(version));
                            if(found == null)
                                return NoPackage404();
                            id = found.PackageInfo.Id;
                            version = found.PackageInfo.Version;
                            PackageUrls urls = new PackageUrls(serviceUrl.AbsoluteUri, 
                                $"{serviceUrl}/Packages(Id='{id}',Version='{found.PackageInfo.Version}')",
                                $"{serviceUrl}/contents/{id.ToLowerInvariant()}/{version.ToLowerInvariant()}");
                            return new ODataPackageResponse(found.PackageInfo, urls);
                        }
                        else
                            throw new ArgumentException("Bad or not supported query");//TODO nice bad request
                    }
                    else
                        throw new NotSupportedException(string.Format("Not supported endpoint path {0} or action {1}",
                            base.Request.Path, path.FirstSegment.Identifier));
                }
                catch(ODataUnrecognizedPathException odataPathError) {
                    _log.Error("Bad odata query", odataPathError);
                    return HttpStatusCode.BadRequest;
                }
            });
            base.Head<object>(@"^(.*)$", new Func<dynamic,object>(ThrowNotSupported));
            base.Post<object>(@"^(.*)$", new Func<dynamic,object>(ThrowNotSupported));
            base.Put<object>(@"^(.*)$", new Func<dynamic,object>(ThrowNotSupported));
            base.Delete<object>(@"^(.*)$", new Func<dynamic,object>(ThrowNotSupported));
        }

        private object NoPackage404()
        {
            return HttpStatusCode.NotFound;
        }

        public Uri GetServiceUrl()
        {
            return new Uri(new Uri(base.Request.Url).GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped) + basePath);
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