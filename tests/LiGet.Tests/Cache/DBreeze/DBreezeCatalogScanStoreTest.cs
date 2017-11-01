using System;
using LiGet.Cache.DBreeze;
using Moq;
using Xunit;
using static DBreeze.DBreezeConfiguration;

namespace LiGet.Tests.Cache.DBreeze
{
    public class DBreezeCatalogScanStoreTest
    {
        private DBreezeEngine engine;
        private DBreezeCatalogScanStore db;

        public DBreezeCatalogScanStoreTest() {
            Mock<IDBreezeConfig> config = new Mock<IDBreezeConfig>(MockBehavior.Loose);
            config.SetupGet(c => c.StorageBackend).Returns(eStorage.MEMORY);
            config.SetupGet(c => c.RootCacheDirectory).Returns("dummy");
            db = new DBreezeCatalogScanStore(engine = new DBreezeEngine(config.Object));
        }
        
        [Fact]
        public void LastScanDateShouldExistInNewDb() {
            Assert.True(db.LastScanEndDate <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void SetLastScanDateShouldPersistInDb() {
           var date = DateTimeOffset.FromUnixTimeMilliseconds(1000);
            db.LastScanEndDate = date;
            Assert.Equal(date, db.LastScanEndDate);
        }
    }
}