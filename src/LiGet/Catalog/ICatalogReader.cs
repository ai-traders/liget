using System;
using System.Collections.Generic;
using NuGet.CatalogReader;

namespace LiGet.Cache.Catalog
{
    public interface ICatalogReader
    {
        IReadOnlyList<CatalogEntry> GetFlattenedEntries(DateTimeOffset start, DateTimeOffset end);
    }
}