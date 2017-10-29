using System;
using System.IO;
using Moq;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;

namespace LiGet.Tests
{
    public class PackageHelper
    {
         public static LocalPackageInfo CreatePackage(string outputDirectory, string id, string version, Action<PackageBuilder> builderSteps) {
            var parsedVersion = NuGetVersion.Parse(version);
            var packageBuilder = new PackageBuilder
            {
                Id = id,
                Version = parsedVersion
            };
            if(builderSteps != null)
                builderSteps(packageBuilder);
                
            new DirectoryInfo(outputDirectory).Create();
            VersionFolderPathResolver pathResolver = new VersionFolderPathResolver(outputDirectory);
            var packageFileName = Path.Combine(outputDirectory, pathResolver.GetPackageFileName(id,NuGetVersion.Parse(version)));
            using (var stream = new FileStream(packageFileName, FileMode.CreateNew))
            {
                packageBuilder.Save(stream);
            }

            using (var package = new PackageArchiveReader(File.OpenRead(packageFileName)))
            {
                var nuspec = package.NuspecReader;
                var packageHelper = new Func<PackageReaderBase>(() => new PackageArchiveReader(File.OpenRead(packageFileName)));
                var nuspecHelper = new Lazy<NuspecReader>(() => nuspec);
                return new LocalPackageInfo(new PackageIdentity(id,NuGetVersion.Parse(version)), packageFileName, DateTime.UtcNow, nuspecHelper, packageHelper);
            };
        }

        public static LocalPackageInfo CreatePackage(string outputDirectory, string id, string version)
        {
            return CreatePackage(outputDirectory, id, version, builder => {
                builder.Description = "Description";
                builder.Authors.Add("Test Author" );
                var mockFile = new Mock<IPackageFile>();
                mockFile.Setup(m => m.Path).Returns("foo");
                mockFile.Setup(m => m.GetStream()).Returns(new MemoryStream());
                builder.Files.Add(mockFile.Object);
            });
        }
    }
}