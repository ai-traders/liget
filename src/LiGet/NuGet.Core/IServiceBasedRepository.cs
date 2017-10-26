using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using NuGet.Versioning;

namespace NuGet
{
    public interface IServiceBasedRepository : IPackageRepository
    {
        IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions);
        IEnumerable<IPackage> GetUpdates(
            IEnumerable<IPackageName> packages, 
            bool includePrerelease, 
            bool includeAllVersions, 
            IEnumerable<FrameworkName> targetFrameworks,
            IEnumerable<VersionRange> versionConstraints);
    }
}