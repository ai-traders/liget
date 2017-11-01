using System;

namespace LiGet.Cache.Catalog
{
    public interface ICatalogScanStore
    {
        DateTimeOffset LastScanEndDate { get; set; }
    }
}