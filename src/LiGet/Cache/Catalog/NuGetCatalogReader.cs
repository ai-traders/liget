using System;
using System.Collections.Generic;
using LiGet.Cache.Proxy;
using NuGet.CatalogReader;

namespace LiGet.Cache.Catalog
{
    public class NuGetCatalogReader : ICatalogReader, IDisposable
    {
        private CatalogReader reader;

        public NuGetCatalogReader(ICachingProxyConfig config)
        {
            var indexUrl = new System.Uri(config.V3NugetIndexSource);
            reader = new CatalogReader(indexUrl);
        }

        public void Dispose()
        {
            if(reader != null)
                reader.Dispose();
        }

        public IReadOnlyList<CatalogEntry> GetFlattenedEntries(DateTimeOffset start, DateTimeOffset end)
        {
            return this.reader.GetFlattenedEntriesAsync(start,end).Result;
        }
    }
}