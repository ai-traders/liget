using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using Moq;
using NuGet;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Versioning;
using Xunit;

namespace LiGet.Tests.NuGet.Server.Infrastructure
{
    public class ExpandedPackageRepositoryTests : IDisposable
    {
        private TemporaryDirectory tmpDir;

        public ExpandedPackageRepositoryTests() {
            TestBootstrapper.ConfigureLogging();
            tmpDir = new TemporaryDirectory ();
        }
        public void Dispose() {
            tmpDir.Dispose();
        }

        public static Mock<IPackageFile> CreateMockedPackageFile(string directory, string fileName, string content = null)
        {
            string path = Path.Combine(directory, fileName);
            content = content ?? path;
            
            var mockFile = new Mock<IPackageFile>();
            mockFile.Setup(m => m.Path).Returns(path);
            mockFile.Setup(m => m.GetStream()).Returns(() => new MemoryStream(Encoding.Default.GetBytes(content)));

            string effectivePath;
            FrameworkName fn = FrameworkNameUtility.ParseFrameworkNameFromFilePath(path, out effectivePath);
            mockFile.Setup(m => m.TargetFramework).Returns(fn);
            mockFile.Setup(m => m.EffectivePath).Returns(effectivePath);
            mockFile.Setup(m => m.TargetFramework).Returns(fn);
            return mockFile;
        }

        private static MemoryStream GetPackageStream(PackageBuilder packageBuilder)
        {
            var memoryStream = new MemoryStream();
            packageBuilder.Save(memoryStream);
            memoryStream.Position = 0;

            return memoryStream;
        } 
        
        [Fact]
        public void GetPackages_ReturnsAllPackagesInsideDirectory()
        {
            // Arrange
            var fooPackage = new PackageBuilder
            {
                Id = "Foo",
                Version = NuGetVersion.Parse("1.0.0"),
                Description = "Some description",
            };
            fooPackage.Authors.Add("test author");
            fooPackage.Files.Add(
                CreateMockedPackageFile(@"lib\net40", "Foo.dll", "lib contents").Object);

            var barPackage = new PackageBuilder
            {
                Id = "Bar",
                Version = NuGetVersion.Parse("1.0.0-beta1"),
                Description = "Some description",
            };
            barPackage.Authors.Add("test author");
            barPackage.Files.Add(
                CreateMockedPackageFile("", "README.md", "lib contents").Object);
            barPackage.Files.Add(
                CreateMockedPackageFile(@"content", "qwerty.js", "bar contents").Object);

            barPackage.Files.Add(
                CreateMockedPackageFile(@"lib\net451", "test.dll", "test-dll").Object);

            var fileSystem = new PhysicalFileSystem(tmpDir.Path);
            var fooRoot = Path.Combine(fileSystem.Root, "foo", "1.0.0");
            fileSystem.AddFile(Path.Combine(fooRoot, "foo.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Foo</id><version>1.0.0</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine(fooRoot, "foo.1.0.0.nupkg.sha512"), "Foo-sha");
            fileSystem.AddFile(Path.Combine(fooRoot, "foo.1.0.0.nupkg"), GetPackageStream(fooPackage));

            var barRoot = Path.Combine(fileSystem.Root, "bar", "1.0.0-beta1");
            fileSystem.AddFile(Path.Combine(barRoot, "bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine(barRoot, "bar.1.0.0-beta1.nupkg.sha512"), "bar-sha");
            fileSystem.AddFile(Path.Combine(barRoot, "bar.1.0.0-beta1.nupkg"), GetPackageStream(barPackage));

            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var packages = repository.GetPackages().OrderBy(p => p.Identity.Id).ToList();

            // Assert
            Assert.Equal(2, packages.Count);

            var package = packages[1];
            Assert.Equal("foo", package.Identity.Id);
            Assert.Equal(SemanticVersion.Parse("1.0.0"), package.Identity.Version);
            using (var reader = package.GetReader()) {
                Assert.Equal("Foo", reader.GetIdentity().Id);
                Assert.Equal(package.Identity, reader.GetIdentity());
                // Assert.Equal(new string[]{
                //     "_rels/.rels", "Foo.nuspec", 
                //     "lib/net40/Foo.dll", "[Content_Types].xml",
                //     "foo.1.0.0.nupkg.sha512",
                // }, reader.GetFiles());
                // var packageFile = Assert.Single(reader.GetFiles());
                // Assert.Equal(@"lib\net40\Foo.dll", packageFile);
                //Assert.Equal(".NETFramework,Version=v4.0", packageFile.TargetFramework.FullName);
            }
            // package = packages[1];
            // Assert.Equal("Bar", package.Id);
            // Assert.Equal(SemanticVersion.Parse("1.0.0-beta1"), package.Version);

            // var files = package.GetFiles().OrderBy(p => p.Path.Length).ToList();
            // Assert.Equal(3, files.Count);
            // Assert.Equal(@"README.md", files[0].Path);

            // packageFile = files[1];
            // Assert.Equal(@"content\qwerty.js", packageFile.Path);
            // Assert.Null(packageFile.TargetFramework);

            // packageFile = files[2];
            // Assert.Equal(@"lib\net451\test.dll", packageFile.Path);
            // Assert.Equal(".NETFramework,Version=v4.5.1", packageFile.TargetFramework.FullName);
        }

        [Fact]
        public void GetPackages_SkipsPackagesWithoutHashFile()
        {
            // Arrange
            var barPackage = new PackageBuilder
            {
                Id = "Foo",
                Version = NuGetVersion.Parse("1.0.0-beta1-345"),
                Description = "Some description",
            };
            barPackage.Authors.Add("test author");
            barPackage.Files.Add(
                CreateMockedPackageFile(@"lib\net45", "Foo.dll", "lib contents").Object);

            var fileSystem = new PhysicalFileSystem(tmpDir.Path);
            var fooRoot = Path.Combine(fileSystem.Root, "foo", "1.0.0");
            fileSystem.AddFile(Path.Combine(fooRoot, "foo.1.0.0.nupkg"), "");
            fileSystem.AddFile(Path.Combine(fooRoot, "foo.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Foo</id><version>1.0.0</version><authors>None</authors><description>None</description></metadata></package>");
            var barRoot = Path.Combine("bar", "1.0.0-beta1-345");
            fileSystem.AddFile(Path.Combine(barRoot, "bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-345</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine(barRoot, "bar.1.0.0-beta1-345.nupkg.sha512"), "123");
            fileSystem.AddFile(Path.Combine(barRoot, "bar.1.0.0-beta1-345.nupkg"), GetPackageStream(barPackage));

            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var packages = repository.GetPackages().ToList();

            // Assert
            var package = Assert.Single(packages);

            Assert.Equal("bar", package.Identity.Id);
            Assert.Equal(NuGetVersion.Parse("1.0.0-beta1-345"), package.Identity.Version);
        }

        /*

        [Fact]
        public void FindPackagesById_ReturnsAllVersionsOfAPackage()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Foo</id><version>1.0.0</version><authors>None</authors><description>None</description></metadata></package>");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-345</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.1.0.0-beta1-345.nupkg.sha512"), "345sha");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-402", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-402</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-402", "Bar.1.0.0-beta1-402.nupkg.sha512"), "402sha");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.1.0.0-beta1.nupkg.sha512"), "beta1sha");

            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var packages = repository.FindPackagesById("Bar").OrderBy(p => p.Version).ToList();

            // Assert
            Assert.Equal(3, packages.Count);
            Assert.Equal(SemanticVersion.Parse("1.0.0-beta1"), packages[0].Version);
            Assert.Equal(SemanticVersion.Parse("1.0.0-beta1-345"), packages[1].Version);
            Assert.Equal(SemanticVersion.Parse("1.0.0-beta1-402"), packages[2].Version);
        }

        [Fact]
        public void FindPackagesById_IgnoresPackagesWithoutHashFiles()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Foo</id><version>1.0.0</version><authors>None</authors><description>None</description></metadata></package>");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-345</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.1.0.0-beta1-345.nupkg.sha512"), "345sha");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-402", "Bar.nuspec"));
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-402", "Bar.1.0.0-beta1-402.nupkg"), "nupkg contents");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.1.0.0-beta1.nupkg.sha512"), "beta1 hash");

            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var packages = repository.FindPackagesById("Bar").OrderBy(p => p.Version).ToList();

            // Assert
            Assert.Equal(2, packages.Count);
            Assert.Equal(SemanticVersion.Parse("1.0.0-beta1"), packages[0].Version);
            Assert.Equal(SemanticVersion.Parse("1.0.0-beta1-345"), packages[1].Version);
        }

        [Fact]
        public void FindPackageById_ReturnsSpecificVersionIfHashFileExists()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Foo</id><version>1.0.0</version><authors>None</authors><description>None</description></metadata></package>");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-345</version><authors>None</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.1.0.0-beta1-345.nupkg.sha512"));

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1</version><authors>test-author</authors><description>None</description></metadata></package>");
            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.1.0.0-beta1.nupkg.sha512"));

            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var package = repository.FindPackage("Bar", SemanticVersion.Parse("1.0.0.0-beta1"));

            // Assert
            Assert.NotNull(package);
            Assert.Equal(SemanticVersion.Parse("1.0.0-beta1"), package.Version);
            var author = Assert.Single(package.Authors);
            Assert.Equal("test-author", author);
        }

        [Fact]
        public void FindPackageById_IgnoresVersionsWithoutHashFiles()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nupkg"), @"Foo.nupkg contents");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1-345", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1-345</version><authors>None</authors><description>None</description></metadata></package>");

            fileSystem.AddFile(Path.Combine("Bar", "1.0.0-beta1", "Bar.nuspec"),
                @"<?xml version=""1.0""?><package><metadata><id>Bar</id><version>1.0.0.0-beta1</version><authors>test-author</authors><description>None</description></metadata></package>");
            var repository = new ExpandedPackageRepository(fileSystem);

            // Act
            var package = repository.FindPackage("Foo", SemanticVersion.Parse("1.0.0"));

            // Assert
            Assert.Null(package);
        }

        [Fact]
        public void RemovePackage_DeletesPackageDirectory_IfItExists()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0-beta2", "Foo.nuspec"), "Nuspec contents");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0-beta2", "Foo.1.0.0-beta2.nupkg.sha512"), "hash contents");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0-beta2", "tools", "net45", "Foo.targets"), "Foo.targets contents");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0-beta4", "Foo.nuspec"), "1.0.0-beta4 Nuspec contents");

            var repository = new ExpandedPackageRepository(fileSystem);
            var package = PackageUtility.CreatePackage("Foo", "1.0-beta2");

            // Act
            repository.RemovePackage(package);

            // Assert
            var deletedItems = Assert.Single(fileSystem.Deleted);
            Assert.Contains(Path.Combine("Foo", "1.0.0-beta2"), deletedItems);
            Assert.True(fileSystem.FileExists(Path.Combine("Foo", "1.0.0-beta4", "Foo.nuspec")));
        }

        [Fact]
        public void RemovePackage_Succeeds_IfPackageDoesNotExist()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "Foo.nuspec"), "Nuspec contents");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0", "tools", "net45", "Foo.targets"), "Foo.targets contents");
            fileSystem.AddFile(Path.Combine("Foo", "1.0.0-beta4", "Foo.nupkg"), "1.0.0-beta4 Nuspec contents");

            var repository = new ExpandedPackageRepository(fileSystem);
            var package = PackageUtility.CreatePackage("Foo", "1.0.0-beta4");

            // Act
            repository.RemovePackage(package);

            // Assert
            Assert.Empty(fileSystem.Deleted);
        }

        */

        [Fact]
        public void AddPackage_AddsExpandedPackageToThePackageDirectory()
        {
            // Arrange
            var fileSystem = new PhysicalFileSystem(tmpDir.Path);
            var repository = new ExpandedPackageRepository(fileSystem);
            var package = PackageCreator.CreatePackage("MyPackage","1.0.0-beta2",tmpDir.Path,packageBuilder => {
                packageBuilder.Authors.Add("test");
                packageBuilder.Files.Add(
                    CreateMockedPackageFile(@"content\net40\App_Code", "PreapplicationStartCode.cs", content: "Preapplication content").Object);
                packageBuilder.Files.Add(
                    CreateMockedPackageFile(@"tools\net40", "package.targets", "package.targets content").Object);
                packageBuilder.Files.Add(
                    CreateMockedPackageFile(@"lib\net40", "MyPackage.dll", "lib contents").Object);
            });

            // Act
            var addedPackage = repository.AddPackage(package);
            string expectedPath = Path.Combine(fileSystem.Root, "mypackage", "1.0.0-beta2", "mypackage.1.0.0-beta2.nupkg");
            Assert.Equal(expectedPath, addedPackage.Path);

            // Assert
            var reader = Manifest.ReadFrom(fileSystem.OpenFile(Path.Combine("mypackage", "1.0.0-beta2", "mypackage.nuspec")), validateSchema: true);
            Assert.Equal("MyPackage", reader.Metadata.Id);
            Assert.Equal(NuGetVersion.Parse("1.0.0-beta2"), reader.Metadata.Version);
            using(var file = File.OpenRead(package.Path)) {
                Assert.True(file.ContentEquals(fileSystem.OpenFile(Path.Combine("mypackage", "1.0.0-beta2", "mypackage.1.0.0-beta2.nupkg"))));
                file.Seek(0, SeekOrigin.Begin);
            }
            var downloader = new LocalPackageArchiveDownloader(package.Path, package.Identity, NullLogger.Instance);
            var sha = downloader.GetPackageHashAsync("SHA512",CancellationToken.None).Result;
            Assert.Equal(sha, fileSystem.ReadAllText(Path.Combine("mypackage", "1.0.0-beta2", "mypackage.1.0.0-beta2.nupkg.sha512")));
        }
    }
}