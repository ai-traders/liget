using System;
using Gelf.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BaGet.Extensions
{
    // See https://github.com/aspnet/MetaPackages/blob/master/src/Microsoft.AspNetCore/WebHost.cs
    public static class IHostBuilderExtensions
    {
        public static IHostBuilder ConfigureBaGetConfiguration(this IHostBuilder builder, string[] args)
        {
            return builder.ConfigureAppConfiguration((context, config) =>
            {               
                config
                    .SetBasePath(Environment.CurrentDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            });

        }

        public static IHostBuilder ConfigureBaGetLogging(this IHostBuilder builder)
        {
            return builder
                .ConfigureServices((hostBuilder, services) => {
                    services.Configure<GelfLoggerOptions>(hostBuilder.Configuration.GetSection("Graylog"));
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();                        
                    logging.AddGelf();
                });
        }

        public static IHostBuilder ConfigureBaGetServices(this IHostBuilder builder)
        {
            return builder
                .ConfigureServices((context, services) => services.ConfigureBaGet(context.Configuration));
        }
    }
}
