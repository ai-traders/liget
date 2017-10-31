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
        [InlineData("index.json", "https://api.nuget.org/v3/index.json")]
        [InlineData("registration3-gz-semver2/log4net/index.json", "https://api.nuget.org/v3/registration3-gz-semver2/log4net/index.json")]
        [InlineData("registration3-gz-semver2/log4net/index.json?sem=2", "https://api.nuget.org/v3/registration3-gz-semver2/log4net/index.json?sem=2")]
        public void GetsOriginUri(string relative, string expectedFull) {
            var origin = provider.GetOriginUri(relative).AbsoluteUri;
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
    }
}