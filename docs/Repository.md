[← Back to Documentation](Index.md)

# Repository

> ⚠️ **Experimental:** the Repository is still experimental and may change.

`Zerra.Repository` is LINQ data access that doesn't depend on the data store. Handlers read and write data models through one `IRepo` interface, and each model type can live in a different store: SQL Server, PostgreSQL, MySQL, MariaDB, or in memory. It also supports event-sourced aggregates on KurrentDB. It is AOT compatible.

## Compared with Entity Framework

It is a lighter alternative designed for Zerra services, not a replacement for Entity Framework.

| | Zerra Repository | Entity Framework |
|---|---|---|
| **Native AOT** | ✅ Fully compatible | ⚠️ Limited support |
| **Data source per model** | ✅ Each model can use a different store | ❌ All models share one `DbContext` |
| **Switching stores** | ✅ Pass a different engine at startup | ❌ Significant refactoring |
| **Bus integration** | ✅ `BaseHandlerWithRepo` | ❌ Manual wiring |
| **Change tracking** | ✅ None; every operation is explicit | ⚠️ Automatic, with overhead |
| **Loading related data** | ✅ A `Graph<T>` per call | ⚠️ `.Include()` chains |
| **Query complexity** | ⚠️ CRUD and filtered reads | ✅ Joins, groupings, projections |
| **Migrations** | ⚠️ Code First generation, no migration history | ✅ Rich migration tooling |
| **Maturity** | ⚠️ Experimental | ✅ Mature, large community |

Against SQL Server, with `AsNoTracking()` and a new `DbContext` per query on the EF side, BenchmarkDotNet's median times put Zerra at approximately:

| Scenario | Speed | Memory |
|---|---|---|
| Query many (all rows) | ~45% faster | ~70% less |
| Query many with a where clause | ~2.3× faster | ~80% less |
| Query first with a where clause | ~2× faster | ~80% less |
| Query many with a one-to-one include | ~20% faster | ~65% less |
| Query many with a one-to-many include | ~3.8× faster | ~90% less |
| Update | ~40% faster | ~95% less |

For one-to-many includes, Zerra loads the related rows of every parent in one additional query, while EF joins them into one query by default. With EF's `AsSplitQuery()`, which loads them separately like Zerra, Zerra is ~30% faster with ~65% less memory. Results vary by environment. The benchmarks are in [`EFBenchmark.cs`](../Benchmarks/Zerra.Repository.Benchmark/Benchmarks/EFBenchmark.cs).

## Packages

| Package | Store |
|---|---|
| `Zerra.Repository` | core interfaces and base classes |
| `Zerra.Repository.MsSql` | SQL Server |
| `Zerra.Repository.PostgreSql` | PostgreSQL |
| `Zerra.Repository.MySql` | MySQL |
| `Zerra.Repository.MariaDb` | MariaDB |
| `Zerra.Repository.Memory` | in memory, for tests and fallbacks |
| `Zerra.Repository.KurrentDB` | KurrentDB event store, for aggregates |

The repository packages target .NET 10 only.

## Data Models

```csharp
[Entity("SalesOrder")]                          // table name
public sealed class OrderDataModel
{
    [Identity(false)]                           // primary key; false: assigned by code, not the database
    public Guid ID { get; set; }

    [StoreProperties(true, 32)]                 // not null, max length 32
    public string? OrderNumber { get; set; }

    public Guid CustomerID { get; set; }

    [Relation(nameof(CustomerID))]              // many-to-one: this model's foreign key
    public CustomerDataModel? Customer { get; set; }

    [Relation(nameof(OrderLineDataModel.OrderID))]   // one-to-many: the related model's foreign key
    public OrderLineDataModel[]? Lines { get; set; }
}
```

- Data models need a parameterless constructor. `[Entity]` also has the source generator produce their type details.
- One-to-many properties can be arrays, `List<T>`, or interfaces such as `IReadOnlyList<T>`.
- Without a precision, PostgreSQL, MySQL, and MariaDB store date and time columns to the microsecond, and SQL Server stores `DateTime` as `datetime` (about 3 ms). Set `[StoreProperties(notNull, precision)]` to choose.

## Setup

### 1. Engines

An engine is a store. Create the one for your database with its connection string: `MsSqlEngine`, `PostgreSqlEngine`, `MySqlEngine`, `MariaDbEngine`, `KurrentDBEngine` (an event store), or `MemoryEngine`.

```csharp
var engine = new MsSqlEngine(connectionString);   // doesn't connect until it's used
```

Each database also has a connection test: `MsSqlConnectionTest`, `PostgreSqlConnectionTest`, `MySqlConnectionTest`, `MariaDbConnectionTest`, and `KurrentDBConnectionTest`. `Test` returns whether the server answered, and if you pass a logger it logs why it didn't. Test at startup to choose the engine the providers get (see step 3), the same way `RabbitMQConnectionTest.TestAsync` chooses a message transport.

### 2. A Provider

A provider connects model types to an engine:

```csharp
public sealed class OrdersStoreProvider<TModel> : TransactStoreProvider<TModel>
    where TModel : class, new()
{
    public OrdersStoreProvider(ITransactStoreEngine engine) : base(engine) { }

    protected override bool EventLinking => false;
    protected override bool QueryLinking => true;    // load relations named in a graph
    protected override bool PersistLinking => true;  // save related models with their parent
}
```

Each in-memory engine is its own store, so a test can start from an empty one with `new MemoryEngine()`. Providers for related models must share an engine.

`EventStoreAsTransactStoreProvider<TModel>` stores a model in an event store (KurrentDB or `MemoryEngine`) instead of a table. Each change is an event, so the model's history can be read with the `Temporal` and `Event` queries, and queries must name the model's identity. Every 100 events it saves the model's state so reads don't replay the whole stream; pass `saveStateEvery` to change that, or 0 to never save it.

### 3. Register the Repo

```csharp
// memory as a fallback when the database isn't running
ITransactStoreEngine ordersEngine = MsSqlConnectionTest.Test(ordersConnectionString, log)
    ? new MsSqlEngine(ordersConnectionString)
    : new MemoryEngine();

var repo = Repo.New();
repo.AddProvider(new OrdersStoreProvider<OrderDataModel>(ordersEngine));
repo.AddProvider(new OrdersStoreProvider<OrderLineDataModel>(ordersEngine));
repo.AddProvider(new CatalogStoreProvider<ProductDataModel>(new PostgreSqlEngine(catalogConnectionString)));   // a different store, same IRepo


var busServices = new BusServices();
busServices.AddRepo(repo);
```

Create the schema at startup with Code First generation, then seed only when the store is empty. See [Repository Generation](RepositoryGeneration.md).

## Using IRepo in Handlers

Handlers deriving from `BaseHandlerWithRepo` get the repo as their `Repo` property:

```csharp
public sealed class OrdersQueryHandler : BaseHandlerWithRepo, IOrdersQueryHandler
{
    public async Task<OrderModel[]> GetOrders(Guid customerID, CancellationToken cancellationToken)
    {
        var orders = await Repo.ManyAsync<OrderDataModel>(
            x => x.CustomerID == customerID,
            QueryOrder<OrderDataModel>.Create(x => x.PlacedOn, true),   // true: descending
            new Graph<OrderDataModel>(true, x => x.Lines));
        return orders.Select(ToModel).ToArray();
    }
}
```

### Queries

| Method | Returns |
|---|---|
| `Single<T>(where, graph?)` | the one matching model, or `null` |
| `First<T>(where?, order?, graph?)` | the first matching model, or `null` |
| `Many<T>(where?, order?, skip?, take?, graph?)` | the matching models |
| `Any<T>(where?)` | whether any match |
| `Count<T>(where?)` | how many match |

### Changes

| Method | Does |
|---|---|
| `Create<T>(model)` | inserts |
| `Update<T>(model, graph?)` | updates, only the graph's columns when one is given |
| `Delete<T>(model)` | deletes |
| `DeleteByID<T>(id)` | deletes by key |

Every method has an `Async` version. Use the async ones in handlers. Pass collections to `Create`, `Update`, and `Delete` as arrays or with an explicit type argument, since a `List<T>` binds to the single-model overload.

### LINQ Support

`where` expressions support:

- **Operators:** comparisons, arithmetic, `&&`, `||`, `!`, bool members, `??`, integer bitwise operators, `array.Contains(x.Prop)`, and `HasValue`/`Value` on nullables.
- **Strings:** `Contains`, `StartsWith`, `EndsWith` (wildcards in the text are matched literally, and a `StringComparison` ignoring case is honored), `Equals`, `string.IsNullOrEmpty`/`IsNullOrWhiteSpace`, `Length`, `ToUpper`/`ToLower`, `Trim`/`TrimStart`/`TrimEnd`, `Substring`, `IndexOf`, `Replace`, `string.Concat`, and `+`.
- **Math:** `Math.Abs`/`Ceiling`/`Floor`/`Round`/`Pow`/`Sqrt`. SQL rounds midpoints away from zero.
- **Dates and times:** parts such as `.Year`, `.Date`, and `.DayOfWeek` on `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, and `TimeSpan`, including `TotalHours` and the other totals.
- **Related collections:** `Any`, `All`, `Count`/`LongCount`, and `Sum`/`Min`/`Max`/`Average` with a selector.

Anything that doesn't use the model, such as `DateTime.Now.Date`, is evaluated before the query. Comparisons with null columns give the same results as C#, so `x.Maybe != 4` includes rows where `Maybe` is null on every engine. Case sensitivity of `==`, `Contains`, `StartsWith`, and `EndsWith` follows the database collation, and `Trim`/`IsNullOrWhiteSpace` only trim spaces.

### Relations and Partial Updates

Relations load only when a [Graph](Graph.md) names them. Pass `true` first to include the model's own columns too:

```csharp
var orders = await Repo.ManyAsync<OrderDataModel>(new Graph<OrderDataModel>(true, x => x.Customer, x => x.Lines));

await Repo.UpdateAsync(order, new Graph<OrderDataModel>(x => x.Status));   // writes only Status
```

With `PersistLinking => false`, related models aren't saved with their parent: create the order, then its lines.

## Event-Sourced Aggregates

An `AggregateRoot` keeps its state as a stream of events in an event store instead of a table. Each event implements `IAggregateEvent` and is applied by a public `On` method taking it. `Append` stores an event and then applies it, and `Rebuild` replays the stream.

```csharp
public sealed class CartAggregate : AggregateRoot
{
    private readonly List<CartItem> items = new();
    public IReadOnlyList<CartItem> Items => items;

    public CartAggregate(Guid customerID, IEventStoreEngine eventStore) : base(customerID, eventStore) { }

    public Task On(CartItemAddedEvent @event)
    {
        items.Add(new CartItem { ProductID = @event.ProductID, Quantity = @event.Quantity });
        return Task.CompletedTask;
    }
}

// in a command handler
var cart = new CartAggregate(command.CustomerID, Context.GetService<IEventStoreEngine>());
await cart.Rebuild();                             // false if the stream has no events
if (cart.Items.Count >= 50)
    throw new InvalidOperationException("The cart is full");   // validate before appending: stored events are replayed as they are
await cart.Append(new CartItemAddedEvent { ProductID = command.ProductID, Quantity = command.Quantity }, validateEventNumber: true);
```

- `validateEventNumber: true` rejects the append if another command changed the stream since this instance rebuilt it.
- `Rebuild(maxEventNumber, maxEventDate)` rebuilds the state as of an earlier point, and `RebuildOneEvent()` steps forward one event at a time.
- `Delete` appends a terminating event. `IsCreated`, `IsDeleted`, `LastEventNumber`, `LastEventDate`, and `LastEventName` describe the stream.

Aggregate events are the aggregate's state, not bus messages: they never implement `IEvent` and live with the aggregate in the service project. See [Aggregate Events Are Not CQRS Events](Events.md#aggregate-events-are-not-cqrs-events). `Demo/Store/Store.Carts` is a complete example on KurrentDB, with an in-memory fallback.

## See Also

- [Repository Generation](RepositoryGeneration.md) - Creating the schema
- [Graph](Graph.md) - Selecting relations and columns
- [Service Injection](ServiceInjection.md) - `AddRepo` and `BaseHandlerWithRepo`
