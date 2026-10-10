[← Back to Documentation](Index.md)

# Commands

A command asks for a change of state. It is **handled once**, by one replica of the handling service, however many replicas are running. That is the difference from events, which reach every replica unless the subscriber registers [`EventConsumerMode.PerService`](Events.md#choosing-per-replica-or-per-service). Work that must happen once belongs in a command. See [Events Are Fanned Out to Every Replica](Events.md#events-are-fanned-out-to-every-replica).

"Once" is about replicas, not delivery: a remote command is delivered at most once and isn't retried if its handler fails or its process crashes. With [resilient commands](Reliability.md#resilient-commands), a command whose process crashes while handling it goes to another replica instead. See [Delivery and Failure Handling](Reliability.md).

A command can be sent fire-and-forget or awaited, can return a result, and is handled locally or remotely depending only on how the bus is set up.

## Defining Commands

A command implements `ICommand`, or `ICommand<TResult>` when it returns a result:

```csharp
using Zerra.CQRS;

public class CreateUserCommand : ICommand
{
    public required string Email { get; init; }
    public required string Name { get; init; }
}

public class ActivateUserCommand : ICommand<User>   // returns the updated user
{
    public required int UserId { get; init; }
}
```

Name commands for their intent: `ActivateUserCommand`, not `UserCommand`.

## Handlers

A handler interface groups a service's commands:

```csharp
public interface IUserCommandHandler :
    ICommandHandler<CreateUserCommand>,         // no result
    ICommandHandler<ActivateUserCommand, User>  // returns User
{
}
```

The implementation derives from `BaseHandler`:

```csharp
public class UserCommandHandler : BaseHandler, IUserCommandHandler
{
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
            throw new ArgumentException("Email is required");

        var repository = Context.GetService<IUserRepository>();
        var user = new User { Email = command.Email, Name = command.Name };
        await repository.CreateAsync(user, cancellationToken);

        // tell other services
        await Bus.DispatchAsync(new UserCreatedEvent { UserId = user.Id, Email = user.Email });
    }

    public async Task<User> Handle(ActivateUserCommand command, CancellationToken cancellationToken)
    {
        var repository = Context.GetService<IUserRepository>();
        var user = await repository.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {command.UserId} not found");

        if (!user.IsActive) // already active is a no-op, so a retry is safe
        {
            user.IsActive = true;
            await repository.UpdateAsync(user, cancellationToken);
        }
        return user;
    }
}
```

`BaseHandler` provides:

- `Bus` (same as `Context.Bus`) to dispatch further commands and events, or call queries
- `Log` (same as `Context.Log`), which may be null
- `Context.GetService<T>()` for registered services, which throws if missing, and `Context.TryGetService<T>(out var service)` for optional ones
- `Context.ServiceName`

Validate input in the handler and throw an exception with a message for the caller. Handler instances are shared by concurrent messages, so keep them stateless.

## Dispatching

| Method | Waits for the handler | Returns handler exceptions |
|---|---|---|
| `DispatchAsync(command)` | no, returns once sent | no |
| `DispatchAwaitAsync(command)` | yes | yes |
| `DispatchAwaitAsync(ICommand<TResult>)` | yes, and returns the result | yes |

```csharp
await bus.DispatchAsync(new CreateUserCommand { Email = "user@example.com", Name = "John Doe" });

await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com", Name = "John Doe" });

var user = await bus.DispatchAwaitAsync(new ActivateUserCommand { UserId = 1 });
```

Use `DispatchAsync` when the caller doesn't need to know the outcome, and `DispatchAwaitAsync` when it must confirm success or react to a failure.

`DispatchAwaitAsync` waits for that command's handler only. Events and commands the handler sends with `DispatchAsync` have been sent but not necessarily handled when it returns. See [What a Dispatch Returns](Reliability.md#what-a-dispatch-returns).

### Timeouts

Every dispatch method has an overload taking a `CancellationToken` and one taking a `TimeSpan`. Without either, the bus's `defaultDispatchTimeout` or `defaultDispatchAwaitTimeout` applies (see [Client Setup](ClientSetup.md#timeout-configuration)). A timeout throws `TimeoutException`.

```csharp
await bus.DispatchAwaitAsync(command, TimeSpan.FromSeconds(10));
```

### Cancellation

Unlike queries, a command's `CancellationToken` does **not** propagate to a remote handler. Cancelling stops the caller waiting, but once the command has been sent the handler runs to completion. A state change cancelled partway could leave the system inconsistent, while a query is safe to cancel at any point.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    await bus.DispatchAwaitAsync(new LongRunningCommand(), cts.Token);
}
catch (OperationCanceledException)
{
    // the caller stopped waiting; the remote handler keeps running
}
```

## Local and Remote Handling

Where a command is handled depends only on registration. The dispatching code doesn't change.

```csharp
// Local: handled in this process
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());

// Remote over TCP: server side
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddCommandConsumer<IUserCommandHandler>(new TcpCqrsServer("localhost:9001", serializer, encryptor, null, log));

// Remote over TCP: client side
bus.AddCommandProducer<IUserCommandHandler>(new TcpCqrsClient("localhost:9001", serializer, encryptor, null, log));

// Remote through a broker (RabbitMQ and Azure Service Bus follow the same pattern)
bus.AddCommandConsumer<IUserCommandHandler>(new KafkaConsumer("localhost:9092", serializer, encryptor, null, log, environment: null, userName: null, password: null));
bus.AddCommandProducer<IUserCommandHandler>(new KafkaProducer("localhost:9092", serializer, encryptor, null, log, environment: null, userName: null, password: null));
```

See [Server Setup](ServerSetup.md), [Client Setup](ClientSetup.md), and the broker setup guides for the details.

## Errors

An exception thrown by a remote handler is returned to the caller as a `RemoteServiceException`, **only** when the command was sent with `DispatchAwaitAsync`. It carries:

- `Message`: the original message
- `ErrorType`: the original exception's type name
- `Source`: the command type that caused it
- `StackTrace`: the remote stack trace

There is no `InnerException`, so branch on `ErrorType`:

```csharp
try
{
    await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com", Name = "John Doe" });
}
catch (RemoteServiceException ex) when (ex.ErrorType == nameof(ArgumentException))
{
    // validation failed on the server
}
catch (TimeoutException)
{
    // no response in time; the command may or may not have been handled
}
```

| Exception | Meaning | Command handled? |
|---|---|---|
| `RemoteServiceException` | the handler threw | yes, and it failed |
| `TimeoutException` | no response in time | unknown |
| `IOException` or other | network or connection failure | no |

`DispatchAsync` returns as soon as the command is sent, so a handler's exception never reaches the caller. Use `DispatchAwaitAsync` when the caller needs to handle failures.

## Idempotency

Zerra doesn't redeliver commands unless they're [resilient](Reliability.md#resilient-commands), but a caller that retries after a `TimeoutException` may send one that already ran. Make handlers safe to run twice where you can, like the "already active" check in `ActivateUserCommand` above, or give the command an ID the caller creates and skip IDs already handled. See [Making Handlers Safe](Reliability.md#making-handlers-safe).

## Coordinating Several Services

A handler can await commands to other services and compensate if a later step fails:

```csharp
public async Task Handle(CreateOrderCommand command, CancellationToken cancellationToken)
{
    var reserved = await Bus.DispatchAwaitAsync(new ReserveInventoryCommand { ProductId = command.ProductId, Quantity = command.Quantity });
    if (!reserved)
        throw new InvalidOperationException("Insufficient inventory");

    try
    {
        await Bus.DispatchAwaitAsync(new ChargePaymentCommand { CustomerId = command.CustomerId, Amount = command.Amount });
        await Bus.DispatchAwaitAsync(new FinalizeOrderCommand { OrderId = command.OrderId });
    }
    catch
    {
        // compensate: release the reservation
        await Bus.DispatchAsync(new ReleaseInventoryCommand { ProductId = command.ProductId, Quantity = command.Quantity });
        throw;
    }
}
```

`Demo/Store` does this when placing an order: Orders awaits `ReserveStockCommand` from Inventory, and sends `ReleaseReservedStockCommand` if saving the order fails.

## See Also

- [Queries](Queries.md) - Read operations
- [Events](Events.md) - State change notifications
- [Service Injection](ServiceInjection.md) - Services in handlers
- [Server Setup](ServerSetup.md) - Command consumers
- [Client Setup](ClientSetup.md) - Command producers
