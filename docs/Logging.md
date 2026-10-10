[← Back to Documentation](Index.md)

# Logging

Zerra logs through two interfaces you implement:

- **`ILogger`** (`Zerra.Logging`): application and framework messages, such as a failed connection or database read.
- **`IBusLogger`** (`Zerra.CQRS`): the start and end of every command, event, and query, with where it came from, where it ran, how long it took, and any exception.

Both are optional. Zerra doesn't ship a concrete logger, so implement them over your logging library. The `ConsoleLogger` and `ConsoleBusLogger` used throughout these docs are the samples [below](#console-implementations).

## Wiring Up

```csharp
using Zerra.Logging;

ILogger log = new ConsoleLogger();
IBusLogger busLog = new ConsoleBusLogger();

var bus = Bus.New("MyService", log, busLog, busServices);
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, null, log);   // servers, clients, and consumers take one too
```

Handlers log through `Log`, inherited from `BaseHandler`, which is null when the bus has no logger:

```csharp
Log?.Info($"Creating user {command.Email}");
Log?.Error($"Failed to create user {command.Email}", ex);
```

Code outside a handler uses the `ILogger` it's given, the same one passed to `Bus.New`. Pass it to `CodeFirstGeneration.Generate` too, which logs a failed database read to it. The static `Log` class is obsolete.

## ILogger

```csharp
public interface ILogger
{
    void Trace(string message);
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string? message = null, Exception? ex = null);
    void Error(Exception? ex = null);
    void Critical(string? message = null, Exception? ex = null);
    void Critical(Exception? ex = null);
}
```

`ILogger` has no level checks, so messages arrive already formatted. Guard expensive formatting with your own setting.

## IBusLogger

```csharp
public interface IBusLogger
{
    void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled);
    void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex);

    void BeginEvent(Type eventType, IEvent @event, string service, string source, bool handled);
    void EndEvent(Type eventType, IEvent @event, string service, string source, bool handled, long milliseconds, Exception? ex);

    void BeginCall(Type interfaceType, string methodName, object[] arguments, string service, string source, bool handled);
    void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex);
}
```

| Parameter | Meaning |
|---|---|
| `service` | the service logging it |
| `source` | the service that sent the message |
| `handled` | `true` if handled in this process, `false` if sent on to another service |
| `milliseconds` | how long it took |
| `ex` | the exception if it failed, otherwise `null` |
| `result` | a query's return value |

Because every service logs both the messages it sends and the ones it handles, with `source` and `service`, the bus logs of all services together trace a request across service hops. An `IBusLogger` is also the natural place to start OpenTelemetry activities or record metrics.

## Console Implementations

```csharp
public class ConsoleLogger : ILogger
{
    public void Trace(string message) => Console.WriteLine($"[TRACE] {message}");
    public void Debug(string message) => Console.WriteLine($"[DEBUG] {message}");
    public void Info(string message) => Console.WriteLine($"[INFO] {message}");
    public void Warn(string message) => Console.WriteLine($"[WARN] {message}");
    public void Error(string? message = null, Exception? ex = null) => Console.WriteLine($"[ERROR] {message} {ex}");
    public void Error(Exception? ex = null) => Console.WriteLine($"[ERROR] {ex}");
    public void Critical(string? message = null, Exception? ex = null) => Console.WriteLine($"[CRITICAL] {message} {ex}");
    public void Critical(Exception? ex = null) => Console.WriteLine($"[CRITICAL] {ex}");
}

public class ConsoleBusLogger : IBusLogger
{
    public void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled) { }
    public void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex)
        => Console.WriteLine($"[BUS] {commandType.Name} from {source} {(ex is null ? "ok" : "failed")} ({milliseconds}ms)");

    public void BeginEvent(Type eventType, IEvent @event, string service, string source, bool handled) { }
    public void EndEvent(Type eventType, IEvent @event, string service, string source, bool handled, long milliseconds, Exception? ex)
        => Console.WriteLine($"[BUS] {eventType.Name} from {source} {(ex is null ? "ok" : "failed")} ({milliseconds}ms)");

    public void BeginCall(Type interfaceType, string methodName, object[] arguments, string service, string source, bool handled) { }
    public void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex)
        => Console.WriteLine($"[BUS] {interfaceType.Name}.{methodName} from {source} {(ex is null ? "ok" : "failed")} ({milliseconds}ms)");
}
```

An adapter for Serilog, NLog, or `Microsoft.Extensions.Logging` maps each method to the library's matching level in the same way.

## ASP.NET Core

`Zerra.Web` adds the reverse bridge: `ZerraLoggerProvider` sends ASP.NET Core's own `Microsoft.Extensions.Logging` output to your Zerra `ILogger`. Register it with `builder.Logging.AddProvider(new ZerraLoggerProvider(log))`, or `loggerFactory.AddZerraLogger(log)`. See [Zerra.Web](ZerraWeb.md#logging-integration).

In ASP.NET projects `ILogger` is ambiguous with Microsoft's, so write `Zerra.Logging.ILogger`.

## See Also

- [Server Setup](ServerSetup.md) and [Client Setup](ClientSetup.md) - Where loggers are passed
