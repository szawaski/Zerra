[← Back to Documentation](Index.md)

# Service Injection

`BusServices` holds the services handlers use, such as repositories, email senders, and configuration. Each is registered once, by interface, and handlers get it from their `Context`.

## Registering Services

Register every service before creating the bus:

```csharp
using Zerra.CQRS;

var busServices = new BusServices();
busServices.AddService<IUserRepository>(new UserRepository(connectionString));
busServices.AddService<IEmailService>(new EmailService(smtpConfig));
busServices.AddService(typeof(IConfiguration), configuration); // non-generic form

var bus = Bus.New("UserService", log, busLog, busServices);
```

- The type must be an interface, and the instance must implement it. A class type throws `ArgumentException`, and a `null` instance throws `ArgumentNullException`.
- Registration isn't thread-safe while handlers are reading services, so finish registering before the bus starts receiving.

## Using Services in Handlers

Handlers deriving from `BaseHandler` get services from `Context`:

```csharp
public class UserCommandHandler : BaseHandler, IUserCommandHandler
{
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // required: throws if not registered
        var repository = Context.GetService<IUserRepository>();
        await repository.CreateAsync(command.Email, cancellationToken);

        // optional
        if (Context.TryGetService<IAuditService>(out var audit))
            await audit.RecordAsync("UserCreated", command.Email);
    }
}
```

`Context` also has `Bus`, `Log`, and `ServiceName`. `BaseHandler` exposes `Bus` and `Log` directly as well, and they are the same instances.

The bus has the same `GetService` and `TryGetService`, so `Program.cs` can check at startup that required services are registered:

```csharp
if (!bus.TryGetService<IUserRepository>(out _))
    throw new InvalidOperationException("IUserRepository is not registered");
```

## Lifetime and Thread Safety

Each service is a single instance shared by every handler call, and calls run concurrently. Services must be thread-safe: keep per-call state in local variables, and open a connection per call rather than sharing one.

```csharp
public class UserRepository : IUserRepository
{
    private readonly string connectionString;   // shared, read-only: fine

    public UserRepository(string connectionString) => this.connectionString = connectionString;

    public async Task<User> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);   // per call
        // ...
    }
}
```

## Zerra.Repository

Register an [`IRepo`](Repository.md) with `AddRepo`, and handlers deriving from `BaseHandlerWithRepo` get it as their `Repo` property:

```csharp
using Zerra.Repository;

var repo = Repo.New();
repo.AddProvider(new UserStoreProvider<UserDataModel>());
busServices.AddRepo(repo);

public class UserCommandHandler : BaseHandlerWithRepo, IUserCommandHandler
{
    public Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
        => Repo.CreateAsync(new UserDataModel { Email = command.Email });
}
```

## Using ASP.NET Core Services

`BusServices` has only singletons and isn't tied to ASP.NET Core's container. To share services with an ASP.NET Core app, build them in its container and register the same instances with the bus:

```csharp
builder.Services.AddSingleton<IUserRepository, UserRepository>();
var app = builder.Build();

var busServices = new BusServices();
busServices.AddService<IUserRepository>(app.Services.GetRequiredService<IUserRepository>());

var bus = Bus.New("MyService", log, busLog, busServices);
```

## See Also

- [Server Setup](ServerSetup.md) - Building a service
- [Repository](Repository.md) - Data access with `IRepo`
