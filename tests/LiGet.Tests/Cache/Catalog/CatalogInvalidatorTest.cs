using System;
using System.Collections.Generic;
using LiGet.Cache.Catalog;
using LiGet.Cache.Proxy;
using LiGet.NuGet.Server.Infrastructure;
using Moq;
using NuGet.CatalogReader;
using Xunit;

namespace LiGet.Tests.Cache.Catalog
{
    public class CatalogInvalidatorTest
    {
        ICachingProxyConfig config;
        Mock<ICatalogScanStore> store;
        Mock<ICatalogReader> reader;
        
        public CatalogInvalidatorTest()
        {
            config = new LiGetEnvironmentConfig();
            store = new Mock<ICatalogScanStore>(MockBehavior.Strict);
            reader = new Mock<ICatalogReader>(MockBehavior.Strict);
        }

        [Fact]
        public void ShouldNotSaveEndDateWhenHandlerFailed()
        {
            store.SetupProperty(s => s.LastScanEndDate);
            reader.Setup(r => r.GetFlattenedEntries(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>()))
                .Returns(new List<CatalogEntry>());
            using(var invalidator = new CatalogInvalidator(config, store.Object, reader.Object)) {
                invalidator.UpdatedEntry += (s,e) => {
                    throw new Exception("test error");
                };
                invalidator.Run();
                store.VerifySet(s => s.LastScanEndDate = It.IsAny<DateTimeOffset>(), Times.Never());
            }
        }

        [Fact]
        public void ShouldSaveEndDateWhenHandlerPassed()
        {
            store.SetupProperty(s => s.LastScanEndDate);
            reader.Setup(r => r.GetFlattenedEntries(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>()))
                .Returns(new List<CatalogEntry>());
            using(var invalidator = new CatalogInvalidator(config, store.Object, reader.Object)) {
                invalidator.UpdatedEntry += (s,e) => {
                    // ok
                };
                invalidator.Run();
                store.VerifySet(s => s.LastScanEndDate = It.IsAny<DateTimeOffset>(), Times.Once());
            }
        }
    }
}