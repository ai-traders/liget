namespace LiGet.Cache.Proxy
{
    public interface ICachingProxyConfig
    {
        /// <summary>
        /// Gets the origin V3 nuget address to cache.
        /// E.g. https://api.nuget.org/v3/index.json
        /// </summary>
        /// <returns></returns>
         string V3NugetIndexSource { get; }
    }
}