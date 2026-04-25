[← Back to Documentation](README.md)

# Logging

Zerra provides comprehensive logging capabilities through two primary interfaces: `ILogger` for standard application logging and `IBusLogger` for cross-service message lifecycle tracking.

## Overview

Logging in Zerra serves two purposes:

1. **Application Logging (`ILogger`)**: Standard logging for application events, errors, and diagnostics
2. **Bus Logging (`IBusLogger`)**: Specialized logging for tracking message flow across services

Both loggers are optional but highly recommended for production applications.

## ILogger Interface

The `ILogger` interface provides standard logging capabilities for application-level events.

### Interface Definition

```csharp
namespace Zerra.Logging
{
    public interface ILogger
    {
        void Trace(string message);
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Error(Exception exception);
        void Error(string message, Exception exception);
        void Fatal(string message);
        void Fatal(Exception exception);
        void Fatal(string message, Exception exception);
    }
}
```

### Usage

```csharp
using Zerra.Logging;

// Create logger instance
ILogger logger = new Logger();

// Configure Bus with logger
var bus = Bus.New(
    service: "MyService",
    log: logger,
    busLog: busLogger,
    busScopes: busScopes
);

// Use logger in network components
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, logger);
```

### Accessing Logger in Handlers

Handlers inherit the logger through `BaseHandler`:

```csharp
public class UserCommandHandler : BaseHandler, ICommandHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Access logger via Log property
        Log?.Trace($"Creating user: {command.Email}");

        try
        {
            // Your logic here
            Log?.Info($"User created successfully: {command.Email}");
        }
        catch (Exception ex)
        {
            Log?.Error($"Failed to create user: {command.Email}", ex);
            throw;
        }
    }
}
```

### Log Levels

| Level | Purpose | Example Use Case |
|-------|---------|------------------|
| Trace | Detailed diagnostic information | Method entry/exit, loop iterations |
| Debug | Debug information | Variable values, state changes |
| Info | General information | Operation started/completed |
| Warn | Warning conditions | Recoverable errors, deprecated usage |
| Error | Error conditions | Exceptions, failures |
| Fatal | Critical failures | System crash, data corruption |

## IBusLogger Interface

The `IBusLogger` interface provides specialized logging for tracking message lifecycle across service boundaries.

### Interface Definition

```csharp
namespace Zerra.CQRS
{
    public interface IBusLogger
    {
        // Begin lifecycle events
        void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled);
        void BeginEvent(Type eventType, IEvent @event, string service, string source, bool handled);
        void BeginCall(Type interfaceType, string methodName, object[] arguments, string service, string source, bool handled);

        // End lifecycle events
        void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex);
        void EndEvent(Type eventType, IEvent @event, string service, string source, bool handled, long milliseconds, Exception? ex);
        void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex);
    }
}
```

### Usage

```csharp
using Zerra.CQRS;

// Create bus logger instance
IBusLogger busLogger = new BusLogger();

// Configure Bus with bus logger
var bus = Bus.New(
    service: "MyService",
    log: logger,
    busLog: busLogger,
    busScopes: busScopes
);
```

### Lifecycle Tracking

`IBusLogger` tracks the complete lifecycle of each message:

#### Command Lifecycle

```csharp
// Called when command processing starts
void BeginCommand(
    Type commandType,        // Type of command (e.g., CreateUserCommand)
    ICommand command,        // The command instance
    string service,          // Current service name
    string source,           // Service that originated the command
    bool handled            // True if handled locally, false if remote
);

// Called when command processing completes
void EndCommand(
    Type commandType,
    ICommand command,
    string service,
    string source,
    bool handled,
    long milliseconds,      // Execution time in milliseconds
    Exception? ex           // Exception if failed, null if succeeded
);
```

#### Event Lifecycle

```csharp
// Called when event processing starts
void BeginEvent(
    Type eventType,         // Type of event (e.g., UserCreatedEvent)
    IEvent @event,          // The event instance
    string service,         // Current service name
    string source,          // Service that originated the event
    bool handled           // True if handled locally, false if remote
);

// Called when event processing completes
void EndEvent(
    Type eventType,
    IEvent @event,
    string service,
    string source,
    bool handled,
    long milliseconds,     // Execution time in milliseconds
    Exception? ex          // Exception if failed, null if succeeded
);
```

#### Query Call Lifecycle

```csharp
// Called when query call starts
void BeginCall(
    Type interfaceType,     // Query interface type (e.g., IUserQueries)
    string methodName,      // Method being called (e.g., "GetUserById")
    object[] arguments,     // Method arguments
    string service,         // Current service name
    string source,          // Service that originated the call
    bool handled           // True if handled locally, false if remote
);

// Called when query call completes
void EndCall(
    Type interfaceType,
    string methodName,
    object[] arguments,
    object? result,        // Return value (null for void)
    string service,
    string source,
    bool handled,
    long milliseconds,     // Execution time in milliseconds
    Exception? ex          // Exception if failed, null if succeeded
);
```

## Implementation Examples

### Simple Console Logger

```csharp
using Zerra.Logging;

public class ConsoleLogger : ILogger
{
    public void Trace(string message) => Console.WriteLine($"[TRACE] {message}");
    public void Debug(string message) => Console.WriteLine($"[DEBUG] {message}");
    public void Info(string message) => Console.WriteLine($"[INFO] {message}");
    public void Warn(string message) => Console.WriteLine($"[WARN] {message}");
    public void Error(string message) => Console.WriteLine($"[ERROR] {message}");
    public void Error(Exception exception) => Console.WriteLine($"[ERROR] {exception}");
    public void Error(string message, Exception exception) => Console.WriteLine($"[ERROR] {message}: {exception}");
    public void Fatal(string message) => Console.WriteLine($"[FATAL] {message}");
    public void Fatal(Exception exception) => Console.WriteLine($"[FATAL] {exception}");
    public void Fatal(string message, Exception exception) => Console.WriteLine($"[FATAL] {message}: {exception}");
}
```

### Simple Console Bus Logger

```csharp
using Zerra.CQRS;

public class ConsoleBusLogger : IBusLogger
{
    public void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled)
    {
        Console.WriteLine($"[BUS] Command Begin: {commandType.Name} - Service: {service}, Source: {source}, Local: {handled}");
    }

    public void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex)
    {
        var status = ex == null ? "Success" : "Failed";
        Console.WriteLine($"[BUS] Command End: {commandType.Name} - {status} ({milliseconds}ms)");
    }

    public void BeginEvent(Type eventType, IEvent @event, string service, string source, bool handled)
    {
        Console.WriteLine($"[BUS] Event Begin: {eventType.Name} - Service: {service}, Source: {source}, Local: {handled}");
    }

    public void EndEvent(Type eventType, IEvent @event, string service, string source, bool handled, long milliseconds, Exception? ex)
    {
        var status = ex == null ? "Success" : "Failed";
        Console.WriteLine($"[BUS] Event End: {eventType.Name} - {status} ({milliseconds}ms)");
    }

    public void BeginCall(Type interfaceType, string methodName, object[] arguments, string service, string source, bool handled)
    {
        Console.WriteLine($"[BUS] Query Begin: {interfaceType.Name}.{methodName} - Service: {service}, Source: {source}, Local: {handled}");
    }

    public void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex)
    {
        var status = ex == null ? "Success" : "Failed";
        Console.WriteLine($"[BUS] Query End: {interfaceType.Name}.{methodName} - {status} ({milliseconds}ms)");
    }
}
```

### Structured Logging with Serilog

```csharp
using Serilog;
using Zerra.Logging;

public class SerilogLogger : ILogger
{
    private readonly Serilog.ILogger logger;

    public SerilogLogger()
    {
        logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public void Trace(string message) => logger.Verbose(message);
    public void Debug(string message) => logger.Debug(message);
    public void Info(string message) => logger.Information(message);
    public void Warn(string message) => logger.Warning(message);
    public void Error(string message) => logger.Error(message);
    public void Error(Exception exception) => logger.Error(exception, "An error occurred");
    public void Error(string message, Exception exception) => logger.Error(exception, message);
    public void Fatal(string message) => logger.Fatal(message);
    public void Fatal(Exception exception) => logger.Fatal(exception, "A fatal error occurred");
    public void Fatal(string message, Exception exception) => logger.Fatal(exception, message);
}
```

### Distributed Tracing Bus Logger

```csharp
using Zerra.CQRS;
using System.Diagnostics;

public class DistributedTracingBusLogger : IBusLogger
{
    private static readonly ActivitySource activitySource = new ActivitySource("Zerra.Bus");

    public void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled)
    {
        var activity = activitySource.StartActivity($"Command: {commandType.Name}");
        activity?.SetTag("service", service);
        activity?.SetTag("source", source);
        activity?.SetTag("handled.locally", handled);
        activity?.SetTag("command.type", commandType.FullName);
    }

    public void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex)
    {
        var activity = Activity.Current;
        activity?.SetTag("duration.ms", milliseconds);
        if (ex != null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
        activity?.Stop();
    }

    // Similar implementations for BeginEvent, EndEvent, BeginCall, EndCall...
}
```

## Best Practices

### 1. Always Use Null-Conditional Operator

```csharp
// ✅ Correct - handles null logger gracefully
Log?.Info("Operation completed");

// ❌ Wrong - will throw if logger is null
Log.Info("Operation completed");
```

### 2. Include Context in Messages

```csharp
// ✅ Good - includes relevant context
Log?.Info($"User {userId} created with email {email}");

// ❌ Poor - lacks context
Log?.Info("User created");
```

### 3. Log Exceptions with Context

```csharp
// ✅ Good - provides context with exception
try
{
    await ProcessUser(userId);
}
catch (Exception ex)
{
    Log?.Error($"Failed to process user {userId}", ex);
    throw;
}

// ❌ Poor - no context
catch (Exception ex)
{
    Log?.Error(ex);
    throw;
}
```

### 4. Use Appropriate Log Levels

```csharp
// ✅ Good - appropriate levels
Log?.Trace("Entering method ProcessUser");
Log?.Debug($"User data: {userData}");
Log?.Info("User processing completed successfully");
Log?.Warn("User email not verified, sending reminder");
Log?.Error("Failed to send email notification", ex);

// ❌ Poor - everything is Info
Log?.Info("Entering method ProcessUser");
Log?.Info($"User data: {userData}");
Log?.Info("User processing completed successfully");
Log?.Info("User email not verified, sending reminder");
Log?.Info($"Failed to send email notification: {ex}");
```

### 5. Implement Both Loggers in Production

```csharp
// ✅ Recommended for production
ILogger logger = new ProductionLogger();
IBusLogger busLogger = new ProductionBusLogger();

var bus = Bus.New("MyService", logger, busLogger, busScopes);

// ⚠️ Development only
var bus = Bus.New("MyService", null, null, busScopes);
```

## Common Scenarios

### Development Environment

```csharp
// Simple console logging for development
ILogger logger = new ConsoleLogger();
IBusLogger busLogger = new ConsoleBusLogger();

var bus = Bus.New("DevService", logger, busLogger, busScopes);
```

### Production with Structured Logging

```csharp
// Production with Serilog and Application Insights
ILogger logger = new SerilogLogger();
IBusLogger busLogger = new ApplicationInsightsBusLogger();

var bus = Bus.New("ProdService", logger, busLogger, busScopes);
```

### No Logging (Not Recommended)

```csharp
// Disable logging (development/testing only)
var bus = Bus.New("TestService", null, null, busScopes);
```

## Performance Considerations

1. **Async Logging**: Consider async logging for high-throughput scenarios
2. **Log Level Filtering**: Filter logs before formatting to avoid unnecessary allocations
3. **Sampling**: Sample high-frequency events (e.g., log 1 in 100 queries)
4. **Buffering**: Use buffered logging for better performance
5. **Conditional Logging**: Check log levels before expensive operations

```csharp
// ✅ Efficient - only formats if needed
if (logger.IsDebugEnabled)
{
    var expensiveDebugInfo = BuildDebugInfo();
    logger.Debug($"Debug info: {expensiveDebugInfo}");
}

// ❌ Inefficient - always formats even if not logged
logger.Debug($"Debug info: {BuildDebugInfo()}");
```

## Integration with Popular Logging Frameworks

Zerra's logging interfaces can easily integrate with:

- **Serilog**: Structured logging with sinks
- **NLog**: Flexible configuration
- **Microsoft.Extensions.Logging**: ASP.NET Core integration
- **log4net**: Enterprise logging
- **Application Insights**: Azure monitoring
- **OpenTelemetry**: Distributed tracing

## See Also

- [Service Injection](ServiceInjection.md) - Injecting dependencies into handlers
- [Server Setup](ServerSetup.md) - Server-side logging configuration
- [Client Setup](ClientSetup.md) - Client-side logging configuration
- [Queries](Queries.md) - Using logging in query handlers
- [Commands](Commands.md) - Using logging in command handlers
- [Events](Events.md) - Using logging in event handlers
