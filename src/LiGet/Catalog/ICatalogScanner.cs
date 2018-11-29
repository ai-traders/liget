using System;

namespace LiGet.Cache.Catalog
{
    public interface ICatalogScanner
    {
        event EventHandler<CatalogEntryEventArgs> UpdatedEntry;
    }
}