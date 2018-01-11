using System;
using System.Collections.Generic;
using LiGet.Cache.Proxy;
using NuGet.CatalogReader;
using NuGet.Protocol.Core.Types;

namespace LiGet.Cache.Catalog
{
    public class NuGetCatalogReader : ICatalogReader, IDisposable
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(NuGetCatalogReader));

        private CatalogReader reader;

        public NuGetCatalogReader(ICachingProxyConfig config)
        {
            var indexUrl = new System.Uri(config.V3NugetIndexSource);
            var cache = new SourceCacheContext() {
                NoCache = true,
                DirectDownload = true
            };
            reader = new CatalogReader(indexUrl, null, cache, TimeSpan.Zero, new Log4NetLoggerAdapter(_log));
        }

        public void Dispose()
        {
            if(reader != null)
                reader.Dispose();
            reader.ClearCache();
        }

        public IReadOnlyList<CatalogEntry> GetFlattenedEntries(DateTimeOffset start, DateTimeOffset end)
        {
            return this.reader.GetFlattenedEntriesAsync(start,end).Result;
        }
    }
}