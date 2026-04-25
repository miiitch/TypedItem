using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Testcontainers.CosmosDb;
using Xunit;
// ReSharper disable ClassNeverInstantiated.Global

namespace TypedItem.Tests
{
    // Uses Testcontainers' CosmosDbContainer with the vnext-preview emulator (HTTP-based).
    // GetConnectionString() returns http://localhost:<random_port>/ and HttpClient rewrites all
    // SDK requests to that endpoint via the built-in UriRewriter DelegatingHandler.
    public class CosmosDbDatabaseFixture : IDisposable, IAsyncLifetime
    {
        private CosmosDbContainer _cosmosDbContainer = null!;
        private CosmosClient _cosmosClient = null!;
        private string _databaseId = null!;

        public Database Database { get; private set; } = null!;

        public string GenerateId() => Guid.NewGuid().ToString("N");

        public void Dispose()
        {
        }

        public async Task InitializeAsync()
        {
            _cosmosDbContainer = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview")
                .Build();

            // The vnext-preview image takes 3–5 minutes to fully start (PostgreSQL + Citus init).
            // StartAsync waits for "Application started." in logs; no cancellation = no timeout.
            await _cosmosDbContainer.StartAsync();

            // HttpClient uses the built-in UriRewriter that rewrites every request URI to
            // http://<host>:<mapped_port>/ — required because the emulator returns internal
            // addresses in its responses that would otherwise be unreachable from the host.
            var httpClient = _cosmosDbContainer.HttpClient;

            _cosmosClient = new CosmosClient(
                _cosmosDbContainer.GetConnectionString(),
                new CosmosClientOptions
                {
                    HttpClientFactory = () => httpClient,
                    ConnectionMode = ConnectionMode.Gateway,
                });

            _databaseId = GenerateId();
            Database = await _cosmosClient.CreateDatabaseAsync(
                _databaseId,
                ThroughputProperties.CreateManualThroughput(4000));
        }

        public async Task DisposeAsync()
        {
            if (Database != null)
            {
                await Database.DeleteAsync();
            }
            _cosmosClient?.Dispose();
            await _cosmosDbContainer.DisposeAsync();
        }
    }
}
