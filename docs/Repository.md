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
| **Loading related data** | ✅ Named in a graph or the `Select` | ⚠️ `.Include()` chains |
| **Query complexity** | ⚠️ Filters, calculations, groupings, and projections, through relations, even across stores | ✅ Any LINQ, including joins between unrelated models |
| **Migrations** | ⚠️ Code First generation, no migration history | ✅ Rich migration tooling |
| **Maturity** | ⚠️ Newer, small community | ✅ Mature, large community |

Against SQL Server, with `AsNoTracking()` and a new `DbContext` per query on the EF side, BenchmarkDotNet's median times put Zerra at approximately:

| Scenario | Speed | Memory |
|---|---|---|
| Query many (all rows) | ~45% faster | ~40% less |
| Query many with a where clause | ~1.8× faster | ~65% less |
| Query first with a where clause | ~1.9× faster | ~65% less |
| Query many with a one-to-one relation | ~30% faster | ~15% less |
| Query many with a one-to-many relation | ~4.5× faster | ~80% less |
| Update | ~40% faster | ~95% less |

For one-to-many includes, Zerra loads the related rows of every parent in one additional query, while EF joins them into one query by default. With EF's `AsSplitQuery()`, which loads them separately like Zerra, Zerra is ~25% faster with ~40% less memory. Results vary by environment. The benchmarks are in [`EFBenchmark.cs`](../Benchmarks/Zerra.Repository.Benchmark/Benchmarks/EFBenchmark.cs).

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

Each in-memory engine is its own store, so a test can start from an empty one with `new MemoryEngine()`. Related models can use different engines: graphs load relations through each model's own provider, and a `where` that reaches into a relation on another engine is split. Its part on the related model, such as `x.Customer.Name == name` or `x.Lines.Any(l => l.Quantity > 1)`, runs on that engine first and the identities it matched filter this one. `Count`, `Sum`, `Average`, `Min`, and `Max` of a related collection compared to a value work the same way, with a model that has nothing related counting as zero. One part of a condition can't mix the two sides, such as `x.Quantity > x.Customer.Limit`. A calculation key or value, a projected member, or an order through such a relation can't be split, so the matching models load with just the members it reads, the relation through its own provider, and the calculation, projection, or ordering and paging run in memory. That is the work a handler would do by hand, so keep it off large tables. Related models on the same engine run as one statement.

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

`Query<T>()` starts an `IQueryable<T>` that the store converts in one pass: one statement over the model, and one more for each related collection the `Select` reads.

```csharp
var order = await Repo.Query<OrderDataModel>().SingleOrDefaultAsync(x => x.ID == orderID);

var recent = await Repo.Query<OrderDataModel>()
    .Where(x => x.CustomerID == customerID && x.Status == "Placed")
    .OrderByDescending(x => x.PlacedOn)
    .Skip(20).Take(20)
    .Select(x => new
    {
        x.ID,
        Customer = x.Customer!.Name,
        Total = x.Lines!.Sum(l => l.UnitPrice * l.Quantity),
        Lines = x.Lines!.OrderBy(l => l.Position).Select(l => new { l.ProductID, l.Quantity }).ToList(),
    })
    .ToArrayAsync();

var names = Repo.Query<ProductDataModel>().Where(x => x.Active).Select(x => x.Name).Distinct().ToArray();
var owed = await Repo.Query<InvoiceDataModel>().Where(x => !x.Paid).SumAsync(x => x.Amount);
var sales = Repo.Query<OrderLineDataModel>().GroupBy(x => x.ProductID)
    .Select(g => new { ProductID = g.Key, Lines = g.Count(), Revenue = g.Sum(l => l.UnitPrice * l.Quantity) })
    .Where(r => r.Lines >= 10).OrderByDescending(r => r.Revenue).Take(20).ToArray();
```

- `Where`, `OrderBy`/`OrderByDescending`/`ThenBy`/`ThenByDescending`, `Skip`, `Take`, `Select`, `Distinct`, and `GroupBy` compose the query. Filter and order before `Select` and before paging.
- End it by enumerating it, or with `First`, `FirstOrDefault`, `Single`, `SingleOrDefault`, `Count`, `LongCount`, `Any`, `All`, `Sum`, `Min`, `Max`, or `Average`, with a condition or value in the call or selected before it. `First` and `Single` throw when there is nothing, as LINQ does. The `Async` versions, `ToArrayAsync`, `ToListAsync`, `FirstAsync`, `CountAsync`, `SumAsync`, and the rest, are in `RepoQueryableExtensions`.
- `Last` and `Reverse` need an `OrderBy`, and the store reads that order the other way round.
- `ElementAt` skips to its place in the store, and `Count` or a calculation after `Skip` or `Take` is of that page.
- `Select(x => x.ID).Contains(id)` looks for one value.
- `SelectMany(x => x.Lines)` goes over to the related model, and what follows is a query of it. The rows have no order unless you add one after it. `SelectMany(x => x.Lines, (x, l) => new { x.Name, l.Quantity })` reads the model through the relation back to it, so that relation has to exist.
- `Where(x => ids.Contains(x.ID))` where `ids` is a repository query selecting one value is one statement in the same store, and reads `ids` first otherwise. It works in a `Select` value and in an update or delete by condition too. It can't read the outer model.
- A `Where` or `OrderBy` after a `Select` is written back onto the model, from what the `Select` set, so it has to read members the `Select` set and come before paging.
- `ToHashSetAsync` and `ToDictionaryAsync` build their result after the rows are read, so their selectors run in memory.
- A call the store can't run, such as `Count` after `GroupBy`, throws `NotSupportedException` instead of running in memory.
- The `Async` endings, the [history queries](#history-queries), and the [changes](#changes) take a `CancellationToken` last. Cancelling stops the statement in the store and throws `OperationCanceledException`, whichever engine it's on. A change of several rows or relations keeps what it already wrote, as there is no transaction around them.
- With Entity Framework in the same file, the `Async` endings of both are in scope, so call EF's through `EntityFrameworkQueryableExtensions`.

### Selecting and Loading Relations

`Select` builds an anonymous object, a class with a constructor or settable members, or the model itself, nested as deep as needed, or picks one value such as `x => x.Name`.

- A member through a single relation, `x.Customer!.Name`, joins.
- A related collection, whole or after `Where`, `OrderBy`, `Skip`, `Take`, and a `Select` of its rows, is read by one more statement for all the models at once, and a `Skip` or `Take` is per model, into an array, a `List<T>`, or a collection interface.
- A single related model read whole, `x.Customer`, joins too, and is `null` when there's no related row. One whose relation has relations of its own to read uses one more statement instead.
- A row's result is built from the row alone, not the model. It can have collections of its own, as long as no row's model repeats one above it, so a model with a collection of itself nests one level.

To load models with their relations, give `Query` a [Graph](Graph.md) instead of a `Select`:

```csharp
var orders = await Repo.Query<OrderDataModel>(new Graph<OrderDataModel>(true, x => x.Customer, x => x.Lines))
    .Where(x => x.Status == "Open")
    .OrderBy(x => x.PlacedOn)
    .ToArrayAsync();

var totals = await Repo.Query<OrderDataModel>(new Graph<OrderDataModel>(x => x.ID, x => x.Lines!.Select(l => l.Quantity))).ToArrayAsync();
```

- `true` reads every member. A relation loads only when the graph names it.
- Naming a relation reads it whole. `x => x.Lines!.Select(l => l.Quantity)` reads only that member of it, and models read exactly what the graph names, so add the keys if you need them.
- Relations are read the same way as a `Select`, and the condition, order, and paging still run in the store.
- A query has a graph or a `Select`, not both, and a `GroupBy` has no graph. A `Count`, `Any`, or calculation doesn't need one.

### Calculations

The database does the arithmetic and returns only the results:

```csharp
decimal revenue = await Repo.Query<OrderLineDataModel>().Where(x => x.OrderID == orderID).SumAsync(x => x.UnitPrice * x.Quantity);
double rating   = await Repo.Query<ReviewDataModel>().Where(x => x.ProductID == productID).AverageAsync(x => x.Rating);   // an average of integers is a double
DateTime? last  = await Repo.Query<OrderDataModel>().MaxAsync(x => x.ClosedOn);
int customers   = await Repo.Query<OrderDataModel>().Select(x => x.CustomerID).Distinct().CountAsync();

var perStatus = await Repo.Query<OrderDataModel>().GroupBy(x => x.Status).Select(g => new { g.Key, Rows = g.Count() }).ToArrayAsync();
var perPair   = await Repo.Query<OrderDataModel>().GroupBy(x => new { x.CustomerID, x.Status }).Select(g => new { g.Key.CustomerID, g.Key.Status, Rows = g.Count() }).ToArrayAsync();
```

- The value and the key take what a `where` takes, including arithmetic, date parts, and related models, which become joins or subqueries.
- The results follow LINQ: `Sum` of nothing is 0, `Average`/`Min`/`Max` of nothing are `null` or throw when the type can't be null, and nulls are skipped. A null key is a group.
- A group's `Select` builds from its whole `Key`, a part of it such as `Key.CustomerID`, `Count()`, `Count(x => condition)`, `Select(x => x.Member).Distinct().Count()`, or `Sum`, `Average`, `Min`, or `Max` of a value. After it, a `Where` is the having, and an order and paging apply to the groups.
- Groups come back in no particular order.
- On a `MemoryEngine`, string keys that differ only in case are one group unless the engine is case sensitive, like its `where`.
- Under Native AOT, anonymous objects in a `Select` or a group key work, but the compiler warns `IL2026` at that line about reading their members by reflection, which this doesn't rely on. `new ValueTuple<Guid, string?>(x.CustomerID, x.Status)` is the same group key without the warning, read back as `Key.Item1` and `Key.Item2`, or `Select` into a class.

### Layers and Event Stores

- A rule's `WhereExpression` applies to a query, and its `OnGet` to the models given back.
- An encryption or compression layer refuses a `Select` or order of one of its properties, and gives the models back plain. A graph reads them, and only the ones it names are decrypted or decompressed.
- A member, an order, or a graph's relation on another engine, or on a related model with its own layers, loads the models with what the query reads and runs the query over them in memory.
- An event store runs the query over the current state of its models. Each change it kept is read with the [History Queries](#history-queries).

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

- **Begin and commit in the same method.** Starting one inside an async method doesn't carry over to the caller, as it ends with the method.
- **One at a time.** Beginning a second inside the first throws. Parallel calls inside one take turns, as they share a connection.
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
- The condition takes what a `where` takes. One through a related model becomes a subquery selecting the identities, which needs a single identity.
- A rule's `WhereExpression` is added to the condition. Rules see the condition in `OnUpdateWhere`/`OnDeleteWhere` instead of the rows in `OnUpdate`/`OnDelete`, since the rows aren't loaded.
- An encryption or compression layer encrypts the values it sets and refuses a condition on one of its properties, and refuses a computed update that reads or sets one.
- Related models named in a persist graph aren't deleted along with the rows, as they are with `Delete(model, graph)`; delete them first with their own condition.
- On an event store, the condition names the identities as every event store query does, and each matching stream gets the change as an event.

### LINQ Support

A `Where`, and the values of a `Select` and a group, support:

- **Operators:** comparisons, arithmetic, `&&`, `||`, `!`, bool members, `??`, integer bitwise operators, `array.Contains(x.Prop)`, and `HasValue`/`Value` on nullables.
- **Strings:** `Contains`, `StartsWith`, `EndsWith` (wildcards in the text are matched literally, and a `StringComparison` ignoring case is honored), `Equals`, `string.IsNullOrEmpty`/`IsNullOrWhiteSpace`, `Length`, `ToUpper`/`ToLower`, `Trim`/`TrimStart`/`TrimEnd`, `Substring`, `IndexOf`, `Replace`, `string.Concat`, and `+`.
- **Text:** `ToString()` on numbers, bools, enums, and Guids, and with a format on dates (a custom one such as `yyyy-MM-dd`) and numbers (`F2`, `D4`), `PadLeft`/`PadRight`, `string.Join` of `new[] { .. }`, of a related collection, or of a group's values, and `int.Parse` and the other number parsers and `Guid.Parse`. A date without a format, or a culture, is refused because the store doesn't have one.
- **Conversions:** `Convert.ToInt32` and the other number conversions, `ToString`, and `ToBoolean`. A midpoint rounds to the even number, as in C#.
- **Enums:** comparing, ordering, `Min`, `Max`, and casting to a number use the enum's numbers, as in C#, and `ToString` gives the field's name. A `[Flags]` enum can be read, saved, and compared for equality, but sorting it, casting it to a number, or calling `ToString` in the query is refused.
- **Math:** `Math.Abs`/`Ceiling`/`Floor`/`Round`/`Pow`/`Sqrt`. SQL rounds midpoints away from zero.
- **Dates and times:** parts such as `.Year`, `.Date`, and `.DayOfWeek` on `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, and `TimeSpan`, including `TotalHours` and the other totals. `AddDays` and the other `Add` methods, `+` and `-` with a `TimeSpan`, and the time between two values, read through a total or part such as `(x.End - x.Start).TotalHours` or compared to a `TimeSpan`. A computed `TimeSpan` itself can't be selected. Going below zero between two `TimeOnly` values stays negative, where C# wraps around midnight.
- **Related collections:** `Any`, `All`, `Count`/`LongCount`, `Sum`/`Min`/`Max`/`Average` with a selector, and a member of one row picked with `First`, `FirstOrDefault`, `Single`, `Last`, or `ElementAt` after `Where`, `OrderBy`/`OrderByDescending`/`ThenBy`, and `Skip`, such as `x.Lines.OrderByDescending(l => l.PlacedOn).First().Status`. The row becomes a subquery of one row, so `Single` takes the first and `Last` needs an order. A row that isn't there reads as null, in memory too, so a comparison with it is false and `!=` is true as with a null column. The row's own relations can be read as well, except one back to the queried model.

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

var orders = await Repo.Query<OrderDataModel>().Where(where.Build()).ToArrayAsync();
```

Each call returns the builder, so conditions can chain: `HostedRunning().And(x => x.JoinCode == code).Build()`. `And` binds tighter than `Or`, as in C#: `a`, then `Or(b)`, then `And(c)` means `a || (b && c)`. Use `StartGroupAnd`/`StartGroupOr` and `EndGroup` for parentheses. The first condition inside a group can use either `And` or `Or`.

To join two existing expressions, `LinqAppender.AppendAnd<T>(first, second)` returns `first && second`. To log an expression, `LinqStringConverter.Convert(expression)` (or `where.ToString()`) writes it as readable C#-like text.

### Relations and Partial Updates

A [Graph](Graph.md) says what a change writes. A graph or a [`Select`](#selecting-and-loading-relations) says what a query reads.

```csharp
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
