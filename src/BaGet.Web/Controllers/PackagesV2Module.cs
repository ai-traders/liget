using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core.Configuration;
using BaGet.Core.Legacy.OData;
using BaGet.Core.Services;
using BaGet.Web.Extensions;
using Carter;
using Carter.ModelBinding;
using Carter.Request;
using Carter.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace BaGet.Web.Controllers
{
    public class PackagesV2Module : CarterModule
    {
        static readonly string atomXmlContentType = "application/atom+xml";
        static readonly string basePath = "/v2";
        private readonly LiGetCompatibilityOptions _compat;
        private readonly IPackageService _packageService;
        private readonly IPackageStorageService _storage;
        private readonly ILogger<PackagesV2Module> _log;

        public PackagesV2Module(IODataPackageSerializer serializer, IPackageService packageService,
            IPackageStorageService storage, IEdmModel odataModel,
            ILogger<PackagesV2Module> logger, LiGetCompatibilityOptions compat)
        {
            this._compat = compat;
            this._packageService = packageService;
            this._storage = storage;
            this._log = logger;
            this.GetCompat("/v2/contents/{id}/{version}", async (req, res, routeData) => {
                string id = routeData.As<string>("id");
                string version = routeData.As<string>("version");

                if (!NuGetVersion.TryParse(version, out var nugetVersion))
                {
                    res.StatusCode = 400;
                    return;
                }

                var identity = new PackageIdentity(id, nugetVersion);

                if (!await packageService.IncrementDownloadCountAsync(identity))
                {
                     res.StatusCode = 404;
                     return;
                }
                var packageStream = await _storage.GetPackageStreamAsync(identity);

                await res.FromStream(packageStream, "application/zip");
            });
            
            Func<HttpRequest, HttpResponse, RouteData, Task> indexHandler = async (req, res, routeData) => {
                var serviceUrl = GetServiceUrl(req);
                string text = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<service xml:base=""{serviceUrl}"" xmlns=""http://www.w3.org/2007/app"" xmlns:atom=""http://www.w3.org/2005/Atom""><workspace>
<atom:title type=""text"">Default</atom:title><collection href=""Packages""><atom:title type=""text"">Packages</atom:title></collection></workspace>
</service>";
                res.StatusCode = 200;
                res.ContentType = "application/xml; charset=utf-8";
                await res.WriteAsync(text, new UTF8Encoding(false));
            };
            this.GetCompat("/v2/", indexHandler);

            this.GetCompat(@"/v2/FindPackagesById{query}", async (req, res, routeData) => {
                CancellationToken ct = CancellationToken.None;
                try {
                    var serviceUrl = GetServiceUrl(req);
                    var uriParser = new ODataUriParser(odataModel,new Uri(serviceUrl), req.GetUri());
                    var path = uriParser.ParsePath();
                    if(path.FirstSegment.Identifier=="FindPackagesById") {
                        var idOrNull = uriParser.CustomQueryOptions.FirstOrDefault(o => o.Key.ToLowerInvariant() == "id").Value;
                        string id = idOrNull.TrimStart('\'').TrimEnd('\'');
                        _log.LogDebug("Request to FindPackagesById id={0}",id);
                        var found = await _packageService.FindAsync(id, false, true);
                        var odata = new ODataResponse<IEnumerable<PackageWithUrls>>(serviceUrl, found.Select(f => ToPackageWithUrls(req, f)));
                        using(var ms = new MemoryStream()) {
                            serializer.Serialize(ms, odata.Entity, odata.ServiceBaseUrl);
                            ms.Seek(0, SeekOrigin.Begin);
                            await res.FromStream(ms, atomXmlContentType);
                        }
                    }
                    else
                        res.StatusCode = 400;
                }
                catch(ODataException odataPathError) {
                    _log.LogError("Bad odata query", odataPathError);
                    res.StatusCode = 400;
                }
            });

            this.GetCompat(@"/v2/Packages{query}", async (req, res, routeData) => {
                try {
                    var serviceUrl = GetServiceUrl(req);
                    var uriParser = new ODataUriParser(odataModel,new Uri(serviceUrl), req.GetUri());
                    var path = uriParser.ParsePath();
                    if(path.FirstSegment.Identifier == "Packages") {
                        if(path.Count == 2 && path.LastSegment is KeySegment) {
                            KeySegment queryParams = (KeySegment)path.LastSegment;
                            string id = queryParams.Keys.First(k => k.Key == "Id").Value as string;
                            string version = queryParams.Keys.First(k => k.Key == "Version").Value as string;
                            _log.LogDebug("Request to find package by id={0} and version={1}", id, version);
                            var identity = new PackageIdentity(id, NuGetVersion.Parse(version));
                            var found = await _packageService.FindOrNullAsync(identity, false, true);
                            if(found == null) {
                                res.StatusCode = 404;
                                return;
                            }
                            var odataPackage = new ODataResponse<PackageWithUrls>(serviceUrl, ToPackageWithUrls(req, found));
                            using(var ms = new MemoryStream()) {
                                serializer.Serialize(ms, odataPackage.Entity.Pkg,
                                    odataPackage.ServiceBaseUrl, odataPackage.Entity.ResourceIdUrl, odataPackage.Entity.PackageContentUrl);
                                ms.Seek(0, SeekOrigin.Begin);
                                await res.FromStream(ms, atomXmlContentType);
                            }
                        }
                        else
                            res.StatusCode = 400;
                    }
                    else
                        res.StatusCode = 400;
                }
                catch(ODataException odataPathError) {
                    _log.LogError("Bad odata query", odataPathError);
                    res.StatusCode = 400;
                }
            });
            
        }

        private void GetCompat(string path, Func<HttpRequest, HttpResponse, RouteData, Task> handler) {
            base.Get(path, handler);
            if(_compat != null && _compat.Enabled) {
                base.Get("/api" + path, handler);
            }
        }

        public PackageWithUrls ToPackageWithUrls(HttpRequest request, Core.Entities.Package pkg) {
            var serviceUrl = GetServiceUrl(request);
            var id = pkg.Id;
            var version = pkg.Version;
            PackageWithUrls urls = new PackageWithUrls(pkg, 
                $"{serviceUrl}/Packages(Id='{id}',Version='{pkg.Version}')",
                $"{serviceUrl}/contents/{id.ToLowerInvariant()}/{version.ToNormalizedString()}");
            return urls;
        }

        public string GetServiceUrl(HttpRequest req)
        {
            if(_compat != null && _compat.Enabled) {
                if(req.Path.StartsWithSegments(new PathString("/api"))) {
                    return req.AbsoluteUrl("/api" + basePath);
                }
            }
            return req.AbsoluteUrl(basePath);
        }
    }
}