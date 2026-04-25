# TypedItem — API Reference

## Extension methods on `Microsoft.Azure.Cosmos.Container`

All methods throw `TypedItemException` if the type constraint (`sealed`, `[ItemType]` attribute) is not met.

---

### `CreateTypedItemAsync<T>`

```csharp
Task<ItemResponse<T>> CreateTypedItemAsync<T>(
    this Container container,
    T item,
    PartitionKey? partitionKey = null,
    ItemRequestOptions? requestOptions = null,
    CancellationToken cancellationToken = default)
    where T : TypedItemBase, new()
```

Creates a new item. Sets `_type` and generates `id` if not set. **Requires `T` to be `sealed`.**

---

### `UpsertTypedItemAsync<T>`

```csharp
Task<ItemResponse<T>> UpsertTypedItemAsync<T>(
    this Container container,
    T item,
    PartitionKey? partitionKey = null,
    ItemRequestOptions? requestOptions = null,
    CancellationToken cancellationToken = default)
    where T : TypedItemBase, new()
```

Creates or replaces an item. Sets `_type` and generates `id` if not set. **Requires `T` to be `sealed`.**

---

### `ReplaceTypedItemAsync<T>`

```csharp
Task<ItemResponse<T>> ReplaceTypedItemAsync<T>(
    this Container container,
    T item,
    string id,
    PartitionKey? partitionKey = null,
    ItemRequestOptions? requestOptions = null,
    CancellationToken cancellationToken = default)
    where T : TypedItemBase, new()
```

Replaces an existing item. Validates that `_type` matches the expected type. **Requires `T` to be `sealed`.**

---

### `ReadTypedItemAsync<T>`

```csharp
Task<ItemResponse<T>> ReadTypedItemAsync<T>(
    this Container container,
    string id,
    PartitionKey partitionKey,
    TypedItemRequestOptions? options = null,
    CancellationToken cancellationToken = default)
    where T : TypedItemBase, new()
```

Reads an item and validates that `_type` matches `T`. Throws `CosmosException` (404) if:
- the document does not exist
- the `_type` does not match `T`
- the item is soft-deleted (unless `ReadDeleted = true`)

**`TypedItemRequestOptions`:**

| Property | Type | Description |
|----------|------|-------------|
| `ReadDeleted` | `bool` | Set `true` to read items with `_deleted = true` |

---

### `SoftDeleteTypedItemAsync<T>` (by object)

```csharp
Task<ItemResponse<T>> SoftDeleteTypedItemAsync<T>(
    this Container container,
    T item,
    ItemRequestOptions? requestOptions = null,
    CancellationToken cancellationToken = default)
    where T : TypedItemBase, new()
```

Sets `_deleted = true` on the item using a conditional PATCH. Uses the item's `ETag` for optimistic concurrency. Throws `ArgumentException` if the item is already deleted.

---

### `SoftDeleteTypedItemAsync<T>` (by id)

```csharp
Task<ItemResponse<T>> SoftDeleteTypedItemAsync<T>(
    this Container container,
    string id,
    PartitionKey partitionKey,
    ItemRequestOptions? requestOptions = null,
    CancellationToken cancellationToken = default)
    where T : TypedItemBase, new()
```

Same as above but by id + partition key. Throws `CosmosException` (412) if the item is already deleted.

---

### `QueryTypedItemAsync<TFrom, TTo>`

```csharp
Task<DataQueryResponse<TTo>> QueryTypedItemAsync<TFrom, TTo>(
    this Container container,
    Func<IQueryable<TFrom>, IQueryable<TTo>> queryFunc,
    QueryTypedItemsOptions? queryOptions = null,
    CancellationToken cancellationToken = default)
    where TFrom : TypedItemBase, new()
```

Executes a LINQ query filtered by `_type`. If `TFrom` is sealed, uses an exact match; if not sealed, uses a prefix match (`StartsWith`).

**`QueryTypedItemsOptions`:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxItemCount` | `int` | SDK default | Max documents per page |
| `IncludeDeletedItems` | `bool?` | `null` (include all) | Set `false` to exclude `_deleted = true` items |
| `ReadAllPages` | `bool` | `false` | Automatically fetch all pages |
| `ContinuationToken` | `string?` | `null` | Resume from a previous page |
| `MaxConcurrency` | `int?` | `null` | Max parallel partition queries |

**`DataQueryResponse<T>`:**

| Property | Type | Description |
|----------|------|-------------|
| `Results` | `List<T>` | Documents returned |
| `ContinuationToken` | `string?` | Token for next page; `null` if last page |
| `RequestCharge` | `double` | Total RU consumed |
| `SessionToken` | `string?` | Session consistency token |

---

## Extension methods on `Microsoft.Azure.Cosmos.TransactionalBatch`

All typed batch operations mirror their `Container` counterparts. **All operations require a `sealed` type.**

```csharp
TransactionalBatch CreateTypedItem<T>(this TransactionalBatch batch, T item, ...)
TransactionalBatch UpsertTypedItem<T>(this TransactionalBatch batch, T item, ...)
TransactionalBatch ReplaceTypedItem<T>(this TransactionalBatch batch, T item, ...)
TransactionalBatch SoftDeleteTypedItem(this TransactionalBatch batch, string id, ...)
```

---

## `TypedItemHelper<T>`

Static utility class for type metadata. `T` must satisfy `TypedItemBase, new()`.

```csharp
// Full dot-separated _type value (e.g., "event.phonecall")
string TypedItemHelper<T>.ItemType

// Generates an id prefixed with the type (e.g., "phonecall-<guid>")
string TypedItemHelper<T>.GenerateId()

// True if T is sealed (write operations require this)
bool TypedItemHelper<T>.IsFinal

// True if the stored item's _type matches the expected type
bool TypedItemHelper<T>.HasSameType<U>(U item)
```

---

## `TypedItemBase` properties

Every item class ultimately inherits these JSON-mapped properties:

| C# property | JSON field | Description |
|-------------|------------|-------------|
| `Id` | `id` | Document id (string) |
| `ETag` | `_etag` | Cosmos ETag for optimistic concurrency |
| `ItemType` | `_type` | Type identifier set by the library |
| `Deleted` | `_deleted` | Soft-delete flag (`false` by default) |

`ItemType` and `Deleted` are marked `[EditorBrowsable(EditorBrowsableState.Never)]` to reduce IDE noise — they are managed by the library, not by application code.
