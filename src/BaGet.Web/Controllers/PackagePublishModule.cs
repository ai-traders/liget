using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BaGet.Core.Services;
using Carter;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Carter.ModelBinding;
using Carter.Request;
using Carter.Response;
using System.Threading;
using NuGet.Packaging.Core;

namespace BaGet.Controllers
{
    public class PackagePublishModule : CarterModule
    {
        public const string ApiKeyHeader = "X-NuGet-ApiKey";

        private readonly IAuthenticationService _authentication;
        private readonly IIndexingService _indexer;
        private readonly IPackageService _packages;
        private readonly ILogger<PackagePublishModule> _logger;

        public PackagePublishModule(
            IAuthenticationService authentication,
            IIndexingService indexer,
            IPackageService packages,
            ILogger<PackagePublishModule> logger)
        {
            _authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
            _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
            _packages = packages ?? throw new ArgumentNullException(nameof(packages));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.Put("/v2/package", async (req, res, routeData) =>
            {
                CancellationToken ct = CancellationToken.None;
                Stream uploadStream;
                if (req.Form.Files.Count > 0)
                {
                    // If we're using the newer API, the package stream is sent as a file.
                    // use first and ignore the rest
                    // as in https://docs.microsoft.com/en-us/nuget/api/package-publish-resource#multipart-form-data
                    uploadStream = req.Form.Files[0].OpenReadStream();
                }
                else
                {
                    // old clients
                    uploadStream = req.Body;
                }
                if (uploadStream == null)
                {
                    res.StatusCode = 400;
                    _logger.LogWarning("package upload did not contain multipart/form-data or body");
                    return;
                }

                try
                {
                    string apiKey = req.Headers[ApiKeyHeader];
                    if (!await _authentication.AuthenticateAsync(apiKey))
                    {
                        res.StatusCode = 401;
                        return;
                    }
                
                    var result = await _indexer.IndexAsync(uploadStream, ct);

                    switch (result)
                    {
                        case IndexingResult.InvalidPackage:
                            res.StatusCode = 400;
                            break;

                        case IndexingResult.PackageAlreadyExists:
                            res.StatusCode = 409;
                            break;

                        case IndexingResult.Success:
                            res.StatusCode = 201;
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception thrown during package upload");

                    res.StatusCode = 500;
                }
                finally {
                    if(uploadStream != null)
                        uploadStream.Dispose();
                }
            });

            this.Delete("/v2/package/{id}/{version}", async (req, res, routeData) => {
                string id = routeData.As<string>("id");
                string version = routeData.As<string>("version");
                if (!NuGetVersion.TryParse(version, out var nugetVersion))
                {
                    res.StatusCode = 400;
                }

                var identity = new PackageIdentity(id, nugetVersion);

                string apiKey = req.Headers[ApiKeyHeader];
                if (!await _authentication.AuthenticateAsync(apiKey))
                {
                    res.StatusCode = 403;
                }

                if (await _packages.UnlistPackageAsync(identity))
                {
                    res.StatusCode = 204;
                }
                else
                {
                    res.StatusCode = 404;
                }
            });

            this.Post("/v2/package/{id}/{version}", async (req, res, routeData) => {
                string id = routeData.As<string>("id");
                string version = routeData.As<string>("version");
                if (!NuGetVersion.TryParse(version, out var nugetVersion))
                {
                    res.StatusCode = 400;
                }

                string apiKey = req.Headers[ApiKeyHeader];
                if (!await _authentication.AuthenticateAsync(apiKey))
                {
                    res.StatusCode = 403;
                }
                var identity = new PackageIdentity(id, nugetVersion);

                if (await _packages.RelistPackageAsync(identity))
                {
                    res.StatusCode = 200;
                }
                else
                {
                    res.StatusCode = 404;
                }
            });
        }
    }
}
