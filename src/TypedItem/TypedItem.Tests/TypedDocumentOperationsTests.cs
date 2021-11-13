using System;
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

        public TypedDocumentOperationsTests(CosmosDbDatabaseFixture cosmosDb)
        {
            this._cosmosDb = cosmosDb;
        }


        private Container Container { get; set; }

        public async Task InitializeAsync()
        {
            _containerId = _cosmosDb.GenerateId();
            var response = await _cosmosDb.Database.CreateContainerAsync(new ContainerProperties(_containerId, "/_pk"));
            Container = response.Container;
        }

        public async Task DisposeAsync()
        {
            await Container.DeleteContainerAsync();
        }


        [Fact]
        public async Task a_typed_item_is_created_with_type_and_can_be_read_if_good_type_is_specified()
        {
            var expectedPersonItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                PartitionKey = "01"
            };

            await Container.UpsertItemAsync(expectedPersonItem);

            var readResponse =
                await Container.ReadTypedItemAsync<PersonItem>(expectedPersonItem.Id,
                    expectedPersonItem.PartitionKey.AsPartitionKey());

            var actualPerson = readResponse.Resource;

            Check.That(actualPerson.Id).IsEqualTo(expectedPersonItem.Id);
            Check.That(actualPerson.FirstName).IsEqualTo(expectedPersonItem.FirstName);
            Check.That(actualPerson.LastName).IsEqualTo(expectedPersonItem.LastName);
            Check.That(actualPerson.PartitionKey).IsEqualTo(expectedPersonItem.PartitionKey);
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

            var savedPersonResponse =
                await Container.ReadItemAsync<JObject>(personItem.Id, personItem.PartitionKey.AsPartitionKey());

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

            var savedPersonResponse =
                await Container.ReadItemAsync<JObject>(personItem.Id, personItem.PartitionKey.AsPartitionKey());

            var savedPerson = savedPersonResponse.Resource;

            Check.That(savedPerson["_deleted"].ToObject<bool>()).Equals(true);
        }

        [Fact]
        public async Task a_deleted_typed_item_can_be_read_if_readdeleted_flag_is_set()
        {
            var actualPersonItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                PartitionKey = "01"
            };

            await Container.UpsertItemAsync(actualPersonItem);
            await Container.SoftDeleteTypedItemAsync(actualPersonItem);

            var response = await Container.ReadTypedItemAsync<PersonItem>(actualPersonItem.Id,
                actualPersonItem.PartitionKey.AsPartitionKey(),
                new TypedDocumentRequestOptions()
                {
                    ReadDeleted = true
                });

            var actualDeletedPerson = response.Resource;

            Check.That(actualDeletedPerson.Id).IsEqualTo(actualPersonItem.Id);
            Check.That(actualDeletedPerson.FirstName).IsEqualTo(actualPersonItem.FirstName);
            Check.That(actualDeletedPerson.LastName).IsEqualTo(actualPersonItem.LastName);
            Check.That(actualDeletedPerson.PartitionKey).IsEqualTo(actualPersonItem.PartitionKey);
            Check.That(actualDeletedPerson.Deleted).IsTrue();
        }


        [Fact]
        public async Task a_specific_typed_item_cannot_be_read_if_another_type_is_selected()
        {
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                PartitionKey = "01"
            };

            await Container.UpsertItemAsync(personItem);


            Check.ThatAsyncCode(async () =>
                    await Container.ReadTypedItemAsync<AddressItem>(personItem.Id,
                        personItem.PartitionKey.AsPartitionKey()))
                .Throws<CosmosException>();
        }

        [Fact]
        public async Task a_deleted_typed_item_throws_exception_after_a_second_deletion()
        {
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                PartitionKey = "01"
            };

            var response = await Container.UpsertItemAsync(personItem);

            var deletedPerson = await Container.SoftDeleteTypedItemAsync(response.Resource);

            Check.ThatAsyncCode(async () => await Container.SoftDeleteTypedItemAsync(deletedPerson.Resource))
                .Throws<ArgumentException>();
        }

        [Fact]
        public async Task cant_soft_delete_an_item_without_partition_key()
        {
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
            };
            
            Check.ThatAsyncCode(async () => await Container.SoftDeleteTypedItemAsync(personItem))
                .Throws<ArgumentException>();
        }
    }
}
