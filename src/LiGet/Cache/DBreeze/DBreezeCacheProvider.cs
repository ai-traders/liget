using System;
using DBreeze;
using DBreeze.Transactions;

namespace LiGet.Cache.DBreeze
{
    public class DBreezeCacheProvider : INupkgCacheProvider, IDisposable
    {
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

        public void Dispose()
        {
            if (engine != null) {
                engine.Dispose();
                _log.Info("Closed dbreeze engine");
            }
        }

        public ICacheTransaction OpenTransaction() {
            return new DBreezeCacheTransaction(this.engine.GetTransaction()); 
        }
    }
}