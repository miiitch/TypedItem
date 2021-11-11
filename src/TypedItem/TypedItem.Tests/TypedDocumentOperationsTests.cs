using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using NFluent;
using TypedItem.Lib;
using TypedItem.Tests.ItemModels;
using Xunit;

namespace TypedItem.Tests
{
    public class TypedDocumentOperationsTests
        : IClassFixture<CosmosDbDatabaseFixture>, IAsyncLifetime
    {
        private readonly CosmosDbDatabaseFixture _cosmosDb;
        private string _containerId;

        
        private Container Container { get; set; }

        public TypedDocumentOperationsTests(CosmosDbDatabaseFixture cosmosDb)
        {
            this._cosmosDb = cosmosDb;
        }
        
        public async Task InitializeAsync()
        {
            _containerId = _cosmosDb.GenerateId();
            var response = await _cosmosDb.Database.CreateContainerAsync(new ContainerProperties(_containerId,"/_pk"));
            Container = response.Container;
        }

        public async Task DisposeAsync()
        {
            await Container.DeleteContainerAsync();
        }
        
        
        
        [Fact]
        public async Task a_typed_item_is_created_with_type_and_deleted_fields()
        {
            
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                PartitionKey = "01"
            };

            await Container.UpsertItemAsync(personItem);

            var savedPersonResponse = await Container.ReadItemAsync<JObject>(personItem.Id, personItem.PartitionKey.AsPartitionKey());

            var savedPerson = savedPersonResponse.Resource;

            Check.That(savedPerson["_type"].ToString()).Equals(personItem.DocumentType);
            Check.That(savedPerson["_deleted"].ToObject<bool>()).Equals(personItem.Deleted);
        }
        
        [Fact]
        public async Task a_deleted_typed_item_has_this_deleted_field_to_false_and_cant_be_read()
        {
            
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                PartitionKey = "01"
            };

            await Container.UpsertItemAsync(personItem);
            await Container.SoftDeleteTypedItemAsync(personItem);

            Check.ThatAsyncCode(async () =>
                    await Container.ReadTypedItemAsync<PersonItem>(personItem.Id,
                        personItem.PartitionKey.AsPartitionKey()))
                .Throws<CosmosException>();
            
            var savedPersonResponse = await Container.ReadItemAsync<JObject>(personItem.Id, personItem.PartitionKey.AsPartitionKey());

            var savedPerson = savedPersonResponse.Resource;
            
            Check.That(savedPerson["_deleted"].ToObject<bool>()).Equals(true);
        }
    }
}
