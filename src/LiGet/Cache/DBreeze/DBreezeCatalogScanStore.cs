using System;
using LiGet.Cache.Catalog;

namespace LiGet.Cache.DBreeze
{
    public class DBreezeCatalogScanStore : ICatalogScanStore
    {
        private const string ScansTableName = "scans";
        private const string LastScanKeyName = "last";
        private readonly global::DBreeze.DBreezeEngine engine;

        public DBreezeCatalogScanStore(IDBreezeEngineProvider engine)
        {
            this.engine = engine.Engine;
            using (var tx = this.engine.GetTransaction())
            {
                var found = tx.Select<string, long>(ScansTableName, LastScanKeyName);
                if (!found.Exists)
                {
                    tx.Insert<string, long>(ScansTableName, LastScanKeyName, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    tx.Commit();
                }
            }
        }
    

        public DateTimeOffset LastScanEndDate
        {
            get
            {
                using (var tx = engine.GetTransaction())
                {
                    var found = tx.Select<string, long>(ScansTableName, LastScanKeyName);
                    if (!found.Exists)
                    {
                        throw new InvalidOperationException("Last scan date does not exist in db");
                    }
                    return DateTimeOffset.FromUnixTimeMilliseconds(found.Value);
                }
            }
            set
            {
                using (var tx = engine.GetTransaction())
                {
                    tx.Insert<string, long>(ScansTableName, LastScanKeyName, value.ToUnixTimeMilliseconds());
                    tx.Commit();
                }
            }
        }
    }
}