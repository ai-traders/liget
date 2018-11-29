using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using BaGet.Azure.Configuration;
using BaGet.Azure.Extensions;
using BaGet.Azure.Search;
using BaGet.Configurations;
using BaGet.Core;
using BaGet.Core.Configuration;
using BaGet.Core.Entities;
using BaGet.Core.Extensions;
using BaGet.Core.Legacy.OData;
using BaGet.Core.Mirror;
using BaGet.Core.Services;
using BaGet.Entities;
using BaGet.Web.Controllers;
using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;

namespace BaGet.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureBaGet(
            this IServiceCollection services,
            IConfiguration configuration,
            bool httpServices = false)
        {
            services.Configure<BaGetOptions>(configuration);

            services.AddBaGetContext();
            services.ConfigureAzure(configuration);

            if (httpServices)
            {
                services.ConfigureHttpServices();
            }

            services.AddTransient<IPackageService, PackageService>();
            services.AddTransient<IIndexingService, IndexingService>();
            services.AddTransient<IPackageDeletionService, PackageDeletionService>();
            services.AddMirrorServices();

            services.ConfigureStorageProviders(configuration);
            services.ConfigureSearchProviders();
            services.ConfigureAuthenticationProviders();

            services.AddTransient<PackageImporter, PackageImporter>();

            return services;
        }

        public static IServiceCollection AddBaGetContext(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddScoped<IContext>(provider =>
            {
                var databaseOptions = provider.GetRequiredService<IOptions<BaGetOptions>>()
                    .Value
                    .Database;

                databaseOptions.EnsureValid();

                switch (databaseOptions.Type)
                {
                    case DatabaseType.Sqlite:
                        return provider.GetRequiredService<SqliteContext>();

                    case DatabaseType.SqlServer:
                        return provider.GetRequiredService<SqlServerContext>();

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported database provider: {databaseOptions.Type}");
                }
            });

            services.AddDbContext<SqliteContext>((provider, options) =>
            {
                var databaseOptions = provider.GetRequiredService<IOptions<BaGetOptions>>()
                    .Value
                    .Database;

                options.UseSqlite(databaseOptions.ConnectionString);
            });

            services.AddDbContext<SqlServerContext>((provider, options) =>
            {
                var databaseOptions = provider.GetRequiredService<IOptions<BaGetOptions>>()
                    .Value
                    .Database;

                options.UseSqlServer(databaseOptions.ConnectionString);
            });

            return services;
        }

        public static void AddCarter(this IServiceCollection services)
        {
            //PATCH around same issues as in https://github.com/CarterCommunity/Carter/pull/88
            // we rather just explicitly state assembly with modules to fix loading issues.
            var assemblies = new [] { typeof(IndexModule).Assembly };

            CarterDiagnostics diagnostics = new CarterDiagnostics();
            services.AddSingleton(diagnostics);

            var validators = assemblies.SelectMany(ass => ass.GetTypes())
                .Where(typeof(IValidator).IsAssignableFrom)
                .Where(t => !t.GetTypeInfo().IsAbstract);

            foreach (var validator in validators)
            {
                diagnostics.AddValidator(validator);
                services.AddSingleton(typeof(IValidator), validator);
            }

            services.AddSingleton<IValidatorLocator, DefaultValidatorLocator>();

            services.AddRouting();

            var modules = assemblies.SelectMany(x => x.GetTypes()
                .Where(t =>
                    !t.IsAbstract &&
                    typeof(CarterModule).IsAssignableFrom(t) &&
                    t != typeof(CarterModule) &&
                    t.IsPublic
                ));

            foreach (var module in modules)
            {
                diagnostics.AddModule(module);
                services.AddScoped(module);
                services.AddScoped(typeof(CarterModule), module);
            }

            var schs = assemblies.SelectMany(x => x.GetTypes().Where(t => typeof(IStatusCodeHandler).IsAssignableFrom(t) && t != typeof(IStatusCodeHandler)));
            foreach (var sch in schs)
            {
                diagnostics.AddStatusCodeHandler(sch);
                services.AddScoped(typeof(IStatusCodeHandler), sch);
            }

            var responseNegotiators = assemblies.SelectMany(x => x.GetTypes()
                .Where(t =>
                    !t.IsAbstract &&
                    typeof(IResponseNegotiator).IsAssignableFrom(t) &&
                    t != typeof(IResponseNegotiator) &&
                    t != typeof(DefaultJsonResponseNegotiator)
                ));

            foreach (var negotiatator in responseNegotiators)
            {
                diagnostics.AddResponseNegotiator(negotiatator);
                services.AddSingleton(typeof(IResponseNegotiator), negotiatator);
            }

            services.AddSingleton<IResponseNegotiator, DefaultJsonResponseNegotiator>();
        }

        public static IServiceCollection ConfigureHttpServices(this IServiceCollection services)
        {
            services.AddTransient<LiGetCompatibilityOptions, LiGetCompatibilityOptions>(provider =>
                provider
                    .GetRequiredService<IOptions<BaGetOptions>>()
                    .Value
                    .LiGetCompat
            );

            services.AddSingleton<IEdmModel>(provider => {
                var odataModelBuilder = new NuGetWebApiODataModelBuilder();
                odataModelBuilder.Build();
                return odataModelBuilder.Model;
            });
            services.AddTransient<IODataPackageSerializer, ODataPackageSerializer>();

            AddCarter(services);
            services.AddCors();
            services.AddSingleton<IConfigureOptions<CorsOptions>, ConfigureCorsOptions>();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                // Do not restrict to local network/proxy
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            return services;
        }

        public static IServiceCollection ConfigureStorageProviders(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<FileSystemStorageOptions>(configuration.GetSection(nameof(BaGetOptions.Storage)));

            services.AddTransient<IPackageStorageService>(provider =>
            {
                var storageOptions = provider
                    .GetRequiredService<IOptions<BaGetOptions>>()
                    .Value
                    .Storage;

                storageOptions.EnsureValid();

                switch (storageOptions.Type)
                {
                    case StorageType.FileSystem:
                        return provider.GetRequiredService<FilePackageStorageService>();

                    case StorageType.AzureBlobStorage:
                        return provider.GetRequiredService<BlobPackageStorageService>();

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported storage service: {storageOptions.Type}");
                }
            });

            services.AddTransient(provider =>
            {
                var options = provider
                    .GetRequiredService<IOptions<FileSystemStorageOptions>>()
                    .Value;

                options.EnsureValid();

                return new FilePackageStorageService(options.Path);
            });

            services.AddBlobPackageStorageService();

            return services;
        }

        public static IServiceCollection ConfigureSearchProviders(this IServiceCollection services)
        {
            services.AddTransient<ISearchService>(provider =>
            {
                var searchOptions = provider
                    .GetRequiredService<IOptions<BaGetOptions>>()
                    .Value
                    .Search;

                searchOptions.EnsureValid();

                switch (searchOptions.Type)
                {
                    case SearchType.Database:
                        return provider.GetRequiredService<DatabaseSearchService>();

                    case SearchType.Azure:
                        return provider.GetRequiredService<AzureSearchService>();

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported search service: {searchOptions.Type}");
                }
            });

            services.AddTransient<DatabaseSearchService>();
            services.AddAzureSearch();

            return services;
        }

        /// <summary>
        /// Add the services that mirror an upstream package source.
        /// </summary>
        /// <param name="services">The defined services.</param>
        public static IServiceCollection AddMirrorServices(this IServiceCollection services)
        {
            services.AddTransient<IPackageCacheService>(provider => 
            {
                var options = provider.GetRequiredService<IOptions<BaGetOptions>>().Value;
                return new FileSystemPackageCacheService(options.Mirror.PackagesPath);
            });
            services.AddTransient<INuGetClient, NuGetClient>();
            services.AddSingleton<IMirrorService>(provider =>
            {
                var mirrorOptions = provider
                    .GetRequiredService<IOptions<BaGetOptions>>()
                    .Value
                    .Mirror;

                mirrorOptions.EnsureValid();

                if (!mirrorOptions.Enabled)
                {
                    return new FakeMirrorService();
                }

                return new MirrorService(
                    provider.GetRequiredService<INuGetClient>(),
                    provider.GetRequiredService<IPackageCacheService>(),
                    provider.GetRequiredService<IPackageDownloader>(),
                    provider.GetRequiredService<ILogger<MirrorService>>(), 
                    mirrorOptions);
            });

            services.AddTransient<IPackageDownloader, PackageDownloader>();

            services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<BaGetOptions>>().Value;

                var client = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate),
                });

                client.Timeout = TimeSpan.FromSeconds(options.Mirror.PackageDownloadTimeoutSeconds);

                return client;
            });

            services.AddSingleton<DownloadsImporter>();

            services.AddSingleton<IPackageDownloadsSource>(provider =>
            {
                return new PackageDownloadsJsonSource(
                    new HttpClient(),
                    provider.GetRequiredService<ILogger<PackageDownloadsJsonSource>>());
            });

            return services;
        }

        public static IServiceCollection ConfigureAuthenticationProviders(this IServiceCollection services)
        {
            services.AddSingleton<IAuthenticationService, ApiKeyAuthenticationService>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<BaGetOptions>>().Value;

                return new ApiKeyAuthenticationService(options.ApiKeyHash);
            });

            return services;
        }
    }
}
