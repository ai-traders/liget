
using NuGet.Versioning;

namespace NuGet
{
    public interface IPackageName //TODO use PackageIdentity
    {
        string Id { get; }
        SemanticVersion Version { get; }
    }
}