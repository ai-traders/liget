using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiGet.Entities;
using LiGet.Cache;
using LiGet.Services;
using LiGet.Extensions;
using LiGet.WebModels;
using Carter;
using Carter.ModelBinding;
using Carter.Request;
using Carter.Response;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace LiGet.CarterModules.Registration
{
    /// <summary>
    /// The API to retrieve the metadata of a specific package.
    /// </summary>
    public class CacheRegistrationIndexModule : CarterModule
    {
        const string prefix = "api/cache";
        private readonly ICacheService _mirror;

        public CacheRegistrationIndexModule(ICacheService mirror)
        {
            this._mirror = mirror ?? throw new ArgumentNullException(nameof(mirror));
            this.Get("api/cache/v3/registration/{id}/index.json", async (req, res, routeData) =>
            {
                string id = routeData.As<string>("id");
                // Documentation: https://docs.microsoft.com/en-us/nuget/api/registration-base-url-resource
                var upstreamPackages = (await _mirror.FindUpstreamMetadataAsync(id, CancellationToken.None)).ToList();
                var versions = upstreamPackages.Select(p => p.Identity.Version).ToList();

                if (!upstreamPackages.Any())
                {
                    res.StatusCode = 404;
                    return;
                }

                // TODO: Paging of registration items.
                // "Un-paged" example: https://api.nuget.org/v3/registration3/newtonsoft.json/index.json
                // Paged example: https://api.nuget.org/v3/registration3/fake/index.json
                await res.AsJson(new
                {
                    Count = upstreamPackages.Count,
                    TotalDownloads = upstreamPackages.Sum(p => p.DownloadCount),
                    Items = new[]
                    {
                        new RegistrationIndexItem(
                            packageId: id,
                            items: upstreamPackages.Select(p => ToRegistrationIndexLeaf(req, p)).ToList(),
                            lower: versions.Min().ToNormalizedString(),
                            upper: versions.Max().ToNormalizedString()
                        ),
                    }
                });
            });

            this.Get("api/cache/v3/registration/{id}/{version}.json", async (req, res, routeData) =>
            {
                string id = routeData.As<string>("id");
                string version = routeData.As<string>("version");

                if (!NuGetVersion.TryParse(version, out var nugetVersion))
                {
                    res.StatusCode = 400;
                    return;
                }

                var pid = new PackageIdentity(id, nugetVersion);

                // Allow read-through caching to happen if it is confiured.
                await _mirror.CacheAsync(pid, CancellationToken.None);

                var package = await _mirror.FindAsync(new PackageIdentity(id, nugetVersion));

                if (package == null)
                {
                    res.StatusCode = 404;
                    return;
                }

                // Documentation: https://docs.microsoft.com/en-us/nuget/api/registration-base-url-resource
                var result = new RegistrationLeaf(
                    registrationUri: req.PackageRegistration(pid.Id, prefix),
                    listed: package.IsListed,
                    downloads: package.DownloadCount.GetValueOrDefault(),
                    packageContentUri: req.PackageDownload(pid, prefix),
                    published: package.Published.GetValueOrDefault(),
                    registrationIndexUri: req.PackageRegistration(id, prefix));

                await res.AsJson(result);
            });
        }

        private RegistrationIndexLeaf ToRegistrationIndexLeaf(HttpRequest request, IPackageSearchMetadata package) =>
            new RegistrationIndexLeaf(
                packageId: package.Identity.Id,
                catalogEntry: new CatalogEntry(
                    package: package,
                    catalogUri: $"https://api.nuget.org/v3/catalog0/data/2015.02.01.06.24.15/{package.Identity.Id}.{package.Identity.Version}.json",
                    packageContent: request.PackageDownload(package.Identity, prefix),
                    getRegistrationUrl: id => new System.Uri(request.PackageRegistration(id, prefix))),
                packageContent: request.PackageDownload(package.Identity, prefix));

    }
}