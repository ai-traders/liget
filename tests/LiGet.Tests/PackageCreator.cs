using System;
using System.IO;
using Moq;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;

namespace LiGet.Tests
{
    public class PackageCreator
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(PackageCreator));


        // adapted from https://github.com/NuGet/NuGet.Client/blob/16ae864cb5a4d6e051a369d6eae682643d4088a6/test/NuGet.Clients.Tests/NuGet.CommandLine.Test/PackageCreator.cs
        public static LocalPackageInfo CreatePackage(string id, string version, string outputDirectory,
            Action<PackageBuilder> additionalAction = null)
        {
            PackageBuilder builder = new PackageBuilder()
            {
                Id = id,
                Version = NuGetVersion.Parse(version),
                Description = "Descriptions",
            };
            builder.Authors.Add("test");
            builder.Files.Add(CreatePackageFile(Path.Combine("content", "test1.txt")));
            if (additionalAction != null)
            {
                additionalAction(builder);
            }
            VersionFolderPathResolver pathResolver = new VersionFolderPathResolver("dumb");
            var packageFileName = Path.Combine(outputDirectory, pathResolver.GetPackageFileName(id,NuGetVersion.Parse(version)));
            using (var stream = new FileStream(packageFileName, FileMode.CreateNew))
            {
                builder.Save(stream);
            }

            using (var package = new PackageArchiveReader(File.OpenRead(packageFileName)))
            {
                var nuspec = package.NuspecReader;
                var packageHelper = new Func<PackageReaderBase>(() => new PackageArchiveReader(File.OpenRead(packageFileName)));
                var nuspecHelper = new Lazy<NuspecReader>(() => nuspec);
                return new LocalPackageInfo(new PackageIdentity(id,NuGetVersion.Parse(version)), packageFileName, DateTime.UtcNow, nuspecHelper, packageHelper);
            };            
        }

        private static IPackageFile CreatePackageFile(string name)
        {
            var file = new Mock<IPackageFile>();
            file.SetupGet(f => f.Path).Returns(name);
            file.Setup(f => f.GetStream()).Returns(new MemoryStream());

            string effectivePath;
            var fx = FrameworkNameUtility.ParseFrameworkNameFromFilePath(name, out effectivePath);
            file.SetupGet(f => f.EffectivePath).Returns(effectivePath);
            file.SetupGet(f => f.TargetFramework).Returns(fx);

            return file.Object;
        }

    }
}