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
    /// The mirror service used when mirroring has been disabled.
    /// </summary>
    public class FakeCacheService : ICacheService
    {
        Task<IReadOnlyList<string>> emptyVersions = Task.FromResult(new List<string>() as IReadOnlyList<string>);
        Task<IEnumerable<IPackageSearchMetadata>> emptyMeta = Task.Factory.StartNew(() => new List<IPackageSearchMetadata>() as IEnumerable<IPackageSearchMetadata>);

        public Task<bool> ExistsAsync(PackageIdentity identity)
        {
            throw new System.NotImplementedException();
        }

        public Task<IPackageSearchMetadata> FindAsync(PackageIdentity identity)
        {
            throw new System.NotImplementedException();
        }

        public Task<IReadOnlyList<string>> FindUpstreamAsync(string id, CancellationToken ct)
        {
            return emptyVersions;
        }

        public Task<IEnumerable<IPackageSearchMetadata>> FindUpstreamMetadataAsync(string id, CancellationToken ct)
        {
            return emptyMeta;
        }

        public Task<Stream> GetNuspecStreamAsync(PackageIdentity identity)
        {
            throw new System.NotImplementedException();
        }

        public Task<Stream> GetPackageStreamAsync(PackageIdentity identity)
        {
            throw new System.NotImplementedException();
        }

        public Task<Stream> GetReadmeStreamAsync(PackageIdentity identity)
        {
            throw new System.NotImplementedException();
        }

        public Task CacheAsync(
            PackageIdentity packageIdentity,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<IPackageSearchMetadata>> SearchAsync(string searchTerm, SearchFilter filter, int skip, int take, CancellationToken ct)
        {
            throw new System.NotImplementedException();
        }
    }
}
