# TypedItem

**TypedItem** is a .NET extension library for **Azure Cosmos DB (SQL API)** that adds typed item management, soft delete, and hierarchical type queries to any Cosmos DB container.

Each document stores a `_type` field computed from C# inheritance and `[ItemType]` attributes. This lets you safely store multiple item types in a single container and query them with full type safety.

## Installation

```bash
dotnet add package TypedItem
```

Requires **Microsoft.Azure.Cosmos 3.x** and **.NET 10**.

## Quick start

### 1 — Define a partition-key base class

```csharp
public class MyContainerItem : TypedItemBase
{
    [JsonProperty("part")]
    public string Part { get; set; }

    public override PartitionKey GetPartitionKey()
        => CreatePartitionKey(Part);
}
```

### 2 — Define typed item classes

Annotate with `[ItemType("name")]`. Write operations require a `sealed` class.

```csharp
[ItemType("person")]
public sealed class PersonItem : MyContainerItem
{
    [JsonProperty("firstName")] public string FirstName { get; set; }
    [JsonProperty("lastName")]  public string LastName  { get; set; }
}
```

### 3 — Use extension methods

All methods extend `Microsoft.Azure.Cosmos.Container` and `TransactionalBatch`:

```csharp
// Write
await container.CreateTypedItemAsync(person);
await container.UpsertTypedItemAsync(person);
await container.ReplaceTypedItemAsync(person, person.Id);

// Read — throws CosmosException (404) if soft-deleted or wrong type
var response = await container.ReadTypedItemAsync<PersonItem>(id, partitionKey);

// Soft-delete (sets _deleted = true via conditional PATCH)
await container.SoftDeleteTypedItemAsync(person);

// Query
var result = await container.QueryTypedItemAsync<PersonItem, PersonItem>(q => q);
```

## Type hierarchy

Use C# inheritance with multiple `[ItemType]` attributes to build dot-separated type hierarchies:

```csharp
[ItemType("event")]                   // non-sealed = queryable parent
public class EventItem : MyContainerItem
{
    [JsonProperty("date")] public DateTime Date { get; set; }
}

[ItemType("phonecall")]               // _type stored as "event.phonecall"
public sealed class PhonecallItem : EventItem
{
    [JsonProperty("duration")] public int Duration { get; set; }
}

[ItemType("meeting")]                 // _type stored as "event.meeting"
public sealed class MeetingItem : EventItem
{
    [JsonProperty("attendees")] public string[] Attendees { get; set; }
}
```

`QueryTypedItemAsync` automatically filters by type:

```csharp
// Returns only phone calls
var calls = await container.QueryTypedItemAsync<PhonecallItem, PhonecallItem>(q => q);

// Returns ALL events (phone calls + meetings)
var events = await container.QueryTypedItemAsync<EventItem, EventItem>(q => q);
```

## Fields stored in documents

| JSON field | Description |
|---|---|
| `_type` | Dot-separated type identifier (e.g., `"event.phonecall"`) |
| `_deleted` | Soft-delete flag (`true` = logically deleted) |
| `id` | Document id (auto-generated if not set) |

## Links

- [Getting Started](docs/user/getting-started.md) — full usage guide with all options
- [API Reference](docs/technical/api-reference.md) — all extension methods and types
- [Running Tests](docs/technical/running-tests.md) — test infrastructure and known limitations
