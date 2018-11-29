using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiGet;
using LiGet.Configuration;
using LiGet.Cache;
using LiGet.Tests.Support;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace LiGet.Tests
{
    public class CacheServiceIntegrationTest : IDisposable
    {
        private CacheService mirrorService;
        private TempDir tempDir;

        PackageIdentity log4netId = new PackageIdentity("log4net", NuGetVersion.Parse("2.0.8"));

        public CacheServiceIntegrationTest(ITestOutputHelper helper) {
            var logger = new XunitLoggerProvider(helper);
            tempDir = new TempDir();
            IPackageCacheService localPackages = new FileSystemPackageCacheService(tempDir.UniqueTempFolder);
            IPackageDownloader downloader = new PackageDownloader(new System.Net.Http.HttpClient(), 
                logger.CreateLogger<PackageDownloader>("CacheServiceItest"));
            CacheOptions options = new CacheOptions() {
                Enabled = true,
                UpstreamIndex = new System.Uri("https://api.nuget.org/v3/index.json"),
                PackagesPath = tempDir.UniqueTempFolder,
                PackageDownloadTimeoutSeconds = 10
            };
            mirrorService = new CacheService(new NuGetClient(logger.CreateLogger<NuGetClient>("CacheServiceItest")),localPackages, downloader, logger.CreateLogger<CacheService>("CacheServiceItest"), options);
        }
        public void Dispose()
        {
            tempDir.Dispose();
        }

        [Fact]
        public async Task CacheAsync() {
            await mirrorService.CacheAsync(new PackageIdentity("log4net", NuGetVersion.Parse("2.0.8")), CancellationToken.None);
            Assert.True(await mirrorService.ExistsAsync(log4netId));
        }

        [Fact]
        public async Task CacheAsyncThenStream() {
            await mirrorService.CacheAsync(new PackageIdentity("log4net", NuGetVersion.Parse("2.0.8")), CancellationToken.None);
            using(var stream = await mirrorService.GetPackageStreamAsync(log4netId)) {
                
            }            
        }

        [Fact]
        public async Task FindUpstreamMetadataAsyncShouldReturnUnlistedPackages() {
            var result = await mirrorService.FindUpstreamMetadataAsync("fsharp.core", CancellationToken.None);
            var versions = result.Select(r => r.Identity.Version);
            Assert.Contains(versions, v => v.Equals(NuGetVersion.Parse("4.5.3")));
            Assert.Contains(result, p => !p.IsListed);
        }
    }
}