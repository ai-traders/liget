using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using BaGet.Core.Entities;
using BaGet.Core.Legacy.OData;
using BaGet.Core.Services;
using BaGet.Tests;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace BaGet.Tests
{
    public class PackagesV2ModuleTest
    {
        static readonly string V2Index = "/v2";
        static readonly string CompatV2Index = "/api/v2";
        public static IEnumerable<object[]> V2Cases = new[] {
            new object[] { V2Index },
            new object[] { CompatV2Index }
        };
        private ITestOutputHelper _helper;
        Mock<IPackageService> packageRepo;
        private Mock<IPackageStorageService> storageRepo;
        private TestServerBuilder serverBuilder;
        Package dummy1_0_0 = new Package() {
            Id = "Dummy",
            Title = "Dummy",
            Version = NuGetVersion.Parse("1.0.0"),
            Authors = new [] { "Anton Setiawan" },
            LicenseUrl = new Uri("https://github.com/antonmaju/dummy/blob/master/LICENSE"),
            MinClientVersion = null,
            Published = DateTime.Parse("1900-01-01T00:00:00"),
            Dependencies = new List<Core.Entities.PackageDependency>() {
                new Core.Entities.PackageDependency() { Id="Consul", VersionRange="[0.7.2.6, )", TargetFramework=".NETStandard2.0" }
            }
        };

        private void AssertDummyEntry(V2FeedPackageInfo dummyEntry)
        {
            Assert.Equal(dummy1_0_0.Title, dummyEntry.Title);
            Assert.Equal(dummy1_0_0.Id, dummyEntry.Id);
            Assert.Equal(dummy1_0_0.Version, dummyEntry.Version);
            Assert.Equal(dummy1_0_0.Authors, dummyEntry.Authors);
            Assert.Equal(dummy1_0_0.LicenseUrl.AbsoluteUri, dummyEntry.LicenseUrl);
            Assert.Null(dummyEntry.MinClientVersion);
            Assert.Equal(dummy1_0_0.Published, dummyEntry.Published);
            Assert.Equal("Consul:[0.7.2.6, ):.NETStandard2.0", dummyEntry.Dependencies);
        }

        public PackagesV2ModuleTest(ITestOutputHelper helper)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));        
            packageRepo = new Mock<IPackageService>(MockBehavior.Strict);
            storageRepo = new Mock<IPackageStorageService>(MockBehavior.Strict);
            serverBuilder = TestServerBuilder.Create()
                .TraceToTestOutputHelper(_helper, LogLevel.Error)
                .WithMock(typeof(IPackageService), packageRepo)
                .WithMock(typeof(IPackageStorageService), storageRepo);
        }

        [Fact]
        public async Task InvalidPathResponds400() {
            using (TestServer server = serverBuilder.Build())
            {
                var httpClient = server.CreateClient();
                var response = await httpClient.GetAsync("/v2/FindPackagesById()foo=bar");
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("/v2")]
        [InlineData("/v2/")]
        public async Task GetRootPath(string path) {
            using (TestServer server = serverBuilder.Build())
            {
                var httpClient = server.CreateClient();                
                var result = await httpClient.GetAsync(path);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.Equal("application/xml", result.Content.Headers.ContentType.MediaType);
                string response = await result.Content.ReadAsStringAsync();
                Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<service xml:base=""http://localhost/v2"" xmlns=""http://www.w3.org/2007/app"" xmlns:atom=""http://www.w3.org/2005/Atom""><workspace>
<atom:title type=""text"">Default</atom:title><collection href=""Packages""><atom:title type=""text"">Packages</atom:title></collection></workspace>
</service>", response);
            }
        }

        [Theory]
        [InlineData("/api/v2")] // compat for liget
        [InlineData("/api/v2/")] // compat for liget
        public async Task GetCompatRootPath(string path) {
            using (TestServer server = serverBuilder.Build())
            {
                var httpClient = server.CreateClient();                
                var result = await httpClient.GetAsync(path);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.Equal("application/xml", result.Content.Headers.ContentType.MediaType);
                string response = await result.Content.ReadAsStringAsync();
                Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<service xml:base=""http://localhost/api/v2"" xmlns=""http://www.w3.org/2007/app"" xmlns:atom=""http://www.w3.org/2005/Atom""><workspace>
<atom:title type=""text"">Default</atom:title><collection href=""Packages""><atom:title type=""text"">Packages</atom:title></collection></workspace>
</service>", response);
            }
        }

        [Theory]
        [MemberData(nameof(V2Cases))]
        public async Task GetPackagesSpecifiedIdAndVersionEmptyRepository(string index) {
            packageRepo.Setup(r => r.FindOrNullAsync(It.IsAny<PackageIdentity>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(null as Package).Verifiable();
            using (TestServer server = serverBuilder.Build())
            {
                var httpClient = server.CreateClient();                
                var result = await httpClient.GetAsync(index + "/Packages(Id='dummy',Version='1.0.0')");
                Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
                packageRepo.Verify(r => r.FindOrNullAsync(new PackageIdentity("dummy",NuGetVersion.Parse("1.0.0")), false, true), Times.Exactly(1));
            }
        }

        [Theory]
        [MemberData(nameof(V2Cases))]
        public async Task GetPackagesSpecifiedIdAndVersionWhenExists(string index) {
            packageRepo.Setup(r => r.FindOrNullAsync(It.IsAny<PackageIdentity>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(dummy1_0_0).Verifiable();
            using (TestServer server = serverBuilder.Build())
            {
                var httpClient = server.CreateClient(); 
                var result = await httpClient.GetAsync(index + "/Packages(Id='dummy',Version='1.0.0')");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.Equal("application/atom+xml", result.Content.Headers.ContentType.MediaType);
                packageRepo.Verify(r => r.FindOrNullAsync(new PackageIdentity("dummy",NuGetVersion.Parse("1.0.0")), false, true), Times.Exactly(1));
                var responseText = await result.Content.ReadAsStringAsync();               
                var entries = XmlFeedHelper.ParsePage(XDocument.Parse(responseText));
                var dummyEntry = Assert.Single(entries);
                AssertDummyEntry(dummyEntry);
            }
        }

        [Theory]
        [MemberData(nameof(V2Cases))]
        public async Task GetPackageContentWhenExists(string index) {
            packageRepo.Setup(r => r.FindOrNullAsync(It.IsAny<PackageIdentity>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(dummy1_0_0).Verifiable();
            packageRepo.Setup(r => r.IncrementDownloadCountAsync(It.IsAny<PackageIdentity>()))
                .ReturnsAsync(true);
            storageRepo.Setup(r => r.GetPackageStreamAsync(It.IsAny<PackageIdentity>()))
                .ReturnsAsync(new MemoryStream(new byte[3] { 1, 2, 3}));
            using (TestServer server = serverBuilder.Build())
            {
                var httpClient = server.CreateClient(); 
                var result = await httpClient.GetAsync(index + "/contents/dummy/1.0.0");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                Assert.Equal("application/zip", result.Content.Headers.ContentType.MediaType);
                storageRepo.Verify(r => r.GetPackageStreamAsync(It.Is<PackageIdentity>(p =>
                    p.Version.Equals(NuGetVersion.Parse("1.0.0")) &&
                    p.Id == "dummy")), Times.Exactly(1));
                var bytes = await result.Content.ReadAsByteArrayAsync();
                Assert.Equal(new byte[] { 1,2,3 }, bytes);
            }
        }

        [InlineData("aaabbb")]
        [InlineData("'aaabbb'")]
        [Theory]
        public async Task FindPackageByIdWhenEmptyRepository(string queryName) {
            packageRepo.Setup(r => r.FindAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Package>()).Verifiable();
            using (TestServer server = serverBuilder.Build())
            {
                var httpClient = server.CreateClient(); 
                var result = await httpClient.GetAsync("/v2/FindPackagesById()?id=" + queryName);
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                packageRepo.Verify(r => r.FindAsync("aaabbb", false, true), Times.Exactly(1));
                Assert.Equal("application/atom+xml", result.Content.Headers.ContentType.MediaType);
                var responseText = await result.Content.ReadAsStringAsync();      
                var entries = XmlFeedHelper.ParsePage(XDocument.Parse(responseText));
                Assert.Empty(entries);
                Assert.Contains("http://localhost/v2", responseText);
            }
        }

        [Theory]
        [MemberData(nameof(V2Cases))]
        public async Task FindPackageByIdWhenOneFound(string index) {
            packageRepo.Setup(r => r.FindAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Package>() { dummy1_0_0 }).Verifiable();
                using (TestServer server = serverBuilder.Build())
            {
                var httpClient = server.CreateClient(); 
                var result = await httpClient.GetAsync(index + "/FindPackagesById()?id=dummy");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                packageRepo.Verify(r => r.FindAsync("dummy", false, true), Times.Exactly(1));
                Assert.Equal("application/atom+xml", result.Content.Headers.ContentType.MediaType);
                var responseText = await result.Content.ReadAsStringAsync();      
                var entries = XmlFeedHelper.ParsePage(XDocument.Parse(responseText));
                var dummyEntry = Assert.Single(entries);
                AssertDummyEntry(dummyEntry);
                Assert.Contains("http://localhost" + index, responseText);
            }
        }
    }
}