using System;
using System.IO;
using LiGet.Cache.Proxy;
using Microsoft.Extensions.Configuration;

namespace LiGet.NuGet.Server.Infrastructure
{
    public class LiGetEnvironmentConfig : IServerPackageRepositoryConfig, ICachingProxyConfig
    {
        private readonly IConfiguration configuration;
        
        public LiGetEnvironmentConfig()
            : this(new ConfigurationBuilder()
                        .AddEnvironmentVariables()
                        .Build())
        { }
        
        public LiGetEnvironmentConfig(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private bool GetBoolean(IConfigurationSection configurationSection, bool fallBack)
        {
            if(string.IsNullOrEmpty(configurationSection.Value))
                return fallBack;
            return bool.Parse(configurationSection.Value);
        }

        private string GetString(IConfigurationSection configurationSection, string fallBack)
        {
            if(string.IsNullOrEmpty(configurationSection.Value))
                return fallBack;
            return configurationSection.Value;
        }

        public bool AllowOverrideExistingPackageOnPush 
        {
            get
            {
                return GetBoolean(configuration.GetSection("LIGET_ALLOW_OVERWRITE"), false);
            }
        }

        public bool IgnoreSymbolsPackages 
        {
            get
            {
                return GetBoolean(configuration.GetSection("LIGET_IGNORE_SYMBOLS"), false);
            }
        }

        public bool EnableDelisting 
        {
            get
            {
                return GetBoolean(configuration.GetSection("LIGET_ENABLE_DELISTING"), true);
            }
        }

        public bool EnableFrameworkFiltering 
        {
            get
            {
                return GetBoolean(configuration.GetSection("LIGET_FRAMEWORK_FILTERING"), true);
            }
        }

        public bool EnableFileSystemMonitoring
        {
            get
            {
                return GetBoolean(configuration.GetSection("LIGET_FS_MONITORING"), true);
            }
        }

        public bool RunBackgroundTasks 
        {
            get
            {
                return GetBoolean(configuration.GetSection("LIGET_BACKGROUND_TASKS"), true);
            }
        }

        public string RootPath {
            get {
                return GetString(configuration.GetSection("LIGET_SIMPLE_ROOT_PATH"), 
                    Path.Combine(Directory.GetCurrentDirectory(), "simple"));
            }
        }

        public string V3NugetIndexSource {
            get {
                return GetString(configuration.GetSection("LIGET_CACHE_PROXY_SOURCE_INDEX"), 
                    "https://api.nuget.org/v3/index.json");
            }
        }
    }
}