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
    public class TypedItemOperationsSinglePkTests
        : IClassFixture<CosmosDbDatabaseFixture>, IAsyncLifetime
    {
        private readonly CosmosDbDatabaseFixture _cosmosDb;
        private string _containerId;

        private Container Container { get; set; }

        public async Task InitializeAsync()
        {
            _containerId = _cosmosDb.GenerateId();
            var response = await _cosmosDb.Database.CreateContainerAsync(new ContainerProperties(_containerId, "/part"));
            Container = response.Container;
        }

        public async Task DisposeAsync()
        {
            await Container.DeleteContainerAsync();
        }
        
        public TypedItemOperationsSinglePkTests(CosmosDbDatabaseFixture cosmosDb)
        {
            this._cosmosDb = cosmosDb;
        }

        private async Task<List<EventItem>> FillWithEvents()
        {
            var result = new List<EventItem>();
            var phoneCall = new PhonecallItem()
            {
                Id = TypedItemHelper<PhonecallItem>.GenerateId(),
                Date = DateTime.Now,
                Duration = 40,
            };
            result.Add(phoneCall);
            await Container.CreateTypedItemAsync(phoneCall);

            phoneCall = new PhonecallItem()
            {
                Id = TypedItemHelper<PhonecallItem>.GenerateId(),
                Date = DateTime.Now-TimeSpan.FromDays(2),
                Duration = 4,
            };
            result.Add(phoneCall);
            await Container.CreateTypedItemAsync(phoneCall);

            var teamsMeeting = new TeamsMeeting()
            {
                Id = TypedItemHelper<TeamsMeeting>.GenerateId(),
                Date = DateTime.Now,
                Peoples = new[] { "John" }
            };
            result.Add(teamsMeeting);
            await Container.CreateTypedItemAsync(teamsMeeting);
            
            teamsMeeting = new TeamsMeeting()
            {
                Id = TypedItemHelper<TeamsMeeting>.GenerateId(),
                Date = DateTime.Now-TimeSpan.FromDays(3),
                Peoples = new[] { "Jack" }
            };
            result.Add(teamsMeeting);
            await Container.CreateTypedItemAsync(teamsMeeting);

            return result;

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
                    Part = $"P{(i / 100)}",
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
                
                await Container.CreateTypedItemAsync(item);
            }

            return (items, nonDeletedCount, deleteCount);
        }




        
        [Fact]
        public async Task a_typed_item_is_created_with_type_and_can_be_read_and_the_good_type_is_specified()
        {
            var expectedPersonItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                Part = "01"
            };

            await Container.CreateTypedItemAsync(expectedPersonItem);
            Check.That(expectedPersonItem.ItemType).IsEqualTo("person");

            var readResponse =
                await Container.ReadTypedItemAsync<PersonItem>(expectedPersonItem.Id,
                    expectedPersonItem.GetPartitionKey());

            var actualPerson = readResponse.Resource;

            Check.That(actualPerson.Id).IsEqualTo(expectedPersonItem.Id);
            Check.That(actualPerson.FirstName).IsEqualTo(expectedPersonItem.FirstName);
            Check.That(actualPerson.LastName).IsEqualTo(expectedPersonItem.LastName);
            Check.That(actualPerson.Part).IsEqualTo(expectedPersonItem.Part);
            Check.That(actualPerson.ItemType).IsEqualTo("person");
        }
        
        [Fact]
        public async Task a_typed_item_is_created_in_a_transactional_batch_with_type_and_can_be_read_and_the_good_type_is_specified()
        {
            var expectedPersonItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                Part = "01"
            };

            var batch = Container.CreateTransactionalBatch(new PartitionKey("01"));
            batch.CreateTypedItem(expectedPersonItem);
            await batch.ExecuteAsync();
            Check.That(expectedPersonItem.ItemType).IsEqualTo("person");
            var readResponse =
                await Container.ReadTypedItemAsync<PersonItem>(expectedPersonItem.Id,
                    expectedPersonItem.GetPartitionKey());

            var actualPerson = readResponse.Resource;

            Check.That(actualPerson.Id).IsEqualTo(expectedPersonItem.Id);
            Check.That(actualPerson.FirstName).IsEqualTo(expectedPersonItem.FirstName);
            Check.That(actualPerson.LastName).IsEqualTo(expectedPersonItem.LastName);
            Check.That(actualPerson.Part).IsEqualTo(expectedPersonItem.Part);
            Check.That(actualPerson.ItemType).IsEqualTo("person");
        }
        
        [Fact]
        public async Task a_typed_item_is_created_with_type_and_can_be_replaced()
        {
            var expectedPersonItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                Part = "01"
            };

            var createResult = await Container.CreateTypedItemAsync(expectedPersonItem);

            var createdPerson = createResult.Resource!;
            createdPerson.LastName = "Doee";
            var readResponse =
                await Container.ReplaceTypedItemAsync<PersonItem>(createResult,createdPerson.Id!,
                    createdPerson.GetPartitionKey());

            var actualPerson = readResponse.Resource;

            Check.That(actualPerson.Id).IsEqualTo(createdPerson.Id);
            Check.That(actualPerson.FirstName).IsEqualTo(createdPerson.FirstName);
            Check.That(actualPerson.LastName).IsEqualTo(createdPerson.LastName);
            Check.That(actualPerson.Part).IsEqualTo(expectedPersonItem.Part);
            Check.That(actualPerson.ItemType).IsEqualTo("person");
        }
        
        [Fact]
        public async Task a_typed_item_is_created_with_type_and_can_be_replaced_by_batch()
        {
            var expectedPersonItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                Part = "01"
            };

            var createResult = await Container.CreateTypedItemAsync(expectedPersonItem);

            var createdPerson = createResult.Resource!;
            createdPerson.LastName = "Doee";

            var batch = Container.CreateTransactionalBatch(new PartitionKey("01"));
            batch.ReplaceTypedItem(createdPerson);
            var response = await batch.ExecuteAsync();
            
            var actualPerson = response.GetOperationResultAtIndex<PersonItem>(0).Resource;

            Check.That(actualPerson.Id).IsEqualTo(createdPerson.Id);
            Check.That(actualPerson.FirstName).IsEqualTo(createdPerson.FirstName);
            Check.That(actualPerson.LastName).IsEqualTo(createdPerson.LastName);
            Check.That(actualPerson.Part).IsEqualTo(expectedPersonItem.Part);
            Check.That(actualPerson.ItemType).IsEqualTo("person");
        }

        [Fact]
        public async Task a_typed_item_cannot_be_created_if_the_specified_type_is_wrong()
        {
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                Part = "01",
                ItemType = "foo"
            };

            Check.ThatAsyncCode(async () => await Container.UpsertTypedItemAsync(personItem)).ThrowsAny();
        }
        
        [Fact]
        public void a_typed_item_cannot_be_created_by_batch_if_the_specified_type_is_wrong()
        {
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                Part = "01",
                ItemType = "foo"
            };

            var batch = Container.CreateTransactionalBatch(new PartitionKey("01"));

            Check.ThatCode(() => batch.UpsertTypedItem(personItem)).ThrowsAny();
        }

        [Fact]
        public async Task a_typed_item_is_upserted_with_type_and_deleted_fields()
        {
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                Part = "01"
            };

            await Container.UpsertTypedItemAsync(personItem);

            var savedPersonResponse =
                await Container.ReadItemAsync<JObject>(personItem.Id, personItem.GetPartitionKey());

            var savedPerson = savedPersonResponse.Resource;

            Check.That(savedPerson["_type"].ToString()).Equals(personItem.ItemType);
            Check.That(savedPerson["_deleted"].ToObject<bool>()).Equals(personItem.Deleted);
            Check.That(savedPerson["_type"].ToString()).IsEqualTo("person");
        }
        
        [Fact]
        public async Task a_typed_item_is_upserted_in_a_batch_with_type_and_deleted_fields()
        {
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                Part = "01"
            };

            var batch = Container.CreateTransactionalBatch(new PartitionKey("01"));
            batch.UpsertTypedItem(personItem);

            await batch.ExecuteAsync();

            var savedPersonResponse =
                await Container.ReadItemAsync<JObject>(personItem.Id, personItem.GetPartitionKey());

            var savedPerson = savedPersonResponse.Resource;

            Check.That(savedPerson["_type"].ToString()).Equals(personItem.ItemType);
            Check.That(savedPerson["_deleted"].ToObject<bool>()).Equals(personItem.Deleted);
            Check.That(savedPerson["_type"].ToObject<string>()).IsEqualTo("person");
        }

        [Fact]
        public async Task a_deleted_typed_item_has_this_deleted_field_to_false_and_cant_be_read()
        {
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                Part = "01"
            };

            await Container.UpsertTypedItemAsync(personItem);
            await Container.SoftDeleteTypedItemAsync(personItem);

            Check.ThatAsyncCode(async () =>
                    await Container.ReadTypedItemAsync<PersonItem>(personItem.Id,
                        personItem.GetPartitionKey()))
                .Throws<CosmosException>();

            var savedPersonResponse =
                await Container.ReadItemAsync<JObject>(personItem.Id, personItem.GetPartitionKey());

            var savedPerson = savedPersonResponse.Resource;

            Check.That(savedPerson["_deleted"].ToObject<bool>()).Equals(true);
        }
        
        [Fact]
        public async Task a_deleted_by_batch_typed_item_has_this_deleted_field_to_false_and_cant_be_read()
        {
            var personItem = new PersonItem()
            {
                Id = _cosmosDb.GenerateId(),
                FirstName = "John",
                LastName = "Doe",
                Part = "01"
            };

            await Container.UpsertTypedItemAsync(personItem);

            var batch = Container.CreateTransactionalBatch(new PartitionKey("01"));
            batch.SoftDeleteTypedItem(personItem.Id);

            await batch.ExecuteAsync();
            
            Check.ThatAsyncCode(async () =>
                    await Container.ReadTypedItemAsync<PersonItem>(personItem.Id,
                        personItem.GetPartitionKey()))
                .Throws<CosmosException>();

            var savedPersonResponse =
                await Container.ReadItemAsync<JObject>(personItem.Id, personItem.GetPartitionKey());

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
                Part = "01"
            };

            await Container.UpsertTypedItemAsync(personItem);
            await Container.SoftDeleteTypedItemAsync<PersonItem>(
                id: personItem.Id, 
                partitionKey: personItem.GetPartitionKey(),
                requestOptions: new ItemRequestOptions());

            Check.ThatAsyncCode(async () =>
                    await Container.ReadTypedItemAsync<PersonItem>(personItem.Id,
                        personItem.GetPartitionKey()))
                .Throws<CosmosException>();

            var savedPersonResponse =
                await Container.ReadItemAsync<JObject>(personItem.Id, personItem.GetPartitionKey());

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
                Part = "01"
            };

            await Container.UpsertTypedItemAsync(actualPersonItem);
            await Container.SoftDeleteTypedItemAsync(actualPersonItem);

            var response = await Container.ReadTypedItemAsync<PersonItem>(actualPersonItem.Id,
                actualPersonItem.GetPartitionKey(),
                new TypedItemRequestOptions()
                {
                    ReadDeleted = true
                });

            var actualDeletedPerson = response.Resource;

            Check.That(actualDeletedPerson.Id).IsEqualTo(actualPersonItem.Id);
            Check.That(actualDeletedPerson.FirstName).IsEqualTo(actualPersonItem.FirstName);
            Check.That(actualDeletedPerson.LastName).IsEqualTo(actualPersonItem.LastName);
            Check.That(actualDeletedPerson.Part).IsEqualTo(actualPersonItem.Part);
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
                Part = "01"
            };

            await Container.UpsertTypedItemAsync(personItem);


            Check.ThatAsyncCode(async () =>
                    await Container.ReadTypedItemAsync<AddressItem>(personItem.Id,
                        personItem.GetPartitionKey()))
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
                Part = "01"
            };

            var response = await Container.UpsertTypedItemAsync(personItem);

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
                Part = "JK",
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
            Check.ThatAsyncCode(async () => await Container.SoftDeleteTypedItemAsync<PersonItem>("toto",new PartitionKey("titi")))
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
                MaxItemCount = 10,
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
            Check.That(result.Results.All(_ => _.Part == pk));
            
            // ConsistencyLevel
            options = new QueryTypedItemsOptions()
            {
                ConsistencyLevel = ConsistencyLevel.Eventual
            };
            result = await Container.QueryTypedItemAsync<PersonItem, PersonItem>(p => p.OrderBy(_ => _.Index) ,options);
            Check.That(result.Results.All(_ => _.Part == pk));
        }


        [Fact]
        public async Task query_on_root_class_returns_all_the_items_attached_to_the_subclass_types()
        {
            var events = await FillWithEvents();

            var queryResult =
                await Container.QueryTypedItemAsync<EventItem, EventItem>(e => e,
                    new QueryTypedItemsOptions() { ReadAllPages = true });

            Check.That(queryResult.Count).IsEqualTo(events.Count);
            foreach (var e in queryResult.Results)
            {
                Check.That(e.ItemType).IsOneOf(TypedItemHelper<PhonecallItem>.ItemType,
                    TypedItemHelper<TeamsMeeting>.ItemType);
            }
        }
        
        [Fact]
        public async Task query_on_a_class_returns_all_the_items_of_the_specified_class()
        {
            var events = await FillWithEvents();

            var queryResult =
                await Container.QueryTypedItemAsync<PhonecallItem, PhonecallItem>(e => e,
                    new QueryTypedItemsOptions() { ReadAllPages = true });

            var phoneCallType = TypedItemHelper<PhonecallItem>.ItemType;
            Check.That(queryResult.Count).IsEqualTo(events.Count(_ => _.ItemType == phoneCallType));
            foreach (var e in queryResult.Results)
            {
                Check.That(e.ItemType).IsEqualTo(TypedItemHelper<PhonecallItem>.ItemType);
            }
        }
    }
}
