using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using LiGet.NuGet.Server.Infrastructure;
using Nancy;
using Nancy.Responses.Negotiation;
using Nancy.Testing;
using NuGet.Protocol;
using Xunit;

namespace LiGet.Tests
{
    public class PackagesNancyModuleIntegrationTest : IDisposable
    {
        private Browser browser;
        private TemporaryDirectory tmpDir;
        ServerPackageRepositoryConfig config;

        public PackagesNancyModuleIntegrationTest() {
            tmpDir = new TemporaryDirectory();
            config = new ServerPackageRepositoryConfig() {
                // defaults from original impl
                EnableDelisting = false,
                EnableFrameworkFiltering = false,
                EnableFileSystemMonitoring = true,
                IgnoreSymbolsPackages = false,
                AllowOverrideExistingPackageOnPush = true,
                RunBackgroundTasks = false,
                RootPath = tmpDir.Path
            };
            var bootstrapper = new TestBootstrapper(typeof(PackagesNancyModule),b => {
                b.RegisterModule(new NuGetServerAutofacModule(config));
            });
            this.browser = new Browser(bootstrapper, ctx => {
                ctx.HostName("testhost");
                ctx.Accept(new MediaRange("application/atom+xml")); //TODO test with others
            });
        }

        public void Dispose() {
            tmpDir.Dispose();
        }

        [Fact]
        public void GetPackagesSpecifiedIdAndVersionEmptyRepository() {
            var result = browser.Get("/api/v2/Packages(Id='dummy',Version='1.0.0')", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public void GetPackagesSpecifiedIdAndVersionWhenExists() {
            var packagesInDropFolder = new Dictionary<string, LocalPackageInfo>
                {
                    {"test.1.11.nupkg", PackageHelper.CreatePackage(tmpDir.Path,"test", "1.11")},
                    {"test.1.9.nupkg", PackageHelper.CreatePackage(tmpDir.Path,"test", "1.9")},
                    {"test.2.0.0-0test.nupkg", PackageHelper.CreatePackage(tmpDir.Path,"test", "2.0.0-0test")},
                };
            var result = browser.Get("/api/v2/Packages(Id='test',Version='2.0.0-0test')", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("application/atom+xml", result.ContentType);
            var entries = XmlFeedHelper.ParsePage(result.BodyAsXml());
            var entry = Assert.Single(entries);
            Assert.Equal("2.0.0-0test", entry.Version.OriginalVersion);
            Assert.Equal("Test Author", Assert.Single(entry.Authors));
            Assert.Equal("test", entry.Id);
        }

        [Fact]
        public void GetFindPackagesByIdWhen3Exist() {
            var packagesInDropFolder = new Dictionary<string, LocalPackageInfo>
                {
                    {"test.1.11.nupkg", PackageHelper.CreatePackage(tmpDir.Path,"test", "1.11")},
                    {"test.1.9.nupkg", PackageHelper.CreatePackage(tmpDir.Path,"test", "1.9")},
                    {"test.2.0.0-0test.nupkg", PackageHelper.CreatePackage(tmpDir.Path,"test", "2.0.0-0test")},
                };
            var result = browser.Get("/api/v2/FindPackagesById()?id='test'", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("application/atom+xml", result.ContentType);
            var entries = XmlFeedHelper.ParsePage(result.BodyAsXml()).ToList();
            Assert.Equal(3, entries.Count);
            Assert.Equal(new [] { "1.11.0", "1.9.0", "2.0.0-0test" }, entries.Select(e => e.Version.OriginalVersion).OrderBy(v => v));
        }
    }
}