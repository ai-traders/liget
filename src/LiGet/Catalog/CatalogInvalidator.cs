using System;
using System.Threading;
using LiGet.Configuration;
using Microsoft.Extensions.Logging;
using NuGet.CatalogReader;

namespace LiGet.Cache.Catalog
{
    public class CatalogInvalidator : IDisposable, ICatalogScanner
    {
        public event EventHandler<CatalogEntryEventArgs> UpdatedEntry;

        private Uri indexUrl;
        private Timer _scanTimer;
        ICatalogReader reader;
        private bool _working;
        private readonly ICatalogScanStore store;
        private readonly Microsoft.Extensions.Logging.ILogger<CatalogInvalidator> _log;

        public CatalogInvalidator(Microsoft.Extensions.Logging.ILogger<CatalogInvalidator> logger,
            CacheOptions config, ICatalogScanStore store, ICatalogReader reader)
        {
            this._log = logger;
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
                _log.LogWarning("Skipping catalog invalidation because previous run has not completed yet");
                return;
            }
            _working = true;
            try
            {
                DateTimeOffset start = store.LastScanEndDate;
                DateTimeOffset end = DateTimeOffset.UtcNow;
                _log.LogInformation("Getting all upstream changes between {0} and {1}", start, end);
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
                        _log.LogError("Failed to execute catalog entry handler", ex);
                    }
                }
                if (!anyFailed)
                    store.LastScanEndDate = end;
                _log.LogInformation("Finished handling {0} upstream changes", changes.Count);
            }
            catch (Exception ex)
            {
                _log.LogError("Catalog scan failed", ex);
            }
            finally
            {
                _working = false;
            }
        }
    }
}