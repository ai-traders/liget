using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace NuGet
{
    [Obsolete("Try using LocalPackageInfo Instead")]
    public interface IPackage : IPackageMetadata, IServerPackageMetadata
    {
        bool IsAbsoluteLatestVersion { get; }

        bool IsLatestVersion { get; }

        bool Listed { get; }

        DateTimeOffset? Published { get; }

        IEnumerable<IPackageAssemblyReference> AssemblyReferences { get; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        IEnumerable<IPackageFile> GetFiles();

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        IEnumerable<NuGetFramework> GetSupportedFrameworks();

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This might be expensive")]
        Stream GetStream();

        void ExtractContents(IFileSystem fileSystem, string extractPath);
    }
}