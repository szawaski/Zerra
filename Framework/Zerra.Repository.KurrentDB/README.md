# Zerra.Repository.KurrentDB

> ⚠️ **Experimental:** the Repository is still experimental and may change.

KurrentDB event store provider for [Zerra.Repository](https://www.nuget.org/packages/Zerra.Repository), for event-sourced aggregates.

## Installation

```bash
dotnet add package Zerra.Repository.KurrentDB
```

## Usage

```csharp
using Zerra.Repository;
using Zerra.Repository.KurrentDB;
using Zerra.Repository.Memory;

// insecure: true for a server without TLS; MemoryEngine is a fallback when KurrentDB isn't running
IEventStoreEngine eventStore = KurrentDBConnectionTest.Test(connectionString, insecure: true, log: log)
    ? new KurrentDBEngine(connectionString, insecure: true)
    : new MemoryEngine();

var busServices = new BusServices();
busServices.AddService<IEventStoreEngine>(eventStore);
```

An `AggregateRoot` keeps its state as a stream of events. Each event implements `IAggregateEvent` and is applied by a public `On` method:

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
await cart.Rebuild();
await cart.Append(new CartItemAddedEvent { ProductID = command.ProductID, Quantity = command.Quantity }, validateEventNumber: true);
```

`validateEventNumber: true` rejects the append if another command changed the stream since this instance rebuilt it.

## Documentation

- [Event-Sourced Aggregates](https://github.com/szawaski/Zerra/blob/master/docs/Repository.md#event-sourced-aggregates)
- [Repository](https://github.com/szawaski/Zerra/blob/master/docs/Repository.md)
- [Zerra documentation](https://github.com/szawaski/Zerra/blob/master/docs/Index.md)

## License

MIT - See [LICENSE](https://github.com/szawaski/Zerra/blob/master/LICENSE)
