using System;
using System.Collections.Generic;
using LiGet.Configuration;
using LiGet.Entities;
using LiGet.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LiGet.Tests
{
    public class ServiceCollectionExtensionsTest
    {
        private readonly string DatabaseTypeKey = $"{nameof(LiGetOptions.Database)}:{nameof(DatabaseOptions.Type)}";
        private readonly string ConnectionStringKey = $"{nameof(LiGetOptions.Database)}:{nameof(DatabaseOptions.ConnectionString)}";

        [Fact]
        public void AskServiceProviderForNotConfiguredDatabaseOptions()
        {
            ServiceProvider provider = new ServiceCollection()
                .AddLiGetContext() //Method Under Test
                .BuildServiceProvider();

            var expected = Assert.Throws<InvalidOperationException>(
                () => provider.GetRequiredService<IContext>().Database
            );

            Assert.Contains(nameof(LiGetOptions.Database), expected.Message);
        }

        [Fact]
        public void AskServiceProviderForWellConfiguredDatabaseOptions()
        {
            //Create a IConfiguration with a minimal "Database" object.
            Dictionary<string, string> initialData = new Dictionary<string, string>();
            initialData.Add(DatabaseTypeKey, DatabaseType.Sqlite.ToString());
            initialData.Add(ConnectionStringKey, "blabla");

            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(initialData).Build();

            ServiceProvider provider = new ServiceCollection()
                .Configure<LiGetOptions>(configuration)
                .AddLiGetContext() //Method Under Test
                .BuildServiceProvider();

            Assert.NotNull(provider.GetRequiredService<IContext>());
        }

        [Fact]
        public void AskServiceProviderForWellConfiguredSqliteContext()
        {
            //Create a IConfiguration with a minimal "Database" object.
            Dictionary<string, string> initialData = new Dictionary<string, string>();
            initialData.Add(DatabaseTypeKey, DatabaseType.Sqlite.ToString());
            initialData.Add(ConnectionStringKey, "blabla");

            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(initialData).Build();

            ServiceProvider provider = new ServiceCollection()
                .Configure<LiGetOptions>(configuration)
                .AddLiGetContext() //Method Under Test
                .BuildServiceProvider();

            Assert.NotNull(provider.GetRequiredService<SqliteContext>());
        }

        [Theory]
        [InlineData("<invalid>")]
        [InlineData("")]
        [InlineData(" ")]
        public void AskServiceProviderForInvalidDatabaseType(string databaseType)
        {
            //Create a IConfiguration with a minimal "Database" object.
            Dictionary<string, string> initialData = new Dictionary<string, string>();
            initialData.Add(DatabaseTypeKey, databaseType);

            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(initialData).Build();

            ServiceProvider provider = new ServiceCollection()
                .Configure<LiGetOptions>(configuration)
                .AddLiGetContext() //Method Under Test
                .BuildServiceProvider();

            var expected = Assert.Throws<InvalidOperationException>(
                           () => provider.GetRequiredService<IContext>().Database
                       );
        }
    }
}
