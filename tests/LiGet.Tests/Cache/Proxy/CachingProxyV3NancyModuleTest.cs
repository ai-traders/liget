using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Autofac;
using LiGet.Cache;
using LiGet.Cache.Proxy;
using Moq;
using Nancy;
using Nancy.Testing;
using Xunit;

namespace LiGet.Tests.Cache.Proxy
{
    public class CachingProxyV3NancyModuleTest
    {
        private readonly Mock<IHttpClient> client;        
        private readonly IV3JsonInterceptor interceptor;
        private readonly Mock<IUrlReplacementsProvider> replacementsProvider;
        private readonly Mock<INupkgCacheProvider> cacheProvider;
        private Browser browser;

        string originalResponseContent = "{ \"message\" : \"response body\" }";
        private HttpResponseMessage originalResponse;

        Mock<ICacheTransaction> tx;

        public CachingProxyV3NancyModuleTest()
        {
            client = new Mock<IHttpClient>(MockBehavior.Strict);
            originalResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes(originalResponseContent))
            };
            originalResponse.Content.Headers.Add("Content-Type","application/json");
            client.Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>()))
                .ReturnsAsync(originalResponse);
            interceptor = new GenericV3JsonInterceptor();
            replacementsProvider = new Mock<IUrlReplacementsProvider>(MockBehavior.Strict);
            replacementsProvider.Setup(p => p.GetReplacements(It.IsAny<string>())).Returns(new Dictionary<string,string>());
            cacheProvider = new Mock<INupkgCacheProvider>(MockBehavior.Strict);
            tx = new Mock<ICacheTransaction>(MockBehavior.Strict);
            cacheProvider.Setup(c => c.OpenTransaction()).Returns(tx.Object);
            tx.Setup(t => t.TryGet(It.IsAny<string>(), It.IsAny<string>())).Returns(null as byte[]);
            tx.Setup(t => t.Dispose());
            tx.Setup(t => t.Insert(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()));

            var bootstrapper = new TestBootstrapper(typeof(CachingProxyV3NancyModule),b => {
                b.RegisterInstance(client.Object).As<IHttpClient>();
                b.RegisterInstance(interceptor).As<IV3JsonInterceptor>();
                b.RegisterInstance(replacementsProvider.Object).As<IUrlReplacementsProvider>();
                b.RegisterInstance(cacheProvider.Object).As<INupkgCacheProvider>();
            });
            this.browser = new Browser(bootstrapper, ctx => {
                ctx.HostName("testhost");
            });
        }

        [Fact]
        public void RequestToCatalogIndexGoesToSource()
        {
            replacementsProvider.Setup(p => p.GetOriginUri(new Uri("http://testhost/api/cache/v3/index.json")))
                .Returns(new Uri("http://origin.com/v3/index.json"));
            var result = browser.Get("/api/cache/v3/index.json", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            client.Verify(c => c.SendAsync(It.Is<HttpRequestMessage>(req => 
                req.Method == HttpMethod.Get &&
                req.RequestUri.AbsoluteUri == "http://origin.com/v3/index.json")), 
                Times.Once());
        }

        [Fact]
        public void ShouldForwardQueryParameters()
        {
            replacementsProvider.Setup(p => p.GetOriginUri(new Uri("http://testhost/api/cache/v3/index.json?semVerLevel=2.0.0")))
                .Returns(new Uri("http://origin.com/v3/index.json?semVerLevel=2.0.0"));
            var result = browser.Get("/api/cache/v3/index.json", with =>
            {
                with.Query("semVerLevel", "2.0.0");
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            client.Verify(c => c.SendAsync(It.Is<HttpRequestMessage>(req => 
                req.Method == HttpMethod.Get &&
                req.RequestUri.Query == "?semVerLevel=2.0.0")), 
                Times.Once());
        }

        [Fact]
        public void RequestToIndexRespondsWithCompactSourceWhenNoReplacements()
        {
            replacementsProvider.Setup(p => p.GetOriginUri(new Uri("http://testhost/api/cache/v3/index.json")))
                .Returns(new Uri("http://origin.com/v3/index.json"));
            var result = browser.Get("/api/cache/v3/index.json", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            string response = result.Body.AsString();
            Assert.Equal("{\"message\":\"response body\"}", response);
        }

        [Fact]
        public void RequestToIndexRespondsWithAlteredSourceWhenReplacementMatches()
        {
            replacementsProvider.Setup(p => p.GetReplacements(It.IsAny<string>()))
                .Returns(new Dictionary<string,string>() {
                    { "response body", "alter" }
                });
            replacementsProvider.Setup(p => p.GetOriginUri(new Uri("http://testhost/api/cache/v3/index.json")))
                .Returns(new Uri("http://origin.com/v3/index.json"));
            var result = browser.Get("/api/cache/v3/index.json", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            string response = result.Body.AsString();
            Assert.Equal("{\"message\":\"alter\"}", response);
        }

        [Fact]
        public void RequestToIndexRespondsWithStatusCodeAsInOrigin()
        {
            originalResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
            replacementsProvider.Setup(p => p.GetOriginUri(new Uri("http://testhost/api/cache/v3/index.json")))
                .Returns(new Uri("http://origin.com/v3/index.json"));
            var result = browser.Get("/api/cache/v3/index.json", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public void ServeNupkgWhenCacheMissShouldForwardToOriginAndInsertPkgToCache()
        {
            replacementsProvider.Setup(p => p.GetOriginUri(new Uri("http://testhost/api/cache/v3-flatcontainer/system.net.http/4.3.3/system.net.http.4.3.3.nupkg")))
                .Returns(new Uri("http://origin.com/v3-flatcontainer/system.net.http/4.3.3/system.net.http.4.3.3.nupkg"));
            var result = browser.Get("/api/cache/v3-flatcontainer/system.net.http/4.3.3/system.net.http.4.3.3.nupkg", with =>
            {
                with.HttpRequest();
            }).Result;
            string response = result.Body.AsString();
            Assert.Equal(originalResponseContent, response);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            client.Verify(c => c.SendAsync(It.Is<HttpRequestMessage>(req => 
                req.Method == HttpMethod.Get &&
                req.RequestUri.AbsoluteUri == "http://origin.com/v3-flatcontainer/system.net.http/4.3.3/system.net.http.4.3.3.nupkg")), 
                Times.Once());
            tx.Verify(t => t.TryGet("system.net.http","4.3.3"), Times.Once());
            tx.Verify(t => t.Insert("system.net.http","4.3.3", It.IsAny<byte[]>()), Times.Once());
        }

        [Fact]
        public void ServeNupkgWhenCacheHitShouldNotForwardToOriginAndNotInsertPkgToCache()
        {
            tx.Setup(t => t.TryGet(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Encoding.UTF8.GetBytes("cached"));
            replacementsProvider.Setup(p => p.GetOriginUri(new Uri("http://testhost/api/cache/v3-flatcontainer/system.net.http/4.3.3/system.net.http.4.3.3.nupkg")))
                .Returns(new Uri("http://origin.com/v3-flatcontainer/system.net.http/4.3.3/system.net.http.4.3.3.nupkg"));
            var result = browser.Get("/api/cache/v3-flatcontainer/system.net.http/4.3.3/system.net.http.4.3.3.nupkg", with =>
            {
                with.HttpRequest();
            }).Result;
            string response = result.Body.AsString();
            Assert.Equal("cached", response);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            client.Verify(c => c.SendAsync(It.IsAny<HttpRequestMessage>()), Times.Never());
            tx.Verify(t => t.TryGet("system.net.http","4.3.3"), Times.Once());
            tx.Verify(t => t.Insert("system.net.http","4.3.3", It.IsAny<byte[]>()), Times.Never());
        }
    }
}