﻿using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace LiGet.Cache
{
    /// <summary>
    /// Indexes packages from an external source.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// If the package is unknown, attempt to index it from an upstream source.
        /// </summary>
        /// <param name="id">The package's id</param>
        /// <param name="version">The package's version</param>
        /// <param name="cancellationToken">The token to cancel the mirroring</param>
        /// <returns>A task that completes when the package has been mirrored.</returns>
        Task CacheAsync(PackageIdentity pid, CancellationToken cancellationToken);

        Task<IReadOnlyList<string>> FindUpstreamAsync(string id, CancellationToken ct);

        Task<IEnumerable<IPackageSearchMetadata>> FindUpstreamMetadataAsync(string id, CancellationToken ct);
        Task<Stream> GetPackageStreamAsync(PackageIdentity identity);
        Task<bool> ExistsAsync(PackageIdentity identity);
        Task<Stream> GetNuspecStreamAsync(PackageIdentity identity);
        Task<Stream> GetReadmeStreamAsync(PackageIdentity identity);
        Task<IPackageSearchMetadata> FindAsync(PackageIdentity identity);

        Task<IEnumerable<IPackageSearchMetadata>> SearchAsync(string searchTerm, SearchFilter filter, int skip, int take, CancellationToken ct);
    }
}
