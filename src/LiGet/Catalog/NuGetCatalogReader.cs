using System;
using System.Collections.Generic;
using LiGet.Configuration;
using Microsoft.Extensions.Logging;
using NuGet.CatalogReader;
using NuGet.Protocol.Core.Types;

namespace LiGet.Cache.Catalog
{
    public class NuGetCatalogReader : ICatalogReader, IDisposable
    {
        private CatalogReader reader;
        private readonly ILogger<NuGetCatalogReader> _log;

        public NuGetCatalogReader(ILogger<NuGetCatalogReader> logger, CacheOptions config)
        {
            this._log = logger;
            var indexUrl = config.UpstreamIndex;
            var cache = new SourceCacheContext()
            {
                NoCache = true,
                DirectDownload = true
            };
            reader = new CatalogReader(indexUrl, null, cache, TimeSpan.Zero, new NuGetLoggerAdapter<NuGetCatalogReader>(_log));
        }

        public void Dispose()
        {
            if (reader != null)
                reader.Dispose();
            reader.ClearCache();
        }

        public IReadOnlyList<CatalogEntry> GetFlattenedEntries(DateTimeOffset start, DateTimeOffset end)
        {
            return this.reader.GetFlattenedEntriesAsync(start, end).Result;
        }
    }
}