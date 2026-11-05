[← Back to Documentation](Index.md)

# Repository

`Zerra.Repository` is LINQ data access that doesn't depend on the data store. Handlers read and write data models through one `IRepo` interface, and each model type can live in a different store: SQL Server, PostgreSQL, MySQL, MariaDB, or in memory. It also supports event-sourced aggregates on KurrentDB. It is AOT compatible.

## Compared with Entity Framework

It replaces Entity Framework in Zerra services, with less overhead and the same code on every store.

| | Zerra Repository | Entity Framework |
|---|---|---|
| **Native AOT** | ✅ Fully compatible | ⚠️ Limited support |
| **Data source per model** | ✅ Each model can use a different store | ❌ All models share one `DbContext` |
| **Switching stores** | ✅ Pass a different engine at startup | ❌ Significant refactoring |
| **Bus integration** | ✅ `BaseHandlerWithRepo` | ❌ Manual wiring |
| **Change tracking** | ✅ None; every operation is explicit | ⚠️ Automatic, with overhead |
| **Loading related data** | ✅ Named in a graph or the `Select` | ⚠️ `.Include()` chains |
| **Query complexity** | ✅ LINQ, including joins of any models and operations by position, even across stores | ✅ LINQ, including joins of any models |
| **Migrations** | ⚠️ Code First generation, no migration history | ✅ Rich migration tooling |
| **Maturity** | ⚠️ Newer, small community | ✅ Mature, large community |

Against SQL Server, with `AsNoTracking()` and a new `DbContext` per query on the EF side, BenchmarkDotNet's median times put Zerra at approximately:

| Scenario | Speed | Memory |
|---|---|---|
| Query many (all rows) | ~2.2× faster | ~45% less |
| Query many with a where clause | ~1.7× faster | ~75% less |
| Query first with a where clause | ~1.9× faster | ~75% less |
| Query many with a one-to-one relation | ~3.3× faster | ~45% less |
| Query many with a one-to-many relation | ~6.3× faster | ~85% less |
| Update | ~2.2× faster | ~90% less |
| Create 1,000 models in one call | ~7.9× faster | ~85% less |
| Stream 10,000 rows | ~1.7× faster | ~25% less |

For one-to-many includes, Zerra loads the related rows of every parent in one additional query, while EF joins them into one query by default. With EF's `AsSplitQuery()`, which loads them separately like Zerra, Zerra is ~2× faster with ~65% less memory. Reading 10,000 rows as a stream takes about 20% less time than reading them into a list, and a rule layer's chunks add no measurable time. Measured November 2, 2026. Results vary by environment. The benchmarks are in [`EFBenchmark.cs`](../Benchmarks/Zerra.Repository.Benchmark/Benchmarks/EFBenchmark.cs) and [`StreamBenchmark.cs`](../Benchmarks/Zerra.Repository.Benchmark/Benchmarks/StreamBenchmark.cs).

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

The repository packages target .NET 10 and .NET 11 only.

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
- A model identified by several `[Identity]` members is referred to by as many foreign key members, named in the order of its identities: `[Relation(nameof(Region), nameof(OrderNumber))]`. It works wherever a relation does, and code first generation adds a foreign key constraint of all its columns.
- Without a precision, PostgreSQL, MySQL, and MariaDB store date and time columns to the microsecond, and SQL Server stores `DateTime` as `datetime` (about 3 ms). Set `[StoreProperties(notNull, precision)]` to choose.
- An enum is stored as text: its name, or the text from `[EnumName]`. The column is as long as the longest name unless `[StoreProperties]` sets a length, so an enum can be indexed. A value the enum doesn't name can't be saved.
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

Each in-memory engine is its own store, so a test can start from an empty one with `new MemoryEngine()`. Related models can use different engines: graphs load relations through each model's own provider, and a `where` that reaches into a relation on another engine is split. Its part on the related model, such as `x.Customer.Name == name` or `x.Lines.Any(l => l.Quantity > 1)`, runs on that engine first and the identities it matched filter this one. `Count`, `Sum`, `Average`, `Min`, and `Max` of a related collection compared to a value work the same way, with a model that has nothing related counting as zero. One part of a condition can't mix the two sides, such as `x.Quantity > x.Customer.Limit`. An aggregate key or value, a projected member, or an order through such a relation can't be split, so the matching models load with just the members it reads, the relation through its own provider, and the aggregate, projection, or ordering and paging run in memory. That is the work a handler would do by hand, so keep it off large tables. Related models on the same engine run as one statement.

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
    public async Task<OrderSummary[]> GetOrders(Guid customerID, CancellationToken cancellationToken)
    {
        return await Repo.Query<OrderDataModel>()
            .Where(x => x.CustomerID == customerID)
            .OrderByDescending(x => x.PlacedOn)
            .Select(x => new OrderSummary { ID = x.ID, Lines = x.Lines!.Select(l => new LineSummary { Quantity = l.Quantity }).ToArray() })
            .ToArrayAsync(cancellationToken);
    }
}
```

### Queries

`Repo.Query<T>()` reads data models through LINQ, converted by the store in one pass. See [Repository Queries](RepositoryQueries.md) for the LINQ functions, joins, relations, aggregates, and history queries.

```csharp
var order = await Repo.Query<OrderDataModel>().SingleOrDefaultAsync(x => x.ID == orderID, cancellationToken);
```

### Changes

| Method | Does |
|---|---|
| `Create<T>(model)` | inserts |
| `Update<T>(model, graph?)` | updates, only the graph's columns when one is given |
| `Delete<T>(model)` | deletes |
| `DeleteByID<T>(id)` | deletes by key |
| `Update<T>(where, model, graph)` | sets the graph's members on every matching row without loading them, and returns how many |
| `Update<T>(where, x => new T { … })` | sets members on every matching row to values computed from the row, such as `Price = x.Price * 1.1m`, and returns how many |
| `Delete<T>(where)` | deletes every matching row without loading them, and returns how many |

Every method has an `Async` version. Use the async ones in handlers. Pass collections to `Create`, `Update`, and `Delete` as arrays or with an explicit type argument, since a `List<T>` binds to the single-model overload.

### Transactions

`BeginTransaction` groups work so it all saves or none of it does. Disposing without committing rolls back, so an exception undoes the work:

```csharp
await using var transaction = Repo.BeginTransaction();
await Repo.CreateAsync(order, cancellationToken);
await Repo.UpdateAsync<Stock>(x => x.ID == stockID, x => new Stock { Count = x.Count - 1 }, cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

Everything you read and write through the repository in that flow of work is in the transaction, and it reads its own writes. Pass an `IsolationLevel` to `BeginTransaction` to change it from the engine's default.

`Rollback` undoes the work so far and leaves the transaction open, so what comes next is new work in it to commit or roll back. `Commit` ends the transaction.

Creating or updating a list of models in one call saves them together or not at all without one. Related models in a graph are saved separately, so use a transaction to include them.

- **Begin and commit in the same method.** Starting one inside an async method doesn't carry over to the caller, as it ends with the method.
- **One at a time.** Beginning a second inside the first throws. Parallel calls inside one take turns, as they share a connection.
- **Streams:** a stream read with `AsAsyncEnumerable` or `AsEnumerable` holds the connection until its loop ends, so another call to the same engine or a commit inside the loop throws. A rule's `OnGet` runs while the stream reads too, so it can't query the same engine in a transaction either. A plain `foreach` over the query reads everything first, so writing inside it works.
- **After it finishes,** the flow that began it carries on outside it. A background task it started that still uses it gets an error.
- **Handlers:** a handler the bus runs is its own unit of work, even when it runs in the same process.
- **Layers:** a layer's `OnCreateComplete` and similar methods run before the commit, so a layer that caches shouldn't keep what it sees until the work is committed.
- **After a rollback,** a model keeps any identity it was given. A deadlock throws, and retrying the whole transaction is up to you.

SQL Server, PostgreSQL, MySQL, and MariaDB use the database's transaction, on InnoDB tables for MySQL and MariaDB. The in-memory engine works on a private copy and applies the writes at commit, so others don't see them until then. Its row counts come from that copy, and it can throw at commit if another writer took the same generated identity. Event stores can't take part: writing to one in a transaction throws.

**Across engines:** each engine used joins, and they roll back together on any failure before the commit. The commit itself runs one engine at a time, in the order they were first used and with the in-memory engine last, so a failure partway leaves the work partly saved. That throws `RepoPartialCommitException`, which lists the engines that did and didn't commit. A transaction on one engine is all or nothing.

### Changing Many Rows

An update or delete by condition is one statement in the database, so nothing is read first:

```csharp
int archived = await Repo.UpdateAsync<OrderDataModel>(x => x.PlacedOn < cutoff && x.Status == "Closed", new OrderDataModel { Status = "Archived" }, new Graph<OrderDataModel>(x => x.Status));
int repriced = await Repo.UpdateAsync<ProductDataModel>(x => x.CategoryID == categoryID, x => new ProductDataModel { Price = x.Price * 1.1m, UpdatedOn = DateTime.UtcNow });
int removed  = await Repo.DeleteAsync<OrderLineDataModel>(x => x.OrderID == orderID);
```

- The graph says which members take the model's values, so it is required and can't name the identity or a relation. The other members of the model are ignored.
- A computed update sets each member from the row as the database reads it, so `Total = x.Price * x.Quantity` is one statement. A value takes what a `where` takes of the model's own members, not a relation, and can't set the identity or a relation.
- Zero matching rows is a normal result of 0, not an error.
- The condition takes what a `where` takes. One through a related model becomes a subquery selecting the identities.
- A rule's `WhereExpression` is added to the condition. Rules see the condition in `OnUpdateWhere`/`OnDeleteWhere` instead of the rows in `OnUpdate`/`OnDelete`, since the rows aren't loaded.
- An encryption or compression layer encrypts the values it sets and refuses a condition on one of its properties, and refuses a computed update that reads or sets one.
- Related models named in a persist graph aren't deleted along with the rows, as they are with `Delete(model, graph)`; delete them first with their own condition.
- On an event store, the condition names the identities as every event store query does, and each matching stream gets the change as an event.

### Relations and Partial Updates

A [Graph](Graph.md) says what a change writes. A graph or a [`Select`](RepositoryQueries.md#selecting-and-loading-relations) says what a query reads.

```csharp
await Repo.UpdateAsync(order, new Graph<OrderDataModel>(x => x.Status));   // writes only Status
```

Related models named in the graph are saved or deleted with their parent.

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
| `OnGet` | after models are read, to filter or change them, once per chunk when they're streamed |
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
- `Delete` appends a terminating event and applies it like any other, and `Rebuild` applies it too. `IsCreated`, `IsDeleted`, `LastEventNumber`, `LastEventDate`, and `LastEventName` describe the stream.

Aggregate events are the aggregate's state, not bus messages: they never implement `IEvent` and live with the aggregate in the service project. See [Aggregate Events Are Not CQRS Events](Events.md#aggregate-events-are-not-cqrs-events). `Demo/Store/Store.Carts` is a complete example on KurrentDB, with an in-memory fallback.

## Byte Store

`ByteStoreProvider` saves and reads named streams, such as files, through an `IByteStoreEngine` you write for your storage (a folder, blob storage, and so on). The engine has `Get`, `Save`, and `Exists`, each with an async version. Derive layers from `BaseByteStoreLayerProvider` to change the streams on the way in and out, such as encrypting them. The byte store isn't part of `IRepo`; register the `IByteStoreProvider` as a service.

```csharp
var files = new ByteStoreProvider(new FolderByteStoreEngine(path));   // your IByteStoreEngine
await files.SaveAsync("invoice-1001.pdf", stream);
using var saved = await files.GetAsync("invoice-1001.pdf");
```

## See Also

- [Repository Queries](RepositoryQueries.md) - LINQ functions, joins, relations, and aggregates
- [Repository Generation](RepositoryGeneration.md) - Creating the schema
- [Graph](Graph.md) - Selecting relations and columns
- [Service Injection](ServiceInjection.md) - `AddRepo` and `BaseHandlerWithRepo`
