using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using NFluent;
using TypedItem.Lib;
using TypedItem.Tests.ItemModels;
using Xunit;
using System.Linq;

namespace TypedItem.Tests;

public class TypedItemOperationsHPkTests
    : IClassFixture<CosmosDbDatabaseFixture>, IAsyncLifetime
{
    private readonly CosmosDbDatabaseFixture _cosmosDb;
    private string _containerId;

    private Container Container { get; set; }

    public async Task InitializeAsync()
    {
        _containerId = _cosmosDb.GenerateId();
        var response =
            await _cosmosDb.Database.CreateContainerAsync(new ContainerProperties(_containerId,
                new List<string> { "/part", "/subPart" }.AsReadOnly()));
        Container = response.Container;
    }

    public async Task DisposeAsync()
    {
        await Container.DeleteContainerAsync();
    }

    public TypedItemOperationsHPkTests(CosmosDbDatabaseFixture cosmosDb)
    {
        this._cosmosDb = cosmosDb;
    }

    [Fact]
    public async Task a_typed_item_is_created_with_type_and_can_be_read_and_the_good_type_is_specified()
    {
        var expectedPersonItem = new HCPersonItem()
        {
            Id = _cosmosDb.GenerateId(),
            FirstName = "John",
            LastName = "Doe",
            Part = "01",
            SubPart = "AA"
        };

        await Container.CreateTypedItemAsync(expectedPersonItem);
        Check.That(expectedPersonItem.ItemType).IsEqualTo("person");

        var readResponse =
            await Container.ReadTypedItemAsync<HCPersonItem>(expectedPersonItem.Id,
                expectedPersonItem.GetPartitionKey());

        var actualPerson = readResponse.Resource;

        Check.That(actualPerson.Id).IsEqualTo(expectedPersonItem.Id);
        Check.That(actualPerson.FirstName).IsEqualTo(expectedPersonItem.FirstName);
        Check.That(actualPerson.LastName).IsEqualTo(expectedPersonItem.LastName);
        Check.That(actualPerson.Part).IsEqualTo(expectedPersonItem.Part);
        Check.That(actualPerson.SubPart).IsEqualTo(expectedPersonItem.SubPart);
        Check.That(actualPerson.ItemType).IsEqualTo("person");
    }
        
    [Fact]
    public async Task a_typed_item_is_created_in_a_transactional_batch_with_type_and_can_be_read_and_the_good_type_is_specified()
    {
        var expectedPersonItem = new HCPersonItem()
        {
            Id = _cosmosDb.GenerateId(),
            FirstName = "John",
            LastName = "Doe",
            Part = "01",
            SubPart = "AA"
                
        };

        var batch = Container.CreateTransactionalBatch(expectedPersonItem.GetPartitionKey());
        batch.CreateTypedItem(expectedPersonItem);
        await batch.ExecuteAsync();
        Check.That(expectedPersonItem.ItemType).IsEqualTo("person");
        var readResponse =
            await Container.ReadTypedItemAsync<HCPersonItem>(expectedPersonItem.Id,
                expectedPersonItem.GetPartitionKey());

        var actualPerson = readResponse.Resource;

        Check.That(actualPerson.Id).IsEqualTo(expectedPersonItem.Id);
        Check.That(actualPerson.FirstName).IsEqualTo(expectedPersonItem.FirstName);
        Check.That(actualPerson.LastName).IsEqualTo(expectedPersonItem.LastName);
        Check.That(actualPerson.Part).IsEqualTo(expectedPersonItem.Part);
        Check.That(actualPerson.SubPart).IsEqualTo(expectedPersonItem.SubPart);
        Check.That(actualPerson.ItemType).IsEqualTo("person");
    }
        
    [Fact]
    public async Task a_typed_item_is_created_with_type_and_can_be_replaced()
    {
        var expectedPersonItem = new HCPersonItem()
        {
            Id = _cosmosDb.GenerateId(),
            FirstName = "John",
            LastName = "Doe",
            Part = "01",
            SubPart = "AA"
        };

        var createResult = await Container.CreateTypedItemAsync(expectedPersonItem);

        var createdPerson = createResult.Resource!;
        createdPerson.LastName = "Doee";
        var readResponse =
            await Container.ReplaceTypedItemAsync<HCPersonItem>(createResult,createdPerson.Id!,
                createdPerson.GetPartitionKey());

        var actualPerson = readResponse.Resource;

        Check.That(actualPerson.Id).IsEqualTo(createdPerson.Id);
        Check.That(actualPerson.FirstName).IsEqualTo(createdPerson.FirstName);
        Check.That(actualPerson.LastName).IsEqualTo(createdPerson.LastName);
        Check.That(actualPerson.Part).IsEqualTo(expectedPersonItem.Part);
        Check.That(actualPerson.SubPart).IsEqualTo(expectedPersonItem.SubPart);
        Check.That(actualPerson.ItemType).IsEqualTo("person");
    }
        
    [Fact]
    public async Task a_typed_item_is_created_with_type_and_can_be_replaced_by_batch()
    {
        var expectedPersonItem = new HCPersonItem()
        {
            Id = _cosmosDb.GenerateId(),
            FirstName = "John",
            LastName = "Doe",
            Part = "01",
            SubPart = "AA"
        };

        var createResult = await Container.CreateTypedItemAsync(expectedPersonItem);

        var createdPerson = createResult.Resource!;
        createdPerson.LastName = "Doee";

        var batch = Container.CreateTransactionalBatch(expectedPersonItem.GetPartitionKey());
        batch.ReplaceTypedItem(createdPerson);
        var response = await batch.ExecuteAsync();
            
        var actualPerson = response.GetOperationResultAtIndex<HCPersonItem>(0).Resource;

        Check.That(actualPerson.Id).IsEqualTo(createdPerson.Id);
        Check.That(actualPerson.FirstName).IsEqualTo(createdPerson.FirstName);
        Check.That(actualPerson.LastName).IsEqualTo(createdPerson.LastName);
        Check.That(actualPerson.Part).IsEqualTo(expectedPersonItem.Part);
        Check.That(actualPerson.SubPart).IsEqualTo(expectedPersonItem.SubPart);
        Check.That(actualPerson.ItemType).IsEqualTo("person");
    }

    [Fact]
    public async Task a_typed_item_cannot_be_created_if_the_specified_type_is_wrong()
    {
        var personItem = new HCPersonItem()
        {
            Id = _cosmosDb.GenerateId(),
            FirstName = "John",
            LastName = "Doe",
            Part = "01",
            SubPart = "AA",
            ItemType = "foo"
        };

        Check.ThatAsyncCode(async () => await Container.UpsertTypedItemAsync(personItem)).ThrowsAny();
    }
        
    [Fact]
    public void a_typed_item_cannot_be_created_by_batch_if_the_specified_type_is_wrong()
    {
        var personItem = new HCPersonItem()
        {
            Id = _cosmosDb.GenerateId(),
            FirstName = "John",
            LastName = "Doe",
            Part = "01",
            SubPart = "AA",
            ItemType = "foo"
        };

        var batch = Container.CreateTransactionalBatch(personItem.GetPartitionKey());

        Check.ThatCode(() => batch.UpsertTypedItem(personItem)).ThrowsAny();
    }

    [Fact]
    public async Task a_typed_item_is_upserted_with_type_and_deleted_fields()
    {
        var personItem = new HCPersonItem()
        {
            Id = _cosmosDb.GenerateId(),
            FirstName = "John",
            LastName = "Doe",
            Part = "01",
            SubPart = "AA"
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
        var personItem = new HCPersonItem()
        {
            Id = _cosmosDb.GenerateId(),
            FirstName = "John",
            LastName = "Doe",
            Part = "01",
            SubPart = "AA"
        };

        var batch = Container.CreateTransactionalBatch(personItem.GetPartitionKey());
        batch.UpsertTypedItem(personItem);

        await batch.ExecuteAsync();

        var savedPersonResponse =
            await Container.ReadItemAsync<JObject>(personItem.Id, personItem.GetPartitionKey());

        var savedPerson = savedPersonResponse.Resource;

        Check.That(savedPerson["_type"].ToString()).Equals(personItem.ItemType);
        Check.That(savedPerson["_deleted"].ToObject<bool>()).Equals(personItem.Deleted);
        Check.That(savedPerson["_type"].ToObject<string>()).IsEqualTo("person");
    }
    
    private async Task<List<HCEventItem>> FillWithEvents()
    {
        var result = new List<HCEventItem>();
        var phoneCall = new HCPhonecallItem()
        {
            Id = TypedItemHelper<HCPhonecallItem>.GenerateId(),
            Date = DateTime.Now,
            Duration = 40,
            Part = "01",
            SubPart = "A"
        };
        result.Add(phoneCall);
        await Container.CreateTypedItemAsync(phoneCall);
        
        phoneCall = new HCPhonecallItem()
        {
            Id = TypedItemHelper<HCPhonecallItem>.GenerateId(),
            Date = DateTime.Now,
            Duration = 40,
            Part = "01",
            SubPart = "B"
        };
        result.Add(phoneCall);
        await Container.CreateTypedItemAsync(phoneCall);

        phoneCall = new HCPhonecallItem()
        {
            Id = TypedItemHelper<HCPhonecallItem>.GenerateId(),
            Date = DateTime.Now-TimeSpan.FromDays(2),
            Duration = 4,
            Part = "01",
            SubPart = "A"
        };
        result.Add(phoneCall);
        await Container.CreateTypedItemAsync(phoneCall);

        var teamsMeeting = new HCTeamsMeeting()
        {
            Id = TypedItemHelper<HCTeamsMeeting>.GenerateId(),
            Date = DateTime.Now,
            Peoples = new[] { "John" },
            Part = "01",
            SubPart = "C"
        };
        result.Add(teamsMeeting);
        await Container.CreateTypedItemAsync(teamsMeeting);
            
        teamsMeeting = new HCTeamsMeeting()
        {
            Id = TypedItemHelper<HCTeamsMeeting>.GenerateId(),
            Date = DateTime.Now-TimeSpan.FromDays(3),
            Peoples = new[] { "Jack" },
            Part = "01",
            SubPart = "C"
        };
        result.Add(teamsMeeting);
        await Container.CreateTypedItemAsync(teamsMeeting);

        return result;

    }
    
    [Fact]
    public async Task query_on_a_class_returns_all_the_items_of_the_specified_class()
    {
        var events = await FillWithEvents();

        var queryResult =
            await Container.QueryTypedItemAsync<HCPhonecallItem, HCPhonecallItem>(e => e,
                new QueryTypedItemsOptions() { ReadAllPages = true });

        var phoneCallType = TypedItemHelper<HCPhonecallItem>.ItemType;
        Check.That(queryResult.Count).IsEqualTo(events.Count(_ => _.ItemType == phoneCallType));
        foreach (var e in queryResult.Results)
        {
            Check.That(e.ItemType).IsEqualTo(TypedItemHelper<PhonecallItem>.ItemType);
        }
    }
    
    [Fact]
    public async Task query_on_a_class_returns_all_the_items_of_the_specified_class_in_a_partition()
    {
        var events = await FillWithEvents();

        var pk = new PartitionKeyBuilder().Add("01").Add("A").Build();
        var queryResult =
            await Container.QueryTypedItemAsync<HCPhonecallItem, HCPhonecallItem>(e => e,
                new QueryTypedItemsOptions() { ReadAllPages = true, PartitionKey = pk });

        var phoneCallType = TypedItemHelper<HCPhonecallItem>.ItemType;
        Check.That(queryResult.Count).IsEqualTo(events.Count(_ => _.ItemType == phoneCallType && _.Part == "01" && _.SubPart == "A"));
        foreach (var e in queryResult.Results)
        {
            Check.That(e.ItemType).IsEqualTo(TypedItemHelper<HCPhonecallItem>.ItemType);
        }
    }
    
    [Fact]
    public async Task query_on_a_class_returns_all_the_items_of_the_specified_class_in_a_partial_partition()
    {
        var events = await FillWithEvents();

        var pk = new PartitionKeyBuilder().Add("01").Build();
        var queryResult =
            await Container.QueryTypedItemAsync<HCPhonecallItem, HCPhonecallItem>(e => e,
                new QueryTypedItemsOptions() { ReadAllPages = true, PartitionKey = pk });

        var phoneCallType = TypedItemHelper<HCPhonecallItem>.ItemType;
        Check.That(queryResult.Count).IsEqualTo(events.Count(_ => _.ItemType == phoneCallType && _.Part == "01"));
        foreach (var e in queryResult.Results)
        {
            Check.That(e.ItemType).IsEqualTo(TypedItemHelper<HCPhonecallItem>.ItemType);
        }
    }
}