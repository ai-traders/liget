using System;
using System.Threading.Tasks;
using BaGet.Core.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using BaGet.Core.Entities;
using Newtonsoft.Json.Linq;
using System.IO;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System.Text;

namespace BaGet.Tests
{

    public class PackageControllerTest 
    {
        protected readonly ITestOutputHelper Helper;

        readonly string IndexUrlFormatString = "v3/package/{0}/index.json";
        private string exampleNuSpec = "<?xml version=\"1.0\"?>";

        public PackageControllerTest(ITestOutputHelper helper)
        {
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        [Theory]
        [InlineData("id01")]
        [InlineData("id02")]
        public async Task AskEmptyServerForNotExistingPackageID(string packageID)
        {
            using (TestServer server = TestServerBuilder.Create().TraceToTestOutputHelper(Helper,LogLevel.Error).Build())
            {
                //Ask Empty Storage for a not existings ID
                var response = await  server.CreateClient().GetAsync(string.Format(IndexUrlFormatString, packageID));
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async Task AskServerForVersionsOfExistingPackageID()
        {
            var pkgService = new Mock<IPackageService>(MockBehavior.Strict);
            pkgService.Setup(p => p.FindAsync("abc", false, false)).ReturnsAsync(new Package[] {
                new Package() { Id = "abc", VersionString = "1.2.3" },
                new Package() { Id = "abc", VersionString = "1.4.7" }
            });
            using (TestServer server = TestServerBuilder.Create()
                .TraceToTestOutputHelper(Helper,LogLevel.Error)
                .WithMock(typeof(IPackageService), pkgService)
                .Build())
            {
                var services = server.Host.Services;
                Assert.Equal(pkgService.Object, services.GetRequiredService<IPackageService>());
                // https://docs.microsoft.com/en-us/nuget/api/package-base-address-resource#enumerate-package-versions
                var response = await  server.CreateClient().GetAsync(string.Format(IndexUrlFormatString, "abc"));
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                var jsonString = await response.Content.ReadAsStringAsync();
                var actual = JObject.Parse(jsonString);
                var expected = new JObject {
                    { "versions", new JArray(
                        "1.2.3",
                        "1.4.7") }
                };
                Assert.True(JToken.DeepEquals(expected, actual));
            }
        }

        [Fact]
        public async Task DownloadExistingPackage()
        {
            var pkgService = new Mock<IPackageService>(MockBehavior.Strict);
            pkgService.Setup(p => p.IncrementDownloadCountAsync(It.IsAny<PackageIdentity>())).ReturnsAsync(true);
            var pkgStorageService = new Mock<IPackageStorageService>(MockBehavior.Strict);
            pkgStorageService.Setup(p => p.GetPackageStreamAsync(It.IsAny<PackageIdentity>()))
                .ReturnsAsync(new MemoryStream(new byte[10]));
            using (TestServer server = TestServerBuilder.Create()
                .TraceToTestOutputHelper(Helper,LogLevel.Error)
                .WithMock(typeof(IPackageStorageService), pkgStorageService)
                .WithMock(typeof(IPackageService), pkgService)
                .Build())
            {
                var services = server.Host.Services;
                Assert.Equal(pkgStorageService.Object, services.GetRequiredService<IPackageStorageService>());
                // https://docs.microsoft.com/en-us/nuget/api/package-base-address-resource#download-package-content-nupkg
                var response = await server.CreateClient().GetAsync(string.Format("v3/package/{0}/{1}/{0}.{1}.nupkg", "abc","1.2.3"));
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("application/octet-stream", response.Content.Headers.ContentType.MediaType);
                var byteArray = await response.Content.ReadAsByteArrayAsync();
                Assert.Equal(10, byteArray.Length);
                pkgStorageService.Verify(p => p.GetPackageStreamAsync(It.Is<PackageIdentity>(pi => 
                    pi.Id == "abc" &&
                    pi.Version.Equals(NuGetVersion.Parse("1.2.3")))), 
                    Times.Once());
            }
        }

        [Fact]
        public async Task DownloadNonExistingPackage()
        {
            var pkgService = new Mock<IPackageService>(MockBehavior.Strict);
            pkgService.Setup(p => p.IncrementDownloadCountAsync(It.IsAny<PackageIdentity>())).ReturnsAsync(false);
            using (TestServer server = TestServerBuilder.Create()
                .TraceToTestOutputHelper(Helper,LogLevel.Error)
                .WithMock(typeof(IPackageService), pkgService)
                .Build())
            {
                var response = await server.CreateClient().GetAsync(string.Format("v3/package/{0}/{1}/{0}.{1}.nupkg", "abc","1.2.3"));
                Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async Task DownloadExistingPackageManifest()
        {
            var pkgService = new Mock<IPackageService>(MockBehavior.Strict);
            pkgService.Setup(p => p.ExistsAsync(It.IsAny<PackageIdentity>())).ReturnsAsync(true);
            var pkgStorageService = new Mock<IPackageStorageService>(MockBehavior.Strict);
            pkgStorageService.Setup(p => p.GetNuspecStreamAsync(It.IsAny<PackageIdentity>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(exampleNuSpec)));
            using (TestServer server = TestServerBuilder.Create()
                .TraceToTestOutputHelper(Helper,LogLevel.Error)
                .WithMock(typeof(IPackageStorageService), pkgStorageService)
                .WithMock(typeof(IPackageService), pkgService)
                .Build())
            {
                var services = server.Host.Services;
                Assert.Equal(pkgStorageService.Object, services.GetRequiredService<IPackageStorageService>());
                // https://docs.microsoft.com/en-us/nuget/api/package-base-address-resource#download-package-manifest-nuspec
                var response = await server.CreateClient().GetAsync(string.Format("v3/package/{0}/{1}/{0}.nuspec", "abc","1.2.3"));
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/xml", response.Content.Headers.ContentType.MediaType);
                var nuspecContent = await response.Content.ReadAsStringAsync();
                Assert.Equal(exampleNuSpec, nuspecContent);
                pkgStorageService.Verify(p => p.GetNuspecStreamAsync(It.Is<PackageIdentity>(pi => 
                    pi.Id == "abc" &&
                    pi.Version.Equals(NuGetVersion.Parse("1.2.3")))), 
                    Times.Once());
            }
        }

         [Fact]
        public async Task DownloadExistingPackageReadme()
        {
            var pkgService = new Mock<IPackageService>(MockBehavior.Strict);
            pkgService.Setup(p => p.ExistsAsync(It.IsAny<PackageIdentity>())).ReturnsAsync(true);
            var pkgStorageService = new Mock<IPackageStorageService>(MockBehavior.Strict);
            pkgStorageService.Setup(p => p.GetReadmeStreamAsync(It.IsAny<PackageIdentity>()))
                .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("readme content")));
            using (TestServer server = TestServerBuilder.Create()
                .TraceToTestOutputHelper(Helper,LogLevel.Error)
                .WithMock(typeof(IPackageStorageService), pkgStorageService)
                .WithMock(typeof(IPackageService), pkgService)
                .Build())
            {
                var services = server.Host.Services;
                Assert.Equal(pkgStorageService.Object, services.GetRequiredService<IPackageStorageService>());
                //no docs for this.
                var response = await server.CreateClient().GetAsync(string.Format("v3/package/{0}/{1}/readme", "abc","1.2.3"));
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/markdown", response.Content.Headers.ContentType.MediaType);
                var readmeContent = await response.Content.ReadAsStringAsync();
                Assert.Equal("readme content", readmeContent);
                pkgStorageService.Verify(p => p.GetReadmeStreamAsync(It.Is<PackageIdentity>(pi => 
                    pi.Id == "abc" &&
                    pi.Version.Equals(NuGetVersion.Parse("1.2.3")))), 
                    Times.Once());
            }
        }
    }
}
