using System;
using System.Collections.Generic;
using System.IO;
using LiGet.Configuration;
using LiGet.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace LiGet.Tests
{
    /// <summary>
    /// fluent builder pattern implementation.
    /// private/hidden Constructor, please use one of the static methods for creation.
    /// </summary>
    public class TestServerBuilder
    {

        private const string DefaultPackagesFolderName = "Packages";

        private readonly string DatabaseTypeKey = $"{nameof(LiGetOptions.Database)}:{nameof(DatabaseOptions.Type)}";
        private readonly string ConnectionStringKey = $"{nameof(LiGetOptions.Database)}:{nameof(DatabaseOptions.ConnectionString)}";
        private readonly string StorageTypeKey = $"{nameof(LiGetOptions.Storage)}:{nameof(StorageOptions.Type)}";
        private readonly string FileSystemStoragePathKey = $"{nameof(LiGetOptions.Storage)}:{nameof(FileSystemStorageOptions.Path)}";
        private readonly string SearchTypeKey = $"{nameof(LiGetOptions.Search)}:{nameof(SearchOptions.Type)}";
        private readonly string MirrorEnabledKey = $"{nameof(LiGetOptions.Mirror)}:{nameof(MirrorOptions.Enabled)}";
        private readonly string UpstreamIndexKey = $"{nameof(LiGetOptions.Mirror)}:{nameof(MirrorOptions.UpstreamIndex)}";
        private readonly string MirrorCachedDirKey = $"{nameof(LiGetOptions.Mirror)}:{nameof(MirrorOptions.PackagesPath)}";
        private readonly string LiGetCompatEnabledKey = $"{nameof(LiGetOptions.LiGetCompat)}:{nameof(LiGetCompatibilityOptions.Enabled)}";

        private ITestOutputHelper _helper;
        private LogLevel _minimumLevel = LogLevel.None;
        private Action<IServiceCollection> configureTestServices = _ => {};

        /// <summary>
        /// private/hidden Constructor.
        /// Tests should use some of the static methods!
        /// </summary>
        private TestServerBuilder()
        {
            Configuration = new Dictionary<string, string>();
        }

        public TestServerBuilder WithMock<T>(Type serviceType, Mock<T> serviceMock) where T: class
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (serviceMock == null)
            {
                throw new ArgumentNullException(nameof(serviceMock));
            }

            var previous = configureTestServices;
            configureTestServices = services => {
                previous(services);
                services.AddSingleton(serviceType, serviceMock.Object);
            };
            return this;
        }

        /// <summary>
        /// In Memory representation of Config Settings
        /// </summary>
        public Dictionary<string, string> Configuration { get; private set; }

        /// <summary>
        /// Xunit.ITestOutputHelper is used as Logging-Target (Microsoft.Extensions.Logging)
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="minimumLevel"></param>
        /// <returns></returns>
        public TestServerBuilder TraceToTestOutputHelper(ITestOutputHelper helper, LogLevel minimumLevel)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _minimumLevel = minimumLevel;
            return this;
        }

        /// <summary>
        /// Test Server Builder instance that uses a empty subfolder of System.IO.Path.GetTempPath
        /// </summary>
        /// <returns></returns>
        public static TestServerBuilder Create()
        {
            return new TestServerBuilder().UseEmptyTempFolder();
        }


        /// <summary>
        /// Creates a subdirectory (Path.GetTempPath() + Guid) and uses this as location for
        /// - Sqlite file  => .\LiGet.db 
        /// - FilePackageStorageService => .\Packages\*.*
        /// </summary>
        /// <returns></returns>
        private TestServerBuilder UseEmptyTempFolder()
        {
            Configuration.Add(DatabaseTypeKey, DatabaseType.Sqlite.ToString());
            string uniqueTempFolder = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(uniqueTempFolder);
            string resolvedSqliteFile = Path.Combine(uniqueTempFolder, "LiGet.db");
            string storageFolderPath = Path.Combine(uniqueTempFolder, DefaultPackagesFolderName);
            Configuration.Add(ConnectionStringKey, string.Format("Data Source={0}", resolvedSqliteFile));
            Configuration.Add(StorageTypeKey, StorageType.FileSystem.ToString());
            Configuration.Add(FileSystemStoragePathKey, storageFolderPath);
            Configuration.Add(SearchTypeKey, nameof(SearchType.Database));
            Configuration.Add(MirrorEnabledKey, true.ToString());
            Configuration.Add(UpstreamIndexKey, "https://api.nuget.org/v3/index.json");
            Configuration.Add(LiGetCompatEnabledKey, true.ToString());
            string cacheDirName = Path.Combine(uniqueTempFolder, "CachedPackages");
            Configuration.Add(MirrorCachedDirKey, cacheDirName);
            return this;
        }

        public TestServer Build()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(Configuration);
            IWebHostBuilder hostBuilder = new WebHostBuilder()
                .UseConfiguration(configurationBuilder.Build())
                .UseStartup(typeof(Startup))
                .ConfigureTestServices(configureTestServices);

            if (_helper != null)
            {
                hostBuilder.ConfigureLogging((builder) =>
                {
                    builder.AddProvider(new XunitLoggerProvider(_helper));
                    builder.SetMinimumLevel(_minimumLevel);
                });
            }

            TestServer server = new TestServer(hostBuilder);

            //Ensure that the Database is created, we use the same feature like inside the Startup in case of Env.IsDevelopment (EF-Migrations)
            var scopeFactory = server.Host.Services.GetRequiredService<IServiceScopeFactory>();
            using (var scope = scopeFactory.CreateScope())
            {
                scope.ServiceProvider
                    .GetRequiredService<IContext>()
                    .Database
                    .Migrate();
            }
            return server;
        }
    }
}
