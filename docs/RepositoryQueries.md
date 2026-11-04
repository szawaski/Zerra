[← Back to Documentation](Index.md)

# Repository Queries

`Repo.Query<T>()` reads data models through LINQ. This page covers what a query can do; setup and changes are in [Repository](Repository.md).

## Queries

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

Every function the store runs is listed in [LINQ Functions](#linq-functions). In general:

- Filter and order before paging. A query that reads elements by their place, such as `TakeWhile` or `Zip`, needs an `OrderBy` before it.
- Two queries combined by a set operation or a join are one statement when both are on the same engine. Otherwise each runs on its own, and the elements combine in memory.
- `First` and `Single` throw when there is nothing, as LINQ does. The `Async` versions, such as `ToArrayAsync`, `FirstOrDefaultAsync`, and `CountAsync`, are in `RepoQueryableExtensions`.
- A `Where` or `OrderBy` after a `Select` is written back onto the model, from what the `Select` set, so it has to read members the `Select` set and come before paging.
- `ToHashSetAsync` and `ToDictionaryAsync` build their result after the rows are read, so their selectors run in memory.
- `AsAsyncEnumerable(cancellationToken)` and `AsEnumerable()` give each element as it's read, for large results: `await foreach (var order in Repo.Query<OrderDataModel>().AsAsyncEnumerable(cancellationToken))`. The connection stays open until the loop ends. A plain `foreach` over the query, `ToList`, and `ToArray` read everything first, as before. A query that runs in memory, such as a set across two engines or a query of an event store, reads everything before the first element. Relations on another engine load for each chunk of models, and rules see the models in chunks. A chunk is as many models as fit in `StreamChunkBytes` on the provider, 1 MB by default, measured from their columns and text.
- A call the store can't run, such as `Count` after `GroupBy`, or `Aggregate` with its function, throws `NotSupportedException` instead of running in memory.
- The `Async` endings, the [history queries](#history-queries), and the [changes](Repository.md#changes) take a `CancellationToken` last. Cancelling stops the statement in the store and throws `OperationCanceledException`, whichever engine it's on. Creating or updating many models in one call is all or nothing. A change that also writes related models keeps what it already wrote, unless it runs in a [transaction](Repository.md#transactions).
- With Entity Framework in the same file, the `Async` endings of both are in scope, so call EF's through `EntityFrameworkQueryableExtensions`.

## LINQ Functions

In the examples, `orders` is `Repo.Query<OrderDataModel>()` and `customers` is `Repo.Query<CustomerDataModel>()`.

**Building the query**

| Function | Example |
|---|---|
| `Where` | `orders.Where(x => x.Status == "Placed")` |
| `OrderBy`, `ThenBy`, and their `Descending` forms | `orders.OrderBy(x => x.PlacedOn).ThenByDescending(x => x.Total)` |
| `Order`, `OrderDescending` | `orders.Select(x => x.Total).OrderDescending()` orders the values themselves |
| `Skip`, `Take` | `orders.OrderBy(x => x.ID).Skip(20).Take(20)`, or `Take(20..40)` |
| `Reverse` | `orders.OrderBy(x => x.PlacedOn).Reverse()` |
| `Select` | `orders.Select(x => new { x.ID, Customer = x.Customer!.Name, Lines = x.Lines!.Count() })` |
| `SelectMany` | `orders.SelectMany(x => x.Lines!)` continues as a query of the related model |
| `Distinct` | `orders.Select(x => x.Status).Distinct()` |
| `DistinctBy` | `orders.OrderByDescending(x => x.PlacedOn).DistinctBy(x => x.CustomerID)` gives each customer's newest order |
| `GroupBy` | `orders.GroupBy(x => x.Status).Select(g => new { g.Key, Count = g.Count(), Total = g.Sum(x => x.Total) })` |
| `GroupBy` with selectors | `orders.GroupBy(x => x.Status, x => x.Total, (status, totals) => new { status, Sum = totals.Sum() })` |
| `CountBy` | `orders.CountBy(x => x.Status)` gives a `KeyValuePair` of each status and its count |
| `Cast`, `OfType` | `orders.Select(x => x.ShippedOn).OfType<DateTime>()` skips the nulls |
| `Index` | `orders.OrderBy(x => x.PlacedOn).Index()` gives `(Index, Item)` pairs |
| `Select`, `Where` with the index | `orders.OrderBy(x => x.PlacedOn).Where((x, i) => i % 2 == 0)` |
| `TakeWhile`, `SkipWhile` | `orders.OrderBy(x => x.PlacedOn).TakeWhile(x => x.Total < 100)` |
| `TakeLast`, `SkipLast` | `orders.OrderBy(x => x.PlacedOn).TakeLast(5)`, or `Take(^5..)` |
| `Append`, `Prepend` | `orders.Select(x => x.Total).Append(0m)` |
| `DefaultIfEmpty` | `orders.Select(x => x.Total).DefaultIfEmpty().Max()` gives 0 when there are no orders; it comes last before the ending |
| `Chunk` | `orders.OrderBy(x => x.ID).Chunk(100)` comes last, and gives arrays of up to 100 |

**Joining and combining queries**

No relation between the models is needed, and a model can join itself.

| Function | Example |
|---|---|
| `Join` | `orders.Join(customers, o => o.CustomerID, c => c.ID, (o, c) => new { o.OrderNumber, c.Name })` |
| `join` in query syntax | `from o in orders join c in customers on o.CustomerID equals c.ID select new { o.OrderNumber, c.Name }` |
| `Join` on several keys | `orders.Join(invoices, o => new { o.CustomerID, o.OrderNumber }, i => new { i.CustomerID, i.OrderNumber }, (o, i) => new { o, i })` |
| `LeftJoin`, `RightJoin` | `customers.LeftJoin(orders, c => c.ID, o => o.CustomerID, (c, o) => new { c.Name, o.OrderNumber })` gives a null `OrderNumber` for a customer with no orders |
| left join in query syntax | `from c in customers join o in orders on c.ID equals o.CustomerID into g from o in g.DefaultIfEmpty() select new { c.Name, o }` |
| left join with a filter | `... into g from o in g.Where(o => o.Status == "Open").DefaultIfEmpty()` keeps the customers with no open orders |
| `SelectMany` of another query | `from o in orders from c in customers.Where(c => c.ID == o.CustomerID) select new { o.OrderNumber, c.Name }`, with `.DefaultIfEmpty()` for a left join |
| cross join | `from o in orders from c in customers select new { o.ID, CustomerID = c.ID }` pairs every order with every customer |
| `GroupJoin` | `customers.GroupJoin(orders, c => c.ID, o => o.CustomerID, (c, g) => new { c.Name, Orders = g.Count(), Spent = g.Sum(o => o.Total) })` |
| `Zip` | `orders.OrderBy(x => x.ID).Zip(invoices.OrderBy(x => x.OrderID), (o, i) => new { o.ID, i.Amount })` pairs by place |
| `Union`, `Concat`, `Intersect`, `Except` | `orders.Select(x => x.CustomerID).Union(returns.Select(x => x.CustomerID))` |
| `UnionBy` | `orders.OrderBy(x => x.PlacedOn).UnionBy(archived.OrderBy(x => x.PlacedOn), x => x.OrderNumber)` |
| `IntersectBy`, `ExceptBy` | `orders.ExceptBy(invoices.Select(i => i.OrderID), x => x.ID)` gives orders with no invoice |
| `Contains` of another query | `orders.Where(x => customers.Where(c => c.Vip).Select(c => c.ID).Contains(x.CustomerID))` |

A member of a row a left join doesn't have reads as null, on every store and in memory, so `o.OrderNumber` needs no null check. After a join or set operation, the elements can be filtered, ordered, paged, selected from, and ended like any query: `orders.Join(customers, o => o.CustomerID, c => c.ID, (o, c) => new { o, c }).Where(x => x.o.Total > x.c.CreditLimit).Count()`. A `GroupJoin` group is read with `Count`, `Any`, `All`, `Sum`, `Min`, `Max`, or `Average`.

**Ending the query**

| Function | Example |
|---|---|
| `First`, `FirstOrDefault`, `Single`, `SingleOrDefault` | `orders.FirstOrDefault(x => x.ID == id)`, or `FirstOrDefault(x => x.ID == id, fallback)` |
| `Last`, `LastOrDefault` | `orders.OrderBy(x => x.PlacedOn).Last()` |
| `ElementAt`, `ElementAtOrDefault` | `orders.OrderBy(x => x.PlacedOn).ElementAt(2)`, or `ElementAt(^1)` |
| `Count`, `LongCount`, `Any`, `All` | `orders.Count(x => x.Status == "Placed")`, `orders.All(x => x.Total > 0)` |
| `Contains` | `orders.Select(x => x.OrderNumber).Contains(number)` |
| `Sum`, `Min`, `Max`, `Average` | `orders.Where(x => x.CustomerID == id).Sum(x => x.Total)` |
| `MinBy`, `MaxBy` | `orders.MaxBy(x => x.Total)` gives the order with the largest total |
| `SequenceEqual` | `a.OrderBy(x => x.ID).Select(x => x.Total).SequenceEqual(b.OrderBy(x => x.ID).Select(x => x.Total))` |

`Aggregate`, `AggregateBy`, and the overloads taking a comparer throw `NotSupportedException`, since no store can run them.

## Selecting and Loading Relations

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
- A query has a graph or a `Select`, not both, and a `GroupBy` has no graph. A `Count`, `Any`, or aggregate doesn't need one.

## Aggregate Functions

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

## Layers and Event Stores

- A rule's `WhereExpression` applies to a query, and its `OnGet` to the models given back, in chunks when they're streamed.
- An encryption or compression layer refuses a `Select` or order of one of its properties, and gives the models back plain. A graph reads them, and only the ones it names are decrypted or decompressed.
- A member, an order, or a graph's relation on another engine, or on a related model with its own layers, loads the models with what the query reads and runs the query over them in memory.
- An event store runs the query over the current state of its models. Each change it kept is read with the [History Queries](#history-queries).

## LINQ Support

These work in a `Where`, in the values of a `Select`, and in a group's values.

**Operators**

- Comparisons: `==`, `!=`, `<`, `<=`, `>`, `>=`. Nulls compare as in C#, so `x.Maybe != 4` includes rows where `Maybe` is null.
- Arithmetic: `+`, `-`, `*`, `/`, `%`, and unary `-`.
- Logic: `&&`, `||`, `!`, and bool members such as `x => x.Active`.
- `??`, such as `(x.Discount ?? 0) > 5`.
- Integer bitwise operators: `&`, `|`, `^`, `~`.
- `HasValue` and `Value` on nullables.
- `array.Contains(x.Prop)` with a list in memory, such as `ids.Contains(x.ID)`.

**Strings**

- `Contains`, `StartsWith`, `EndsWith`: wildcards in the text are matched literally, and a `StringComparison` ignoring case is honored.
- `Equals`, `string.IsNullOrEmpty`, `string.IsNullOrWhiteSpace`.
- `Length`, `IndexOf`, `Substring`, `Replace`.
- `ToUpper`, `ToLower`.
- `Trim`, `TrimStart`, `TrimEnd`: only spaces are trimmed.
- `PadLeft`, `PadRight`.
- `string.Concat` and `+`.
- `string.Join` of `new[] { .. }`, of a related collection, or of a group's values.

Case sensitivity of `==`, `Contains`, `StartsWith`, and `EndsWith` follows the database collation.

**Text and conversions**

- `ToString()` on numbers, bools, enums, and Guids.
- `ToString(format)` on dates, with a custom format such as `yyyy-MM-dd`, and on numbers, such as `F2` or `D4`. A date without a format, or a culture, is refused because the store doesn't have one.
- `int.Parse` and the other number parsers, and `Guid.Parse`.
- `Convert.ToInt32` and the other number conversions, `Convert.ToString`, and `Convert.ToBoolean`. A midpoint rounds to the even number, as in C#.

**Math**

- `Math.Abs`, `Math.Ceiling`, `Math.Floor`, `Math.Pow`, `Math.Sqrt`.
- `Math.Round`: a midpoint rounds to even unless given another `MidpointRounding`, as in C#.

**Dates and times**

- Parts such as `.Year`, `.Month`, `.Date`, and `.DayOfWeek` on `DateTime`, `DateTimeOffset`, `DateOnly`, and `TimeOnly`.
- `TimeSpan` parts and totals, such as `.Hours` and `.TotalHours`.
- `AddDays`, `AddHours`, and the other `Add` methods, and `+` or `-` with a `TimeSpan`.
- The time between two values, read through a total or part, such as `(x.End - x.Start).TotalHours`, or compared to a `TimeSpan`. A computed `TimeSpan` itself can't be selected.
- Going below zero between two `TimeOnly` values stays negative, where C# wraps around midnight.

**Enums**

- Comparing, ordering, `Min`, `Max`, and casting to a number use the enum's numbers, as in C#.
- `ToString` gives the field's name.
- `[Flags]` enums also support `HasFlag` and `&`, and their `ToString` is the text `EnumName` writes, such as `Read|Write`.

**Related collections**

- `Any`, `All`: `x.Lines.Any(l => l.Quantity > 10)`.
- `Count`, `LongCount`: `x.Lines.Count()`.
- `Sum`, `Min`, `Max`, `Average` with a selector: `x.Lines.Sum(l => l.Price * l.Quantity)`.
- `First`, `FirstOrDefault`, `Single`, `Last`, `ElementAt` for a member of one row, after `Where`, `OrderBy`, and `Skip`: `x.Lines.OrderByDescending(l => l.PlacedOn).First().Status`. `Single` takes the first, and `Last` needs an order. A row that isn't there reads as null, so a comparison with it is false and `!=` is true. The row's own relations can be read too, except one back to the queried model.

Anything that doesn't use the model, such as `DateTime.Now.Date`, is evaluated before the query.

## Building a Where in Code

For filters chosen at run time, such as optional search fields, add a `Where` for each one. Several `Where` calls mean all of them:

```csharp
var query = Repo.Query<OrderDataModel>().Where(x => x.CustomerID == customerID);
if (status is not null)
    query = query.Where(x => x.Status == status);
if (search is not null)
    query = query.Where(x => x.OrderNumber!.Contains(search) || x.Customer!.Name!.Contains(search));

var orders = await query.ToArrayAsync();
```

To log an expression, `LinqStringConverter.Convert(expression)` writes it as readable C#-like text.

## History Queries

Models stored with `EventStoreAsTransactStoreProvider` keep every change. Two queries read it, and they work like `Query<T>()`:

| Query | Gives |
|---|---|
| `TemporalQuery<T>()` | the model as it was after each change |
| `EventQuery<T>()` | each change as an `EventModel<T>` |

Both take an optional `TemporalOrder` (`Newest` first by default, or `Oldest`), a range by date (`dateFrom`, `dateTo`) or by event number (`numberFrom`, `numberTo`), and a `graph`. The `Where` must name the models by identity, since each model's history is read by itself. In an event query, the model is `x.Model`:

```csharp
var lastWeek = await Repo.EventQuery<OrderDataModel>(dateFrom: DateTime.UtcNow.AddDays(-7))
    .Where(x => x.Model.ID == orderID)
    .ToArrayAsync();
foreach (var change in lastWeek)
    Console.WriteLine($"{change.Number} {change.Date} {change.EventName} -> {change.Model.Status}");

var asOfMonday = await Repo.TemporalQuery<OrderDataModel>(dateTo: monday).FirstOrDefaultAsync(x => x.ID == orderID);
var shipped = await Repo.EventQuery<OrderDataModel>().CountAsync(x => x.Model.ID == orderID && x.EventName == "Shipped");
```

`First` is the first in the order, so with the default it's the newest. `Skip` and `Take` page in that order. `Where`, `OrderBy`, `Select`, `GroupBy`, and the aggregates run over the states that are read. History can't be combined with `Union` and the other set operations, or looked in from another query's condition.

`EventModel<T>` has `Number`, `Date`, `EventName`, `Deleted`, `Model` (the full model after the change), `ModelChange` and `GraphChange` (what was written), and `Source`/`SourceType` (what started the change, when it was recorded).

A deleted model's events end with the delete, which has `Deleted` set, the delete's name and source, and the state it deleted as its `Model`. Its `ModelChange` is null. Add `!x.Deleted` to the `Where` to leave it out.

## See Also

- [Repository](Repository.md) - Setup, changes, transactions, and layers
- [Graph](Graph.md) - Selecting relations and columns
