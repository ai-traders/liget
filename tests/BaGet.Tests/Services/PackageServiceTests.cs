using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaGet.Core.Entities;
using BaGet.Core.Services;
using BaGet.Tests;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace BaGet.Core.Tests.Services
{
    public class PackageServiceTests
    {
        public ITestOutputHelper Helper { get; private set; }

        private TestServer server;

        public PackageServiceTests(ITestOutputHelper helper)
        {
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
            server = TestServerBuilder.Create().TraceToTestOutputHelper(Helper, LogLevel.Error).Build();
        }

        [Fact]
        public async Task GetPackageWithDependencies() {
            var packageService = server.Host.Services.GetRequiredService<IPackageService>();

            var result = await packageService.AddAsync(new Package() {
                Id = "Dummy",
                Title = "Dummy",
                Listed = true,
                Version = NuGetVersion.Parse("1.0.0"),
                Authors = new [] { "Anton Setiawan" },
                LicenseUrl = new Uri("https://github.com/antonmaju/dummy/blob/master/LICENSE"),
                MinClientVersion = null,
                Published = DateTime.Parse("1900-01-01T00:00:00"),
                Dependencies = new List<Core.Entities.PackageDependency>() {
                    new Core.Entities.PackageDependency() { Id="Consul", VersionRange="[0.7.2.6, )", TargetFramework=".NETStandard2.0" }
                }
            });
            Assert.Equal(PackageAddResult.Success, result);

            var found = await packageService.FindOrNullAsync(new NuGet.Packaging.Core.PackageIdentity("dummy", NuGetVersion.Parse("1.0.0")), false, true);
            Assert.NotNull(found.Dependencies);
            Assert.NotEmpty(found.Dependencies);
            var one = found.Dependencies.Single();
            Assert.Equal("Consul", one.Id);
            Assert.Equal("[0.7.2.6, )", one.VersionRange);
        }

        public class AddAsync : FactsBase
        {
            [Fact]
            public async Task ReturnsPackageAlreadyExistsOnUniqueConstraintViolation()
            {
                await Task.Yield();
            }

            [Fact]
            public async Task AddsPackage()
            {
                // TODO: Returns Success
                // TODO: Adds package
                await Task.Yield();
            }
        }

        public class ExistsAsync : FactsBase
        {
            [Theory]
            [InlineData("Package", "1.0.0", true)]
            [InlineData("Package", "1.0.0.0", true)]
            [InlineData("Unlisted.Package", "1.0.0", true)]
            [InlineData("Fake.Package", "1.0.0", false)]
            public async Task ReturnsTrueIfPackageExists(string packageId, string packageVersion, bool exists)
            {
                await Task.Yield();
            }
        }

        public class FindAsync : FactsBase
        {
            [Fact]
            public async Task ReturnsEmptyListIfPackageDoesNotExist()
            {
                // Ensure the context has packages with a different id/version
                await Task.Yield();
            }

            [Theory]
            [MemberData(nameof(ReturnsPackagesData))]
            public async Task ReturnsPackages(string packageId, string packageVersion, bool includeUnlisted, bool exists)
            {
                // TODO: Ensure resulting versions are normalized.
                await Task.Yield();
            }

            public static IEnumerable<object[]> ReturnsPackagesData()
            {
                object[] ReturnsPackagesHelper(string packageId, string packageVersion, bool includeUnlisted, bool exists)
                {
                    return new object[] { packageId, packageVersion, includeUnlisted, exists };
                }

                // A package that doesn't exist should never be returned
                yield return ReturnsPackagesHelper("Fake.Package", "1.0.0", includeUnlisted: true, exists: false);

                // A listed package should be returned regardless of the "includeUnlisted" parameter
                yield return ReturnsPackagesHelper("Package", "1.0.0", includeUnlisted: false, exists: true);
                yield return ReturnsPackagesHelper("Package", "1.0.0", includeUnlisted: true, exists: true);

                // The inputted package version should be normalized
                yield return ReturnsPackagesHelper("Package", "1.0.0.0", includeUnlisted: false, exists: true);

                // Unlisted packages should only be returned if "includeUnlisted" is true
                yield return ReturnsPackagesHelper("Unlisted.Package", "1.0.0", includeUnlisted: false, exists: false);
                yield return ReturnsPackagesHelper("Unlisted.Package", "1.0.0", includeUnlisted: true, exists: true);
            }
        }

        public class FindOrNullAsync : FactsBase
        {
            [Fact]
            public async Task ReturnsNullIfPackageDoesNotExist()
            {
                await Task.Yield();
            }

            [Theory]
            [MemberData(nameof(ReturnsPackageData))]
            public async Task ReturnsPackage(string packageId, string packageVersion, bool includeUnlisted, bool exists)
            {
                // TODO: Ensure resulting versions are normalized.
                await Task.Yield();
            }

            public static IEnumerable<object[]> ReturnsPackageData()
            {
                object[] ReturnsPackageHelper(string packageId, string packageVersion, bool includeUnlisted, bool exists)
                {
                    return new object[] { packageId, packageVersion, includeUnlisted, exists };
                }

                // A package that doesn't exist should never be returned
                yield return ReturnsPackageHelper("Fake.Package", "1.0.0", includeUnlisted: true, exists: false);

                // A listed package should be returned regardless of the "includeUnlisted" parameter
                yield return ReturnsPackageHelper("Package", "1.0.0", includeUnlisted: false, exists: true);
                yield return ReturnsPackageHelper("Package", "1.0.0", includeUnlisted: true, exists: true);

                // The inputted package version should be normalized
                yield return ReturnsPackageHelper("Package", "1.0.0.0", includeUnlisted: false, exists: true);

                // Unlisted packages should only be returned if "includeUnlisted" is true
                yield return ReturnsPackageHelper("Unlisted.Package", "1.0.0", includeUnlisted: false, exists: false);
                yield return ReturnsPackageHelper("Unlisted.Package", "1.0.0", includeUnlisted: true, exists: true);
            }
        }

        public class UnlistPackageAsync : FactsBase
        {
            [Fact]
            public async Task ReturnsFalseIfPackageDoesNotExist()
            {
                await Task.Yield();
            }

            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public async Task UnlistsPackage(bool listed)
            {
                // TODO: This should succeed if the package is unlisted.
                // TODO: Returns true
                await Task.Yield();
            }
        }

        public class RelistPackageAsync : FactsBase
        {
            [Fact]
            public async Task ReturnsFalseIfPackageDoesNotExist()
            {
                await Task.Yield();
            }

            [Theory]
            [InlineData(false)]
            [InlineData(true)]
            public async Task RelistsPackage(bool listed)
            {
                // TODO: This should succeed if the package is listed.
                // TODO: Return true
                await Task.Yield();
            }
        }

        public class AddDownloadAsync : FactsBase
        {
            [Fact]
            public async Task ReturnsFalseIfPackageDoesNotExist()
            {
                await Task.Yield();
            }

            [Fact]
            public async Task IncrementsPackageDownloads()
            {
                await Task.Yield();
            }
        }

        public class HardDeletePackageAsync : FactsBase
        {
            [Fact]
            public async Task ReturnsFalseIfPackageDoesNotExist()
            {
                await Task.Yield();
            }

            [Fact]
            public async Task DeletesPackage()
            {
                await Task.Yield();
            }
        }

        public class FactsBase
        {
            protected readonly Mock<IContext> _context;
            protected readonly PackageService _target;

            public FactsBase()
            {
                _context = new Mock<IContext>();
                _target = new PackageService(_context.Object);
            }
        }
    }
}
