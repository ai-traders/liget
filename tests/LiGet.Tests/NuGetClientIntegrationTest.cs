using System;
using System.Linq;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol;
using NuGet.Configuration;
using LiGet.Tests.Support;
using System.Net.Http;
using System.Net;
using System.Threading;
using FluentValidation;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System.IO;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using LiGet.Services;

namespace LiGet.Tests
{
    /// <summary>
    /// Uses official nuget client packages to talk to test host.
    /// </summary>
    public class NuGetClientIntegrationTest : IDisposable
    {
        private const string CacheIndex = "cache/v3/index.json";
        private const string CompatCacheIndex = "api/cache/v3/index.json";
        static readonly string MainIndex = "v3/index.json";
        static readonly string V2Index = "v2";
        static readonly string CompatV2Index = "api/v2";
        public static IEnumerable<object[]> V3Cases = new[] {
            new object[] { MainIndex },
            new object[] { CacheIndex },
            new object[] { CompatCacheIndex },
        };
        public static IEnumerable<object[]> V2Cases = new[] {
            new object[] { V2Index },
            new object[] { CompatV2Index }
        };
        protected readonly ITestOutputHelper Helper;
        private readonly TestServer server;
        private readonly List<Lazy<INuGetResourceProvider>> providers;
        SourceRepository _sourceRepository;
        private SourceCacheContext _cacheContext;
        HttpSourceResource _httpSource;
        private HttpClient _httpClient;
        string indexUrl;
        private NuGet.Common.ILogger logger = new NuGet.Common.NullLogger();
        TempDir tempDir;

        public NuGetClientIntegrationTest(ITestOutputHelper helper)
        {
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
            server = TestServerBuilder.Create().TraceToTestOutputHelper(Helper, LogLevel.Error).Build();
            providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());
            providers.Add(new Lazy<INuGetResourceProvider>(() => new PackageMetadataResourceV3Provider()));
            _httpClient = server.CreateClient();
            providers.Insert(0, new Lazy<INuGetResourceProvider>(() => new HttpSourceResourceProviderTestHost(_httpClient)));
            tempDir = new TempDir();
        }

        private void InitializeClient(string index)
        {
            indexUrl = new Uri(server.BaseAddress, index).AbsoluteUri;
            PackageSource packageSource = new PackageSource(indexUrl);
            _sourceRepository = new SourceRepository(packageSource, providers);
            _cacheContext = new SourceCacheContext() { NoCache = true, MaxAge = new DateTimeOffset(), DirectDownload = true };
            _httpSource = _sourceRepository.GetResource<HttpSourceResource>();
            Assert.IsType<HttpSourceTestHost>(_httpSource.HttpSource);
        }

        private PackageMetadataResourceV3 GetPackageMetadataResource()
        {
            RegistrationResourceV3 regResource = _sourceRepository.GetResource<RegistrationResourceV3>();
            ReportAbuseResourceV3 reportAbuseResource = _sourceRepository.GetResource<ReportAbuseResourceV3>();
            var packageMetadataRes = new PackageMetadataResourceV3(_httpSource.HttpSource, regResource, reportAbuseResource);
            return packageMetadataRes;
        }

        private string GetApiKey(string arg)
        {
            return "";
        }
        public void Dispose()
        {
            if(server != null)
                server.Dispose();
            if(tempDir != null)
                tempDir.Dispose();
        }

        [Theory]
        [MemberData(nameof(V3Cases))]
        public async Task GetIndexShouldReturn200(string indexEndpoint)
        {
            InitializeClient(indexEndpoint);
            var index = await _httpClient.GetAsync(indexUrl);
            Assert.Equal(HttpStatusCode.OK, index.StatusCode);
            return;
        }

        [Theory]
        [MemberData(nameof(V2Cases))]
        public async Task GetV2IndexShouldReturn200(string indexEndpoint)
        {
            InitializeClient(indexEndpoint);
            var index = await _httpClient.GetAsync(indexUrl);
            Assert.Equal(HttpStatusCode.OK, index.StatusCode);
            return;
        }

        [Theory]
        [MemberData(nameof(V3Cases))]
        public async Task IndexResourceHasManyEntries(string indexEndpoint)
        {
            InitializeClient(indexEndpoint);
            var indexResource = await _sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            Assert.NotEmpty(indexResource.Entries);
        }

        [Theory]
        [MemberData(nameof(V2Cases))]
        public async Task V2IndexResourceReturnsODataServiceDocument(string indexEndpoint)
        {
            InitializeClient(indexEndpoint);
            var indexResource = await _sourceRepository.GetResourceAsync<ODataServiceDocumentResourceV2>();
            Assert.NotNull(indexResource);
            Assert.Equal("http://localhost/" + indexEndpoint, indexResource.BaseAddress);
        }

        [Theory]
        [MemberData(nameof(V3Cases))]
        public async Task IndexIncludesAtLeastOneSearchQueryEntry(string indexEndpoint)
        {
            InitializeClient(indexEndpoint);
            var indexResource = await _sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            Assert.NotEmpty(indexResource.GetServiceEntries("SearchQueryService"));
        }

        [Theory]
        [MemberData(nameof(V3Cases))]
        public async Task IndexIncludesAtLeastOneRegistrationsBaseEntry(string indexEndpoint)
        {
            InitializeClient(indexEndpoint);
            var indexResource = await _sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            Assert.NotEmpty(indexResource.GetServiceEntries("RegistrationsBaseUrl"));
        }

        [Theory]
        [MemberData(nameof(V3Cases))]
        public async Task IndexIncludesAtLeastOnePackageBaseAddressEntry(string indexEndpoint)
        {
            InitializeClient(indexEndpoint);
            var indexResource = await _sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            Assert.NotEmpty(indexResource.GetServiceEntries("PackageBaseAddress/3.0.0"));
        }

        [Theory]
        [MemberData(nameof(V3Cases))]
        public async Task IndexIncludesAtLeastOneSearchAutocompleteServiceEntry(string indexEndpoint)
        {
            InitializeClient(indexEndpoint);
            var indexResource = await _sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            Assert.NotEmpty(indexResource.GetServiceEntries("SearchAutocompleteService"));
        }

        [Fact]
        [Trait("Category", "integration")] // because it uses external nupkg files
        public async Task GetV2FindPackagesById()
        {
            InitializeClient(MainIndex);
            var packageResource = await _sourceRepository.GetResourceAsync<PackageUpdateResource>();
            await packageResource.Push(TestResources.GetNupkgBagetTest1(),
                null, 5, false, GetApiKey, GetApiKey, false, logger);
            var response = await _httpClient.GetAsync("/v2/FindPackagesById()?id=liget-test1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseText = await response.Content.ReadAsStringAsync();               
            var entries = XmlFeedHelper.ParsePage(XDocument.Parse(responseText));
            var entry = Assert.Single(entries);
            AssertLiGetTestEntry(entry);
        }

        private void AssertLiGetTestEntry(V2FeedPackageInfo dummyEntry)
        {
            Assert.Equal("liget-test1", dummyEntry.Id);
            Assert.Equal(NuGetVersion.Parse("1.0.0"), dummyEntry.Version);
            Assert.Equal("liget-test1", dummyEntry.Authors.Single());
            Assert.Equal("::netstandard2.0", dummyEntry.Dependencies);
        }

        // Push
        [Fact]
        [Trait("Category", "integration")] // because it uses external nupkg files
        public async Task PushValidPackage()
        {
            InitializeClient(MainIndex);
            var packageResource = await _sourceRepository.GetResourceAsync<PackageUpdateResource>();
            await packageResource.Push(TestResources.GetNupkgBagetTest1(),
                null, 5, false, GetApiKey, GetApiKey, false, logger);
            PackageMetadataResourceV3 packageMetadataRes = GetPackageMetadataResource();
            var meta = await packageMetadataRes.GetMetadataAsync("liget-test1", true, true, _cacheContext, logger, CancellationToken.None);
            Assert.NotEmpty(meta);
            var one = meta.First();
            Assert.Equal(new PackageIdentity("liget-test1", NuGetVersion.Parse("1.0.0")), one.Identity);
        }

        [Fact]
        [Trait("Category", "integration")] // because it uses external nupkg files
        public async Task PushedPackageShouldExistWithPackageDependenciesInPackageService()
        {
            InitializeClient(MainIndex);
            var packageResource = await _sourceRepository.GetResourceAsync<PackageUpdateResource>();
            await packageResource.Push(TestResources.GetNupkgBagetTest1(),
                null, 5, false, GetApiKey, GetApiKey, false, logger);

            var packageService = server.Host.Services.GetRequiredService<IPackageService>();
            Assert.True(await packageService.ExistsAsync(new PackageIdentity("liget-test1", NuGetVersion.Parse("1.0.0"))));
            var found = await packageService.FindOrNullAsync(new PackageIdentity("liget-test1", NuGetVersion.Parse("1.0.0")), false, true);
            Assert.NotNull(found.Dependencies);
            Assert.NotEmpty(found.Dependencies);
            var one = found.Dependencies.Single();
            Assert.Equal("netstandard2.0", one.TargetFramework);
        }

        [Fact]
        [Trait("Category", "integration")] // because it uses external nupkg files
        public async Task PushAndDownloadValidPackage()
        {
            InitializeClient(MainIndex);
            var packageResource = await _sourceRepository.GetResourceAsync<PackageUpdateResource>();
            await packageResource.Push(TestResources.GetNupkgBagetTest1(),
                null, 5, false, GetApiKey, GetApiKey, false, logger);
            var findByIdRes = new RemoteV3FindPackageByIdResource(_sourceRepository, _httpSource.HttpSource);
            var downloader = await findByIdRes.GetPackageDownloaderAsync(
                new PackageIdentity("liget-test1", NuGetVersion.Parse("1.0.0")),
                _cacheContext, logger, CancellationToken.None);
            string tempPath = Path.Combine(tempDir.UniqueTempFolder, "test.nupkg");
            await downloader.CopyNupkgFileToAsync(tempPath, CancellationToken.None);
            Assert.Equal(File.ReadAllBytes(TestResources.GetNupkgBagetTest1()), File.ReadAllBytes(tempPath));
        }

        [Fact]
        [Trait("Category", "integration")] // because it uses external nupkg files
        public async Task Push2VersionsAndGetPackageVersions()
        {
            InitializeClient(MainIndex);
            var packageResource = await _sourceRepository.GetResourceAsync<PackageUpdateResource>();
            await packageResource.Push(TestResources.GetNupkgBagetTwoV1(),
                null, 5, false, GetApiKey, GetApiKey, false, logger);
            await packageResource.Push(TestResources.GetNupkgBagetTwoV2(),
                null, 5, false, GetApiKey, GetApiKey, false, logger);
            var findByIdRes = new RemoteV3FindPackageByIdResource(_sourceRepository, _httpSource.HttpSource);
            var versions = await findByIdRes.GetAllVersionsAsync("liget-two",
                _cacheContext, logger, CancellationToken.None);
            Assert.Contains(versions, p => p.Equals(NuGetVersion.Parse("1.0.0")));
            Assert.Contains(versions, p => p.Equals(NuGetVersion.Parse("2.1.0")));
        }

        [Fact]
        [Trait("Category", "integration")] // because it uses external nupkg files
        public async Task Push2VersionsAndGetDependencyInfo()
        {
            InitializeClient(MainIndex);
            var packageResource = await _sourceRepository.GetResourceAsync<PackageUpdateResource>();
            await packageResource.Push(TestResources.GetNupkgBagetTwoV1(),
                null, 5, false, GetApiKey, GetApiKey, false, logger);
            var findByIdRes = new RemoteV3FindPackageByIdResource(_sourceRepository, _httpSource.HttpSource);
            var info = await findByIdRes.GetDependencyInfoAsync("liget-two", NuGetVersion.Parse("1.0.0"),
                _cacheContext, logger, CancellationToken.None);
            Assert.Equal(new PackageIdentity("liget-two", NuGetVersion.Parse("1.0.0")), info.PackageIdentity);
        }

        [Fact]
        [Trait("Category", "integration")] // because it uses external nupkg files
        public async Task PushAndDeletePackage()
        {
            InitializeClient(MainIndex);
            var packageResource = await _sourceRepository.GetResourceAsync<PackageUpdateResource>();
            await packageResource.Push(TestResources.GetNupkgBagetTest1(),
                null, 5, false, GetApiKey, GetApiKey, false, logger);
            await packageResource.Delete(
                "liget-test1", "1.0.0", GetApiKey, _ => true, false, logger);
            PackageMetadataResourceV3 packageMetadataRes = GetPackageMetadataResource();
            var meta = await packageMetadataRes.GetMetadataAsync("liget-test1", true, true, _cacheContext, logger, CancellationToken.None);
            Assert.Empty(meta);
        }

        // Search
        [Fact]
        [Trait("Category", "integration")] // because it uses external nupkg files
        public async Task PushOneThenSearchPackage()
        {
            InitializeClient(MainIndex);
            var packageResource = await _sourceRepository.GetResourceAsync<PackageUpdateResource>();
            await packageResource.Push(TestResources.GetNupkgBagetTest1(),
                null, 5, false, GetApiKey, GetApiKey, false, logger);
            PackageSearchResourceV3 search = GetSearch();
            var found = await search.SearchAsync("liget-test1", new SearchFilter(true), 0, 10, logger, CancellationToken.None);
            Assert.NotEmpty(found);
            var one = found.First();
            Assert.Equal(new PackageIdentity("liget-test1", NuGetVersion.Parse("1.0.0")), one.Identity);
        }

        private PackageSearchResourceV3 GetSearch()
        {
            PackageMetadataResourceV3 packageMetadataRes = GetPackageMetadataResource();
            RawSearchResourceV3 rawSearchResource = _sourceRepository.GetResource<RawSearchResourceV3>();
            Assert.NotNull(rawSearchResource);
            var search = new PackageSearchResourceV3(rawSearchResource, packageMetadataRes);
            return search;
        }

        [Fact]
        [Trait("Category", "integration")] // because it uses external nupkg files
        public async Task Push2VersionsThenSearchPackage()
        {
            InitializeClient(MainIndex);
            var packageResource = await _sourceRepository.GetResourceAsync<PackageUpdateResource>();
            await packageResource.Push(TestResources.GetNupkgBagetTwoV1(),
                null, 5, false, GetApiKey, GetApiKey, false, logger);
            await packageResource.Push(TestResources.GetNupkgBagetTwoV2(),
                null, 5, false, GetApiKey, GetApiKey, false, logger);
            PackageSearchResourceV3 search = GetSearch();
            var found = await search.SearchAsync("liget-two", new SearchFilter(true), 0, 10, logger, CancellationToken.None);
            Assert.NotEmpty(found);
            var ids = found.Select(p => p.Identity);
            Assert.Contains(ids, p => p.Version.Equals(NuGetVersion.Parse("2.1.0")));
            var versions = await found.First().GetVersionsAsync();
            Assert.Contains(versions, p => p.Version.Equals(NuGetVersion.Parse("1.0.0")));
            Assert.Contains(versions, p => p.Version.Equals(NuGetVersion.Parse("2.1.0")));
        }

        // Cache
        [Fact]
        [Trait("Category", "integration")] // because it talks to nuget.org
        public async Task CacheGetPackageMetadata()
        {
            InitializeClient(CacheIndex);
            PackageMetadataResourceV3 packageMetadataRes = GetPackageMetadataResource();
            var meta = await packageMetadataRes.GetMetadataAsync("log4net", true, true, _cacheContext, logger, CancellationToken.None);
            Assert.NotEmpty(meta);
            var versions = meta.Select(m => m.Identity.Version);
            Assert.Contains(versions, one => NuGetVersion.Parse("2.0.8").Equals(one));
        }

        [Fact]
        [Trait("Category", "integration")] // because it talks to nuget.org
        public async Task CacheDownloadValidPackage()
        {
            InitializeClient(CacheIndex);
            var findByIdRes = new RemoteV3FindPackageByIdResource(_sourceRepository, _httpSource.HttpSource);
            var downloader = await findByIdRes.GetPackageDownloaderAsync(
                new PackageIdentity("log4net", NuGetVersion.Parse("2.0.8")),
                _cacheContext, logger, CancellationToken.None);
            string tempPath = Path.Combine(tempDir.UniqueTempFolder, "test.nupkg");
            await downloader.CopyNupkgFileToAsync(tempPath, CancellationToken.None);
            Assert.True(File.Exists(tempPath));
        }

        [Fact]
        [Trait("Category", "integration")] // because it talks to nuget.org
        public async Task CacheGetPackageVersions()
        {
            InitializeClient(CacheIndex);
            var findByIdRes = new RemoteV3FindPackageByIdResource(_sourceRepository, _httpSource.HttpSource);
            var versions = await findByIdRes.GetAllVersionsAsync("log4net",
                _cacheContext, logger, CancellationToken.None);
            Assert.Contains(versions, p => p.Equals(NuGetVersion.Parse("2.0.8")));
        }

        [Fact]
        [Trait("Category", "integration")] // because it talks to nuget.org
        public async Task CacheGetDependencyInfo()
        {
            InitializeClient(CacheIndex);
            var findByIdRes = new RemoteV3FindPackageByIdResource(_sourceRepository, _httpSource.HttpSource);
            var info = await findByIdRes.GetDependencyInfoAsync("log4net", NuGetVersion.Parse("2.0.8"),
                _cacheContext, logger, CancellationToken.None);
            Assert.Equal(new PackageIdentity("log4net", NuGetVersion.Parse("2.0.8")), info.PackageIdentity);
        }
    }
}
