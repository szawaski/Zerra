[← Back to Documentation](Index.md)

# Events

Events represent state changes that have occurred in your application. Zerra implements a publish-subscribe pattern where events are dispatched to zero or more subscribers for eventual consistency and reactive behavior.

## Overview

Events in Zerra:
- Represent state changes that have already occurred
- Follow publish-subscribe pattern (one-to-many)
- Multiple handlers can respond to the same event
- **Delivered to every replica of every subscriber** by default, so a handler must be correct when N instances all run it (see [Events Are Fanned Out to Every Replica](#events-are-fanned-out-to-every-replica)), unless the subscriber registers its consumer with [`EventConsumerMode.PerService`](#choosing-per-replica-or-per-service)
- Dispatched asynchronously to local or remote handlers
- Support distributed event-driven architecture
- Used for eventual consistency and reactive workflows
- Can be consumed from message brokers for scalable processing

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

### Basic Event

```csharp
using Zerra.CQRS;

public class UserCreatedEvent : IEvent
{
    public required int UserId { get; set; }
    public required string Email { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class UserUpdatedEvent : IEvent
{
    public required int UserId { get; set; }
    public required string Email { get; set; }
    public required DateTime UpdatedAt { get; set; }
}

public class UserDeletedEvent : IEvent
{
    public required int UserId { get; set; }
    public required DateTime DeletedAt { get; set; }
}
```

### Domain Event

```csharp
public class OrderPlacedEvent : IEvent
{
    public required int OrderId { get; set; }
    public required int CustomerId { get; set; }
    public required decimal TotalAmount { get; set; }
    public required List<OrderItem> Items { get; set; }
    public required DateTime PlacedAt { get; set; }
}

public class OrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class PaymentProcessedEvent : IEvent
{
    public required int PaymentId { get; set; }
    public required int OrderId { get; set; }
    public required decimal Amount { get; set; }
    public required string PaymentMethod { get; set; }
    public required DateTime ProcessedAt { get; set; }
}
```

### Integration Event

```csharp
public class EmailSentEvent : IEvent
{
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required bool Success { get; set; }
    public required DateTime SentAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class InventoryUpdatedEvent : IEvent
{
    public required int ProductId { get; set; }
    public required int OldQuantity { get; set; }
    public required int NewQuantity { get; set; }
    public required DateTime UpdatedAt { get; set; }
}
```

## Event Naming Convention

Events should be named in past tense to indicate something has already happened:

```csharp
// ✅ Good - past tense
UserCreatedEvent
OrderPlacedEvent
PaymentProcessedEvent
EmailSentEvent
InventoryUpdatedEvent

// ❌ Poor - present/imperative tense
CreateUserEvent
PlaceOrderEvent
ProcessPaymentEvent
SendEmailEvent
UpdateInventoryEvent
```

## Defining Event Handlers

### Handler Interface

Event handlers implement `IEventHandler<T>`:

```csharp
using Zerra.CQRS;

public interface IUserEventHandler :
    IEventHandler<UserCreatedEvent>,
    IEventHandler<UserUpdatedEvent>,
    IEventHandler<UserDeletedEvent>
{
}

public interface IEmailEventHandler :
    IEventHandler<UserCreatedEvent>,
    IEventHandler<OrderPlacedEvent>
{
}
```

### Handler Implementation

Handlers inherit from `BaseHandler` to access bus context. Unlike command handlers, `IEventHandler<T>.Handle` takes only the event (`Task Handle(T @event)`) with no `CancellationToken`:

```csharp
public class UserEventHandler : BaseHandler, IUserEventHandler
{
    public async Task Handle(UserCreatedEvent @event)
    {
        Log?.Info($"User created event received: {@event.UserId}");

        // Send welcome email
        var emailService = Context.GetService<IEmailService>();
        await emailService.SendWelcomeEmailAsync(@event.Email);

        // Update analytics
        var analytics = Context.GetService<IAnalyticsService>();
        await analytics.TrackUserCreatedAsync(@event.UserId, @event.CreatedAt);
    }

    public async Task Handle(UserUpdatedEvent @event)
    {
        Log?.Info($"User updated event received: {@event.UserId}");

        // Invalidate cache
        var cache = Context.GetService<ICacheService>();
        await cache.RemoveAsync($"user:{@event.UserId}");

        // Update search index
        var searchService = Context.GetService<ISearchService>();
        await searchService.UpdateUserIndexAsync(@event.UserId);
    }

    public async Task Handle(UserDeletedEvent @event)
    {
        Log?.Info($"User deleted event received: {@event.UserId}");

        // Remove from cache
        var cache = Context.GetService<ICacheService>();
        await cache.RemoveAsync($"user:{@event.UserId}");

        // Archive user data
        var archiveService = Context.GetService<IArchiveService>();
        await archiveService.ArchiveUserAsync(@event.UserId, @event.DeletedAt);
    }
}
```

### Multiple Handlers for Same Event

Multiple services can handle the same event:

```csharp
// Email Service Handler
public class EmailEventHandler : BaseHandler, IEmailEventHandler
{
    public async Task Handle(UserCreatedEvent @event)
    {
        Log?.Info($"Sending welcome email to {@event.Email}");

        var emailService = Context.GetService<IEmailService>();
        await emailService.SendWelcomeEmailAsync(@event.Email);
    }

    public async Task Handle(OrderPlacedEvent @event)
    {
        Log?.Info($"Sending order confirmation for order {@event.OrderId}");

        var emailService = Context.GetService<IEmailService>();
        await emailService.SendOrderConfirmationAsync(@event.OrderId);
    }
}

// Analytics Service Handler
public class AnalyticsEventHandler : BaseHandler, IAnalyticsEventHandler
{
    public async Task Handle(UserCreatedEvent @event)
    {
        Log?.Info($"Tracking user creation: {@event.UserId}");

        var analytics = Context.GetService<IAnalyticsService>();
        await analytics.TrackEventAsync("UserCreated", new 
        { 
            UserId = @event.UserId,
            CreatedAt = @event.CreatedAt
        });
    }

    public async Task Handle(OrderPlacedEvent @event)
    {
        Log?.Info($"Tracking order placement: {@event.OrderId}");

        var analytics = Context.GetService<IAnalyticsService>();
        await analytics.TrackEventAsync("OrderPlaced", new 
        { 
            OrderId = @event.OrderId,
            CustomerId = @event.CustomerId,
            Amount = @event.TotalAmount
        });
    }
}
```

### Accessing BusContext

Event handlers have full access to bus context:

> The handler below reserves inventory and dispatches a payment command. Shown for the context API only: registered `PerReplica`, with several replicas, it would reserve the items and charge the customer once per replica. Real work like that belongs in a command, or in a consumer registered [`PerService`](#choosing-per-replica-or-per-service). See [Events Are Fanned Out to Every Replica](#events-are-fanned-out-to-every-replica).

```csharp
public class OrderEventHandler : BaseHandler, IOrderEventHandler
{
    public async Task Handle(OrderPlacedEvent @event)
    {
        // Access the bus for dispatching other messages
        IBus bus = this.Bus;  // or Context.Bus

        // Access logger
        ILogger? logger = this.Log;  // or Context.Log

        // Get service name
        string serviceName = Context.ServiceName;

        // Retrieve injected services
        var inventoryService = Context.GetService<IInventoryService>();
        var paymentService = Context.GetService<IPaymentService>();

        // Process order
        await inventoryService.ReserveItemsAsync(@event.Items);

        // Dispatch command to process payment
        await Bus.DispatchAsync(new ProcessPaymentCommand 
        { 
            OrderId = @event.OrderId,
            Amount = @event.TotalAmount
        });

        // Dispatch event to notify shipping
        await Bus.DispatchAsync(new OrderReadyForShippingEvent 
        { 
            OrderId = @event.OrderId 
        });
    }
}
```

## Dispatching Events

### Simple Event Dispatch

```csharp
// Dispatch event to all registered handlers/producers
await bus.DispatchAsync(new UserCreatedEvent 
{ 
    UserId = 123,
    Email = "user@example.com",
    CreatedAt = DateTime.UtcNow
});

// Execution continues immediately
// Event handlers process asynchronously
Console.WriteLine("Event dispatched");
```

### From Command Handler

```csharp
public class UserCommandHandler : BaseHandler, IUserCommandHandler
{
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var repository = Context.GetService<IUserRepository>();

        // Create user
        var user = new User 
        { 
            Email = command.Email, 
            Name = command.Name 
        };
        await repository.CreateAsync(user, cancellationToken);

        // Dispatch event to notify other services
        await Bus.DispatchAsync(new UserCreatedEvent 
        { 
            UserId = user.Id,
            Email = user.Email,
            CreatedAt = DateTime.UtcNow
        });

        Log?.Info($"User created and event dispatched: {user.Id}");
    }
}
```

### Event Chains

Events can trigger other events:

> Every replica that receives the first event runs the chain, so a three-replica service reserves inventory three times and dispatches three payment commands. Chain state-changing steps with commands, not events; use an event only for the notification at the end.

```csharp
public class OrderEventHandler : BaseHandler, IOrderEventHandler
{
    public async Task Handle(OrderPlacedEvent @event)
    {
        // Reserve inventory
        var inventoryService = Context.GetService<IInventoryService>();
        await inventoryService.ReserveItemsAsync(@event.Items);

        // Dispatch inventory reserved event
        await Bus.DispatchAsync(new InventoryReservedEvent 
        { 
            OrderId = @event.OrderId,
            ReservedAt = DateTime.UtcNow
        });
    }

    public async Task Handle(InventoryReservedEvent @event)
    {
        // Process payment
        var paymentService = Context.GetService<IPaymentService>();
        var paymentResult = await paymentService.ProcessPaymentAsync(@event.OrderId);

        // Dispatch payment processed event
        await Bus.DispatchAsync(new PaymentProcessedEvent 
        { 
            PaymentId = paymentResult.PaymentId,
            OrderId = @event.OrderId,
            Amount = paymentResult.Amount,
            PaymentMethod = paymentResult.Method,
            ProcessedAt = DateTime.UtcNow
        });
    }

    public async Task Handle(PaymentProcessedEvent @event)
    {
        // Notify shipping
        await Bus.DispatchAsync(new OrderReadyForShippingEvent 
        { 
            OrderId = @event.OrderId 
        });
    }
}
```

## Local vs Remote Event Processing

### Local Event Handling

```csharp
// Register handler locally
bus.AddHandler<IUserEventHandler>(new UserEventHandler());

// Dispatch - executes locally
await bus.DispatchAsync(new UserCreatedEvent 
{ 
    UserId = 123,
    Email = "user@example.com",
    CreatedAt = DateTime.UtcNow
});
```

### Remote Event Handling via Message Brokers

#### Kafka

```csharp
using Zerra.CQRS.Kafka;

// Publisher side
var kafkaProducer = new KafkaProducer("localhost:9092", serializer, encryptor, logger, environment: null, userName: null, password: null);
bus.AddEventProducer<IUserEventHandler>(kafkaProducer);

// Consumer side
var kafkaConsumer = new KafkaConsumer("localhost:9092", serializer, encryptor, logger, environment: null, userName: null, password: null);
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddEventConsumer<IUserEventHandler>(kafkaConsumer, EventConsumerMode.PerReplica);

// Dispatch - publishes to Kafka topic
await bus.DispatchAsync(new UserCreatedEvent 
{ 
    UserId = 123,
    Email = "user@example.com",
    CreatedAt = DateTime.UtcNow
});
```

#### RabbitMQ

```csharp
using Zerra.CQRS.RabbitMQ;

// Publisher side
var rabbitProducer = new RabbitMQProducer("localhost", serializer, encryptor, logger, environment: null);
bus.AddEventProducer<IUserEventHandler>(rabbitProducer);

// Consumer side
var rabbitConsumer = new RabbitMQConsumer("localhost", serializer, encryptor, logger, environment: null);
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddEventConsumer<IUserEventHandler>(rabbitConsumer, EventConsumerMode.PerReplica);
```

#### Azure Service Bus

```csharp
using Zerra.CQRS.AzureServiceBus;

// Publisher side
var asbProducer = new AzureServiceBusProducer(asbConnectionString, serializer, encryptor, logger, environment: null);
bus.AddEventProducer<IUserEventHandler>(asbProducer);

// Consumer side
var asbConsumer = new AzureServiceBusConsumer(asbConnectionString, serializer, encryptor, logger, environment: null);
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddEventConsumer<IUserEventHandler>(asbConsumer, EventConsumerMode.PerReplica);
```

### Hybrid - Local and Remote

Events can be handled both locally and remotely:

```csharp
// Register local handler
bus.AddHandler<IUserEventHandler>(new LocalUserEventHandler());

// Register remote producer
var kafkaProducer = new KafkaProducer("localhost:9092", serializer, encryptor, logger, environment: null, userName: null, password: null);
bus.AddEventProducer<IUserEventHandler>(kafkaProducer);

// Dispatch - executes locally AND publishes to Kafka
await bus.DispatchAsync(new UserCreatedEvent 
{ 
    UserId = 123,
    Email = "user@example.com",
    CreatedAt = DateTime.UtcNow
});
```

## Error Handling

### Handler Error Handling

```csharp
public class UserEventHandler : BaseHandler, IUserEventHandler
{
    public async Task Handle(UserCreatedEvent @event)
    {
        try
        {
            Log?.Info($"Processing UserCreatedEvent for user {@event.UserId}");

            var emailService = Context.GetService<IEmailService>();
            await emailService.SendWelcomeEmailAsync(@event.Email);

            Log?.Info($"Successfully processed UserCreatedEvent for user {@event.UserId}");
        }
        catch (Exception ex)
        {
            Log?.Error($"Failed to process UserCreatedEvent for user {@event.UserId}", ex);

            // Option 1: Swallow error (event processing continues)
            // return;

            // Option 2: Rethrow (may trigger retry mechanism if configured)
            throw;
        }
    }
}
```

### Idempotent Event Handling

Design handlers to be idempotent since events may be delivered multiple times. Note that idempotency handles redelivery to the *same* replica; it does not stop several replicas from doing the work concurrently, which is a separate question answered by [choosing a command](#events-are-fanned-out-to-every-replica):

```csharp
public async Task Handle(UserCreatedEvent @event)
{
    var cache = Context.GetService<ICacheService>();
    var processedKey = $"event:UserCreated:{@event.UserId}";

    // Check if already processed
    if (await cache.ExistsAsync(processedKey))
    {
        Log?.Debug($"Event already processed: UserCreatedEvent for user {@event.UserId}");
        return;
    }

    try
    {
        // Process event
        var emailService = Context.GetService<IEmailService>();
        await emailService.SendWelcomeEmailAsync(@event.Email);

        // Mark as processed
        await cache.SetAsync(processedKey, true, TimeSpan.FromDays(7));

        Log?.Info($"Successfully processed UserCreatedEvent for user {@event.UserId}");
    }
    catch (Exception ex)
    {
        Log?.Error($"Failed to process UserCreatedEvent for user {@event.UserId}", ex);
        throw;
    }
}
```

## Event Sourcing Pattern

Use events as the source of truth:

```csharp
// Event definitions
public class AccountCreatedEvent : IEvent
{
    public required Guid AccountId { get; set; }
    public required string AccountNumber { get; set; }
    public required decimal InitialBalance { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class MoneyDepositedEvent : IEvent
{
    public required Guid AccountId { get; set; }
    public required decimal Amount { get; set; }
    public required DateTime DepositedAt { get; set; }
}

public class MoneyWithdrawnEvent : IEvent
{
    public required Guid AccountId { get; set; }
    public required decimal Amount { get; set; }
    public required DateTime WithdrawnAt { get; set; }
}

// Event store handler
public class AccountEventStoreHandler : BaseHandler,
    IEventHandler<AccountCreatedEvent>,
    IEventHandler<MoneyDepositedEvent>,
    IEventHandler<MoneyWithdrawnEvent>
{
    public async Task Handle(AccountCreatedEvent @event)
    {
        var eventStore = Context.GetService<IEventStore>();
        await eventStore.AppendEventAsync(@event.AccountId, @event);
    }

    public async Task Handle(MoneyDepositedEvent @event)
    {
        var eventStore = Context.GetService<IEventStore>();
        await eventStore.AppendEventAsync(@event.AccountId, @event);
    }

    public async Task Handle(MoneyWithdrawnEvent @event)
    {
        var eventStore = Context.GetService<IEventStore>();
        await eventStore.AppendEventAsync(@event.AccountId, @event);
    }
}

// Projection handler
public class AccountProjectionHandler : BaseHandler,
    IEventHandler<AccountCreatedEvent>,
    IEventHandler<MoneyDepositedEvent>,
    IEventHandler<MoneyWithdrawnEvent>
{
    public async Task Handle(AccountCreatedEvent @event)
    {
        var repository = Context.GetService<IAccountRepository>();

        var account = new Account
        {
            Id = @event.AccountId,
            AccountNumber = @event.AccountNumber,
            Balance = @event.InitialBalance,
            CreatedAt = @event.CreatedAt
        };

        await repository.CreateAsync(account);
    }

    public async Task Handle(MoneyDepositedEvent @event)
    {
        var repository = Context.GetService<IAccountRepository>();
        var account = await repository.GetByIdAsync(@event.AccountId);

        account.Balance += @event.Amount;
        await repository.UpdateAsync(account);
    }

    public async Task Handle(MoneyWithdrawnEvent @event)
    {
        var repository = Context.GetService<IAccountRepository>();
        var account = await repository.GetByIdAsync(@event.AccountId);

        account.Balance -= @event.Amount;
        await repository.UpdateAsync(account);
    }
}
```

## Best Practices

### 1. Name Events in Past Tense

```csharp
// ✅ Good - past tense
public class UserCreatedEvent : IEvent
public class OrderPlacedEvent : IEvent
public class PaymentProcessedEvent : IEvent

// ❌ Poor - present/imperative tense
public class CreateUserEvent : IEvent
public class PlaceOrderEvent : IEvent
public class ProcessPaymentEvent : IEvent
```

### 2. Make Events Immutable

```csharp
// ✅ Good - required properties, init-only
public class UserCreatedEvent : IEvent
{
    public required int UserId { get; init; }
    public required string Email { get; init; }
    public required DateTime CreatedAt { get; init; }
}

// ❌ Poor - mutable
public class UserCreatedEvent : IEvent
{
    public int UserId { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3. Include All Relevant Data

```csharp
// ✅ Good - includes context
public class OrderPlacedEvent : IEvent
{
    public required int OrderId { get; init; }
    public required int CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required List<OrderItem> Items { get; init; }
    public required DateTime PlacedAt { get; init; }
}

// ❌ Poor - missing context
public class OrderPlacedEvent : IEvent
{
    public required int OrderId { get; init; }
}
```

### 4. Design Idempotent Handlers

```csharp
// ✅ Good - idempotent
public async Task Handle(UserCreatedEvent @event)
{
    // Check if already processed
    if (await IsAlreadyProcessedAsync(@event.UserId))
        return;

    // Process event
    await ProcessEventAsync(@event);

    // Mark as processed
    await MarkAsProcessedAsync(@event.UserId);
}
```

### 5. Handle Errors Gracefully

```csharp
// ✅ Good - handles errors, continues processing
public async Task Handle(UserCreatedEvent @event)
{
    try
    {
        await SendWelcomeEmailAsync(@event.Email);
    }
    catch (Exception ex)
    {
        Log?.Error($"Failed to send welcome email to {@event.Email}", ex);
        // Continue - don't let email failure break event processing
    }

    try
    {
        await UpdateAnalyticsAsync(@event);
    }
    catch (Exception ex)
    {
        Log?.Error("Failed to update analytics", ex);
        // Continue
    }
}
```

### 6. Keep Handlers Focused

```csharp
// ✅ Good - single responsibility
public class EmailEventHandler : BaseHandler, IEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent @event)
    {
        var emailService = Context.GetService<IEmailService>();
        await emailService.SendWelcomeEmailAsync(@event.Email);
    }
}

public class AnalyticsEventHandler : BaseHandler, IEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent @event)
    {
        var analytics = Context.GetService<IAnalyticsService>();
        await analytics.TrackUserCreatedAsync(@event.UserId);
    }
}

// ❌ Poor - doing too much
public class UserEventHandler : BaseHandler, IEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent @event)
    {
        // Sending email
        // Updating analytics
        // Updating cache
        // Updating search index
        // Dispatching other events
        // Too many responsibilities!
    }
}
```

## Common Patterns

### Saga Coordination with Events

> A saga step must run once. Under `PerReplica` every replica of the service below receives each event and dispatches its own copy of the next command, so the saga advances once per replica. Register the coordinator's consumer [`PerService`](#choosing-per-replica-or-per-service) so its replicas compete, drive the steps with commands, or give the coordinator its own single-instance deployment.

```csharp
public class OrderSagaEventHandler : BaseHandler,
    IEventHandler<OrderPlacedEvent>,
    IEventHandler<InventoryReservedEvent>,
    IEventHandler<InventoryReservationFailedEvent>,
    IEventHandler<PaymentProcessedEvent>,
    IEventHandler<PaymentFailedEvent>
{
    public async Task Handle(OrderPlacedEvent @event)
    {
        // Start saga - reserve inventory
        await Bus.DispatchAsync(new ReserveInventoryCommand 
        { 
            OrderId = @event.OrderId,
            Items = @event.Items
        });
    }

    public async Task Handle(InventoryReservedEvent @event)
    {
        // Continue saga - process payment
        await Bus.DispatchAsync(new ProcessPaymentCommand 
        { 
            OrderId = @event.OrderId
        });
    }

    public async Task Handle(InventoryReservationFailedEvent @event)
    {
        // Saga failed - cancel order
        await Bus.DispatchAsync(new CancelOrderCommand 
        { 
            OrderId = @event.OrderId,
            Reason = "Insufficient inventory"
        });
    }

    public async Task Handle(PaymentProcessedEvent @event)
    {
        // Saga success - ship order
        await Bus.DispatchAsync(new ShipOrderCommand 
        { 
            OrderId = @event.OrderId
        });
    }

    public async Task Handle(PaymentFailedEvent @event)
    {
        // Saga failed - release inventory and cancel order
        await Bus.DispatchAsync(new ReleaseInventoryCommand 
        { 
            OrderId = @event.OrderId
        });

        await Bus.DispatchAsync(new CancelOrderCommand 
        { 
            OrderId = @event.OrderId,
            Reason = "Payment failed"
        });
    }
}
```

### CQRS Read Model Projection

> A projection into a **shared** store, as below, must be written once: drive it with a command, run exactly one projector instance, or register the consumer with [`EventConsumerMode.PerService`](#choosing-per-replica-or-per-service) so the replicas compete for the events. Projecting into a read model each replica keeps in its **own memory** is the case events are made for.

```csharp
public class UserReadModelProjection : BaseHandler,
    IEventHandler<UserCreatedEvent>,
    IEventHandler<UserUpdatedEvent>,
    IEventHandler<UserDeletedEvent>
{
    public async Task Handle(UserCreatedEvent @event)
    {
        var readModelRepo = Context.GetService<IUserReadModelRepository>();

        var readModel = new UserReadModel
        {
            UserId = @event.UserId,
            Email = @event.Email,
            CreatedAt = @event.CreatedAt,
            IsActive = true
        };

        await readModelRepo.CreateAsync(readModel);
    }

    public async Task Handle(UserUpdatedEvent @event)
    {
        var readModelRepo = Context.GetService<IUserReadModelRepository>();
        var readModel = await readModelRepo.GetByIdAsync(@event.UserId);

        readModel.Email = @event.Email;
        readModel.UpdatedAt = @event.UpdatedAt;

        await readModelRepo.UpdateAsync(readModel);
    }

    public async Task Handle(UserDeletedEvent @event)
    {
        var readModelRepo = Context.GetService<IUserReadModelRepository>();
        await readModelRepo.DeleteAsync(@event.UserId);
    }
}
```

## See Also

- [Commands](Commands.md) - Execute state-changing operations
- [Queries](Queries.md) - Execute read operations
- [Service Injection](ServiceInjection.md) - Access services in handlers
- [Server Setup](ServerSetup.md) - Configure event consumers
- [Client Setup](ClientSetup.md) - Configure event producers
- [Logging](Logging.md) - Implement logging in handlers
