using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core.Services;
using Microsoft.Extensions.Logging;
using System.Linq;
using NuGet.Packaging.Core;
using System.IO;
using BaGet.Core.Configuration;
using System.Collections.Concurrent;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Protocol;
using NuGet.Common;

namespace BaGet.Core.Mirror
{
    public class MirrorService : IMirrorService
    {
        private readonly object _startLock;
        private readonly Dictionary<PackageIdentity, Task> _downloads;
        private readonly IPackageCacheService _localPackages;
        private readonly IPackageDownloader _downloader;
        private readonly ILogger<MirrorService> _logger;
        private readonly ISourceRepository _sourceRepository;
        NuGetLoggerAdapter<MirrorService> _loggerAdapter;

        public MirrorService(
            INuGetClient client,
            IPackageCacheService localPackages,
            IPackageDownloader downloader,
            ILogger<MirrorService> logger,
            MirrorOptions options)
        {
            _startLock = new object();
            _downloads = new Dictionary<PackageIdentity, Task>();
            _localPackages = localPackages ?? throw new ArgumentNullException(nameof(localPackages));
            _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._loggerAdapter = new NuGetLoggerAdapter<MirrorService>(_logger);
            _sourceRepository = client.GetRepository(options.UpstreamIndex);
        }

        public async Task<IEnumerable<IPackageSearchMetadata>> FindUpstreamMetadataAsync(string id, CancellationToken ct) {
            //TODO: possibly cache response
            return await _sourceRepository.GetMetadataAsync(id, true, true, ct);
        }

        public async Task<IReadOnlyList<string>> FindUpstreamAsync(string id, CancellationToken ct)
        {
            var versions = await _sourceRepository.GetAllVersionsAsync(id, ct);
            //TODO: possibly cache response
            return versions.Select(v => v.ToNormalizedString()).ToList();
        }

        public Task MirrorAsync(PackageIdentity pid, CancellationToken cancellationToken)
        {
            if (_localPackages.ExistsAsync(pid).Result)
            {
                return Task.CompletedTask;
            }

            lock(_startLock) {
                if(!_downloads.TryGetValue(pid, out var task)) {
                    task = IndexFromSourceAsync(pid, cancellationToken);
                    _downloads.Add(pid, task);
                }
                int count = _downloads.Count;
                _logger.LogDebug("Total count of downloads in progress {Count}", count);
                return task;
            }
        }

        private async Task IndexFromSourceAsync(PackageIdentity pid, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string id = pid.Id.ToLowerInvariant();
            string version = pid.Version.ToNormalizedString().ToLowerInvariant();

            _logger.LogInformation("Attempting to mirror package {Id} {Version}...", id, version);

            try
            {
                Uri packageUri = await _sourceRepository.GetPackageUriAsync(id, version, cancellationToken);

                using (var stream = await _downloader.DownloadOrNullAsync(packageUri, cancellationToken))
                {
                    if (stream == null)
                    {
                        _logger.LogWarning(
                            "Failed to download package {Id} {Version} at {PackageUri}",
                            id,
                            version,
                            packageUri);

                        return;
                    }

                    _logger.LogInformation("Downloaded package {Id} {Version}, adding to cache...", id, version);

                    await _localPackages.AddPackageAsync(stream);

                    _logger.LogInformation(
                        "Finished adding package {Id} {Version}",
                        id,
                        version);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to mirror package {Id} {Version}", id, version);
            }
            finally {
                lock(_startLock) {
                    _downloads.Remove(pid);
                }
            }
        }

        public Task<Stream> GetPackageStreamAsync(PackageIdentity identity)
        {
            //TODO: possibly stream from in-memory cache
            return _localPackages.GetPackageStreamAsync(identity);
        }

        public Task<bool> ExistsAsync(PackageIdentity identity)
        {
            return _localPackages.ExistsAsync(identity);
        }

        public Task<Stream> GetNuspecStreamAsync(PackageIdentity identity)
        {
            //TODO: possibly stream from in-memory cache
            return _localPackages.GetNuspecStreamAsync(identity);
        }

        public Task<Stream> GetReadmeStreamAsync(PackageIdentity identity)
        {
            //TODO: possibly stream from in-memory cache
            return _localPackages.GetReadmeStreamAsync(identity);
        }

        public Task<IPackageSearchMetadata> FindAsync(PackageIdentity identity)
        {
            //TODO: possibly cache and stream from in-memory cache
            return _sourceRepository.GetMetadataAsync(identity, CancellationToken.None);
        }
    }
}
