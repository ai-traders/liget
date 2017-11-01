using System;
using System.Linq;
using LiGet.Cache.Proxy;
using Moq;
using Xunit;

namespace LiGet.Tests.Cache.Proxy
{
    public class UrlReplacementsProviderTest
    {
        private UrlReplacementsProvider provider; 
        Mock<ICachingProxyConfig> config;

        public UrlReplacementsProviderTest() {
            config = new Mock<ICachingProxyConfig>(MockBehavior.Strict);
            config.SetupGet(c => c.V3NugetIndexSource).Returns("https://api.nuget.org/v3/index.json");
            provider = new UrlReplacementsProvider(config.Object);
        }

        [Theory]
        [InlineData("http://testhost/api/cache/v3/index.json", "https://api.nuget.org/v3/index.json")]
        [InlineData("http://testhost/api/cache/v3/registration3-gz-semver2/log4net/index.json", "https://api.nuget.org/v3/registration3-gz-semver2/log4net/index.json")]
        [InlineData("http://testhost/api/cache/v3/registration3-gz-semver2/log4net/index.json?sem=2", "https://api.nuget.org/v3/registration3-gz-semver2/log4net/index.json?sem=2")]
        [InlineData("http://testhost/api/cache/v3-flatcontainer/log4net/4.3.0/log4net.nupkg", "https://api.nuget.org/v3-flatcontainer/log4net/4.3.0/log4net.nupkg")]
        public void GetsOriginUri(string liget, string expectedFull) {
            var origin = provider.GetOriginUri(new Uri(liget)).AbsoluteUri;
            Assert.Equal(expectedFull, origin);
        }

        [Fact]
        public void GetReplacementsReturnsCurrentLigetUrl() {
            var rep = provider.GetReplacements("http://liget.com:9011/api/cache/v3");
            Assert.Contains(rep, kv => 
                kv.Key.Equals("https://api.nuget.org/v3") &&
                kv.Value.Equals("http://liget.com:9011/api/cache/v3")
            );
        }

        [Fact]
        public void GetReplacementsShouldReplaceFlatcontainer() {
            var rep = provider.GetReplacements("http://liget.com:9011/api/cache/v3");
            var single = rep.First(kv => 
                kv.Key.Equals("https://api.nuget.org/v3") &&
                kv.Value.Equals("http://liget.com:9011/api/cache/v3"));
            string original = "https://api.nuget.org/v3-flatcontainer";
            string replaced = original.Replace(single.Key, single.Value);
            Assert.Equal(replaced, "http://liget.com:9011/api/cache/v3-flatcontainer");
        }
    }
}