using Autofac;
using LiGet.Cache.Catalog;

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
            builder.RegisterType<DBreezeCatalogScanStore>()
                .As<ICatalogScanStore>()
                .SingleInstance();
            builder.RegisterType<DBreezeEngine>()
                .As<IDBreezeEngineProvider>()
                .SingleInstance();
            builder.RegisterInstance(config).As<IDBreezeConfig>();
            builder.RegisterType<CatalogInvalidator>()
                .As<ICatalogScanner>()
                .SingleInstance();
            builder.RegisterType<NuGetCatalogReader>()
                .As<ICatalogReader>()
                .SingleInstance();
        }
    }
}