# TypedItem — Getting Started

TypedItem is a .NET extension library for Azure Cosmos DB (SQL API) that adds **typed item management**, **soft delete**, and **hierarchical type queries** to any CosmosDB container.

---

## Installation

```bash
dotnet add package TypedItem
```

Requires **Microsoft.Azure.Cosmos 3.x** (Cosmos SDK v3) and **.NET 10**.

---

## Core concepts

Each document in a Cosmos DB container stores a `_type` field computed from C# inheritance and `[ItemType]` attributes. This field drives type-safe reads and queries.

| Field | Description |
|-------|-------------|
| `_type` | Dot-separated type identifier (e.g., `"event.phonecall"`) |
| `_deleted` | Soft-delete flag (`true` = logically deleted) |

---

## Step 1 — Define a partition-key base class

Create a root class that extends `TypedItemBase` and implements `GetPartitionKey()`:

```csharp
public class MyContainerItem : TypedItemBase
{
    [JsonProperty("part")]
    public string Part { get; set; }

    public override PartitionKey GetPartitionKey()
        => CreatePartitionKey(Part);
}
```

For hierarchical partition keys (up to 3 levels):

```csharp
public class HierarchicalItem : TypedItemBase
{
    [JsonProperty("part")]   public string Part { get; set; }
    [JsonProperty("sub")]    public string Sub  { get; set; }

    public override PartitionKey GetPartitionKey()
        => CreatePartitionKey(Part, Sub);
}
```

---

## Step 2 — Define typed item classes

Annotate classes with `[ItemType("name")]`. **Write operations require a `sealed` class.** Non-sealed classes are valid only for queries.

```csharp
// Single level — _type stored as "person"
[ItemType("person")]
public sealed class PersonItem : MyContainerItem
{
    [JsonProperty("firstName")] public string FirstName { get; set; }
    [JsonProperty("lastName")]  public string LastName  { get; set; }
}
```

---

## Step 3 — Use extension methods

All extension methods live in the `Microsoft.Azure.Cosmos` namespace (alongside the official SDK types):

```csharp
// Create
await container.CreateTypedItemAsync(person);

// Upsert
await container.UpsertTypedItemAsync(person);

// Read — throws CosmosException (404) if deleted or wrong type
var response = await container.ReadTypedItemAsync<PersonItem>(id, partitionKey);

// Read (include deleted items)
var response = await container.ReadTypedItemAsync<PersonItem>(id, partitionKey,
    new TypedItemRequestOptions { ReadDeleted = true });

// Replace
await container.ReplaceTypedItemAsync(person, person.Id);

// Soft-delete (sets _deleted = true via PATCH)
await container.SoftDeleteTypedItemAsync(person);

// Query
var result = await container.QueryTypedItemAsync<PersonItem, PersonItem>(q => q);
```

---

## Type hierarchy

Use C# inheritance + multiple `[ItemType]` attributes to build dot-separated type hierarchies.

### 2-level example

```csharp
[ItemType("event")]             // non-sealed = queryable parent
public class EventItem : MyContainerItem
{
    [JsonProperty("date")] public DateTime Date { get; set; }
}

[ItemType("phonecall")]         // _type stored as "event.phonecall"
public sealed class PhonecallItem : EventItem
{
    [JsonProperty("duration")] public int Duration { get; set; }
}

[ItemType("meeting")]           // _type stored as "event.meeting"
public sealed class MeetingItem : EventItem
{
    [JsonProperty("attendees")] public string[] Attendees { get; set; }
}
```

### 3-level example

```csharp
[ItemType("animal")]
public class AnimalItem : MyContainerItem { }

[ItemType("mammal")]
public class MammalItem : AnimalItem { }

[ItemType("dog")]               // _type stored as "animal.mammal.dog"
public sealed class DogItem : MammalItem
{
    [JsonProperty("breed")] public string Breed { get; set; }
}

[ItemType("bird")]
public class BirdItem : AnimalItem { }

[ItemType("parrot")]            // _type stored as "animal.bird.parrot"
public sealed class ParrotItem : BirdItem
{
    [JsonProperty("canSpeak")] public bool CanSpeak { get; set; }
}
```

---

## Cross-hierarchy queries

`QueryTypedItemAsync<TFrom, TTo>` filters by type automatically:

| `TFrom` | `sealed`? | Filter applied |
|---------|-----------|----------------|
| `DogItem` | yes | `WHERE _type = 'animal.mammal.dog'` |
| `MammalItem` | no | `WHERE _type STARTSWITH 'animal.mammal.'` |
| `AnimalItem` | no | `WHERE _type STARTSWITH 'animal.'` |

```csharp
// Returns only dogs
var dogs = await container.QueryTypedItemAsync<DogItem, DogItem>(q => q);

// Returns dogs AND cats (all mammals) — cross-hierarchy level-2 query
var mammals = await container.QueryTypedItemAsync<MammalItem, MammalItem>(q => q);

// Returns ALL animals (dogs, cats, parrots, ...)
var animals = await container.QueryTypedItemAsync<AnimalItem, AnimalItem>(q => q);
```

Query options:

```csharp
var result = await container.QueryTypedItemAsync<PersonItem, PersonItem>(q => q,
    new QueryTypedItemsOptions
    {
        MaxItemCount = 50,
        IncludeDeletedItems = false,  // default: include only non-deleted items
        ReadAllPages = true,          // iterate until all pages are fetched
        ContinuationToken = token,    // resume pagination
    });

Console.WriteLine(result.Results.Count);
Console.WriteLine(result.ContinuationToken);  // null when all pages are read
Console.WriteLine(result.RequestCharge);       // total RU cost
```

---

## Soft delete

`SoftDeleteTypedItemAsync` sets `_deleted = true` via a conditional PATCH (only if not already deleted). Items with `_deleted = true` are invisible to `ReadTypedItemAsync` and `QueryTypedItemAsync` by default.

```csharp
// Soft-delete by item object (uses ETag for optimistic concurrency)
await container.SoftDeleteTypedItemAsync(person);

// Soft-delete by id + partition key
await container.SoftDeleteTypedItemAsync<PersonItem>(id, partitionKey);

// Soft-delete in a transactional batch
var batch = container.CreateTransactionalBatch(partitionKey);
batch.SoftDeleteTypedItem(personId);
await batch.ExecuteAsync();
```

---

## Transactional batch support

All typed operations are available on `TransactionalBatch`:

```csharp
var batch = container.CreateTransactionalBatch(partitionKey);
batch.CreateTypedItem(person);
batch.UpsertTypedItem(address);
batch.ReplaceTypedItem(updatedPerson, updatedPerson.Id);
batch.SoftDeleteTypedItem(obsoleteId);
await batch.ExecuteAsync();
```

---

## `TypedItemHelper<T>` utilities

```csharp
// Get the _type string for a sealed type (compile-time)
string type = TypedItemHelper<PersonItem>.ItemType;  // "person"

// Generate a type-prefixed ID
string id = TypedItemHelper<PersonItem>.GenerateId();  // "person-<guid>"

// Check if a stored item matches the expected type
bool match = TypedItemHelper<PersonItem>.HasSameType(storedItem);

// Check if the type is final (sealed)
bool isFinal = TypedItemHelper<PersonItem>.IsFinal;  // true for sealed classes
```
