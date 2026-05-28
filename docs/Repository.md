[← Back to Documentation](Index.md)

# Repository

> ⚠️ **Experimental:** The Repository feature is still experimental and subject to change.

The Zerra Repository is a LINQ-based data access interface that is **data store agnostic**.

## Overview

- LINQ expression-based filtering, ordering, and pagination
- Consistent API across all supported data stores
- Pluggable provider model via `DataContext` and provider classes — **each entity type can be backed by a different data source**
- Integrated with the Zerra bus via `BaseHandlerWithRepo`
- Supports eager loading with `Graph<TModel>`
- AOT compatible

## How It Differs from Entity Framework

Zerra Repository is not a replacement for Entity Framework — it is a lighter alternative designed specifically for the Zerra bus architecture. The table below highlights the key differences:

| | Zerra Repository | Entity Framework |
|---|---|---|
| **AOT / Native AOT** | ✅ Fully compatible | ⚠️ Limited support |
| **Per-entity data source** | ✅ Each model can use a different store | ❌ All models share one `DbContext` |
| **Data store switching** | ✅ Swap via `DataContextSelector` | ❌ Requires significant refactoring |
| **Bus integration** | ✅ First-class via `BaseHandlerWithRepo` | ❌ Manual wiring required |
| **Dependency footprint** | ✅ Lightweight | ❌ Large dependency surface |
| **Change tracking** | ✅ None — operations are explicit | ⚠️ Automatic but adds overhead |
| **Eager loading control** | ✅ `Graph<T>` — precise per-call | ⚠️ `.Include()` chains — easy to over/under-fetch |
| **LINQ filtering** | ✅ Expression-based, consistent across all stores | ✅ Expression-based |
| **Query complexity** | ⚠️ Simple CRUD and filtered reads | ✅ Joins, groupings, projections |
| **Migration tooling** | ⚠️ Code First generation, no migration history | ✅ Rich migration tooling |
| **Ecosystem maturity** | ⚠️ Experimental | ✅ Mature, large community |

## Performance vs. Entity Framework

Benchmarks were run against Microsoft SQL Server using [BenchmarkDotNet](https://benchmarkdotnet.org/) with `AsNoTracking()` on the EF side. All Zerra results use the default `IRepo` API. Results are approximate and will vary by environment.

| Scenario | Speed | Memory |
|---|---|---|
| **Query many (all rows)** | Zerra is moderately faster (~20–25%) | Zerra allocates significantly less (~40% less) |
| **Query many with a where clause** | Zerra is substantially faster (~3–4×) | Zerra allocates dramatically less (~75% less) |
| **Query first with a where clause** | Zerra is substantially faster (~3–4×) | Zerra allocates dramatically less (~75% less) |
| **Query many with one-to-one include** | Zerra is moderately faster (~30–35%) | Zerra allocates significantly less (~60% less) |
| **Query many with one-to-many include** | Zerra is dramatically faster (~15–16×) | Zerra allocates dramatically less (~95% less) |
| **Update** | Zerra is moderately faster (~40%) | Zerra allocates dramatically less (~80% less) |

The one-to-many include result is particularly notable: EF performs a separate query per parent row (N+1 style) unless carefully tuned, while Zerra batches the related rows in a single additional query, dramatically reducing both round-trips and allocations at scale.

> Benchmarks are in [`EFBenchmark.cs`](../Tests/Zerra.Repository.Benchmark/Benchmarks/EFBenchmark.cs).

## NuGet Packages

| Package | Data Store |
|---|---|
| `Zerra.Repository` | Core interfaces and base classes |
| `Zerra.Repository.MsSql` | Microsoft SQL Server |
| `Zerra.Repository.MySql` | MySQL |
| `Zerra.Repository.PostgreSql` | PostgreSQL |
| `Zerra.Repository.MariaDb` | MariaDB |

## Setup

### 1. Define a Data Context

Create a class inheriting from the appropriate data store's `DataContext` base class. Each supported data store provides its own base (e.g., `MsSqlDataContext`, `MySqlDataContext`, `PostgreSqlDataContext`, `MariaDbDataContext`, `MemoryDataContext`):

```csharp
// SQL Server example
public sealed class MyMsSqlContext : MsSqlDataContext
{
    public override string GetConnectionString() =>
        "Data Source=.;Initial Catalog=MyDatabase;Integrated Security=True;TrustServerCertificate=True";
}

// In-memory example (useful for testing)
public sealed class MyMemoryContext : MemoryDataContext { }
```

A `DataContextSelector` can also be used to automatically select from multiple contexts at runtime (e.g., switching between environments):

```csharp
public class MyDbContext : DataContextSelector
{
    protected override ICollection<DataContext> LoadDataContexts() =>
    [
        new MyMemoryContext(),
        new MyMsSqlContext()
    ];
}
```

### 2. Create a Provider

Create a typed provider that links your models to the context:

```csharp
public sealed class MySqlProvider<TModel> : TransactStoreProvider<MyDbContext, TModel>
    where TModel : class, new()
{
    protected override bool EventLinking => false;
    protected override bool QueryLinking => true;
    protected override bool PersistLinking => true;
}
```

### 3. Register the Repo in Program.cs

Build the repo, add your providers, and register it with `BusServices`. Each entity type gets its own provider — and different providers can point to entirely different data sources:

```csharp
// Providers for different entity types can use different data contexts (and therefore different data stores)
public sealed class SqlProvider<TModel> : TransactStoreProvider<MyMsSqlContext, TModel>
    where TModel : class, new()
{
    protected override bool EventLinking => false;
    protected override bool QueryLinking => true;
    protected override bool PersistLinking => true;
}

public sealed class MemoryProvider<TModel> : TransactStoreProvider<MyMemoryContext, TModel>
    where TModel : class, new()
{
    protected override bool EventLinking => false;
    protected override bool QueryLinking => true;
    protected override bool PersistLinking => true;
}
```

```csharp
var repo = Repo.New();
repo.AddProvider(new SqlProvider<PetDataModel>());       // stored in SQL Server
repo.AddProvider(new MemoryProvider<PetTypeDataModel>()); // stored in memory

var busServices = new BusServices();
busServices.AddRepo(repo);
```

All entity types are accessed through the same `IRepo` interface regardless of where each one is stored.

## Using `IRepo` in Handlers

Handlers that need data access should extend `BaseHandlerWithRepo` instead of the standard `BaseHandler`. This base class automatically resolves and exposes an `IRepo Repo` property.

```csharp
public sealed class PetsCommandHandler : BaseHandlerWithRepo, IPetsCommandHandler
{
    public async Task<int> Handle(AddPetTypeCommand command, CancellationToken cancellationToken)
    {
        var model = new PetTypeDataModel() { Name = command.Name };
        await Repo.CreateAsync(model);

        var created = await Repo.SingleAsync<PetTypeDataModel>(x => x.Name == command.Name);
        if (created == null)
            throw new InvalidOperationException("Failed to retrieve after creation.");
        return created.Id;
    }
}
```

```csharp
public sealed class PetsQueryHandler : BaseHandlerWithRepo, IPetsQueryHandler
{
    public async Task<PetSimpleModel[]> GetPetsFromRepo()
    {
        var items = await Repo.ManyAsync<PetDataModel>(
            x => x.Id > 0,
            QueryOrder<PetDataModel>.Create(x => x.Name));

        return items.Select(item => new PetSimpleModel
        {
            ID = item.Id,
            Name = item.Name,
            Type = item.PetType?.Name
        }).ToArray();
    }
}
```

## IRepo API Reference

### Query Methods

| Method | Description |
|---|---|
| `Single<T>(where, graph?)` | Returns a single matching model, or `null`. Throws if multiple match. |
| `First<T>(where?, order?, graph?)` | Returns the first matching model, or `null`. |
| `Many<T>(where?, order?, skip?, take?, graph?)` | Returns a collection of matching models. |
| `Any<T>(where?)` | Returns `true` if any matching model exists. |
| `Count<T>(where?)` | Returns the count of matching models. |

All query methods have `Async` variants (e.g., `SingleAsync`, `ManyAsync`, `AnyAsync`, `CountAsync`).

### Persist Methods

| Method | Description |
|---|---|
| `Create<T>(model)` | Inserts a model into the data store. |
| `Update<T>(model)` | Updates an existing model. |
| `Delete<T>(model)` | Deletes a model from the data store. |
| `DeleteByID<T>(id)` | Deletes a model by its identity key. |

All persist methods have `Async` variants and optional `eventName`, `source`, and `Graph` overloads.

### Filtering with LINQ Expressions

```csharp
// Single record
var pet = await Repo.SingleAsync<PetDataModel>(x => x.Name == "Fido");

// Many with filter and ordering
var pets = await Repo.ManyAsync<PetDataModel>(
    x => x.PetTypeId == typeId,
    QueryOrder<PetDataModel>.Create(x => x.Name));

// With pagination
var page = await Repo.ManyAsync<PetDataModel>(order: null, skip: 0, take: 20);

// Check existence
bool exists = await Repo.AnyAsync<PetDataModel>(x => x.Name == "Fido");

// Count
int total = await Repo.CountAsync<PetDataModel>(x => x.PetTypeId == typeId);
```

### Eager Loading with Graph

Use `Graph<TModel>` to control which related properties are loaded:

```csharp
var graph = new Graph<PetDataModel>(x => x.PetType);
var pets = await Repo.ManyAsync<PetDataModel>(graph: graph);
```

See [Graph](Graph.md) for full documentation on graph-based property control.

## Schema Generation

Use Code First generation or the T4 reverse-engineer tool to create and maintain your data store schema.
See [Repository Generation](RepositoryGeneration.md) for full details.
