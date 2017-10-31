using Autofac;

namespace LiGet.Cache.Proxy
{
    public class CachingProxyAutofacModule : Module
    {
        private readonly ICachingProxyConfig cacheConfig;
        public CachingProxyAutofacModule(ICachingProxyConfig cacheConfig)
        {
            this.cacheConfig = cacheConfig ?? throw new System.ArgumentNullException(nameof(cacheConfig));            
        }
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<GenericV3JsonInterceptor>().As<IV3JsonInterceptor>().SingleInstance();
            builder.RegisterType<UrlReplacementsProvider>().As<IUrlReplacementsProvider>().SingleInstance();
            builder.RegisterType<NetHttpClient>().As<IHttpClient>().SingleInstance();
            builder.RegisterInstance(cacheConfig)
                .As<ICachingProxyConfig>();
        }
    }
}