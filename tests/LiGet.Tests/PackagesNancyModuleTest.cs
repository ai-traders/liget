using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Autofac;
using LiGet.Models;
using Moq;
using Nancy;
using Nancy.Responses.Negotiation;
using Nancy.Testing;
using NuGet;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;
using Xunit;

namespace LiGet.Tests
{
    public class PackagesNancyModuleTest
    {
        private Browser browser;
        Mock<IPackageService> packageRepo;

        HostedPackage dummy1_0_0 = new HostedPackage(new ODataPackage() {
            Title = "Dummy",
            Version = "1.0.0.0",
            NormalizedVersion = "1.0.0",
            Authors = "Anton Setiawan",
            LicenseUrl = "https://github.com/antonmaju/dummy/blob/master/LICENSE",
            Description = "Portable class library for creating objects based on spec",
            PackageHash = "U4qLh9MQsEdfShm70Pda8ouHiQyHUTybFqIbBoyKXYJoMz8b8PDoAq+uN8QZmwBZNtlDKeQkyVS/WuJe1HWCSA==",
            PackageHashAlgorithm = "SHA512",
            ReleaseNotes = "Initial release",
            Tags = "dummy object creation",
            MinClientVersion = null,
            Published = DateTime.Parse("1900-01-01T00:00:00")
        });

        private void AssertDummyEntry(V2FeedPackageInfo dummyEntry)
        {
            Assert.Equal(dummy1_0_0.PackageInfo.Title, dummyEntry.Title);
            Assert.Equal(dummy1_0_0.PackageInfo.Version, dummyEntry.Version.OriginalVersion);
            Assert.Equal(dummy1_0_0.PackageInfo.Authors, Assert.Single(dummyEntry.Authors));
            Assert.Equal(dummy1_0_0.PackageInfo.LicenseUrl, dummyEntry.LicenseUrl);
            Assert.Equal(dummy1_0_0.PackageInfo.PackageHash, dummyEntry.PackageHash);
            Assert.Equal(dummy1_0_0.PackageInfo.PackageHashAlgorithm, dummyEntry.PackageHashAlgorithm);
            Assert.Null(dummyEntry.MinClientVersion);
            Assert.Equal(dummy1_0_0.PackageInfo.Published, dummyEntry.Published);
        }

        public PackagesNancyModuleTest() {
            packageRepo = new Mock<IPackageService>(MockBehavior.Strict);
            var bootstrapper = new TestBootstrapper(b => {
                b.RegisterInstance(packageRepo.Object).As<IPackageService>();
            });
            this.browser = new Browser(bootstrapper, ctx => {
                ctx.HostName("testhost");
                ctx.Accept(new MediaRange("application/atom+xml")); //TODO test with others
            });
        }

        [Fact]
        public void InvalidPathRespons400() {
            var result = browser.Get("/api/v2/blah", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public void GetPackagesSpecifiedIdAndVersionEmptyRepository() {
            packageRepo.Setup(r => r.FindPackage(It.IsAny<string>(),It.IsAny<NuGetVersion>())).Returns(null as HostedPackage).Verifiable();
            var result = browser.Get("/api/v2/Packages(Id='dummy',Version='1.0.0')", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            packageRepo.Verify(r => r.FindPackage("dummy",NuGetVersion.Parse("1.0.0")), Times.Exactly(1));
        }

        [Fact]
        public void GetPackagesSpecifiedIdAndVersionWhenExists() {
            packageRepo.Setup(r => r.FindPackage(It.IsAny<string>(),It.IsAny<NuGetVersion>())).Returns(dummy1_0_0).Verifiable();
            var result = browser.Get("/api/v2/Packages(Id='dummy',Version='1.0.0')", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("application/atom+xml", result.ContentType);
            packageRepo.Verify(r => r.FindPackage("dummy",NuGetVersion.Parse("1.0.0")), Times.Exactly(1));
            var entries = XmlFeedHelper.ParsePage(result.BodyAsXml());
            var dummyEntry = Assert.Single(entries);
            AssertDummyEntry(dummyEntry);
        }

        

        // [Fact]
        // public void FindPackageByIdWhenEmptyRepository() {
        //     packageRepo.Setup(r => r.FindPackagesById(It.IsAny<string>())).Returns(new List<ODataPackage>()).Verifiable();
        //     var result = browser.Get("/api/v2/FindPackagesById()", with =>
        //     {
        //         with.Query("id","'aaabbb'");
        //         with.HttpRequest();
        //     }).Result;
        //     Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        //     packageRepo.Verify(r => r.FindPackagesById("aaabbb"), Times.Exactly(1));
        // }
    }
}