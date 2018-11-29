using System;
using System.Threading.Tasks;
using BaGet.Core.Configuration;
using BaGet.Core.Mirror;
using BaGet.Tests.Support;
using Moq;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using Xunit.Abstractions;
using Xunit;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using BaGet.Core;
using NuGet.Protocol.Core.Types;

namespace BaGet.Tests
{
    public class MirrorServiceTest
    {
        private Mock<INuGetClient> client;
        private Mock<ISourceRepository> sourceRepo;
        private MirrorService mirrorService;
        private Mock<IPackageCacheService> localPackages;
        private Mock<IPackageDownloader> downloader;

        private PackageIdentity log4net = new PackageIdentity("log4net", NuGetVersion.Parse("2.0.8"));

        public MirrorServiceTest(ITestOutputHelper helper) {
            var logger = new XunitLoggerProvider(helper);
            localPackages = new Mock<IPackageCacheService>(MockBehavior.Strict);
            localPackages.Setup(p => p.AddPackageAsync(It.IsAny<Stream>())).Returns(Task.CompletedTask);
            downloader = new Mock<IPackageDownloader>(MockBehavior.Strict);
            downloader.Setup(d => d.DownloadOrNullAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream());
            MirrorOptions options = new MirrorOptions() {
                Enabled = true,
                PackageDownloadTimeoutSeconds = 10,
                UpstreamIndex = new Uri("http://example.com")
            };
            client = new Mock<INuGetClient>(MockBehavior.Strict);
            sourceRepo = new Mock<ISourceRepository>(MockBehavior.Strict);
            sourceRepo.Setup(s => s.GetPackageUriAsync(It.Is<string>(p => p == "log4net"), It.Is<string>(p => p == "2.0.8"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Uri("http://example.com/package/1"));
            client.Setup(c => c.GetRepository(options.UpstreamIndex)).Returns(sourceRepo.Object);
            mirrorService = new MirrorService(client.Object, localPackages.Object, downloader.Object, logger.CreateLogger<MirrorService>("MirrorServiceTest"), options);
        }

        [Fact]
        public async Task FindUpstreamMetadataAsyncShouldReturnUnlistedPackages() {
            sourceRepo.Setup(c => c.GetMetadataAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IPackageSearchMetadata[0]);
            var result = await mirrorService.FindUpstreamMetadataAsync("fsharp.core", CancellationToken.None);
            sourceRepo.Verify(c => c.GetMetadataAsync("fsharp.core", true, true, CancellationToken.None), Times.Once());
        }

        [Fact]
        public async Task MirrorAsyncShouldDownloadAndAddPackageWhenDoesNotExist() {
            localPackages.Setup(p => p.ExistsAsync(It.IsAny<PackageIdentity>())).ReturnsAsync(false);
            await mirrorService.MirrorAsync(log4net, CancellationToken.None);
            downloader.Verify(d => d.DownloadOrNullAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Once());
            localPackages.Verify(p => p.AddPackageAsync(It.IsAny<Stream>()), Times.Once());
        }

        [Fact]
        public async Task MirrorAsyncShouldNotDownloadAndAddPackageWhenExists() {
            localPackages.Setup(p => p.ExistsAsync(It.IsAny<PackageIdentity>())).ReturnsAsync(true);
            await mirrorService.MirrorAsync(log4net, CancellationToken.None);
            downloader.Verify(d => d.DownloadOrNullAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Never());
            localPackages.Verify(p => p.AddPackageAsync(It.IsAny<Stream>()), Times.Never());
        }

        [Fact]
        public async Task MirrorAsyncShouldDownloadAndAddPackageOnlyOnceWhenConcurrentRequests() {
            // long download
            downloader.Setup(d => d.DownloadOrNullAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>())).Returns(
               async () => {
                    await Task.Delay(100);
                    return new MemoryStream() as Stream;
               }                
            );
            localPackages.Setup(p => p.ExistsAsync(It.IsAny<PackageIdentity>())).ReturnsAsync(false);
            List<Task> tasks = new List<Task>();
            for(int i = 0; i< 10; i++) {
                tasks.Add(mirrorService.MirrorAsync(log4net, CancellationToken.None));
            }            
            await Task.WhenAll(tasks.ToArray());
            downloader.Verify(d => d.DownloadOrNullAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Once());
            localPackages.Verify(p => p.AddPackageAsync(It.IsAny<Stream>()), Times.Once());
        }
    }
}