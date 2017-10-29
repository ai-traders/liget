namespace LiGet.NuGet.Server.Infrastructure
{
    public class ServerPackageRepositoryConfig : IServerPackageRepositoryConfig
    {
        public ServerPackageRepositoryConfig()
        {
            this.EnableFrameworkFiltering = true;          
            this.RootPath = System.Environment.CurrentDirectory;  
            this.RunBackgroundTasks = true;
        }
        public bool AllowOverrideExistingPackageOnPush
        {
            get;set;
        }

        public bool IgnoreSymbolsPackages
        {
            get;set;
        }

        public bool EnableDelisting
        {
            get;set;
        }

        public bool EnableFrameworkFiltering
        {
            get;set;
        }

        public bool EnableFileSystemMonitoring
        {
            get;set;
        }

        public string RootPath  {
            get;set;
        }

        public bool RunBackgroundTasks  {
            get;set;
        }
    }
}