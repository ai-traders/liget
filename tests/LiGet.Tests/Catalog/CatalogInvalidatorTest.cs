using System;
using System.Collections.Generic;
using LiGet.Cache.Catalog;
using LiGet.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NuGet.CatalogReader;
using Xunit;
using Xunit.Abstractions;

namespace LiGet.Tests.Cache.Catalog
{
    public class CatalogInvalidatorTest
    {
        private XunitLoggerProvider logProvider;
        private ILogger<CatalogInvalidator> logger;
        MirrorOptions config;
        Mock<ICatalogScanStore> store;
        Mock<ICatalogReader> reader;
        
        public CatalogInvalidatorTest(ITestOutputHelper helper) {
            logProvider = new XunitLoggerProvider(helper);
            logger = logProvider.CreateLogger<CatalogInvalidator>("CatalogInvalidatorTest");
            config = new MirrorOptions();
            store = new Mock<ICatalogScanStore>(MockBehavior.Strict);
            reader = new Mock<ICatalogReader>(MockBehavior.Strict);
        }

        [Fact]
        public void ShouldNotSaveEndDateWhenHandlerFailed()
        {
            store.SetupProperty(s => s.LastScanEndDate);
            reader.Setup(r => r.GetFlattenedEntries(It.IsAny<DateTimeOffset>(),It.IsAny<DateTimeOffset>()))
                .Returns(new List<CatalogEntry>());
            using(var invalidator = new CatalogInvalidator(logger, config, store.Object, reader.Object)) {
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
            using(var invalidator = new CatalogInvalidator(logger, config, store.Object, reader.Object)) {
                invalidator.UpdatedEntry += (s,e) => {
                    // ok
                };
                invalidator.Run();
                store.VerifySet(s => s.LastScanEndDate = It.IsAny<DateTimeOffset>(), Times.AtLeastOnce());
            }
        }
    }
}