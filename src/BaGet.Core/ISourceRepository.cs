using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace BaGet.Core
{
    public interface ISourceRepository
    {
        Task<IEnumerable<IPackageSearchMetadata>> GetMetadataAsync(string packageId, bool includePrerelease, bool includeUnlisted, CancellationToken ct);
        Task<IEnumerable<NuGetVersion>> GetAllVersionsAsync(string id, CancellationToken ct);
        Task<Uri> GetPackageUriAsync(string id, string version, CancellationToken cancellationToken);
        Task<IPackageSearchMetadata> GetMetadataAsync(PackageIdentity identity, CancellationToken none);
    }
}