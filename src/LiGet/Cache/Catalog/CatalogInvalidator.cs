using System;
using System.Threading;
using LiGet.Cache.Proxy;
using NuGet.CatalogReader;

namespace LiGet.Cache.Catalog
{
    public class CatalogInvalidator : IDisposable, ICatalogScanner
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(CatalogInvalidator));

        public event EventHandler<CatalogEntryEventArgs> UpdatedEntry;

        private Uri indexUrl;
        private Timer _scanTimer;
        ICatalogReader reader;
        private bool _working;
        private readonly ICatalogScanStore store;

        public CatalogInvalidator(
            ICachingProxyConfig config, ICatalogScanStore store, ICatalogReader reader)
        {
            this.store = store;
            var period = TimeSpan.FromSeconds(config.InvalidationCheckSeconds);
            _scanTimer = new Timer(state => Run(), null, period, period);
            this.reader = reader;
        }

        public void Dispose()
        {
            if (_scanTimer != null)
                _scanTimer.Dispose();
        }

        public void Run()
        {
            if (_working)
            {
                _log.Warn("Skipping catalog invalidation because previous run has not completed yet");
                return;
            }
            _working = true;
            try
            {
                DateTimeOffset start = store.LastScanEndDate;
                DateTimeOffset end = DateTimeOffset.UtcNow;
                _log.InfoFormat("Getting all upstream changes between {0} and {1}", start, end);
                var changes = reader.GetFlattenedEntries(start, end);
                bool anyFailed = false;
                foreach (var h in this.UpdatedEntry.GetInvocationList())
                {
                    try
                    {
                        var del = (EventHandler<CatalogEntryEventArgs>)h;
                        del(this, new CatalogEntryEventArgs(changes));
                    }
                    catch (Exception ex)
                    {
                        anyFailed = true;
                        _log.Error("Failed to execute catalog entry handler", ex);
                    }
                }                
                if(!anyFailed)
                    store.LastScanEndDate = end;
                _log.InfoFormat("Finished handling {0} upstream changes", changes.Count);
            }
            catch(Exception ex) {
                _log.Error("Catalog scan failed",ex);
            }
            finally
            {
                _working = false;
            }
        }
    }
}