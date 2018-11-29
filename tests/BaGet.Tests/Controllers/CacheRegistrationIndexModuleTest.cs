using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BaGet.Core.Mirror;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace BaGet.Tests.Controllers
{
    public class CacheRegistrationIndexModuleTest
    {
        protected readonly ITestOutputHelper Helper;

        readonly string RegistrationIndexUrlFormatString = "cache/v3/registration/{0}/index.json";

        public CacheRegistrationIndexModuleTest(ITestOutputHelper helper)
        {
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        [Fact]
        public async Task RegistrationIndexCatalogEntryShouldContainDependencyGroups()
        {
            var pkgService = new Mock<IMirrorService>(MockBehavior.Strict);
            Mock<IPackageSearchMetadata> abcPackage = new Mock<IPackageSearchMetadata>();
            abcPackage.SetupGet(a => a.Identity).Returns(new NuGet.Packaging.Core.PackageIdentity("abc", NuGetVersion.Parse("1.2.3")));
            abcPackage.SetupGet(a => a.DependencySets).Returns(new PackageDependencyGroup[1] {
                new PackageDependencyGroup(NuGetFramework.Parse("netstandard2.0"), new NuGet.Packaging.Core.PackageDependency[0])
            });
            pkgService.Setup(p => p.FindUpstreamMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IPackageSearchMetadata>() {
                    abcPackage.Object
                });
            using (TestServer server = TestServerBuilder.Create()
                .TraceToTestOutputHelper(Helper, LogLevel.Error)
                .WithMock(typeof(IMirrorService), pkgService)
                .Build())
            {
                var services = server.Host.Services;
                Assert.Equal(pkgService.Object, services.GetRequiredService<IMirrorService>());
                // https://docs.microsoft.com/en-us/nuget/api/registration-base-url-resource#registration-pages-and-leaves
                var response = await  server.CreateClient().GetAsync(string.Format(RegistrationIndexUrlFormatString, "abc"));
                Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
                var jsonString = await response.Content.ReadAsStringAsync();
                var actual = JObject.Parse(jsonString);
                var groups = actual["items"][0]["items"][0]["catalogEntry"]["dependencyGroups"];
                var expected = new JArray(
                    new JObject {
                        { "@id", $"https://api.nuget.org/v3/catalog0/data/2015.02.01.06.24.15/abc.1.2.3.json#dependencygroup/.netstandard2.0" },
                        { "@type", "PackageDependencyGroup" },
                        { "targetFramework", "netstandard2.0" },
                        { "dependencies", null }
                    }
                );
                string message = "Actual part of response: \n" + groups.ToString();
                Assert.True(JToken.DeepEquals(expected, groups), message);
            }
        }
    }
}