using System;
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
    public class PackageModule : CarterModule
    {
        private readonly IPackageService _packages;
        private readonly IPackageStorageService _storage;

        public PackageModule(IPackageService packageService, IPackageStorageService storage)
            :base("v3/package")
        {
            _packages = packageService ?? throw new ArgumentNullException(nameof(packageService));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            this.Get("/{id}/index.json", async (req, res, routeData) => {
                string id = routeData.As<string>("id");
                var packages = await _packages.FindAsync(id);

                if (!packages.Any())
                {
                    res.StatusCode = 404;
                    return;
                }

                await res.AsJson(new
                {
                    Versions = packages.Select(p => p.VersionString).ToList()
                });
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
                if (!await _packages.IncrementDownloadCountAsync(identity))
                {
                     res.StatusCode = 404;
                     return;
                }

                
                var packageStream = await _storage.GetPackageStreamAsync(identity);

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

                if (!await _packages.ExistsAsync(identity))
                {
                    res.StatusCode = 404;
                    return;
                }

                await res.FromStream(await _storage.GetNuspecStreamAsync(identity), "text/xml");
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

                if (!await _packages.ExistsAsync(identity))
                {
                    res.StatusCode = 404;
                    return;
                }                

                await res.FromStream(await _storage.GetReadmeStreamAsync(identity), "text/markdown");
            });
        }
    }
}