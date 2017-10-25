using Nancy;
using Nancy.Testing;
using Xunit;

namespace LiGet.Tests
{
    public class PackagesNancyModuleTest
    {
        private Browser browser;

        public PackagesNancyModuleTest() {
            var bootstrapper = new TestBootstrapper();
            this.browser = new Browser(bootstrapper);
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