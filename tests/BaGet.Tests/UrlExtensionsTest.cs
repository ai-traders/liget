using System;
using BaGet.Web.Extensions;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace BaGet.Tests
{
    public class UrlExtensionsTest
    {
        private HttpRequest request;

        public UrlExtensionsTest() {
            var requestMock = new Mock<HttpRequest>(MockBehavior.Strict);
            requestMock.SetupGet(x => x.Path).Returns("/some/req");
            requestMock.SetupGet(x => x.PathBase).Returns("/");
            requestMock.SetupGet(x => x.Host).Returns(new HostString("localhost",9090));
            requestMock.SetupGet(x => x.Scheme).Returns("http");

            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.SetupGet(x => x.Request).Returns(requestMock.Object);
            this.request = requestMock.Object;
        }

        [Fact]
        public void PackageBaseWhenNoPrefix() {
            string url = request.PackageBase("");
            Assert.Equal("http://localhost:9090/v3/package/", url);
        }

        [Fact]
        public void PackageBaseWhenPrefix() {
            string url = request.PackageBase("cache");
            Assert.Equal("http://localhost:9090/cache/v3/package/", url);
        }

        [Fact]
        public void PackageSearchWhenNoPrefix() {
            string url = request.PackageSearch("");
            Assert.Equal("http://localhost:9090/v3/search", url);
        }

        [Fact]
        public void PackageSearchWhenPrefix() {
            string url = request.PackageSearch("cache");
            Assert.Equal("http://localhost:9090/cache/v3/search", url);
        }
    }
}