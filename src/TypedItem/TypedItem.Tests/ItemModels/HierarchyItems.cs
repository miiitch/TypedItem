using Newtonsoft.Json;
using TypedItem.Lib;

namespace TypedItem.Tests.ItemModels;

// =============================================================================
// 1-LEVEL hierarchy — single sealed class with no parent ItemType
// Expected _type: "product"
// =============================================================================

[ItemType("product")]
public sealed class ProductItem : ContainerItem
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("price")]
    public decimal Price { get; set; }
}

// =============================================================================
// 2-LEVEL hierarchy — non-sealed parent + sealed children
// Expected _type: "vehicle.car"  /  "vehicle.truck"
// =============================================================================

[ItemType("vehicle")]
public class VehicleItem : ContainerItem
{
    [JsonProperty("licensePlate")]
    public string LicensePlate { get; set; }
}

[ItemType("car")]
public sealed class CarItem : VehicleItem
{
    [JsonProperty("brand")]
    public string Brand { get; set; }
}

[ItemType("truck")]
public sealed class TruckItem : VehicleItem
{
    [JsonProperty("payloadTons")]
    public int PayloadTons { get; set; }
}

// =============================================================================
// 3-LEVEL hierarchy — two levels of non-sealed parents + sealed leaves
// Expected _type:
//   "animal.mammal.dog"  /  "animal.mammal.cat"  /  "animal.bird.parrot"
// =============================================================================

[ItemType("animal")]
public class AnimalItem : ContainerItem { }

[ItemType("mammal")]
public class MammalItem : AnimalItem { }

[ItemType("dog")]
public sealed class DogItem : MammalItem
{
    [JsonProperty("breed")]
    public string Breed { get; set; }
}

[ItemType("cat")]
public sealed class CatItem : MammalItem
{
    [JsonProperty("indoor")]
    public bool Indoor { get; set; }
}

[ItemType("bird")]
public class BirdItem : AnimalItem { }

[ItemType("parrot")]
public sealed class ParrotItem : BirdItem
{
    [JsonProperty("canSpeak")]
    public bool CanSpeak { get; set; }
}
