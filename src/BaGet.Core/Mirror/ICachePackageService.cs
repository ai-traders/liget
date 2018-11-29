using System.IO;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace BaGet.Core.Mirror
{
    public interface IPackageCacheService
    {
        Task<bool> ExistsAsync(PackageIdentity package);
        Task AddPackageAsync(Stream stream);
        Task<Stream> GetPackageStreamAsync(PackageIdentity identity);
        Task<Stream> GetNuspecStreamAsync(PackageIdentity identity);
        Task<Stream> GetReadmeStreamAsync(PackageIdentity identity);
    }
}