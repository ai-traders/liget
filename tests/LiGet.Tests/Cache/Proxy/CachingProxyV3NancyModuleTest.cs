using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Autofac;
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
        private Browser browser;

        string originalResponseContent = "{ \"message\" : \"response body\" }";
        private HttpResponseMessage originalResponse;

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

            var bootstrapper = new TestBootstrapper(typeof(CachingProxyV3NancyModule),b => {
                b.RegisterInstance(client.Object).As<IHttpClient>();
                b.RegisterInstance(interceptor).As<IV3JsonInterceptor>();
                b.RegisterInstance(replacementsProvider.Object).As<IUrlReplacementsProvider>();
            });
            this.browser = new Browser(bootstrapper, ctx => {
                ctx.HostName("testhost");
            });
        }

        [Fact]
        public void RequestToIndexGoesToSource()
        {
            replacementsProvider.Setup(p => p.GetOriginUri("index.json"))
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
            replacementsProvider.Setup(p => p.GetOriginUri("index.json?semVerLevel=2.0.0"))
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
            replacementsProvider.Setup(p => p.GetOriginUri("index.json"))
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
            replacementsProvider.Setup(p => p.GetOriginUri("index.json"))
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
            replacementsProvider.Setup(p => p.GetOriginUri("index.json"))
                .Returns(new Uri("http://origin.com/v3/index.json"));
            var result = browser.Get("/api/cache/v3/index.json", with =>
            {
                with.HttpRequest();
            }).Result;
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }
}