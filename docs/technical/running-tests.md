# TypedItem — Running Tests

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine on Linux)  
  At least 4 GB RAM allocated to Docker
- .NET 10 SDK

---

## How the test infrastructure works

Tests use **Testcontainers** to automatically start and stop a CosmosDB emulator Docker container. No manual setup is needed — `dotnet test` handles everything.

Each `dotnet test` invocation:
1. Pulls (if needed) `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview`
2. Starts a fresh container — first startup takes **3–5 minutes** (PostgreSQL + Citus initialization)
3. Runs all tests against a single shared container
4. Stops and removes the container automatically after the run

This guarantees a clean database on every test run.

---

## Running tests

```bash
cd src/TypedItem
dotnet test
```

With verbose output:

```bash
dotnet test -v normal
```

First run (cold Docker image):

```bash
dotnet test  # allow up to 10 minutes for image pull + container init
```

Subsequent runs (image cached, but database reinitializes every time):

```bash
dotnet test  # typically completes in ~5 minutes
```

---

## Code coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage-results
reportgenerator -reports:"./coverage-results/**/*.xml" -targetdir:"./coverage-report" -reporttypes:"Html;TextSummary"
```

Reports are generated in `./coverage-report/`. The `reportgenerator` tool can be installed globally with:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

---

## Test architecture

| File | Purpose |
|------|---------|
| `CosmosDBDatabaseFixture.cs` | Starts/stops the container; creates a shared CosmosDB database |
| `Global.cs` | xUnit `[CollectionDefinition]` — ensures ONE container per `dotnet test` invocation |
| `TypedItemOperationsSinglePKTests.cs` | Integration tests for single partition key containers |
| `TypedItemOperationsHPkTests.cs` | Integration tests for hierarchical partition key containers |
| `TypedItemHierarchyTests.cs` | Tests covering 1, 2, and 3-level type hierarchies + cross-hierarchy queries |
| `TypedItemHelperUnitTests.cs` | Unit tests for `TypedItemHelper<T>` (no container needed) |
| `OptionTests.cs` | Unit tests for `QueryTypedItemsOptions` |
| `TypedDocumentTypeValuesTests.cs` | Unit tests for `_type` value computation |

Each integration test class:
- Implements `IAsyncLifetime`
- Creates a **new container** in `InitializeAsync()` (guaranteed isolation between tests)
- Deletes the container in `DisposeAsync()`

---

## Known limitations

### Linux emulator (all `vnext-*` images)

The Linux emulator images (`vnext-preview`, `vnext-EN*`) are all backed by PostgreSQL/Citus. They do not support:

- `PatchItem` with `FilterPredicate` → returns `PostgresError E42P01`
- LINQ `WHERE _deleted = false` filter in queries

Tests affected by this limitation are annotated with `[Fact(Skip = "...")]`. They pass against real Azure Cosmos DB.

### ARM64 (Apple Silicon)

The `latest` and `stable` tags on MCR have no ARM64 manifest. Use tags with an `-arm64` suffix (e.g., `vnext-EN20260331-arm64`) on Apple Silicon machines.

---

## CI/CD

Tests run automatically on every push and pull request via GitHub Actions (`.github/workflows/ci.yml`). The workflow uses Testcontainers — no CosmosDB service container is required in the workflow definition.
