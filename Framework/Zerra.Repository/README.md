# Zerra.Repository

> ⚠️ **Experimental:** the Repository is still experimental and may change.

LINQ data access for [Zerra](https://www.nuget.org/packages/Zerra) that doesn't depend on the data store. Handlers read and write data models through one `IRepo` interface, and each model type can live in a different store. It also supports event-sourced aggregates, and it is Native AOT compatible.

## Installation

Install this package with the provider for your store:

```bash
dotnet add package Zerra.Repository
dotnet add package Zerra.Repository.MsSql
```

| Package | Store |
|---|---|
| `Zerra.Repository.MsSql` | SQL Server |
| `Zerra.Repository.PostgreSql` | PostgreSQL |
| `Zerra.Repository.MySql` | MySQL |
| `Zerra.Repository.MariaDb` | MariaDB |
| `Zerra.Repository.Memory` | in memory, for tests and fallbacks |
| `Zerra.Repository.KurrentDB` | KurrentDB event store, for aggregates |

## Data Models

```csharp
[Entity("SalesOrder")]
public sealed class OrderDataModel
{
    [Identity(false)]                           // primary key, assigned by code
    public Guid ID { get; set; }

    [StoreProperties(true, 32)]                 // not null, max length 32
    public string? OrderNumber { get; set; }

    public Guid CustomerID { get; set; }

    [Relation(nameof(OrderLineDataModel.OrderID))]   // one-to-many: the related model's foreign key
    public OrderLineDataModel[]? Lines { get; set; }
}
```

## Setup

A provider connects model types to an engine, and the repo holds the providers:

```csharp
public sealed class OrdersStoreProvider<TModel> : TransactStoreProvider<TModel>
    where TModel : class, new()
{
    public OrdersStoreProvider(ITransactStoreEngine engine) : base(engine) { }

    protected override bool EventLinking => false;
    protected override bool QueryLinking => true;    // load relations named in a graph
    protected override bool PersistLinking => true;  // save related models with their parent
}

var engine = new MsSqlEngine(connectionString);
var repo = Repo.New();
repo.AddProvider(new OrdersStoreProvider<OrderDataModel>(engine));
repo.AddProvider(new OrdersStoreProvider<OrderLineDataModel>(engine));

var busServices = new BusServices();
busServices.AddRepo(repo);
```

## Using IRepo in Handlers

```csharp
public sealed class OrdersQueryHandler : BaseHandlerWithRepo, IOrdersQueryHandler
{
    public async Task<OrderModel[]> GetOrders(Guid customerID, CancellationToken cancellationToken)
    {
        var orders = await Repo.ManyAsync<OrderDataModel>(
            x => x.CustomerID == customerID,
            QueryOrder<OrderDataModel>.Create(x => x.OrderNumber),
            new Graph<OrderDataModel>(true, x => x.Lines));
        return orders.Select(ToModel).ToArray();
    }
}
```

## Documentation

- [Repository](https://github.com/szawaski/Zerra/blob/master/docs/Repository.md) - Models, providers, queries, relations, and aggregates
- [Repository Generation](https://github.com/szawaski/Zerra/blob/master/docs/RepositoryGeneration.md) - Code First schema generation
- [Graph](https://github.com/szawaski/Zerra/blob/master/docs/Graph.md) - Selecting relations and columns
- [Zerra documentation](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
