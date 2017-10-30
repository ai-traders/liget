using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace LiGet.NuGet.Server.Infrastructure
{
    public class ServerPackageRepositoryEnvironmentConfig : IServerPackageRepositoryConfig
    {
        private readonly IConfiguration configuration;
        
        public ServerPackageRepositoryEnvironmentConfig()
            : this(new ConfigurationBuilder()
                        .AddEnvironmentVariables()
                        .Build())
        { }
        
        public ServerPackageRepositoryEnvironmentConfig(IConfiguration configuration)
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

        
    }
}