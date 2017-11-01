using System;
using System.IO;
using DBreeze;
using LiGet.Cache.DBreeze;
using LiGet.Cache.Proxy;
using Microsoft.Extensions.Configuration;

namespace LiGet.NuGet.Server.Infrastructure
{
    public class LiGetEnvironmentConfig : IServerPackageRepositoryConfig, ICachingProxyConfig, IDBreezeConfig
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

        private int GetInteger(IConfigurationSection configurationSection, int fallBack)
        {
            if(string.IsNullOrEmpty(configurationSection.Value))
                return fallBack;
            return int.Parse(configurationSection.Value);
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

        public string CacheBackend {
            get {
                return GetString(configuration.GetSection("LIGET_NUPKG_CACHE_BACKEND"), "dbreeze");
            }
        }

        public string RootCacheDirectory {
            get {
                return GetString(configuration.GetSection("LIGET_NUPKG_CACHE_DBREEZE_ROOT_PATH"), 
                    Path.Combine(Directory.GetCurrentDirectory(), "cache", "dbreeze"));
            
            }
        }

        public DBreezeConfiguration.eStorage StorageBackend {
            get {
                string name = GetString(configuration.GetSection("LIGET_NUPKG_CACHE_DBREEZE_BACKEND"), null);
                if(name == null || name == "" || name.Contains("disk"))
                    return DBreezeConfiguration.eStorage.DISK;
                else if(name.Contains("memory"))
                    return DBreezeConfiguration.eStorage.MEMORY;
                else
                    throw new Exception("Invalid dbreeze backend name " + name);
            }
        }

        public int InvalidationCheckSeconds {
            get {
                return GetInteger(configuration.GetSection("LIGET_CACHE_INVALIDATION_CHECK_PERIOD"), 60);
            }
        }
    }
}