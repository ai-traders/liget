using NuGet;

namespace LiGet
{
    public interface IPackageRepository : IPackageLookup
    {
        //NOTE: that nuget server also has IEnumerable<ServerPackage> FindPackagesById(string packageId, ClientCompatibility compatibility);
    }
}