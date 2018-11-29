using System;
using System.Collections.Generic;
using NuGet.Protocol.Core.Types;
using NuGet.Configuration;
using NuGet.Versioning;
using NuGet.Protocol;
using BaGet.Core.Mirror;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using Microsoft.Extensions.Logging;

namespace BaGet.Core
{
    public class NuGetClient : INuGetClient
    {
        private readonly Microsoft.Extensions.Logging.ILogger<NuGetClient> _logger;
        private List<Lazy<INuGetResourceProvider>> _providers;

        public NuGetClient(Microsoft.Extensions.Logging.ILogger<NuGetClient> logger)
        {
            _providers = new List<Lazy<INuGetResourceProvider>>();
            _providers.AddRange(Repository.Provider.GetCoreV3());
            _providers.Add(new Lazy<INuGetResourceProvider>(() => new PackageMetadataResourceV3Provider()));
            this._logger = logger;
        }

        public ISourceRepository GetRepository(Uri repositoryUrl)
        {
            return new NuRepository(_providers, repositoryUrl, _logger);
        }

        private class NuRepository : ISourceRepository
        {
            private List<Lazy<INuGetResourceProvider>> providers;
            private Uri repositoryUrl;
            private readonly Microsoft.Extensions.Logging.ILogger<NuGetClient> _logger;
            private SourceRepository _sourceRepository;
            private SourceCacheContext _cacheContext;
            private RegistrationResourceV3 _regResource;
            private PackageMetadataResourceV3 _metadataSearch;
            private RemoteV3FindPackageByIdResource _versionSearch;
            private NuGetLoggerAdapter<NuGetClient> _loggerAdapter;

            public NuRepository(List<Lazy<INuGetResourceProvider>> providers, Uri repositoryUrl, Microsoft.Extensions.Logging.ILogger<NuGetClient> logger)
            {
                this.providers = providers;
                this.repositoryUrl = repositoryUrl;
                this._logger = logger;
                PackageSource packageSource = new PackageSource(repositoryUrl.AbsoluteUri);
                _sourceRepository = new SourceRepository(packageSource, providers);
                _cacheContext = new SourceCacheContext();
                var httpSource = _sourceRepository.GetResource<HttpSourceResource>();
                _regResource = _sourceRepository.GetResource<RegistrationResourceV3>();
                ReportAbuseResourceV3 reportAbuseResource = _sourceRepository.GetResource<ReportAbuseResourceV3>();
                _metadataSearch = new PackageMetadataResourceV3(httpSource.HttpSource, _regResource, reportAbuseResource);
                _versionSearch = new RemoteV3FindPackageByIdResource(_sourceRepository, httpSource.HttpSource);
                this._loggerAdapter = new NuGetLoggerAdapter<NuGetClient>(logger);
            }

            public Task<IEnumerable<NuGetVersion>> GetAllVersionsAsync(string id, CancellationToken ct)
            {
                return _versionSearch.GetAllVersionsAsync(id, _cacheContext, _loggerAdapter, ct);
            }

            public Task<IEnumerable<IPackageSearchMetadata>> GetMetadataAsync(
                string packageId, bool includePrerelease, bool includeUnlisted, CancellationToken ct)
            {
                return _metadataSearch.GetMetadataAsync(packageId, includePrerelease, includeUnlisted, _cacheContext, _loggerAdapter, ct);
            }

            public async Task<Uri> GetPackageUriAsync(string id, string version, CancellationToken cancellationToken)
            {
                var serviceIndex = await _sourceRepository.GetResourceAsync<ServiceIndexResourceV3>(cancellationToken);
                var packageBaseAddress = serviceIndex.GetServiceEntryUri(ServiceTypes.PackageBaseAddress);
                if (packageBaseAddress != null)
                {
                    return new Uri(packageBaseAddress, $"{id}/{version}/{id}.{version}.nupkg");
                }
                else
                {
                    _logger.LogDebug("Upstream repository does not support flat container, falling back to registration");
                    // If there is no flat container resource fall back to using the registration resource to find the download url.
                    using (var sourceCacheContext = new SourceCacheContext())
                    {
                        // Read the url from the registration information
                        var pid = new PackageIdentity(id, NuGetVersion.Parse(version));
                        var blob = await _regResource.GetPackageMetadata(pid, sourceCacheContext, _loggerAdapter, cancellationToken);
                        if (blob != null && blob["packageContent"] != null)
                        {
                            return new Uri(blob["packageContent"].ToString());
                        }
                        else
                            throw new InvalidOperationException("Could not determine upstream url for download");
                    }
                }
            }

            public Task<IPackageSearchMetadata> GetMetadataAsync(PackageIdentity identity, CancellationToken ct)
            {
                return _metadataSearch.GetMetadataAsync(identity, _cacheContext, _loggerAdapter, ct);
            }
        }
    }
}