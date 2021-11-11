using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Xunit;
// ReSharper disable ClassNeverInstantiated.Global

namespace TypedItem.Tests
{
    public class CosmosDbDatabaseFixture : IDisposable, IAsyncLifetime
    {
        private CosmosClient _cosmosClient;
        private string _databaseId;

        public Database Database { get; set; }

        public string GenerateId() => Guid.NewGuid().ToString("N");
       
        
        public void Dispose()
        {
            
        }

        public async Task InitializeAsync()
        {
            var emulatorConnectionString =
                "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

            _cosmosClient = new CosmosClient(emulatorConnectionString);
            _databaseId = GenerateId();
            Database = await _cosmosClient.CreateDatabaseAsync(
                _databaseId,
                ThroughputProperties.CreateManualThroughput(4000));

        }

        public async Task DisposeAsync()
        {
            await Database.DeleteAsync();
            _cosmosClient.Dispose();
        }
    }
}
