[← Back to Documentation](Index.md)

# Repository

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
| **Maturity** | ⚠️ Newer, small community | ✅ Mature, large community |

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

    [StoreIndex]                                // index this column
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
- `[StoreIndex]` indexes a column in the SQL engines; the in-memory and KurrentDB engines ignore it. Members with the same `[StoreIndex("Name")]` form one combined index, ordered by `Order` and then by declaration. `Unique = true` allows any number of rows with a null in the index. Indexed strings and `byte[]` need a length from `[StoreProperties]`. Without `NoDelete`, code first generation drops indexes that aren't in the model, except primary keys, unique constraints, and the indexes MySQL and MariaDB keep for foreign keys.

## Setup

### 1. Engines

An engine is a store. Create the one for your database with its connection string: `MsSqlEngine`, `PostgreSqlEngine`, `MySqlEngine`, `MariaDbEngine`, `KurrentDBEngine` (an event store), or `MemoryEngine`.

```csharp
var engine = new MsSqlEngine(connectionString);   // doesn't connect until it's used
```

Each database also has a connection test: `MsSqlConnectionTest`, `PostgreSqlConnectionTest`, `MySqlConnectionTest`, `MariaDbConnectionTest`, and `KurrentDBConnectionTest`. `Test` returns whether the server answered, and if you pass a logger it logs why it didn't. Test at startup to choose the engine the providers get (see step 3), the same way `RabbitMQConnectionTest.TestAsync` chooses a message transport.

### 2. A Provider

A provider connects a model type to an engine. Register a `TransactStoreProvider<TModel>` for each model (step 3).

Each in-memory engine is its own store, so a test can start from an empty one with `new MemoryEngine()`. Related models can use different engines: graphs load relations through each model's own provider. A `where` that reaches into a relation, such as `x.Customer.Name == name`, becomes one SQL statement, so it needs the related table in the same database.

`EventStoreAsTransactStoreProvider<TModel>` stores a model in an event store (KurrentDB or `MemoryEngine`) instead of a table. Each change is an event, so the model's history can be read with the `Temporal` and `Event` queries, and queries must name the model's identity. Every 100 events it saves the model's state so reads don't replay the whole stream; pass `saveStateEvery` to change that, or 0 to never save it.

### 3. Register the Repo

```csharp
// memory as a fallback when the database isn't running
ITransactStoreEngine ordersEngine = MsSqlConnectionTest.Test(ordersConnectionString, log)
    ? new MsSqlEngine(ordersConnectionString)
    : new MemoryEngine();

var repo = Repo.New();
repo.AddProvider(new TransactStoreProvider<OrderDataModel>(ordersEngine));
repo.AddProvider(new TransactStoreProvider<OrderLineDataModel>(ordersEngine));
repo.AddProvider(new TransactStoreProvider<ProductDataModel>(new PostgreSqlEngine(catalogConnectionString)));   // a different store, same IRepo


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

### Building a Where in Code

For filters chosen at run time, such as optional search fields, use `WhereBuilder<T>` from `Zerra.Linq`:

```csharp
var where = new WhereBuilder<OrderDataModel>(x => x.CustomerID == customerID);
if (status is not null)
    where.And(x => x.Status == status);
if (search is not null)
{
    where.StartGroupAnd();                               // && ( ... )
    where.And(x => x.OrderNumber!.Contains(search));
    where.Or(x => x.Customer!.Name!.Contains(search));
    where.EndGroup();
}

var orders = await Repo.ManyAsync<OrderDataModel>(where.Build());
```

Conditions combine in the order they are added, not by C# precedence: `a`, then `Or(b)`, then `And(c)` means `(a || b) && c`. Use `StartGroupAnd`/`StartGroupOr` and `EndGroup` for parentheses. The first condition inside a group can use either `And` or `Or`.

To join two existing expressions, `LinqAppender.AppendAnd<T>(first, second)` returns `first && second`. To log an expression, `LinqStringConverter.Convert(expression)` (or `where.ToString()`) writes it as readable C#-like text.

### Relations and Partial Updates

Relations load only when a [Graph](Graph.md) names them. Pass `true` first to include the model's own columns too:

```csharp
var orders = await Repo.ManyAsync<OrderDataModel>(new Graph<OrderDataModel>(true, x => x.Customer, x => x.Lines));

await Repo.UpdateAsync(order, new Graph<OrderDataModel>(x => x.Status));   // writes only Status
```

Related models named in the graph are saved or deleted with their parent.

### History Queries

Models stored with `EventStoreAsTransactStoreProvider` keep every change, and these read it:

| Method | Returns |
|---|---|
| `TemporalMany`/`TemporalFirst`/`TemporalAny`/`TemporalCount<T>` | the model as it was after each change |
| `EventMany`/`EventFirst`/`EventSingle`/`EventAny`/`EventCount<T>` | each change as an `EventModel<T>` |

Each takes a `TemporalOrder` (`Newest` or `Oldest` first), a range by date (`temporalDateFrom`, `temporalDateTo`) or by event number (`temporalNumberFrom`, `temporalNumberTo`), and `temporalSkip`/`temporalTake` over the events, then the `where` and, for the methods returning models, `order`, `skip`, `take`, and `graph`:

```csharp
var lastWeek = await Repo.EventManyAsync<OrderDataModel>(TemporalOrder.Newest, DateTime.UtcNow.AddDays(-7), null, null, null, x => x.ID == orderID);
foreach (var change in lastWeek)
    Console.WriteLine($"{change.Number} {change.Date} {change.EventName} -> {change.Model.Status}");
```

`EventModel<T>` has `Number`, `Date`, `EventName`, `Deleted`, `Model` (the full model after the change), `ModelChange` and `GraphChange` (what was written), and `Source`/`SourceType` (what started the change, when it was recorded).

## Provider Layers

A layer wraps another provider and is registered in its place. Layers can stack, such as rules over encryption over the table.

### Rules

Derive from `BaseTransactStoreRuleProvider` to filter every query and check or change models before they are saved:

```csharp
public sealed class OrderRules(ITransactStoreEngine engine)
    : BaseTransactStoreRuleProvider<ITransactStoreProvider<OrderDataModel>, OrderDataModel>(new TransactStoreProvider<OrderDataModel>(engine))
{
    public override LambdaExpression? WhereExpression(Graph? graph)
        => (Expression<Func<OrderDataModel, bool>>)(x => x.Status != "Archived");   // added to every query

    protected override IEnumerable OnCreate(IEnumerable models, Graph? graph)
    {
        foreach (OrderDataModel model in models)
        {
            if (String.IsNullOrEmpty(model.OrderNumber))
                throw new ArgumentException("Order number is required");
        }
        return models;
    }
}

repo.AddProvider(new OrderRules(engine));
```

| Override | Runs |
|---|---|
| `WhereExpression` | on every query, joined to its `where` with `&&` |
| `OnQuery` | before a query, to change the graph |
| `OnGet` | after models are read, to filter or change them |
| `OnCreate`/`OnUpdate`/`OnDelete` | before saving, to check, change, or filter what is saved |
| `OnCreateComplete`/`OnUpdateComplete`/`OnDeleteComplete` | after saving |

### Encryption and Compression

`BaseTransactStoreEncryptionProvider` encrypts string and `byte[]` columns before they are stored and decrypts them when read. `BaseTransactStoreCompressionProvider` does the same with GZip:

```csharp
public sealed class CustomerEncryption(ITransactStoreProvider<CustomerDataModel> next)
    : BaseTransactStoreEncryptionProvider<ITransactStoreProvider<CustomerDataModel>, CustomerDataModel>(next)
{
    public override IEncryptor Encryptor => encryptor;                                     // a fixed key
    public override Graph<CustomerDataModel>? Properties => new(x => x.Email);           // null: every string and byte[]
}

repo.AddProvider(new CustomerEncryption(new TransactStoreProvider<CustomerDataModel>(engine)));
```

- Stored values are encrypted or compressed, so don't use those columns in a `where` or `order`: they would compare the stored bytes.
- Stored values are longer than the plain text, so size the columns for them.
- Use a fixed key. A `ZerraEncryptor` with an `ISymmetricKeySource` is rejected, since stored data outlives the keys it rotates through. See [Encryptors](Encryptors.md).
- Override `Enabled` to turn a layer off without removing it.

### A Table and an Event Store Together

Derive from `BaseDualEventStoreProvider` to save each change to both an event store and a table. History queries read the event store, and all other queries read the table:

```csharp
public sealed class OrderProvider(IEventStoreEngine eventStore, ITransactStoreEngine database)
    : BaseDualEventStoreProvider<ITransactStoreProvider<OrderDataModel>, ITransactStoreProvider<OrderDataModel>, OrderDataModel>(
        new EventStoreAsTransactStoreProvider<OrderDataModel>(eventStore),
        new TransactStoreProvider<OrderDataModel>(database))
{
}
```

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

## Byte Store

`ByteStoreProvider` saves and reads named streams, such as files, through an `IByteStoreEngine` you write for your storage (a folder, blob storage, and so on). The engine has `Get`, `Save`, and `Exists`, each with an async version. Derive layers from `BaseByteStoreLayerProvider` to change the streams on the way in and out, such as encrypting them. The byte store isn't part of `IRepo`; register the `IByteStoreProvider` as a service.

```csharp
var files = new ByteStoreProvider(new FolderByteStoreEngine(path));   // your IByteStoreEngine
await files.SaveAsync("invoice-1001.pdf", stream);
using var saved = await files.GetAsync("invoice-1001.pdf");
```

## See Also

- [Repository Generation](RepositoryGeneration.md) - Creating the schema
- [Graph](Graph.md) - Selecting relations and columns
- [Service Injection](ServiceInjection.md) - `AddRepo` and `BaseHandlerWithRepo`
