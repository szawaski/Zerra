[← Back to Documentation](Index.md)

# Events

Events represent state changes that have occurred in your application. Zerra implements a publish-subscribe pattern where events are dispatched to zero or more subscribers for eventual consistency and reactive behavior.

## Overview

Events in Zerra:
- Represent state changes that have already occurred
- Follow publish-subscribe pattern (one-to-many)
- Multiple handlers can respond to the same event
- Dispatched asynchronously to local or remote handlers
- Support distributed event-driven architecture
- Used for eventual consistency and reactive workflows
- Can be consumed from message brokers for scalable processing

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

Handlers inherit from `BaseHandler` to access bus context:

```csharp
public class UserEventHandler : BaseHandler, IUserEventHandler
{
    public async Task Handle(UserCreatedEvent @event)
    {
        Log?.Info($"User created event received: {@@event.UserId}");

        // Send welcome email
        var emailService = Context.GetService<IEmailService>();
        await emailService.SendWelcomeEmailAsync(@event.Email);

        // Update analytics
        var analytics = Context.GetService<IAnalyticsService>();
        await analytics.TrackUserCreatedAsync(@event.UserId, @event.CreatedAt);
    }

    public async Task Handle(UserUpdatedEvent @event)
    {
        Log?.Info($"User updated event received: {@@event.UserId}");

        // Invalidate cache
        var cache = Context.GetService<ICacheService>();
        await cache.RemoveAsync($"user:{@@event.UserId}");

        // Update search index
        var searchService = Context.GetService<ISearchService>();
        await searchService.UpdateUserIndexAsync(@event.UserId);
    }

    public async Task Handle(UserDeletedEvent @event)
    {
        Log?.Info($"User deleted event received: {@@event.UserId}");

        // Remove from cache
        var cache = Context.GetService<ICacheService>();
        await cache.RemoveAsync($"user:{@@event.UserId}");

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
        Log?.Info($"Sending welcome email to {@@event.Email}");

        var emailService = Context.GetService<IEmailService>();
        await emailService.SendWelcomeEmailAsync(@event.Email);
    }

    public async Task Handle(OrderPlacedEvent @event)
    {
        Log?.Info($"Sending order confirmation for order {@@event.OrderId}");

        var emailService = Context.GetService<IEmailService>();
        await emailService.SendOrderConfirmationAsync(@event.OrderId);
    }
}

// Analytics Service Handler
public class AnalyticsEventHandler : BaseHandler, IAnalyticsEventHandler
{
    public async Task Handle(UserCreatedEvent @event)
    {
        Log?.Info($"Tracking user creation: {@@event.UserId}");

        var analytics = Context.GetService<IAnalyticsService>();
        await analytics.TrackEventAsync("UserCreated", new 
        { 
            UserId = @event.UserId,
            CreatedAt = @event.CreatedAt
        });
    }

    public async Task Handle(OrderPlacedEvent @event)
    {
        Log?.Info($"Tracking order placement: {@@event.OrderId}");

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
        string serviceName = Context.Service;

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
var kafkaProducer = new KafkaProducer(kafkaConfig, serializer, encryptor, logger);
bus.AddEventProducer<IUserEventHandler>(kafkaProducer);

// Consumer side
var kafkaConsumer = new KafkaConsumer(kafkaConfig, serializer, encryptor, logger);
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddEventConsumer<IUserEventHandler>(kafkaConsumer);

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
var rabbitProducer = new RabbitMQProducer(rabbitConfig, serializer, encryptor, logger);
bus.AddEventProducer<IUserEventHandler>(rabbitProducer);

// Consumer side
var rabbitConsumer = new RabbitMQConsumer(rabbitConfig, serializer, encryptor, logger);
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddEventConsumer<IUserEventHandler>(rabbitConsumer);
```

#### Azure Service Bus

```csharp
using Zerra.CQRS.AzureServiceBus;

// Publisher side
var asbProducer = new AzureServiceBusProducer(asbConfig, serializer, encryptor, logger);
bus.AddEventProducer<IUserEventHandler>(asbProducer);

// Consumer side
var asbConsumer = new AzureServiceBusConsumer(asbConfig, serializer, encryptor, logger);
bus.AddHandler<IUserEventHandler>(new UserEventHandler());
bus.AddEventConsumer<IUserEventHandler>(asbConsumer);
```

### Hybrid - Local and Remote

Events can be handled both locally and remotely:

```csharp
// Register local handler
bus.AddHandler<IUserEventHandler>(new LocalUserEventHandler());

// Register remote producer
var kafkaProducer = new KafkaProducer(kafkaConfig, serializer, encryptor, logger);
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
            Log?.Info($"Processing UserCreatedEvent for user {@@event.UserId}");

            var emailService = Context.GetService<IEmailService>();
            await emailService.SendWelcomeEmailAsync(@event.Email);

            Log?.Info($"Successfully processed UserCreatedEvent for user {@@event.UserId}");
        }
        catch (Exception ex)
        {
            Log?.Error($"Failed to process UserCreatedEvent for user {@@event.UserId}", ex);

            // Option 1: Swallow error (event processing continues)
            // return;

            // Option 2: Rethrow (may trigger retry mechanism if configured)
            throw;
        }
    }
}
```

### Idempotent Event Handling

Design handlers to be idempotent since events may be delivered multiple times:

```csharp
public async Task Handle(UserCreatedEvent @event)
{
    var cache = Context.GetService<ICacheService>();
    var processedKey = $"event:UserCreated:{@@event.UserId}";

    // Check if already processed
    if (await cache.ExistsAsync(processedKey))
    {
        Log?.Debug($"Event already processed: UserCreatedEvent for user {@@event.UserId}");
        return;
    }

    try
    {
        // Process event
        var emailService = Context.GetService<IEmailService>();
        await emailService.SendWelcomeEmailAsync(@event.Email);

        // Mark as processed
        await cache.SetAsync(processedKey, true, TimeSpan.FromDays(7));

        Log?.Info($"Successfully processed UserCreatedEvent for user {@@event.UserId}");
    }
    catch (Exception ex)
    {
        Log?.Error($"Failed to process UserCreatedEvent for user {@@event.UserId}", ex);
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
        Log?.Error($"Failed to send welcome email to {@@event.Email}", ex);
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
