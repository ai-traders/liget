using Autofac;

namespace LiGet.Cache.DBreeze
{
    public class DBreezeCacheAutofacModule : Module
    {
        private readonly IDBreezeConfig config;
        public DBreezeCacheAutofacModule(IDBreezeConfig config)
        {
            this.config = config;
        }

        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<DBreezeCacheProvider>()
                .As<INupkgCacheProvider>()
                .As<IPackageMetadataCache>()
                .SingleInstance();
            builder.RegisterInstance(config).As<IDBreezeConfig>();
        }
    }
}