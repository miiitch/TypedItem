using System;
using System.Collections.Generic;
using System.Linq;
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

        private async Task<(List<PersonItem> items, int nonDeletedCount, int deletecount)> FillContainer(int count)
        {
            var firstNames = new[]
                { "Alice", "Bob", "Candice", "Daniel", "Eric", "Frances", "Georges", "Helena","Igor","Katia","Lionel","Mimi","Nicolas" };
            var lastNames = new[]
            {
                "A", "B", "C", "D", "E", "F", "G", "H","I","J","K","L","M","N","O","P","Q"
            };

            var now = DateTime.UtcNow;
            var rnd = new Random();
            var items = new List<PersonItem>();
            var (nonDeletedCount, deleteCount) = (0, 0);
            for (var i = 0; i < count; i++)
            {
                var birthdate = now - TimeSpan.FromDays(365 * 10 + rnd.Next(365 * 50));
                var item = new PersonItem()
                {
                    Id = TypedItemHelper<PersonItem>.GenerateId(),
                    FirstName = firstNames[rnd.Next(firstNames.Length)],
                    LastName = lastNames[rnd.Next(lastNames.Length)],
                    BirthDate = birthdate,
                    PartitionKey = $"P{(i / 100)}",
                    Deleted = i%10 == 0,
                    Index = i
                };
                if (item.Deleted)
                {
                    deleteCount++;
                }
                else
                {
                    nonDeletedCount++;
                }
                items.Add(item);
                
                await Container.UpsertItemAsync(item);
            }

            return (items, nonDeletedCount, deleteCount);
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
        public async Task a_deleted_typed_item_from_id_has_this_deleted_field_to_false_and_cant_be_read()
        {
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                PartitionKey = "01"
            };

            await Container.UpsertItemAsync(personItem);
            await Container.SoftDeleteTypedItemAsync<PersonItem>(
                id: personItem.Id, 
                partitionKey: personItem.PartitionKey.AsPartitionKey(),
                requestOptions: new ItemRequestOptions());

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
        public void cant_soft_delete_an_item_without_partition_key()
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
        
        [Fact]
        public Task cant_soft_delete_an_item_without_id()
        {
            var personItem = new PersonItem()
            {
                Id = null,
                PartitionKey = "JK",
                FirstName = "John",
                LastName = "Doe",
            };
            
            Check.ThatAsyncCode(async () => await Container.SoftDeleteTypedItemAsync(personItem))
                .Throws<ArgumentException>();

            return Task.CompletedTask;
        }
        
        [Fact]
        public Task cant_soft_delete_an_unknown_item()
        {
            // ReSharper disable once StringLiteralTypo
            Check.ThatAsyncCode(async () => await Container.SoftDeleteTypedItemAsync<PersonItem>("toto","titi".AsPartitionKey()))
                .Throws<CosmosException>();

            return Task.CompletedTask;
        }

        [Fact]
        public async  Task query_all_a_subset_of_items()
        {
            var  (items, nonDeletedCount, _) = await FillContainer(1000);

            var options = new QueryTypedItemsOptions()
            {
                MaxItemCount = 100,
                IncludeDeletedItems = false
            };
            var result = await Container.QueryTypedItemAsync<PersonItem, PersonItem>(p => p,options);

            Check.That(result.Count).IsEqualTo(options.MaxItemCount);
            Check.That(result.RequestCharge).IsStrictlyGreaterThan(0);
            Check.That(result.ContinuationToken).Not.IsNullOrWhiteSpace();
            foreach (var p in result.Results)
            {
                Check.That(p.Deleted).IsFalse();
            }
            
            options = new QueryTypedItemsOptions()
            {
                MaxItemCount = 100,
                IncludeDeletedItems = true,
                MaxConcurrency = 0
            };
            result = await Container.QueryTypedItemAsync<PersonItem, PersonItem>(p => p,options);
            Check.That(result.Results.Any(p => p.Deleted)).IsTrue();
            Check.That(result.Results.Count).IsEqualTo(options.MaxItemCount);
            

            options = new QueryTypedItemsOptions()
            {
                ReadAllPages = true
            };
            result = await Container.QueryTypedItemAsync<PersonItem, PersonItem>(p => p,options);
            Check.That(result.Results.All(p => !p.Deleted)).IsTrue();
            Check.That(result.Results.Count).IsEqualTo(nonDeletedCount);
            Check.That(result.ContinuationToken).IsNull();
            
            options = new QueryTypedItemsOptions()
            {
                ReadAllPages = true,
                IncludeDeletedItems = true,
            };
            result = await Container.QueryTypedItemAsync<PersonItem, PersonItem>(p => p,options);
            Check.That(result.Results.Any(p => p.Deleted)).IsTrue();
            Check.That(result.Results.Count).IsEqualTo(items.Count);
            Check.That(result.ContinuationToken).IsNull();

            
            
            // Continuation token test
            options = new QueryTypedItemsOptions()
            {
                ReadAllPages = false,
                MaxItemCount = 10
            };
            
            result = await Container.QueryTypedItemAsync<PersonItem, PersonItem>(p => p.OrderBy(_ => _.Index) ,options);
            var maxIndex = result.Results.Max(_ => _.Index);

            options.ContinuationToken = result.ContinuationToken;
            options.SessionToken = result.SessionToken;
            result = await Container.QueryTypedItemAsync<PersonItem, PersonItem>(p => p.OrderBy(_ => _.Index) ,options);
            
            var minIndex = result.Results.Min(_ => _.Index);
            Check.That(maxIndex).IsStrictlyLessThan(minIndex);

            
            // Partition Key
            var pk = "P0";
            options = new QueryTypedItemsOptions()
            {
                ReadAllPages = false,
                PartitionKey = new PartitionKey(pk)
            };
            result = await Container.QueryTypedItemAsync<PersonItem, PersonItem>(p => p.OrderBy(_ => _.Index) ,options);
            Check.That(result.Results.All(_ => _.PartitionKey == pk));
            
            // ConsistencyLevel
            options = new QueryTypedItemsOptions()
            {
                ConsistencyLevel = ConsistencyLevel.Eventual
            };
            result = await Container.QueryTypedItemAsync<PersonItem, PersonItem>(p => p.OrderBy(_ => _.Index) ,options);
            Check.That(result.Results.All(_ => _.PartitionKey == pk));
        }
    }
}
