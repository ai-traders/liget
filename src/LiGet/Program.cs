using System;
using LiGet.Configuration;
using LiGet.Entities;
using LiGet.Cache;
using LiGet.Services;
using LiGet.Extensions;
using Gelf.Extensions.Logging;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace LiGet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "liget",
                Description = "A light-weight NuGet service",
            };

            app.HelpOption(inherited: true);

            app.Command("import", import =>
            {
                import.Command("downloads", downloads =>
                {
                    downloads.OnExecute(async () =>
                    {
                        var provider = CreateHostBuilder(args).Build().Services;

                        await provider
                            .GetRequiredService<DownloadsImporter>()
                            .ImportAsync();
                    });
                });

                var optionImportPath = import.Option("-p|--path <directory>", "Directory with .nupkg files at any depth", CommandOptionType.SingleValue);
                import.OnExecute(async () => {
                    var importDirectory = optionImportPath.HasValue()
                        ? optionImportPath.Value()
                        : ".";

                    Console.WriteLine($"Importing packages from {importDirectory}");
                    var provider = CreateHostBuilder(args).Build().Services;

                    var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
                    // Run migrations if enabled
                    var databaseOptions = provider.GetRequiredService<IOptions<LiGetOptions>>()
                            .Value
                            .Database;
                    if(databaseOptions.RunMigrations) {
                        using (var scope = scopeFactory.CreateScope())
                        {
                            scope.ServiceProvider
                                .GetRequiredService<IContext>()
                                .Database
                                .Migrate();
                        }
                    }

                    await provider
                        .GetRequiredService<PackageImporter>()
                        .ImportAsync(importDirectory, Console.Out);
                    return 0;
                });
            });

            app.OnExecute(() =>
            {
                CreateWebHostBuilder(args).Build().Run();
            });

            app.Execute(args);
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) {
            var b = WebHost.CreateDefaultBuilder(args);
            string threadCount = Environment.GetEnvironmentVariable("LIGET_LIBUV_THREAD_COUNT");
            if(!string.IsNullOrEmpty(threadCount))
                b.UseLibuv(opts => opts.ThreadCount = int.Parse(threadCount));

            b.UseStartup<Startup>()
                .ConfigureLogging((context, builder) =>
                {
                    var graylogSection = context.Configuration.GetSection("Graylog");
                    // Read GelfLoggerOptions from appsettings.json
                    if(graylogSection.GetChildren().Any())
                        builder.Services.Configure<GelfLoggerOptions>(graylogSection);

                    // Read Logging settings from appsettings.json and add providers.
                    var cfg = builder.AddConfiguration(context.Configuration.GetSection("Logging"))
                        .AddConsole()
                        .AddDebug();
                    if(graylogSection.GetChildren().Any())
                        cfg.AddGelf();
                })
                .UseUrls("http://0.0.0.0:9011")
                .UseKestrel(options =>
                {
                    // Remove the upload limit from Kestrel. If needed, an upload limit can
                    // be enforced by a reverse proxy server, like IIS.
                    options.Limits.MaxRequestBodySize = null;
                });
            return b;
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return new HostBuilder()
                .ConfigureLiGetConfiguration(args)
                .ConfigureLiGetServices()
                .ConfigureLiGetLogging();
        }
    }
}
