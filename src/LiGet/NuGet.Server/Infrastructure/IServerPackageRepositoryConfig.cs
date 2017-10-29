namespace LiGet.NuGet.Server.Infrastructure
{
    public interface IServerPackageRepositoryConfig
    {
        bool AllowOverrideExistingPackageOnPush
        {
            get;
        }

        bool IgnoreSymbolsPackages
        {
            get;
        }

        bool EnableDelisting
        {
            get;
        }

        bool EnableFrameworkFiltering
        {
            get;
        }

        bool EnableFileSystemMonitoring
        {
            get;
        }
        string RootPath { get; }
        bool RunBackgroundTasks { get; }
    }
}