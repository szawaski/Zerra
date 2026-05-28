[← Back to Documentation](Index.md)

# Commands

Commands represent operations that modify state in your application. Zerra provides a robust command pattern with support for fire-and-forget, awaitable, and result-returning commands.

## Overview

Commands in Zerra:
- Represent state-changing operations
- Can be fire-and-forget or awaitable
- Can return results (`ICommand<TResult>`)
- Dispatch asynchronously to local or remote handlers
- Support timeouts and cancellation
- CancellationTokens do **NOT** propagate remotely (important difference from queries)
- Handler execution can be local or distributed via message brokers
- Exceptions thrown in a remote handler are returned as `RemoteServiceException` **only** when using `DispatchAwaitAsync`; fire-and-forget `DispatchAsync` does not return exceptions

## Command Types

### Simple Command (No Result)

Commands that modify state without returning data:

```csharp
using Zerra.CQRS;

public class CreateUserCommand : ICommand
{
    public required string Email { get; set; }
    public required string Name { get; set; }
}

public class UpdateUserCommand : ICommand
{
    public required int UserId { get; set; }
    public required string Email { get; set; }
}

public class DeleteUserCommand : ICommand
{
    public required int UserId { get; set; }
}
```

### Command with Result

Commands that return data after execution:

```csharp
using Zerra.CQRS;

public class CreateUserCommand : ICommand<int>  // Returns user ID
{
    public required string Email { get; set; }
    public required string Name { get; set; }
}

public class UpdateUserCommand : ICommand<User>  // Returns updated user
{
    public required int UserId { get; set; }
    public required string Email { get; set; }
}

public class ProcessOrderCommand : ICommand<OrderResult>
{
    public required int OrderId { get; set; }
    public required decimal Amount { get; set; }
}

public class OrderResult
{
    public int OrderId { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
}
```

## Defining Command Handlers

### Handler Interface

Commands are handled through handler interfaces:

```csharp
using Zerra.CQRS;

public interface IUserCommandHandler :
    ICommandHandler<CreateUserCommand>,           // No result
    ICommandHandler<UpdateUserCommand>,           // No result
    ICommandHandler<DeleteUserCommand>,           // No result
    ICommandHandler<ActivateUserCommand, User>,   // Returns User
    ICommandHandler<DeactivateUserCommand, bool>  // Returns bool
{
}
```

### Handler Implementation

Handlers inherit from `BaseHandler` to access bus context:

```csharp
using Zerra.CQRS;

public class UserCommandHandler : BaseHandler, IUserCommandHandler
{
    // Simple command - no result
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        Log?.Info($"Creating user: {command.Email}");

        var repository = Context.GetService<IUserRepository>();

        var user = new User
        {
            Email = command.Email,
            Name = command.Name
        };

        await repository.CreateAsync(user, cancellationToken);

        Log?.Info($"User created: {command.Email}");
    }

    // Command with result
    public async Task<User> Handle(ActivateUserCommand command, CancellationToken cancellationToken)
    {
        Log?.Info($"Activating user: {command.UserId}");

        var repository = Context.GetService<IUserRepository>();
        var user = await repository.GetByIdAsync(command.UserId, cancellationToken);

        if (user == null)
            throw new KeyNotFoundException($"User {command.UserId} not found");

        user.IsActive = true;
        user.ActivatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(user, cancellationToken);

        Log?.Info($"User activated: {command.UserId}");
        return user;
    }

    public async Task Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var repository = Context.GetService<IUserRepository>();
        var user = await repository.GetByIdAsync(command.UserId, cancellationToken);

        if (user == null)
            throw new KeyNotFoundException($"User {command.UserId} not found");

        user.Email = command.Email;
        await repository.UpdateAsync(user, cancellationToken);

        // Dispatch event to notify of change
        await Bus.DispatchAsync(new UserUpdatedEvent 
        { 
            UserId = user.Id, 
            Email = user.Email 
        });
    }

    public async Task<bool> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        var repository = Context.GetService<IUserRepository>();
        var user = await repository.GetByIdAsync(command.UserId, cancellationToken);

        if (user == null)
            return false;

        user.IsActive = false;
        await repository.UpdateAsync(user, cancellationToken);

        return true;
    }

    public async Task Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var repository = Context.GetService<IUserRepository>();
        await repository.DeleteAsync(command.UserId, cancellationToken);
    }
}
```

### Accessing BusContext

Like query handlers, command handlers inherit from `BaseHandler`:

```csharp
public class UserCommandHandler : BaseHandler, IUserCommandHandler
{
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Access the bus for dispatching events or other commands
        IBus bus = this.Bus;  // or Context.Bus

        // Access logger
        ILogger? logger = this.Log;  // or Context.Log

        // Get service name
        string serviceName = Context.Service;

        // Retrieve injected services
        var repository = Context.GetService<IUserRepository>();
        var emailService = Context.GetService<IEmailService>();

        // Create user
        var user = new User { Email = command.Email, Name = command.Name };
        await repository.CreateAsync(user, cancellationToken);

        // Dispatch event
        await Bus.DispatchAsync(new UserCreatedEvent { UserId = user.Id, Email = user.Email });

        // Send email (optional service)
        if (Context.TryGetService<IEmailService>(out var email))
        {
            await email.SendWelcomeEmailAsync(user.Email, cancellationToken);
        }
    }
}
```

## Dispatching Commands

### DispatchAsync - Fire and Forget

Dispatch a command without waiting for completion:

```csharp
// Fire and forget - returns immediately
await bus.DispatchAsync(new CreateUserCommand 
{ 
    Email = "user@example.com",
    Name = "John Doe"
});

// Continues execution immediately (command may still be processing)
Console.WriteLine("Command dispatched");
```

**Use when:**
- You don't need to wait for completion
- You want maximum throughput
- The command is idempotent
- You're using message brokers for eventual consistency

### DispatchAwaitAsync - Wait for Completion

Wait for remote service to complete command processing:

```csharp
// Wait for command to complete on remote service
await bus.DispatchAwaitAsync(new CreateUserCommand 
{ 
    Email = "user@example.com",
    Name = "John Doe"
});

// Command has completed (or failed with exception)
Console.WriteLine("Command completed");
```

**Use when:**
- You need confirmation of completion
- You want to handle failures immediately
- Order of operations matters

### DispatchAwaitAsync with Result

Commands with results automatically await remote completion:

```csharp
// Automatically waits for result from remote service
var userId = await bus.DispatchAwaitAsync(new CreateUserCommand 
{ 
    Email = "user@example.com",
    Name = "John Doe"
});

Log?.Info($"Created user with ID: {userId}");

// Or with complex result
var user = await bus.DispatchAwaitAsync(new ActivateUserCommand 
{ 
    UserId = userId 
});

Log?.Info($"Activated user: {user.Email}");
```

## CancellationToken Behavior

⚠️ **Important**: CancellationTokens behave differently for commands vs queries:

### Commands - No Remote Propagation

```csharp
// CancellationToken does NOT propagate to remote service
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    // If the command is sent remotely before cancellation, it will complete
    // even if the token is cancelled on the client side
    await bus.DispatchAwaitAsync(new LongRunningCommand(), cts.Token);
}
catch (OperationCanceledException)
{
    // Thrown if cancelled before dispatch completes
    // Remote handler continues executing
    Log?.Warning("Dispatch was cancelled, but remote handler continues");
}
```

### Queries - Remote Propagation

```csharp
// CancellationToken DOES propagate to remote service
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    // Token propagates to remote service, which can respond to cancellation
    var result = await bus.Call<IQueries>().LongRunningQuery(cts.Token);
}
catch (OperationCanceledException)
{
    // Remote query respects cancellation
    Log?.Info("Query was cancelled");
}
```

### Why the Difference?

- **Commands**: Represent state changes; cancelling mid-flight could leave system in inconsistent state
- **Queries**: Read-only operations; safe to cancel at any time

## Local vs Remote Dispatch

### Local Command Handling

```csharp
// Server/Handler registration
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());

// Dispatch - executes locally
await bus.DispatchAwaitAsync(new CreateUserCommand 
{ 
    Email = "user@example.com",
    Name = "John Doe"
});
```

### Remote Command Handling via TCP/HTTP

```csharp
// Server side
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);
bus.AddCommandConsumer<IUserCommandHandler>(server);

// Client side
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, logger);
bus.AddCommandProducer<IUserCommandHandler>(client);

// Dispatch - executes remotely
await bus.DispatchAwaitAsync(new CreateUserCommand 
{ 
    Email = "user@example.com",
    Name = "John Doe"
});
```

### Remote Command Handling via Message Brokers

```csharp
using Zerra.CQRS.Kafka;
using Zerra.CQRS.RabbitMQ;
using Zerra.CQRS.AzureServiceBus;

// Server side - Kafka consumer
var kafkaConsumer = new KafkaConsumer(kafkaConfig, serializer, encryptor, logger);
bus.AddCommandConsumer<IUserCommandHandler>(kafkaConsumer);

// Client side - Kafka producer
var kafkaProducer = new KafkaProducer(kafkaConfig, serializer, encryptor, logger);
bus.AddCommandProducer<IUserCommandHandler>(kafkaProducer);

// Dispatch - sends to Kafka topic
await bus.DispatchAsync(new CreateUserCommand 
{ 
    Email = "user@example.com",
    Name = "John Doe"
});
```

## Dispatching Methods

### DispatchAsync vs DispatchAwaitAsync

Zerra provides two dispatch methods with different behaviors:

#### DispatchAsync - Fire and Forget

```csharp
// ✅ Fire and forget - returns immediately after sending
await bus.DispatchAsync(new CreateUserCommand 
{ 
    Email = "user@example.com",
    Name = "John Doe"
});

// Continues execution immediately (command may still be processing)
Log?.Info("Command dispatched");
```

**Use when:**
- You don't need to wait for completion
- You want maximum throughput
- The command is idempotent
- You're using message brokers for eventual consistency

#### DispatchAwaitAsync - Wait for Completion

```csharp
// ✅ Wait for command to complete on remote service
await bus.DispatchAwaitAsync(new CreateUserCommand 
{ 
    Email = "user@example.com",
    Name = "John Doe"
});

// Command has completed (or failed with exception)
Console.WriteLine("Command completed");
```

**Use when:**
- You need confirmation of completion
- You want to handle failures immediately
- Order of operations matters
- You need a result from the command

**Always prefer async methods for better scalability and resource utilization.**

## Error Handling

### Server-Side Error Handling

Handlers should throw meaningful exceptions to communicate failures. Any unhandled exception in a handler is captured and, when the command was awaited, returned to the caller.

```csharp
public class UserCommandHandler : BaseHandler, IUserCommandHandler
{
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Validate input - throw descriptive exceptions the caller can act on
        if (string.IsNullOrWhiteSpace(command.Email))
            throw new ArgumentException("Email is required", nameof(command.Email));

        var repository = Context.GetService<IUserRepository>();

        var existingUser = await repository.FindByEmailAsync(command.Email, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException($"User with email {command.Email} already exists");

        var user = new User { Email = command.Email, Name = command.Name };
        await repository.CreateAsync(user, cancellationToken);

        Log?.Info($"User created: {command.Email}");
    }
}
```

### Client-Side Error Handling

When a remote handler throws an exception it is captured by the framework and returned to the caller as a `RemoteServiceException`. The exception carries the original error message (`Message`), the original exception type name (`ErrorType`), the command that caused it (`Source`), and the remote stack trace (`StackTrace`). There is no `InnerException`.

This only applies when using `DispatchAwaitAsync`. Fire-and-forget `DispatchAsync` does not wait for the handler to complete and therefore cannot return exceptions — see [Fire-and-Forget Does Not Return Exceptions](#fire-and-forget-does-not-return-exceptions) below.

```csharp
try
{
    await bus.DispatchAwaitAsync(new CreateUserCommand 
    { 
        Email = "user@example.com",
        Name = "John Doe"
    });

    Log?.Info("User created successfully");
}
catch (RemoteServiceException ex)
{
    // The remote handler threw an exception.
    // Use ErrorType to distinguish the original exception without an InnerException.
    Log?.Error($"Source:     {ex.Source}");    // e.g. "CreateUserCommand"
    Log?.Error($"Error type: {ex.ErrorType}"); // e.g. "ArgumentException"
    Log?.Error($"Message:    {ex.Message}");

    if (ex.ErrorType == nameof(ArgumentException))
        Log?.Error("Validation error on server");
    else if (ex.ErrorType == nameof(InvalidOperationException))
        Log?.Error("Business rule violation on server");
    else if (ex.ErrorType == nameof(KeyNotFoundException))
        Log?.Warning("Entity not found on server");
}
catch (TimeoutException)
{
    // The server did not respond in time.
    // The command may or may not have been processed.
    Log?.Warning("Command timed out");
}
catch (Exception ex)
{
    // Network or connection error — command was not delivered.
    Log?.Error($"Command failed: {ex.Message}");
}
```

### Exception Types at a Glance

Understanding which exception type is thrown helps distinguish between different failure scenarios:

| Exception | Meaning | Command delivered? |
|---|---|---|
| `RemoteServiceException` | Handler code threw — validation failed, business rule violated, etc. Check `ErrorType` and `Message` for details. | ✅ Yes |
| `TimeoutException` | Server did not respond in time | ⚠️ Unknown |
| `IOException` / other | Network or connection failure | ❌ No |

### Fire-and-Forget Does Not Return Exceptions

⚠️ **Important**: `DispatchAsync` (fire-and-forget) returns as soon as the command is sent and does **not** propagate exceptions from the remote handler. If you need to handle server-side failures, use `DispatchAwaitAsync` instead.

```csharp
// DispatchAsync returns immediately - exceptions from the handler are never surfaced
await bus.DispatchAsync(new CreateUserCommand 
{ 
    Email = "invalid-email",
    Name = "John"
});

// Execution continues here regardless of what happens on the server

// ✅ Use DispatchAwaitAsync to receive exceptions
try
{
    await bus.DispatchAwaitAsync(new CreateUserCommand 
    { 
        Email = "invalid-email",
        Name = "John"
    });
}
catch (RemoteServiceException ex)
{
    Log?.Error($"Server error ({ex.ErrorType}): {ex.Message}");
}
```

## Idempotency

Design commands to be idempotent when possible:

```csharp
public class ActivateUserCommand : ICommand
{
    public required int UserId { get; set; }
}

// Idempotent handler - safe to call multiple times
public async Task Handle(ActivateUserCommand command, CancellationToken cancellationToken)
{
    var repository = Context.GetService<IUserRepository>();
    var user = await repository.GetByIdAsync(command.UserId, cancellationToken);

    if (user == null)
        throw new KeyNotFoundException($"User {command.UserId} not found");

    // Check if already active - makes operation idempotent
    if (user.IsActive)
    {
        Log?.Debug($"User {command.UserId} already active");
        return;  // No-op if already active
    }

    user.IsActive = true;
    user.ActivatedAt = DateTime.UtcNow;
    await repository.UpdateAsync(user, cancellationToken);
}
```

## Command Chaining

Commands can dispatch other commands or events:

```csharp
public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
{
    var repository = Context.GetService<IUserRepository>();

    // Create user
    var user = new User { Email = command.Email, Name = command.Name };
    await repository.CreateAsync(user, cancellationToken);

    // Dispatch event to notify other services
    await Bus.DispatchAsync(new UserCreatedEvent 
    { 
        UserId = user.Id, 
        Email = user.Email 
    });

    // Dispatch command to send welcome email (could be to different service)
    await Bus.DispatchAsync(new SendWelcomeEmailCommand 
    { 
        UserId = user.Id,
        Email = user.Email
    });
}
```

## Best Practices

### 1. Name Commands with Intent

```csharp
// ✅ Good - clear intent
CreateUserCommand
UpdateUserEmailCommand
ActivateUserCommand
DeactivateUserCommand

// ❌ Poor - vague
UserCommand
UpdateCommand
ProcessCommand
```

### 2. Make Commands Immutable

```csharp
// ✅ Good - required properties, init-only
public class CreateUserCommand : ICommand
{
    public required string Email { get; init; }
    public required string Name { get; init; }
}

// ❌ Poor - mutable
public class CreateUserCommand : ICommand
{
    public string Email { get; set; }
    public string Name { get; set; }
}
```

### 3. Validate in Handler

```csharp
// ✅ Good - validate in handler
public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(command.Email))
        throw new ArgumentException("Email is required");

    if (!IsValidEmail(command.Email))
        throw new ArgumentException("Email is not valid");

    // Process command
}
```

### 4. Use Appropriate Dispatch Method

```csharp
// ✅ Fire-and-forget for non-critical operations
await bus.DispatchAsync(new LogUserActivityCommand { UserId = userId });

// ✅ Await for critical operations
await bus.DispatchAwaitAsync(new ChargePaymentCommand { OrderId = orderId, Amount = 100.00m });

// ✅ Automatically awaits for commands with results
var userId = await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com" });
```

### 5. Handle Errors Appropriately

```csharp
// ✅ Good - specific exceptions
if (user == null)
    throw new KeyNotFoundException($"User {id} not found");

if (user.IsDeleted)
    throw new InvalidOperationException("Cannot update deleted user");

// ❌ Poor - generic exceptions
throw new Exception("Something went wrong");
```

### 6. Log Important Operations

```csharp
// ✅ Good - contextual logging
public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
{
    Log?.Info($"Creating user: {command.Email}");

    try
    {
        // Process...
        Log?.Info($"User created successfully: {command.Email}");
    }
    catch (Exception ex)
    {
        Log?.Error($"Failed to create user: {command.Email}", ex);
        throw;
    }
}
```

## Common Patterns

### Transaction Pattern

```csharp
public async Task Handle(TransferFundsCommand command, CancellationToken cancellationToken)
{
    var repository = Context.GetService<IAccountRepository>();

    // Use transaction for atomicity
    using var transaction = await repository.BeginTransactionAsync(cancellationToken);

    try
    {
        var fromAccount = await repository.GetByIdAsync(command.FromAccountId, cancellationToken);
        var toAccount = await repository.GetByIdAsync(command.ToAccountId, cancellationToken);

        fromAccount.Balance -= command.Amount;
        toAccount.Balance += command.Amount;

        await repository.UpdateAsync(fromAccount, cancellationToken);
        await repository.UpdateAsync(toAccount, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        await Bus.DispatchAsync(new FundsTransferredEvent 
        { 
            FromAccountId = command.FromAccountId,
            ToAccountId = command.ToAccountId,
            Amount = command.Amount
        });
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

### Saga Pattern

```csharp
public async Task Handle(CreateOrderCommand command, CancellationToken cancellationToken)
{
    // Step 1: Reserve inventory
    var inventoryReserved = await Bus.DispatchAwaitAsync(new ReserveInventoryCommand 
    { 
        ProductId = command.ProductId, 
        Quantity = command.Quantity 
    });

    if (!inventoryReserved)
        throw new InvalidOperationException("Insufficient inventory");

    try
    {
        // Step 2: Charge payment
        await Bus.DispatchAwaitAsync(new ChargePaymentCommand 
        { 
            CustomerId = command.CustomerId, 
            Amount = command.Amount 
        });

        // Step 3: Create order
        await Bus.DispatchAwaitAsync(new FinalizeOrderCommand 
        { 
            OrderId = command.OrderId 
        });
    }
    catch
    {
        // Compensating action: release inventory
        await Bus.DispatchAsync(new ReleaseInventoryCommand 
        { 
            ProductId = command.ProductId, 
            Quantity = command.Quantity 
        });

        throw;
    }
}
```

## See Also

- [Queries](Queries.md) - Execute read operations
- [Events](Events.md) - Publish state change notifications
- [Service Injection](ServiceInjection.md) - Access services in handlers
- [Server Setup](ServerSetup.md) - Configure command consumers
- [Client Setup](ClientSetup.md) - Configure command producers
- [Logging](Logging.md) - Implement logging in handlers
