using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using NFluent;
using TypedItem.Lib;
using TypedItem.Tests.ItemModels;
using Xunit;

namespace TypedItem.Tests;

/// <summary>
/// Tests verifying the _type hierarchy system across 1, 2, and 3 levels of inheritance,
/// including cross-hierarchy queries (e.g., query a level-2 parent to retrieve all
/// level-3 descendants, but not items from sibling branches).
/// </summary>
[Collection(CosmosDbCollection.Name)]
public class TypedItemHierarchyTests : IAsyncLifetime
{
    private readonly CosmosDbDatabaseFixture _cosmosDb;
    private Container _container = null!;

    public TypedItemHierarchyTests(CosmosDbDatabaseFixture cosmosDb)
    {
        _cosmosDb = cosmosDb;
    }

    public async Task InitializeAsync()
    {
        var response = await _cosmosDb.Database.CreateContainerAsync(
            new ContainerProperties(_cosmosDb.GenerateId(), "/part"));
        _container = response.Container;
    }

    public async Task DisposeAsync()
    {
        await _container.DeleteContainerAsync();
    }

    // =========================================================================
    // 1-LEVEL HIERARCHY
    // =========================================================================

    [Fact]
    public void single_level_item_type_value_is_just_the_type_name()
    {
        Check.That(TypedItemHelper<ProductItem>.ItemType).IsEqualTo("product");
    }

    [Fact]
    public async Task single_level_item_can_be_created_read_and_has_correct_type_in_db()
    {
        var product = new ProductItem { Part = "p1", Name = "Widget", Price = 9.99m };

        await _container.CreateTypedItemAsync(product);

        Check.That(product.ItemType).IsEqualTo("product");

        var raw = await _container.ReadItemAsync<JObject>(product.Id, product.GetPartitionKey());
        Check.That(raw.Resource["_type"]!.Value<string>()).IsEqualTo("product");

        var read = await _container.ReadTypedItemAsync<ProductItem>(product.Id, product.GetPartitionKey());
        Check.That(read.Resource.Name).IsEqualTo("Widget");
        Check.That(read.Resource.Price).IsEqualTo(9.99m);
    }

    [Fact]
    public async Task single_level_query_returns_exact_type_only()
    {
        var p1 = new ProductItem { Part = "p1", Name = "Foo", Price = 1m };
        var p2 = new ProductItem { Part = "p1", Name = "Bar", Price = 2m };
        await _container.CreateTypedItemAsync(p1);
        await _container.CreateTypedItemAsync(p2);

        var result = await _container.QueryTypedItemAsync<ProductItem, ProductItem>(q => q);

        Check.That(result.Results).HasSize(2);
        Check.That(result.Results.All(p => p.ItemType == "product")).IsTrue();
    }

    // =========================================================================
    // 2-LEVEL HIERARCHY
    // =========================================================================

    [Fact]
    public void two_level_sealed_types_have_parent_dot_child_type_value()
    {
        Check.That(TypedItemHelper<CarItem>.ItemType).IsEqualTo("vehicle.car");
        Check.That(TypedItemHelper<TruckItem>.ItemType).IsEqualTo("vehicle.truck");
    }

    [Fact]
    public void two_level_parent_type_value_is_just_its_own_name()
    {
        // VehicleItem is non-sealed → can also be queried
        Check.That(TypedItemHelper<VehicleItem>.ItemType).IsEqualTo("vehicle");
    }

    [Fact]
    public async Task two_level_items_store_full_dotted_type_in_db()
    {
        var car = new CarItem { Part = "p1", LicensePlate = "AA-123-BB", Brand = "Toyota" };
        var truck = new TruckItem { Part = "p1", LicensePlate = "CC-456-DD", PayloadTons = 20 };

        await _container.CreateTypedItemAsync(car);
        await _container.CreateTypedItemAsync(truck);

        var rawCar = await _container.ReadItemAsync<JObject>(car.Id, car.GetPartitionKey());
        var rawTruck = await _container.ReadItemAsync<JObject>(truck.Id, truck.GetPartitionKey());

        Check.That(rawCar.Resource["_type"]!.Value<string>()).IsEqualTo("vehicle.car");
        Check.That(rawTruck.Resource["_type"]!.Value<string>()).IsEqualTo("vehicle.truck");
    }

    [Fact]
    public async Task two_level_query_by_parent_returns_all_children()
    {
        var car = new CarItem { Part = "p1", LicensePlate = "AA-111-BB", Brand = "Honda" };
        var truck = new TruckItem { Part = "p1", LicensePlate = "CC-222-DD", PayloadTons = 15 };

        await _container.CreateTypedItemAsync(car);
        await _container.CreateTypedItemAsync(truck);

        // Querying by the non-sealed parent returns both children
        var vehicles = await _container.QueryTypedItemAsync<VehicleItem, VehicleItem>(q => q);
        Check.That(vehicles.Results).HasSize(2);
    }

    [Fact]
    public async Task two_level_query_by_sealed_child_returns_only_that_type()
    {
        var car = new CarItem { Part = "p1", LicensePlate = "AA-111-BB", Brand = "Honda" };
        var truck = new TruckItem { Part = "p1", LicensePlate = "CC-222-DD", PayloadTons = 15 };

        await _container.CreateTypedItemAsync(car);
        await _container.CreateTypedItemAsync(truck);

        var cars = await _container.QueryTypedItemAsync<CarItem, CarItem>(q => q);
        Check.That(cars.Results).HasSize(1);
        Check.That(cars.Results[0].Brand).IsEqualTo("Honda");

        var trucks = await _container.QueryTypedItemAsync<TruckItem, TruckItem>(q => q);
        Check.That(trucks.Results).HasSize(1);
        Check.That(trucks.Results[0].PayloadTons).IsEqualTo(15);
    }

    [Fact]
    public async Task two_level_read_by_exact_sealed_type_succeeds()
    {
        var car = new CarItem { Part = "p1", LicensePlate = "AA-333-BB", Brand = "Ford" };
        await _container.CreateTypedItemAsync(car);

        var read = await _container.ReadTypedItemAsync<CarItem>(car.Id, car.GetPartitionKey());
        Check.That(read.Resource.Brand).IsEqualTo("Ford");
        Check.That(read.Resource.ItemType).IsEqualTo("vehicle.car");
    }

    [Fact]
    public async Task two_level_read_by_wrong_sibling_type_throws()
    {
        var car = new CarItem { Part = "p1", LicensePlate = "AA-444-BB", Brand = "BMW" };
        await _container.CreateTypedItemAsync(car);

        // Reading a car document as TruckItem should fail: _type mismatch
        Check.ThatCode(async () =>
                await _container.ReadTypedItemAsync<TruckItem>(car.Id, car.GetPartitionKey()))
            .Throws<CosmosException>();
    }

    // =========================================================================
    // 3-LEVEL HIERARCHY
    // =========================================================================

    [Fact]
    public void three_level_sealed_types_have_three_segment_type_value()
    {
        Check.That(TypedItemHelper<DogItem>.ItemType).IsEqualTo("animal.mammal.dog");
        Check.That(TypedItemHelper<CatItem>.ItemType).IsEqualTo("animal.mammal.cat");
        Check.That(TypedItemHelper<ParrotItem>.ItemType).IsEqualTo("animal.bird.parrot");
    }

    [Fact]
    public void three_level_intermediate_types_have_partial_type_values()
    {
        Check.That(TypedItemHelper<AnimalItem>.ItemType).IsEqualTo("animal");
        Check.That(TypedItemHelper<MammalItem>.ItemType).IsEqualTo("animal.mammal");
        Check.That(TypedItemHelper<BirdItem>.ItemType).IsEqualTo("animal.bird");
    }

    [Fact]
    public async Task three_level_items_store_three_segment_type_in_db()
    {
        var dog = new DogItem { Part = "p1", Breed = "Labrador" };
        var cat = new CatItem { Part = "p1", Indoor = true };
        var parrot = new ParrotItem { Part = "p1", CanSpeak = true };

        await _container.CreateTypedItemAsync(dog);
        await _container.CreateTypedItemAsync(cat);
        await _container.CreateTypedItemAsync(parrot);

        var rawDog = await _container.ReadItemAsync<JObject>(dog.Id, dog.GetPartitionKey());
        var rawCat = await _container.ReadItemAsync<JObject>(cat.Id, cat.GetPartitionKey());
        var rawParrot = await _container.ReadItemAsync<JObject>(parrot.Id, parrot.GetPartitionKey());

        Check.That(rawDog.Resource["_type"]!.Value<string>()).IsEqualTo("animal.mammal.dog");
        Check.That(rawCat.Resource["_type"]!.Value<string>()).IsEqualTo("animal.mammal.cat");
        Check.That(rawParrot.Resource["_type"]!.Value<string>()).IsEqualTo("animal.bird.parrot");
    }

    [Fact]
    public async Task three_level_query_by_level1_parent_returns_all_descendants()
    {
        var dog = new DogItem { Part = "p1", Breed = "Poodle" };
        var cat = new CatItem { Part = "p1", Indoor = false };
        var parrot = new ParrotItem { Part = "p1", CanSpeak = false };

        await _container.CreateTypedItemAsync(dog);
        await _container.CreateTypedItemAsync(cat);
        await _container.CreateTypedItemAsync(parrot);

        // AnimalItem is the root — should match all three leaf types
        var animals = await _container.QueryTypedItemAsync<AnimalItem, AnimalItem>(q => q);
        Check.That(animals.Results).HasSize(3);
        Check.That(animals.Results.Any(a => a.ItemType == "animal.mammal.dog")).IsTrue();
        Check.That(animals.Results.Any(a => a.ItemType == "animal.mammal.cat")).IsTrue();
        Check.That(animals.Results.Any(a => a.ItemType == "animal.bird.parrot")).IsTrue();
    }

    /// <summary>
    /// Cross-hierarchy query: querying by a level-2 type (MammalItem) on a 3-level hierarchy
    /// must return only direct descendants of that branch, not items from sibling branches.
    /// </summary>
    [Fact]
    public async Task three_level_cross_hierarchy_query_by_level2_returns_only_its_branch()
    {
        var dog = new DogItem { Part = "p1", Breed = "Beagle" };
        var cat = new CatItem { Part = "p1", Indoor = true };
        var parrot = new ParrotItem { Part = "p1", CanSpeak = true };

        await _container.CreateTypedItemAsync(dog);
        await _container.CreateTypedItemAsync(cat);
        await _container.CreateTypedItemAsync(parrot);

        // MammalItem covers "animal.mammal.*" — dog and cat, but NOT parrot
        var mammals = await _container.QueryTypedItemAsync<MammalItem, MammalItem>(q => q);
        Check.That(mammals.Results).HasSize(2);
        Check.That(mammals.Results.Any(m => m.ItemType == "animal.mammal.dog")).IsTrue();
        Check.That(mammals.Results.Any(m => m.ItemType == "animal.mammal.cat")).IsTrue();
        Check.That(mammals.Results.Any(m => m.ItemType == "animal.bird.parrot")).IsFalse();

        // BirdItem covers "animal.bird.*" — parrot only
        var birds = await _container.QueryTypedItemAsync<BirdItem, BirdItem>(q => q);
        Check.That(birds.Results).HasSize(1);
        Check.That(birds.Results[0].ItemType).IsEqualTo("animal.bird.parrot");
    }

    [Fact]
    public async Task three_level_query_by_leaf_type_returns_only_exact_type()
    {
        var dog1 = new DogItem { Part = "p1", Breed = "Husky" };
        var dog2 = new DogItem { Part = "p1", Breed = "Pug" };
        var cat = new CatItem { Part = "p1", Indoor = true };

        await _container.CreateTypedItemAsync(dog1);
        await _container.CreateTypedItemAsync(dog2);
        await _container.CreateTypedItemAsync(cat);

        // DogItem is sealed → exact match on "animal.mammal.dog"
        var dogs = await _container.QueryTypedItemAsync<DogItem, DogItem>(q => q);
        Check.That(dogs.Results).HasSize(2);
        Check.That(dogs.Results.All(d => d.ItemType == "animal.mammal.dog")).IsTrue();
    }

    [Fact]
    public async Task three_level_read_by_exact_type_succeeds_and_wrong_type_throws()
    {
        var dog = new DogItem { Part = "p1", Breed = "Golden" };
        await _container.CreateTypedItemAsync(dog);

        // Correct type: succeeds
        var read = await _container.ReadTypedItemAsync<DogItem>(dog.Id, dog.GetPartitionKey());
        Check.That(read.Resource.Breed).IsEqualTo("Golden");
        Check.That(read.Resource.ItemType).IsEqualTo("animal.mammal.dog");

        // Wrong sibling type (cat): fails
        Check.ThatCode(async () =>
                await _container.ReadTypedItemAsync<CatItem>(dog.Id, dog.GetPartitionKey()))
            .Throws<CosmosException>();

        // Wrong branch type (parrot): fails
        Check.ThatCode(async () =>
                await _container.ReadTypedItemAsync<ParrotItem>(dog.Id, dog.GetPartitionKey()))
            .Throws<CosmosException>();
    }

    [Fact]
    public async Task three_level_cannot_create_intermediate_type_item()
    {
        // MammalItem is not sealed → PrepareItem should throw TypedItemException
        var mammal = new MammalItem { Part = "p1" };
        Check.ThatCode(async () => await _container.CreateTypedItemAsync(mammal))
            .Throws<TypedItemException>();
    }
}
