using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaGet.Core.Entities;
using BaGet.Core.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace BaGet.Tests.Controllers
{
    public class RegistrationIndexModuleTest
    {
        protected readonly ITestOutputHelper Helper;

        readonly string RegistrationIndexUrlFormatString = "v3/registration/{0}/index.json";

        public RegistrationIndexModuleTest(ITestOutputHelper helper)
        {
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        [Fact]
        public async Task RegistrationIndexCatalogEntryShouldContainDependencyGroupsWhenFrameworkDep()
        {
            var pkgService = new Mock<IPackageService>(MockBehavior.Strict);
            pkgService.Setup(p => p.FindAsync(It.IsAny<string>(), false, true)).ReturnsAsync(new List<Package>() {
                new Package() { Id = "abc", VersionString = "1.2.3", Dependencies = new System.Collections.Generic.List<PackageDependency>() {
                    new PackageDependency() { TargetFramework = "netstandard2.0" } // a framework dependency
                }}
            });
            using (TestServer server = TestServerBuilder.Create()
                .TraceToTestOutputHelper(Helper,LogLevel.Error)
                .WithMock(typeof(IPackageService), pkgService)
                .Build())
            {
                var services = server.Host.Services;
                Assert.Equal(pkgService.Object, services.GetRequiredService<IPackageService>());
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

        [Fact]
        public async Task RegistrationIndexCatalogEntryShouldContainDependencyGroupsWhenPackageDep()
        {
            var pkgService = new Mock<IPackageService>(MockBehavior.Strict);
            pkgService.Setup(p => p.FindAsync(It.IsAny<string>(), false, true)).ReturnsAsync(new List<Package>() {
                new Package() { Id = "abc", VersionString = "1.2.3", Dependencies = new System.Collections.Generic.List<PackageDependency>() {
                    new PackageDependency() { TargetFramework = "netstandard2.0", Id = "dep1" } // a package dependency
                }}
            });
            using (TestServer server = TestServerBuilder.Create()
                .TraceToTestOutputHelper(Helper,LogLevel.Error)
                .WithMock(typeof(IPackageService), pkgService)
                .Build())
            {
                var services = server.Host.Services;
                Assert.Equal(pkgService.Object, services.GetRequiredService<IPackageService>());
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
                        { "dependencies", new JArray(new JObject {
                            { "@id", $"https://api.nuget.org/v3/catalog0/data/2015.02.01.06.24.15/abc.1.2.3.json#dependencygroup/.netstandard2.0/dep1" },
                            { "@type", "PackageDependency" },
                            { "id", "dep1" },
                            { "range", null },
                            { "registration", "http://localhost/v3/registration/dep1/index.json" }
                        }) }
                    }
                );
                string message = "Actual part of response: \n" + groups.ToString();
                Assert.True(JToken.DeepEquals(expected, groups), message);
            }
        }
    }
}