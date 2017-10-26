using System.Collections.Generic;
using Autofac;
using LiGet.Models;
using Moq;
using Nancy;
using Nancy.Testing;
using NuGet;
using NuGet.Protocol;
using Xunit;

namespace LiGet.Tests
{
    public class PackagesNancyModuleTest
    {
        private Browser browser;
        Mock<IPackageService> packageRepo;

        public PackagesNancyModuleTest() {
            packageRepo = new Mock<IPackageService>(MockBehavior.Strict);
            var bootstrapper = new TestBootstrapper(b => {
                b.RegisterInstance(packageRepo.Object).As<IPackageRepository>();
            });
            this.browser = new Browser(bootstrapper, ctx => ctx.HostName("testhost"));
        }

        [Fact]
        public void UnknownPathRespons501() {
            var result = browser.Get("/api/v2/blah", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.NotImplemented, result.StatusCode);
        }
    }
}