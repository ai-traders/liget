using Autofac;
using LiGet.Cache.Proxy;
using NuGet;

namespace LiGet.NuGet.Server.Infrastructure
{
    public class NuGetServerAutofacModule : Module
    {
        private readonly IServerPackageRepositoryConfig serverConfig;
        public NuGetServerAutofacModule(IServerPackageRepositoryConfig serverConfig)
        {
            this.serverConfig = serverConfig ?? throw new System.ArgumentNullException(nameof(serverConfig));
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(serverConfig)
                .As<IServerPackageRepositoryConfig>();            
            builder.Register<ExpandedPackageRepository>(c =>
                new ExpandedPackageRepository(c.Resolve<IServerPackageRepositoryConfig>()))
                .As<ExpandedPackageRepository>()
                .SingleInstance();
            builder
                .RegisterType<ServerPackageRepository>()
                .As<IPackageService>()
                .SingleInstance();
        }
    }

}