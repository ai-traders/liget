using System;
using DBreeze;

namespace LiGet.Cache.DBreeze
{
    public class DBreezeEngine : IDBreezeEngineProvider, IDisposable
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(DBreezeEngine));
        
        global::DBreeze.DBreezeEngine engine;
        private readonly IDBreezeConfig config;

        public global::DBreeze.DBreezeEngine Engine => engine;

        public DBreezeEngine(IDBreezeConfig config)
        {
            this.config = config;
            var dbConfig = new DBreezeConfiguration() {
                DBreezeDataFolderName = config.RootCacheDirectory,
                Storage = config.StorageBackend
            };
            engine = new global::DBreeze.DBreezeEngine(dbConfig);
        }


        public void Dispose()
        {
            if (engine != null) {
                engine.Dispose();
                _log.Info("Closed dbreeze engine");
            }
        }

    }
}