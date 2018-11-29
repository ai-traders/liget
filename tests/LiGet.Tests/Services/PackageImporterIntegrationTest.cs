using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LiGet.Services;
using LiGet.Tests.Support;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LiGet.Tests.Services
{
    [Trait("Category", "integration")] // because it uses external nupkg files
    public class PackageImporterIntegrationTest
    {
        public ITestOutputHelper Helper { get; private set; }

        private TestServer server;

        public PackageImporterIntegrationTest(ITestOutputHelper helper)
        {
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
            server = TestServerBuilder.Create().TraceToTestOutputHelper(Helper, LogLevel.Error).Build();
        }

        [Fact]
        public async Task ImportExamplePackages()
        {
            var importer = server.Host.Services.GetRequiredService<PackageImporter>();
            using(var ms = new MemoryStream()) {
                var writer = new StreamWriter(ms, Encoding.UTF8);
                await importer.ImportAsync(TestResources.GetE2eInputDirectory(), writer);
                await writer.FlushAsync();
                string text = Encoding.UTF8.GetString(ms.ToArray());
                Assert.Contains("liget-test1.1.0.0.nupkg Success", text);
                Assert.Contains("liget-two.1.0.0.nupkg Success", text);
                Assert.Contains("liget-two.2.1.0.nupkg Success", text);
            }
        }
    }
}