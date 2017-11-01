using System;
using DBreeze;
using DBreeze.Transactions;
using DBreeze.Utils;

namespace LiGet.Cache.DBreeze
{
    public class DBreezeCacheProvider : INupkgCacheProvider, IPackageMetadataCache, IDisposable
    {
        private const string MetadataTableName = "metadata";
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(DBreezeCacheProvider));

        DBreezeEngine engine;
        private readonly IDBreezeConfig config;

        public DBreezeCacheProvider(IDBreezeConfig config)
        {
            this.config = config;
            var dbConfig = new DBreezeConfiguration() {
                DBreezeDataFolderName = config.RootCacheDirectory,
                Storage = config.StorageBackend
            };
            engine = new DBreezeEngine(dbConfig);
        }

        public DateTimeOffset CacheCreationDate => throw new NotImplementedException();

        public void Dispose()
        {
            if (engine != null) {
                engine.Dispose();
                _log.Info("Closed dbreeze engine");
            }
        }

        public void Insert(string package, DateTimeOffset timestamp, byte[] content)
        {
            using(var tx = engine.GetTransaction()) {
                tx.InsertPart<string, long>(MetadataTableName, package, timestamp.ToUnixTimeMilliseconds(), 0);
                tx.InsertPart<string, byte[]>(MetadataTableName, package, content, sizeof(long));
                tx.Commit();
            }
        }

        public byte[] TryGet(string package)
        {
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

        public INupkgCacheTransaction OpenTransaction() {
            return new DBreezeCacheTransaction(this.engine.GetTransaction()); 
        }

        
    }
}