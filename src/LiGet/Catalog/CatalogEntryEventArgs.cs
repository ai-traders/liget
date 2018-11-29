using System;
using System.Collections.Generic;
using NuGet.CatalogReader;

namespace LiGet.Cache.Catalog
{
    public class CatalogEntryEventArgs : EventArgs
    {
        private IEnumerable<CatalogEntry> entry;

        public CatalogEntryEventArgs(IEnumerable<CatalogEntry> entry)
        {
            this.entry = entry;
        }

        public IEnumerable<CatalogEntry> Entries { get => entry;  }
    }
}