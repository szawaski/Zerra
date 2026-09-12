[← Back to Documentation](Index.md)

# Service Injection

Zerra provides a simple dependency injection mechanism through the `BusServices` class, allowing handlers to access registered service instances during message processing.

## Overview

The `BusServices` class:
- Manages service dependencies for bus initialization
- Provides a container for registering service instances (one shared instance per interface)
- Makes dependencies available to handlers via `BusContext`
- Supports interface-based dependency registration
- Is read-only during handler execution; complete registration before creating the bus

## BusServices Class

The `BusServices` class is a lightweight dependency injection container designed specifically for Zerra's messaging infrastructure.

### Creating BusServices

```csharp
using Zerra.CQRS;

// Create a new BusServices instance
var busServices = new BusServices();

// Register service dependencies
busServices.AddService<IUserRepository>(userRepository);
busServices.AddService<IEmailService>(emailService);
busServices.AddService<IConfiguration>(configuration);

// Pass to Bus.New()
var bus = Bus.New(
    serviceName: "MyService",
    log: logger,
    busLog: busLogger,
    busServices: busServices
);
```

## Registering Services

### Generic Registration

Register services using the generic `AddService<TInterface>()` method:

```csharp
var busServices = new BusServices();

// Register interface implementations
busServices.AddService<IUserRepository>(new UserRepository());
busServices.AddService<IEmailService>(new EmailService());
busServices.AddService<IConfiguration>(configuration);

// Register any interface type
busServices.AddService<IMyCustomService>(myCustomService);
```

### Non-Generic Registration

Register services using the non-generic `AddService()` method:

```csharp
var busServices = new BusServices();

// Register with explicit type
Type interfaceType = typeof(IUserRepository);
object instance = new UserRepository();
busServices.AddService(interfaceType, instance);
```

### Requirements

⚠️ **Important Constraints:**
- The type parameter must be an interface (not a class)
- The instance must implement the specified interface
- The instance cannot be null

```csharp
// ✅ Correct - interface type
busServices.AddService<IUserRepository>(new UserRepository());

// ❌ Wrong - class type (will throw ArgumentException)
busServices.AddService<UserRepository>(new UserRepository());

// ❌ Wrong - null instance (will throw ArgumentNullException)
busServices.AddService<IUserRepository>(null);
```

## Accessing Services in Handlers

Handlers access registered services through the `Context` property inherited from `BaseHandler`.

### Using GetService<TInterface>()

```csharp
public class UserCommandHandler : BaseHandler, ICommandHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Get service from context
        var userRepository = Context.GetService<IUserRepository>();

        // Use the service
        await userRepository.CreateUserAsync(command.Email, cancellationToken);
    }
}
```

### Using TryGetService<TInterface>()

For optional dependencies, use `TryGetService`:

```csharp
public class UserCommandHandler : BaseHandler, ICommandHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Try to get optional service
        if (Context.TryGetService<IEmailService>(out var emailService))
        {
            // Service available - use it
            await emailService.SendWelcomeEmailAsync(command.Email);
        }
        else
        {
            // Service not available - skip or use fallback
            Log?.Warn("Email service not configured, skipping welcome email");
        }

        // Required service - will throw if not registered
        var userRepository = Context.GetService<IUserRepository>();
        await userRepository.CreateUserAsync(command.Email, cancellationToken);
    }
}
```

### BusContext API

The `BusContext` provides access to services and bus capabilities. When inheriting from `BaseHandler`, both `Bus` and `Log` are also exposed as direct properties for convenience:

```csharp
public class MyHandler : BaseHandler, ICommandHandler<MyCommand>
{
    public async Task Handle(MyCommand command, CancellationToken cancellationToken)
    {
        // Access Bus directly from BaseHandler
        IBus bus = this.Bus;              // Shortcut property
        // Or via Context
        IBus busViaContext = Context.Bus; // Same instance

        // Access Logger directly from BaseHandler
        ILogger? logger = this.Log;         // Shortcut property
        // Or via Context
        ILogger? logViaContext = Context.Log; // Same instance

        // Access current service name (only via Context)
        string serviceName = Context.ServiceName;

        // Get registered service
        var repository = Context.GetService<IRepository>();

        // Try get optional service
        if (Context.TryGetService<IOptionalService>(out var service))
        {
            // Use service
        }
    }
}
```

**Note:** `Bus` and `Log` are convenience properties on `BaseHandler` that point to the same instances as `Context.Bus` and `Context.Log`. Use whichever is more convenient in your code.

## Common Service Types

### Repositories

```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task CreateAsync(User user, CancellationToken cancellationToken);
    Task UpdateAsync(User user, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

// Register
busServices.AddService<IUserRepository>(new UserRepository(connectionString));

// Use in a query handler method
public async Task<User> GetUserById(int userId, CancellationToken cancellationToken)
{
    var repository = Context.GetService<IUserRepository>();
    return await repository.GetByIdAsync(userId, cancellationToken);
}
```

### Email Services

```csharp
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken);
    Task SendWelcomeEmailAsync(string email, CancellationToken cancellationToken);
}

// Register
busServices.AddService<IEmailService>(new EmailService(smtpConfig));

// Use in an event handler (event handlers do not receive a CancellationToken)
public async Task Handle(UserCreatedEvent @event)
{
    var emailService = Context.GetService<IEmailService>();
    await emailService.SendWelcomeEmailAsync(@event.Email, CancellationToken.None);
}
```

### Configuration

```csharp
public interface IConfiguration
{
    string GetValue(string key);
    T GetValue<T>(string key);
}

// Register
busServices.AddService<IConfiguration>(configuration);

// Use in handler
public async Task Handle(MyCommand command, CancellationToken cancellationToken)
{
    var config = Context.GetService<IConfiguration>();
    var maxRetries = config.GetValue<int>("MaxRetries");
}
```

### Caching Services

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken);
}

// Register
busServices.AddService<ICacheService>(new RedisCacheService(redisConnection));

// Use in a query handler method
public async Task<User> GetUserById(int userId, CancellationToken cancellationToken)
{
    var cache = Context.GetService<ICacheService>();

    // Try cache first
    var cachedUser = await cache.GetAsync<User>($"user:{userId}", cancellationToken);
    if (cachedUser != null)
        return cachedUser;

    // Not in cache - get from repository
    var repository = Context.GetService<IUserRepository>();
    var user = await repository.GetByIdAsync(userId, cancellationToken);

    // Cache for next time
    await cache.SetAsync($"user:{userId}", user, TimeSpan.FromMinutes(5), cancellationToken);

    return user;
}
```

## Zerra.Repository Integration

Zerra.Repository provides a special extension method for registering repositories:

```csharp
using Zerra.Repository;

var busServices = new BusServices();

// Create repository instance
var repo = Repo.New();
repo.AddProvider(new MyDatabaseProvider());

// Register repository - special extension method
busServices.AddRepo(repo);

// Use in handler by inheriting from BaseHandlerWithRepo
public class UserCommandHandler : BaseHandlerWithRepo, ICommandHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Access via Repo property
        var user = new User { Email = command.Email };
        await Repo.CreateAsync(user);
    }
}
```

## Complete Setup Example

### Server-Side Configuration

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using MyApp.Services;
using MyApp.Repositories;

// Create service instances
var userRepository = new UserRepository(connectionString);
var emailService = new EmailService(smtpConfig);
var cacheService = new RedisCacheService(redisConnection);
var configuration = new AppConfiguration();

// Register services
var busServices = new BusServices();
busServices.AddService<IUserRepository>(userRepository);
busServices.AddService<IEmailService>(emailService);
busServices.AddService<ICacheService>(cacheService);
busServices.AddService<IConfiguration>(configuration);

// Create bus with services
var bus = Bus.New(
    serviceName: "UserService",
    log: new ConsoleLogger(),
    busLog: new ConsoleBusLogger(),
    busServices: busServices
);

// Register handlers
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserQueries>(new UserQueryHandler());

// Setup network components
var serializer = new ZerraByteSerializer();
var encryptor = new ZerraEncryptor("securePassword", SymmetricAlgorithmType.AESwithPrefix);
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, new ConsoleLogger());

bus.AddCommandConsumer<IUserCommandHandler>(server);
bus.AddQueryServer<IUserQueries>(server);

await bus.WaitForExitAsync(cancellationToken);
```

### Client-Side Configuration

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;

// Client typically doesn't need services unless running local handlers
var busServices = new BusServices();

// Create bus
var bus = Bus.New(
    serviceName: "ClientApp",
    log: new ConsoleLogger(),
    busLog: new ConsoleBusLogger(),
    busServices: busServices
);

// Setup network client
var serializer = new ZerraByteSerializer();
var encryptor = new ZerraEncryptor("securePassword", SymmetricAlgorithmType.AESwithPrefix);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, new ConsoleLogger());

bus.AddCommandProducer<IUserCommandHandler>(client);
bus.AddQueryClient<IUserQueries>(client);

// Now use the bus
var users = await bus.Call<IUserQueries>().GetActiveUsers(cancellationToken);
```

## Best Practices

### 1. Register by Interface

```csharp
// ✅ Correct - register by interface
busServices.AddService<IUserRepository>(new UserRepository());

// ❌ Wrong - trying to register by class
busServices.AddService<UserRepository>(new UserRepository()); // Throws exception
```

### 2. Use TryGetService for Optional Dependencies

```csharp
// ✅ Good - handles optional dependency
if (Context.TryGetService<IEmailService>(out var emailService))
{
    await emailService.SendEmailAsync(...);
}

// ❌ Poor - throws if service not registered
var emailService = Context.GetService<IEmailService>(); // Throws if not registered
```

### 3. Validate Required Services at Startup

```csharp
// ✅ Good - validate at startup
var bus = Bus.New("MyService", logger, busLogger, busServices);

// Verify required services are registered (IBus exposes the same GetService/TryGetService as BusContext)
if (!bus.TryGetService<IUserRepository>(out _))
    throw new InvalidOperationException("IUserRepository not registered");
```

### 4. Keep Services Stateless

```csharp
// ✅ Good - stateless service
public class UserRepository : IUserRepository
{
    private readonly string connectionString;

    public UserRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public async Task<User> GetByIdAsync(int id, CancellationToken ct)
    {
        // Create new connection per call
        using var connection = new SqlConnection(connectionString);
        // ... query logic
    }
}

// ❌ Poor - stateful service (can cause issues with concurrent calls)
public class StatefulUserRepository : IUserRepository
{
    private User? currentUser; // Don't do this!

    public async Task<User> GetByIdAsync(int id, CancellationToken ct)
    {
        currentUser = await LoadUser(id);
        return currentUser;
    }
}
```

### 5. Initialize Services Before Bus Creation

```csharp
// ✅ Correct order
var busServices = new BusServices();
busServices.AddService<IUserRepository>(userRepository);
busServices.AddService<IEmailService>(emailService);

var bus = Bus.New("MyService", logger, busLogger, busServices);

// ❌ Avoid - registering after the bus is created and consumers/servers are running
var bus = Bus.New("MyService", logger, busLogger, busServices);
bus.AddCommandConsumer<IUserCommandHandler>(server);
busServices.AddService<IUserRepository>(userRepository); // races with handlers reading the registrations
```

The bus reads the same underlying registrations, so a late registration does become visible, but the container is not thread-safe for writes while handlers are resolving services. Complete all registrations before creating the bus.

## Singleton vs Scoped Services

Zerra uses a singleton-like pattern for services:
- Services are registered once at startup
- The same instance is used for all handler invocations
- Services should be thread-safe
- Use handler-level state for per-request data

```csharp
// ✅ Thread-safe repository (singleton pattern)
public class UserRepository : IUserRepository
{
    private readonly string connectionString;

    public async Task<User> GetByIdAsync(int id, CancellationToken ct)
    {
        // Each call creates its own connection
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand("SELECT * FROM Users WHERE Id = @Id", connection);
        // ... execute query
    }
}

// ❌ Not thread-safe (don't do this)
public class UnsafeRepository : IUserRepository
{
    private SqlConnection connection; // Shared state - not thread-safe!

    public async Task<User> GetByIdAsync(int id, CancellationToken ct)
    {
        using var command = new SqlCommand("SELECT * FROM Users WHERE Id = @Id", connection);
        // ... execute query
    }
}
```

## Comparison with ASP.NET Core DI

| Feature | Zerra BusServices | ASP.NET Core DI |
|---------|-------------------|-----------------|
| Scope | Singleton per bus | Singleton, Scoped, Transient |
| Registration | Interface only | Interface or class |
| Lifetime | Manual | Container-managed |
| Container | Lightweight | Full-featured |
| Use Case | Message handlers | Web applications |

For ASP.NET Core integration, you can bridge the two systems:

```csharp
// Program.cs - register in ASP.NET Core DI
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IEmailService, EmailService>();

var app = builder.Build();

// Create BusServices from the built ASP.NET Core container
var busServices = new BusServices();
busServices.AddService<IUserRepository>(app.Services.GetRequiredService<IUserRepository>());
busServices.AddService<IEmailService>(app.Services.GetRequiredService<IEmailService>());

var bus = Bus.New("MyService", logger, busLogger, busServices);
```

## See Also

- [Logging](Logging.md) - Configure logging for services
- [Server Setup](ServerSetup.md) - Server-side service configuration
- [Client Setup](ClientSetup.md) - Client-side service configuration
- [Queries](Queries.md) - Using services in query handlers
- [Commands](Commands.md) - Using services in command handlers
- [Events](Events.md) - Using services in event handlers
