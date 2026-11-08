[← Back to Documentation](Index.md)

# Events

An event reports something that already happened. Every subscribing service gets it, and each subscriber chooses whether **every replica** handles it (`PerReplica`) or **one replica** does (`PerService`).

## Events Are Fanned Out to Every Replica

**Read this before putting anything in an event handler.**

A command is handled once. An event registered `PerReplica` is handled by every replica: run three replicas and the handler runs three times, in parallel.

### The rule

Put work in a `PerReplica` event handler only when it is **still correct if every replica does it**. Anything else belongs in a command, or in an event handler registered `PerService`.

```csharp
// ✅ PerReplica: each replica drops its own cache
public Task Handle(ProductPriceChangedEvent @event)
{
    Context.GetService<ProductCache>().Drop(@event.ProductID);
    return Task.CompletedTask;
}

// ❌ PerReplica: three replicas take the stock off three times
public async Task Handle(OrderShippedEvent @event)
{
    var item = await Repo.Query<StockItemDataModel>().SingleAsync(x => x.ProductID == @event.ProductID);
    item.OnHand -= @event.Quantity;
    await Repo.UpdateAsync(item);
}
```

| Every replica (`PerReplica`) | Once (a command, or `PerService`) |
|---|---|
| dropping or refreshing the replica's own cache | writing to a shared database or event store |
| an in-memory read model each replica keeps | moving stock, money, or any counter |
| pushing to the browsers connected to that replica | creating a record, such as a shipment |
| logging and metrics | sending mail, charging a card, calling a third party |

Idempotency doesn't fix a `PerReplica` handler: two replicas can both read "not done yet" and both do the work.

### When one change needs both

`Demo/Store` sends both when a price changes:

```csharp
// every replica of every subscriber drops its cached product
await Bus.DispatchAsync(new ProductPriceChangedEvent { ProductID = ..., NewPrice = ... });

// one replica reprices the carts
await Bus.DispatchAsync(new RepriceCartItemsCommand { ProductID = ..., NewPrice = ... });
```

Put a service-to-service command on an interface the web gateway doesn't register, so browsers can't send it, like `ICartRepricingHandler` in `Demo/Store`.

### Choosing per replica or per service

The mode is required on every event consumer. There is no default.

```csharp
bus.AddEventConsumer<IUserEventHandler>(consumer, EventConsumerMode.PerReplica);   // every replica
bus.AddEventConsumer<IEmailEventHandler>(consumer, EventConsumerMode.PerService);  // one replica
```

The mode is the subscriber's choice alone. It changes nothing for the publisher or for other subscribers: two services subscribed to the same event each still get their own copy.

| | `PerReplica` | `PerService` |
|---|---|---|
| Kafka | a new consumer group per consumer instance | one group named for the topic and the service |
| Azure Service Bus | a new subscription per consumer instance | one subscription named for the service |
| RabbitMQ | an exclusive queue per consumer instance | one queue named for the topic and the service |
| Direct TCP / Kestrel | ignored, see below | ignored, see below |

- **The service name** passed to `Bus.New` keeps services apart. Keep it stable: changing it starts a new subscription.
- **While the whole service is down,** `PerService` events wait in the broker for it (until a broker restart on RabbitMQ). `PerReplica` events aren't held for a replica that's gone.
- **On Kafka,** topics have one partition, so one replica receives everything and the others stand by, as with commands.
- **Direct TCP and Kestrel ignore the mode.** The publisher decides: one client per replica URL reaches every replica, and one client pointed at a load balancer reaches one.

Use `PerService` when the work must happen once and the publisher shouldn't need to know the subscriber exists, such as a projection into a shared read model. When the publisher knows what it wants done, a command says so more clearly.

## Defining Events

An event implements `IEvent` and is named in the past tense: `UserCreatedEvent`, not `CreateUserEvent`. Include what subscribers need, so they don't have to query back:

```csharp
using Zerra.CQRS;

public class UserCreatedEvent : IEvent
{
    public required int UserId { get; init; }
    public required string Email { get; init; }
    public required DateTime CreatedAt { get; init; }
}
```

## Event Handlers

The mode is chosen per handler interface, so split handlers by how many replicas should run them:

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

public class UserEventHandler : BaseHandler, IUserEventHandler
{
    public Task Handle(UserUpdatedEvent @event)
    {
        Context.GetService<UserCache>().Remove(@event.UserId);
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

bus.AddHandler<IEmailEventHandler>(new EmailEventHandler());
bus.AddEventConsumer<IEmailEventHandler>(consumer, EventConsumerMode.PerService);
```

Handlers derive from `BaseHandler` like [command handlers](Commands.md#handlers). `Handle` takes only the event, with no `CancellationToken`.

## Dispatching Events

Dispatch an event after saving the change it reports, usually from the command handler that made it:

```csharp
await Context.GetService<IUserRepository>().CreateAsync(user, cancellationToken);
await Bus.DispatchAsync(new UserCreatedEvent { UserId = user.Id, Email = user.Email, CreatedAt = DateTime.UtcNow });
```

`DispatchAsync` sends the event to every local handler and every producer registered for it. Events have no result, so there's no `DispatchAwaitAsync` for them.

## Local and Remote Handling

Where an event is handled depends only on registration:

```csharp
// Local
bus.AddHandler<IUserEventHandler>(new UserEventHandler());

// Publisher: to a broker
bus.AddEventProducer<IUserEventHandler>(new KafkaProducer("localhost:9092", serializer, encryptor, null, log, environment: null));

// Subscriber: from the broker
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddEventConsumer<IUserEventHandler>(new KafkaConsumer("localhost:9092", serializer, encryptor, null, log, environment: null), EventConsumerMode.PerReplica);
```

- A publisher that registers a local handler and a producer for the same interface handles the event locally **and** publishes it.
- Over direct TCP or HTTP there's no broker, so the publisher registers a producer for each subscribing service. See [Two Services Subscribing to the Same Event](Agents.md#two-services-subscribing-to-the-same-event).

## Errors and Redelivery

An event handler's exception goes to the `IBusLogger` (`EndEvent`). The publisher isn't told, and the event isn't redelivered, so a handler whose work needs a retry handles the failure itself:

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
        // queue a retry, or dispatch a command that will
    }
}
```

A handler can still see the same event twice, such as after a consumer restarts. Where repeating the work would be wrong, record what's done and check it first.

## Aggregate Events Are Not CQRS Events

Zerra has two kinds of events, and the rules above apply only to CQRS events.

| | Aggregate event | CQRS event |
|---|---|---|
| Implements | `IAggregateEvent` | `IEvent` |
| What it is | the aggregate's state, in order | a notification |
| Where it lives | one stream in the event store | nowhere; it's delivered and gone |
| Who reads it | `Rebuild` on that aggregate | every subscriber |
| Goes on the bus | never | always |
| Lives in | the service, beside the aggregate | the contracts |

An aggregate event is appended with `AggregateRoot.Append` and applied by the aggregate's `On` method, so changing state there is expected. `Demo/Store` keeps `CartItemAddedEvent` in `Store.Carts/Service/Aggregates/` beside `CartAggregate`. When other services need to know, the handler that appended it also dispatches a CQRS event or command. See [Event-Sourced Aggregates](Repository.md#event-sourced-aggregates).

## Saga Coordination with Events

A coordinator reacts to each step's outcome by dispatching the next command. Each step must run once, so register it `PerService`:

```csharp
public class OrderSagaEventHandler : BaseHandler,
    IEventHandler<OrderPlacedEvent>,
    IEventHandler<InventoryReservedEvent>,
    IEventHandler<PaymentFailedEvent>
{
    public Task Handle(OrderPlacedEvent @event)
        => Bus.DispatchAsync(new ReserveInventoryCommand { OrderId = @event.OrderId, Items = @event.Items });

    public Task Handle(InventoryReservedEvent @event)
        => Bus.DispatchAsync(new ProcessPaymentCommand { OrderId = @event.OrderId });

    public async Task Handle(PaymentFailedEvent @event)
    {
        await Bus.DispatchAsync(new ReleaseInventoryCommand { OrderId = @event.OrderId });
        await Bus.DispatchAsync(new CancelOrderCommand { OrderId = @event.OrderId, Reason = "Payment failed" });
    }
}
```

For a read model kept from events, register `PerReplica` when each replica holds its own copy in memory, and `PerService` when the copy is in a shared store.

## See Also

- [Commands](Commands.md) - Changes, handled once
- [Delivery and Failure Handling](Reliability.md)
- [Kafka](KafkaSetup.md), [RabbitMQ](RabbitMQSetup.md), [Azure Service Bus](AzureServiceBusSetup.md) - How each broker implements the modes
