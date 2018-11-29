using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core.Entities;
using BaGet.Core.Mirror;
using BaGet.Core.Services;
using BaGet.Web.Extensions;
using BaGet.Web.Models;
using Carter;
using Carter.ModelBinding;
using Carter.Request;
using Carter.Response;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace BaGet.Controllers.Web.Registration
{
    /// <summary>
    /// The API to retrieve the metadata of a specific package.
    /// </summary>
    public class RegistrationIndexModule : CarterModule
    {
        private readonly IPackageService _packages;

        public RegistrationIndexModule(IPackageService packageService)
        {
            _packages = packageService ?? throw new ArgumentNullException(nameof(packageService));
            this.Get("v3/registration/{id}/index.json", async (req, res, routeData) =>
            {
                string id = routeData.As<string>("id");
                // Documentation: https://docs.microsoft.com/en-us/nuget/api/registration-base-url-resource
                var packages = await _packages.FindAsync(id, includeUnlisted: false, includeDependencies: true);
                var versions = packages.Select(p => p.Version).ToList();

                if (!packages.Any())
                {
                    res.StatusCode = 404;
                    return;
                }

                // TODO: Paging of registration items.
                // "Un-paged" example: https://api.nuget.org/v3/registration3/newtonsoft.json/index.json
                // Paged example: https://api.nuget.org/v3/registration3/fake/index.json
                await res.AsJson(new
                {
                    Count = packages.Count,
                    TotalDownloads = packages.Sum(p => p.Downloads),
                    Items = new[]
                    {
                        new RegistrationIndexItem(
                            packageId: id,
                            items: packages.Select(p => ToRegistrationIndexLeaf(req, p)).ToList(),
                            lower: versions.Min().ToNormalizedString(),
                            upper: versions.Max().ToNormalizedString()
                        ),
                    }
                });
            });

            this.Get("v3/registration/{id}/{version}.json", async (req, res, routeData) =>
            {
                string id = routeData.As<string>("id");
                string version = routeData.As<string>("version");

                if (!NuGetVersion.TryParse(version, out var nugetVersion))
                {
                    res.StatusCode = 400;
                    return;
                }
                var pid = new PackageIdentity(id, nugetVersion);
                var package = await _packages.FindOrNullAsync(pid, false, includeDependencies: false);

                if (package == null)
                {
                    res.StatusCode = 404;
                    return;
                }

                // Documentation: https://docs.microsoft.com/en-us/nuget/api/registration-base-url-resource
                var result = new RegistrationLeaf(
                    registrationUri: req.PackageRegistration(pid, ""),
                    listed: package.Listed,
                    downloads: package.Downloads,
                    packageContentUri: req.PackageDownload(pid, ""),
                    published: package.Published,
                    registrationIndexUri: req.PackageRegistration(id, ""));

                await res.AsJson(result);
            });
        }

        private RegistrationIndexLeaf ToRegistrationIndexLeaf(HttpRequest request, Package package) =>
            new RegistrationIndexLeaf(
                packageId: package.Id,
                catalogEntry: new CatalogEntry(
                    package: package,
                    catalogUri: $"https://api.nuget.org/v3/catalog0/data/2015.02.01.06.24.15/{package.Id}.{package.Version}.json",
                    packageContent: request.PackageDownload(new PackageIdentity(package.Id, package.Version), ""),
                    getRegistrationUrl: id => new System.Uri(request.PackageRegistration(id, ""))),
                packageContent: request.PackageDownload(new PackageIdentity(package.Id, package.Version), ""));
    }
}