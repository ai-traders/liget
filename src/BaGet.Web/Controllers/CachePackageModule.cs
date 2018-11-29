using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core.Mirror;
using BaGet.Core.Services;
using Carter;
using Carter.ModelBinding;
using Carter.Request;
using Carter.Response;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace BaGet.Controllers
{
    public class CachePackageModule : CarterModule
    {
        private readonly IMirrorService _mirror;

        public CachePackageModule(IMirrorService mirror)
            :base("cache/v3/package")
        {
            _mirror = mirror ?? throw new ArgumentNullException(nameof(mirror));

            this.Get("/{id}/index.json", async (req, res, routeData) => {
                string id = routeData.As<string>("id");
                IReadOnlyList<string> upstreamVersions = await _mirror.FindUpstreamAsync(id, CancellationToken.None);
                if(upstreamVersions.Any()) {
                    await res.AsJson(new
                    {
                        Versions = upstreamVersions.ToList()
                    });
                    return;
                }
                res.StatusCode = 404;
            });

            this.Get("/{id}/{version}/{idVersion}.nupkg", async (req, res, routeData) => {
                string id = routeData.As<string>("id");
                string version = routeData.As<string>("version");

                if (!NuGetVersion.TryParse(version, out var nugetVersion))
                {
                    res.StatusCode = 400;
                    return;
                }

                var identity = new PackageIdentity(id, nugetVersion);
                await _mirror.MirrorAsync(identity, CancellationToken.None);
                
                var packageStream = await _mirror.GetPackageStreamAsync(identity);

                await res.FromStream(packageStream, "application/octet-stream");
            });

            this.Get("/{id}/{version}/{id2}.nuspec", async (req, res, routeData) => {
                string id = routeData.As<string>("id");
                string version = routeData.As<string>("version");

                if (!NuGetVersion.TryParse(version, out var nugetVersion))
                {
                    res.StatusCode = 400;
                    return;
                }

                var identity = new PackageIdentity(id, nugetVersion);
                await _mirror.MirrorAsync(identity, CancellationToken.None);

                if (!await _mirror.ExistsAsync(new PackageIdentity(id, nugetVersion)))
                {
                    res.StatusCode = 404;
                    return;
                }
                await res.FromStream(await _mirror.GetNuspecStreamAsync(identity), "text/xml");
            });

            this.Get("/{id}/{version}/readme", async (req, res, routeData) => {
                string id = routeData.As<string>("id");
                string version = routeData.As<string>("version");

                if (!NuGetVersion.TryParse(version, out var nugetVersion))
                {
                    res.StatusCode = 400;
                    return;
                }

                var identity = new PackageIdentity(id, nugetVersion);
                await _mirror.MirrorAsync(identity, CancellationToken.None);

                if (!await _mirror.ExistsAsync(new PackageIdentity(id, nugetVersion)))
                {
                    res.StatusCode = 404;
                    return;
                }

                

                await res.FromStream(await _mirror.GetReadmeStreamAsync(identity), "text/markdown");
            });
        }
    }
}
