using System;
using System.Collections.Generic;
using NuGet.Packaging;
using NuGet.Versioning;

namespace NuGet
{
    public interface IPackageMetadata : IPackageName
    {
        string Title { get; }
        string Authors { get; }
        string Owners { get; }
        string IconUrl { get; }
        string LicenseUrl { get; }
        string ProjectUrl { get; }
        bool RequireLicenseAcceptance { get; }
        bool DevelopmentDependency { get; }
        string Description { get; }
        string Summary { get; }
        string ReleaseNotes { get; }
        string Language { get; }
        string Tags { get; }
        string Copyright { get; }

        /// <summary>
        /// Specifies assemblies from GAC that the package depends on.
        /// </summary>
        IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies { get; }
        
        /// <summary>
        /// Returns sets of References specified in the manifest.
        /// </summary>
        ICollection<PackageReferenceSet> PackageAssemblyReferences { get; }

        /// <summary>
        /// Specifies sets other packages that the package depends on.
        /// </summary>
        IEnumerable<PackageDependencyGroup> DependencySets { get; }

        NuGetVersion MinClientVersion { get; }
    }
}