using System;
using DBreeze;
using DBreeze.Transactions;
using DBreeze.Utils;
using LiGet.Cache.Catalog;

namespace LiGet.Cache.DBreeze
{
    public class DBreezeCacheProvider : INupkgCacheProvider, IPackageMetadataCache
    {
        private const string MetadataTableName = "metadata";

        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(DBreezeCacheProvider));

        global::DBreeze.DBreezeEngine engine;

        public DBreezeCacheProvider(IDBreezeEngineProvider engine, ICatalogScanner catalogScanner)
        {
            this.engine = engine.Engine;
            catalogScanner.UpdatedEntry += InvalidateMetadata;
        }

        private string ToLowerPackage(string package) {
            return package.ToLowerInvariant();
        }

        public void Insert(string package, DateTimeOffset timestamp, byte[] content)
        {
            package = ToLowerPackage(package);
            using(var tx = engine.GetTransaction()) {
                tx.InsertPart<string, long>(MetadataTableName, package, timestamp.ToUnixTimeMilliseconds(), 0);
                tx.InsertPart<string, byte[]>(MetadataTableName, package, content, sizeof(long));
                tx.Commit();
            }
        }

        public byte[] TryGet(string package)
        {
            package = ToLowerPackage(package);
            using(var tx = engine.GetTransaction()) {
                var found = tx.Select<string, byte[]>(MetadataTableName, package);
                if(found.Exists) {
                    return found.GetValuePart(sizeof(long));
                }
                return null;
            }
        }

        public void InvalidateIfOlder(string package, DateTimeOffset timestamp)
        {
            package = ToLowerPackage(package);
            using(var tx = engine.GetTransaction()) {
                var found = tx.Select<string, long>(MetadataTableName, package);
                if(!found.Exists)
                    return;
                long unixMs = found.Value;
                if(DateTimeOffset.FromUnixTimeMilliseconds(unixMs) <= timestamp) {
                    tx.RemoveKey(MetadataTableName, package);
                    tx.Commit();
                }
            }
        }

        private void InvalidateMetadata(object sender, CatalogEntryEventArgs e)
        {
            using(var tx = engine.GetTransaction()) {
                foreach(var entry in e.Entries) {
                    string packageName = ToLowerPackage(entry.Id);
                    var found = tx.Select<string, long>(MetadataTableName, packageName);
                    if(!found.Exists)
                        continue;
                    long unixMs = found.Value;
                    if(DateTimeOffset.FromUnixTimeMilliseconds(unixMs) <= entry.CommitTimeStamp) {
                        tx.RemoveKey(MetadataTableName, packageName);
                        _log.InfoFormat("Invalidating cache for {0}", packageName);
                    }
                }
                tx.Commit();
            }
        }

        public INupkgCacheTransaction OpenTransaction() {
            return new DBreezeCacheTransaction(this.engine.GetTransaction()); 
        }

        
    }
}