[← Back to Documentation](Index.md)

# Events

An event reports a state change that already happened. It is published to zero or more subscribers, locally or through a broker, and each subscriber chooses whether **every replica** gets a copy (`PerReplica`) or **one replica** handles each event (`PerService`). There is no default.

## Events Are Fanned Out to Every Replica

**Read this before putting anything in an event handler.**

A command is handled **once**. An event is delivered to **every subscriber**, and each subscriber decides for itself how many of its own replicas get a copy, with the `EventConsumerMode` its `AddEventConsumer` takes. `EventConsumerMode.PerReplica` is the fanout this section is about: run three replicas of a service that subscribes that way and the handler runs three times, once per replica, in parallel, on three copies of the same message.

That is deliberate, and it is what the brokers are configured to do:

| Transport | Commands | Events, `PerReplica` |
|---|---|---|
| Kafka | one consumer group named for the topic, so the replicas compete and **one** handles each command | a **new group id per consumer instance**, so **every replica** gets a copy |
| Azure Service Bus | a shared **queue**, so **one** replica handles each command | a topic with a **new subscription per consumer instance**, so **every replica** gets a copy |
| RabbitMQ | a Direct exchange | a **Fanout** exchange, so **every replica** gets a copy |
| Direct TCP / Kestrel | sent to the endpoint registered for that command | sent to each endpoint registered for that event |

The other mode, [`EventConsumerMode.PerService`](#choosing-per-replica-or-per-service), has the replicas of one subscriber compete for each event instead. There is no default, so every `AddEventConsumer` states which it is. Everything between here and that section is about `PerReplica`.

### The rule

Put work in a `PerReplica` event handler only when it is **still correct if every replica does it**. Anything else is a command, or an event the subscriber registers `PerService`.

```csharp
// ✅ Correct in an event handler - safe when every replica runs it
public Task Handle(ProductPriceChangedEvent @event)
{
    // this replica's own cache, so this replica has to be the one to drop it
    Context.GetService<ProductCache>().Drop(@event.ProductID);
    return Task.CompletedTask;
}

// ❌ Wrong in a PerReplica event handler - three replicas decrement the stock three times
public async Task Handle(OrderShippedEvent @event)
{
    var item = await Repo.SingleAsync<StockItemDataModel>(x => x.ProductID == @event.ProductID);
    item.OnHand -= @event.Quantity;
    await Repo.UpdateAsync(item);
}
```

The second one is a command, or an event the subscriber registers [`PerService`](#choosing-per-replica-or-per-service) so its replicas compete for it. What it cannot be is an event under the default fanout.

Work that belongs in an **event** handler, because every replica must do it for itself, or because doing it N times changes nothing:

- dropping or refreshing a cache the replica holds in its own memory
- an in-memory read model or lookup each replica keeps its own copy of
- pushing to the browsers or sockets connected to *that* replica
- logging, metrics, tracing

Work that belongs in a **command**, because it must happen exactly once:

- writing to a shared database or an event store
- moving stock, money, or any other counter
- creating a record, such as a shipment or an invoice
- anything with an outside effect: sending mail, charging a card, calling a third party

### When one change needs both

A single state change often needs an event *and* a command, for two different reasons. `Demo/Store` does exactly this when a product's price changes:

```csharp
// every replica of every subscriber drops its cached copy of the product
await Bus.DispatchAsync(new ProductPriceChangedEvent() { ProductID = ..., NewPrice = ... });

// one replica reprices the carts holding it
await Bus.DispatchAsync(new RepriceCartItemsCommand() { ProductID = ..., NewPrice = ... });
```

Send the command to a service-to-service command interface that the web gateway does not register, so browsers cannot send it. `ICartRepricingHandler` and `IStockReservationHandler` in `Demo/Store` are both this.

### Idempotency is not enough

Making the handler idempotent guards against the *same* replica getting a duplicate delivery. It does not help when several replicas run concurrently: two replicas can both read "not done yet" and both do the work. Idempotency and the command/event choice are separate concerns, and you often need both.

### Choosing per replica or per service

The fanout above is `EventConsumerMode.PerReplica`, and it is what an event usually wants, because an event is a notification and each replica has its own cache, its own connected browsers and its own in-memory state to keep current.

When a subscriber needs the opposite - each event handled once by the service however many replicas are running - it registers its consumer with `EventConsumerMode.PerService`:

```csharp
// every replica of this service receives each event
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);

// one replica of this service receives each event, the replicas compete for them
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerService);
```

There is no default. `AddEventConsumer` takes the mode on every registration, because which one it is decides what a handler is allowed to do, and that is not something to leave to a default nobody reads.

The mode belongs to the consumer registration, so it is the **subscriber's** choice alone. It changes nothing about what the publisher sends and nothing about what any other subscriber receives: two services subscribed to the same event still each get their own copy, and `PerService` only decides whether the replicas of *that* service share it.

| | `PerReplica` | `PerService` |
|---|---|---|
| Kafka | a new group id per consumer instance | one group named for the topic and the service |
| Azure Service Bus | a new subscription per consumer instance | one subscription named for the service |
| RabbitMQ | an exclusive server-named queue per consumer instance | one queue named for the topic and the service, bound to the same Fanout exchange |
| Direct TCP / Kestrel | no effect, see below | no effect, see below |

The service name in those is the one passed to `Bus.New(serviceName, ...)`, so it is what keeps two services subscribed to the same event from sharing a subscription. Give each service its own, and keep it stable across deployments: changing it starts a new subscription. Where a broker's name limit is short enough to cut it, the consumer logs a warning naming the shortened result.

Because the subscription is shared, `PerService` outlives any one replica: a replica stopping does not take it with it, and the events keep reaching the replicas still running. A `PerReplica` subscription is deleted with the replica that owned it.

Whether events published while the *whole* service is down are still waiting when it returns is up to the broker. Kafka keeps the group's committed offsets, Azure Service Bus keeps the subscription, and RabbitMQ keeps the queue, the same as it does for a command queue, so they are. RabbitMQ holds them only until the broker restarts.

On Kafka a topic is created with one partition, so `PerService` means one replica receives everything and the rest stand by to take over, the same as a command consumer. The other two brokers spread the events across the replicas.

**Direct TCP and Kestrel ignore the mode.** On a direct connection the producer decides who gets a copy through the client URLs it is registered with: one client per replica URL reaches every replica, one client pointed at a load balancer in front of them reaches one of them. The server has nothing to change, so it takes the mode and does nothing with it.

`PerService` is not a way around [choosing a command](#the-rule). Reach for it when the work must happen once *and* the publisher has no business knowing the subscriber exists - a projection into a shared read model, a subscriber that fans a change out to a third party. When the publisher does know what it wants done, say so with a command and it is clearer to everyone reading it.

## Aggregate Events Are Not CQRS Events

Two different things in Zerra are called events, and the rules above apply to only one of them.

**CQRS events** are the bus messages in this document: `IEvent` types you `Bus.DispatchAsync`, handled by `IEventHandler<T>` in other services, fanned out to every replica. They are notifications. Nothing stores them.

**Aggregate events** are the events an `AggregateRoot` appends to its own stream in an event store (see [Repository](Repository.md)). They *are* the aggregate's state: `Rebuild` replays them to reconstruct it, and they are the source of truth for that one aggregate instance. They are not notifications, they are not addressed to anyone, and they belong to a single stream, not to a set of subscribers.

|  | Aggregate event | CQRS event |
|---|---|---|
| What it is | the aggregate's state, in order | a notification that something happened |
| Where it lives | one stream in the event store | nowhere, it is delivered and gone |
| Who reads it | `Rebuild` on that one aggregate | every subscriber, and every replica of each |
| Replay | yes, that is the point | no |
| Fanout rules above | do not apply | apply |

Because they are different things, they are written differently:

- **An aggregate event implements `IAggregateEvent`, not `IEvent`.** `IEvent` marks a bus message, and an aggregate event is never one. `AggregateRoot.Append<TEvent>` and `Delete<TEvent>` require `IAggregateEvent`, and source generation uses the same marker to emit the type detail the event needs to be serialized into the stream in a trimmed or native AOT build.
- **It lives with the aggregate, not in a shared contracts project.** No other domain reads it, so putting it in a `*.Domain` assembly next to the commands and queries advertises it as something other services can subscribe to. `Demo/Store` keeps `CartItemAddedEvent` in `Store.Carts.Service/Aggregates/` beside `CartAggregate`, not in `Store.Carts.Domain`.
- **It never goes on the bus.** `Append` writes it to the stream and stops there.

And the reasoning about them is different:

- An aggregate event is not "fanned out to every replica". It is appended to one stream and read back by whoever rebuilds that aggregate.
- An aggregate event changing state is correct and expected. That is what `On(SomeEvent)` does. The warning against changing state in a handler is about CQRS event handlers.
- Naming a type `...Event` makes it neither. What makes it an aggregate event is being appended with `AggregateRoot.Append`; what makes it a CQRS event is implementing `IEvent` and being dispatched on the bus.

```csharp
using Zerra.Repository;

namespace Store.Carts.Service.Aggregates        //with the aggregate, in the service
{
    public sealed class CartItemAddedEvent : IAggregateEvent
    {
        public required Guid ProductID { get; init; }
        public required decimal UnitPrice { get; init; }
    }

    public sealed class CartAggregate : AggregateRoot
    {
        public Task On(CartItemAddedEvent @event) { ... }   //applied on Append, replayed by Rebuild
    }
}
```

When something outside the service does need to know, the handler that appended the event dispatches a CQRS event or a command of its own, and that one is a contract: it implements `IEvent` or `ICommand` and lives in the domain project. Choosing between those two is what the section above is about.

## Defining Events

An event implements `IEvent` and is named in the past tense, because it reports something that already happened: `UserCreatedEvent`, not `CreateUserEvent`. Include the data subscribers need, so they don't have to query back, and make the properties `init`-only:

```csharp
using Zerra.CQRS;

public class UserCreatedEvent : IEvent
{
    public required int UserId { get; init; }
    public required string Email { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public class OrderPlacedEvent : IEvent
{
    public required int OrderId { get; init; }
    public required int CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required List<OrderItem> Items { get; init; }
}
```

## Event Handlers

A handler interface lists the events a subscriber handles. The consumer mode is chosen per registered interface, so a service splits its handlers by how many replicas should do the work:

```csharp
public interface IUserEventHandler :        // every replica
    IEventHandler<UserUpdatedEvent>,
    IEventHandler<UserDeletedEvent>
{
}

public interface IEmailEventHandler :       // one replica
    IEventHandler<UserCreatedEvent>,
    IEventHandler<OrderPlacedEvent>
{
}
```

Handlers derive from `BaseHandler`, which gives them `Bus`, `Log`, and `Context` as in [Commands](Commands.md#handlers). Unlike a command handler, `Handle` takes only the event, with no `CancellationToken`.

`UserEventHandler` keeps each replica's own cache current, so every replica needs the event:

```csharp
public class UserEventHandler : BaseHandler, IUserEventHandler
{
    public Task Handle(UserUpdatedEvent @event)
    {
        Context.GetService<UserCache>().Remove(@event.UserId); // this replica's in-memory cache
        return Task.CompletedTask;
    }

    public Task Handle(UserDeletedEvent @event)
    {
        Context.GetService<UserCache>().Remove(@event.UserId);
        return Task.CompletedTask;
    }
}

bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);
```

`EmailEventHandler` sends mail, which must happen once, so its replicas compete for each event:

```csharp
public class EmailEventHandler : BaseHandler, IEmailEventHandler
{
    public async Task Handle(UserCreatedEvent @event)
        => await Context.GetService<IEmailService>().SendWelcomeEmailAsync(@event.Email);

    public async Task Handle(OrderPlacedEvent @event)
        => await Context.GetService<IEmailService>().SendOrderConfirmationAsync(@event.OrderId);
}

bus.AddHandler<IEmailEventHandler>(new EmailEventHandler());
bus.AddEventConsumer<IEmailEventHandler>(consumer, EventConsumerMode.PerService);
```

Any number of services can subscribe to the same event. Each gets its own copy and chooses its own mode.

## Dispatching Events

Dispatch an event after the change it reports has been saved, usually from the command handler that made it:

```csharp
public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
{
    var user = new User { Email = command.Email, Name = command.Name };
    await Context.GetService<IUserRepository>().CreateAsync(user, cancellationToken);

    await Bus.DispatchAsync(new UserCreatedEvent { UserId = user.Id, Email = user.Email, CreatedAt = DateTime.UtcNow });
}
```

`DispatchAsync` sends the event to every local handler and every producer registered for it. An event has no result, so there is no `DispatchAwaitAsync` for events.

A `PerService` event handler can itself dispatch commands and further events, which is how a choreographed workflow is built: `OrderPlacedEvent` reserves inventory and dispatches `InventoryReservedEvent`, whose handler takes the payment, and so on. Each such step changes state, so each of those handlers is registered `PerService`. See [Saga Coordination](#saga-coordination-with-events) below.

## Local and Remote Handling

Where an event is handled depends only on registration:

```csharp
// Local: handled in this process
bus.AddHandler<IUserEventHandler>(new UserEventHandler());

// Publisher: send to a broker (RabbitMQ and Azure Service Bus follow the same pattern)
bus.AddEventProducer<IUserEventHandler>(new KafkaProducer("localhost:9092", serializer, encryptor, log, environment: null, userName: null, password: null));

// Subscriber: receive from the broker
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddEventConsumer<IUserEventHandler>(new KafkaConsumer("localhost:9092", serializer, encryptor, log, environment: null, userName: null, password: null), EventConsumerMode.PerReplica);
```

A publisher can register a local handler and a producer for the same interface. The event is then handled locally **and** published.

Over direct TCP or HTTP there is no broker, so the publisher registers one producer per subscribing service, and the bus sends each event to all of them. See [Two Services Subscribing to the Same Event](Agents.md#two-services-subscribing-to-the-same-event).

## Errors

An exception thrown by an event handler is reported to the `IBusLogger` (`EndEvent` receives it). The publisher is not told, since events have no reply. Don't count on the broker to redeliver the event: a handler whose work needs a retry must catch and handle the failure itself.

```csharp
public async Task Handle(UserCreatedEvent @event)
{
    try
    {
        await Context.GetService<IEmailService>().SendWelcomeEmailAsync(@event.Email);
    }
    catch (Exception ex)
    {
        Log?.Error($"Welcome email to {@event.Email} failed", ex);
        // queue it for a retry, or dispatch a command that will
    }
}
```

## Idempotency

A handler can receive the same event twice, for example after a consumer restarts. Where doing the work twice would be wrong, record what has been done and check it first. This guards against redelivery to one replica. It does not stop several replicas doing the work at once, which is what the consumer mode is for.

## Event Sourcing

To keep an aggregate's history as its source of truth, derive it from `AggregateRoot` and append `IAggregateEvent` types to its stream. Those events are its state, not bus messages, so none of the fanout rules above apply to them. See [Aggregate Events Are Not CQRS Events](#aggregate-events-are-not-cqrs-events) and `Demo/Store/Store.Carts.Service/Aggregates/CartAggregate.cs`.

When other services need to know about a change, the command handler that appended the aggregate event also dispatches a CQRS event or command.

## Common Patterns

### Saga Coordination with Events

A coordinator reacts to each step's outcome by dispatching the next command. A saga step must run once, so register the coordinator's consumer `PerService`:

```csharp
public class OrderSagaEventHandler : BaseHandler,
    IEventHandler<OrderPlacedEvent>,
    IEventHandler<InventoryReservedEvent>,
    IEventHandler<InventoryReservationFailedEvent>,
    IEventHandler<PaymentProcessedEvent>,
    IEventHandler<PaymentFailedEvent>
{
    public Task Handle(OrderPlacedEvent @event)
        => Bus.DispatchAsync(new ReserveInventoryCommand { OrderId = @event.OrderId, Items = @event.Items });

    public Task Handle(InventoryReservedEvent @event)
        => Bus.DispatchAsync(new ProcessPaymentCommand { OrderId = @event.OrderId });

    public Task Handle(InventoryReservationFailedEvent @event)
        => Bus.DispatchAsync(new CancelOrderCommand { OrderId = @event.OrderId, Reason = "Insufficient inventory" });

    public Task Handle(PaymentProcessedEvent @event)
        => Bus.DispatchAsync(new ShipOrderCommand { OrderId = @event.OrderId });

    public async Task Handle(PaymentFailedEvent @event)
    {
        await Bus.DispatchAsync(new ReleaseInventoryCommand { OrderId = @event.OrderId });
        await Bus.DispatchAsync(new CancelOrderCommand { OrderId = @event.OrderId, Reason = "Payment failed" });
    }
}
```

### Read Model Projection

A projection keeps a query-optimized copy of data up to date from events. Where the copy lives decides the mode:

- **In each replica's memory:** register `PerReplica`, since every replica has its own copy to update.
- **In a shared store:** register `PerService`, so each event is written once.

```csharp
public class UserReadModelProjection : BaseHandler,
    IEventHandler<UserCreatedEvent>,
    IEventHandler<UserDeletedEvent>
{
    public Task Handle(UserCreatedEvent @event)
        => Context.GetService<IUserReadModelRepository>().CreateAsync(new UserReadModel { UserId = @event.UserId, Email = @event.Email, IsActive = true });

    public Task Handle(UserDeletedEvent @event)
        => Context.GetService<IUserReadModelRepository>().DeleteAsync(@event.UserId);
}
```

## See Also

- [Commands](Commands.md) - State-changing operations, handled once
- [Queries](Queries.md) - Read operations
- [Server Setup](ServerSetup.md) - Event consumers
- [Client Setup](ClientSetup.md) - Event producers
- [Kafka](KafkaSetup.md), [RabbitMQ](RabbitMQSetup.md), [Azure Service Bus](AzureServiceBusSetup.md) - How each broker implements the modes