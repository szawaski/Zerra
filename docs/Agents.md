# Zerra CQRS Framework - AI Agent Context

This document provides architectural context for AI agents working with the Zerra framework.

## Overview

Zerra is a CQRS (Command Query Responsibility Segregation) framework for .NET 10 that enables distributed message-driven architecture. It routes commands, events, and queries locally or remotely via message brokers (Kafka, RabbitMQ, Azure Service Bus) or HTTP.

## Core Concepts

### Commands (`ICommand`)
- Represent actions that modify state
- Fire-and-forget or with acknowledgment from remote service
- **Simple Command** (`ICommand`): No return value
- **Command with Result** (`ICommand<TResult>`): Returns a typed result and automatically awaits remote completion

### Events (`IEvent`)
- Represent state changes that have occurred
- Published to zero or more subscribers
- Multiple handlers can respond to the same event
- Used for event sourcing and eventual consistency

### Queries
- Represent read operations that return data
- Synchronous request-response pattern
- Implemented via interface methods (not explicit types)
- Defined in handler interfaces like `IUserQueries : IQueryHandler`

## The Bus

Central message router created via `Bus.New()`:

**Key Parameters:**
- `service`: Service identifier
- `log`: Logger instance
- `busLog`: IBusLogger for cross-service logging
- `busScopes`: BusScopes containing scoped dependencies
- `commandToReceiveUntilExit`: Optional count for graceful shutdown
- `defaultCallTimeout`, `defaultDispatchTimeout`, `defaultDispatchAwaitTimeout`: Timeout configuration

**Responsibilities:**
- Routes commands/events/queries to local handlers or remote producers
- Manages lifecycle of consumers, producers, clients, servers
- Provides `BusContext` to handlers
- Tracks command processing with `CommandCounter`

## Handlers

All handlers inherit from `BaseHandler` (implements `IHandler`).

### Handler Types

| Type | Interface | Purpose |
|------|-----------|---------|
| Command Handler | `ICommandHandler<T>` where `T : ICommand` | Process command, return `Task` |
| Command Result Handler | `ICommandHandler<T, TResult>` where `T : ICommand<TResult>` | Process command, return `Task<TResult>` |
| Event Handler | `IEventHandler<T>` where `T : IEvent` | Handle event, return `Task` |
| Query Handler | `IQueryHandler` (marker) | Base for query interfaces |

### BusContext Access

Handlers receive `BusContext` via `this.Context`:
- `Context.Bus`: Access to bus for dispatching
- `Context.Log`: Optional logger
- `Context.Get<TInterface>()`: Retrieve scoped dependencies
- `Context.Service`: Current service name

## Routing Modes

### Local Processing
Handler registered locally ? invoked in-process immediately

### Remote Processing
- **Commands**: Producer sends to broker ? Consumer receives and processes
  - `DispatchAsync()`: Fire-and-forget
  - `DispatchAwaitAsync()`: Wait for completion signal
- **Events**: Producer publishes ? Multiple consumers subscribe (parallel processing)
- **Queries**: Client sends HTTP/TCP request ? Server processes and responds

## Message Flow

### Command Dispatch
1. `bus.DispatchAsync(command)` called
2. Bus finds handler (local or remote producer)
3. If local: invoke immediately
4. If remote: send to broker
5. If awaiting: wait for completion or result
6. Return/throw result

**Variants:**
- `DispatchAsync(ICommand)` - Fire-and-forget
- `DispatchAwaitAsync(ICommand)` - Wait for remote service completion signal
- `DispatchAwaitAsync<TResult>(ICommand<TResult>)` - Wait for result (auto-awaits remote)

### Event Dispatch
1. `bus.DispatchAsync(event)` called
2. Bus finds all handlers/producers
3. Invoke locally or send to brokers in parallel
4. Wait for all to complete (or immediate if local)

**Characteristics:**
- Multiple handlers per event
- No return value
- Parallel remote processing

### Query Call
1. `bus.Call<IQueryInterface>().QueryMethod(...)` called
2. Bus finds handler (local or remote client)
3. Invoke locally or send HTTP/TCP request
4. Return result

**Characteristics:**
- Single handler per interface
- Type-safe methods
- Synchronous request-response

## Concurrency & Throttling

```csharp
maxConcurrentQueries = Environment.ProcessorCount * 32
maxConcurrentCommandsPerTopic = Environment.ProcessorCount * 8
maxConcurrentEventsPerTopic = Environment.ProcessorCount * 16
```

**CommandCounter** enables graceful shutdown after N commands:
```csharp
new Bus(..., commandToReceiveUntilExit: 100)
```

## Logging

`IBusLogger` interface provides message lifecycle hooks:

**Methods:**
- `BeginCommand/Event/Call`: Invoked at message start
- `EndCommand/Event/Call`: Invoked at message completion

**Parameters:**
- `service`: Current processing service
- `source`: Originating service
- `handled`: True if local, false if remote
- `milliseconds`: Processing duration
- `ex`: Exception if failed

## Timeout Handling

Three configurable levels:
- `defaultCallTimeout`: Query operations
- `defaultDispatchTimeout`: Dispatch without await
- `defaultDispatchAwaitTimeout`: Dispatch with await

Override per-call:
```csharp
await bus.DispatchAsync(command, new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
```

## Lifecycle

**Startup:**
```csharp
var bus = Bus.New(...);
bus.AddHandler<IHandlers>(handler);
bus.AddCommandProducer/Consumer/EventProducer/Consumer/QueryClient/Server(...);
// Producers/consumers/servers start automatically
```

**Shutdown:**
```csharp
await bus.StopServicesAsync();  // Explicit shutdown
// OR
await bus.WaitForExitAsync(cancellationToken);  // Wait for exit signal
```

Both close consumers/servers, dispose resources, stop processing new messages.

## Integration Points

### Message Broker Implementations
- `Zerra.CQRS.Kafka`: Kafka producer/consumer
- `Zerra.CQRS.RabbitMQ`: RabbitMQ producer/consumer
- `Zerra.CQRS.AzureServiceBus`: Azure Service Bus producer/consumer

### HTTP/Network
- Query servers host HTTP endpoints
- Query clients make HTTP requests
- Cross-platform communication support

## Best Practices

1. **Group handlers by interface**: One interface for related commands/events
2. **Keep query interfaces focused**: One responsibility per query interface
3. **Use scoped dependencies**: Register in `BusScopes` for handler access
4. **Handle exceptions**: Propagate from handlers to callers; use `IBusLogger` for tracking
5. **Eventual consistency**: Events for loose coupling, commands for intent, handle duplicates
6. **Dependency injection**: Handlers receive dependencies via `BusContext.Get<T>()`

## Architecture Summary

```
Application ? Bus (Router) ? Handlers/Producers/Consumers/Clients/Servers
                    ?
                Logging (IBusLogger)
                Command Counter (Throttling)
                Bus Context (Dependency Access)
                    ?
            Message Broker / HTTP Network
                    ?
            Remote Services (same pattern)
```

## Key Implementation Details

### IBusInternal (Generated Proxy Support)
- `_CallMethod<TReturn>()`: Sync query calls
- `_CallMethodTask()`: Async task queries
- `_CallMethodTaskGeneric<TReturn>()`: Async generic queries
- `_DispatchCommandInternalAsync()`: Command dispatch
- `_DispatchEventInternalAsync()`: Event dispatch

### Source Generation
Creates proxy implementations of query interfaces, routing metadata, and handler invocation code.

### Async Pattern
- Commands: `Task` or `Task<TResult>`
- Events: `Task`
- Queries: Sync, `Task`, or `Task<TResult>`

## AI Agent Guidelines

When working with Zerra code:

### When Creating Handlers
- Always inherit from `BaseHandler`
- Implement the appropriate handler interface (`ICommandHandler<T>`, `IEventHandler<T>`, etc.)
- Access dependencies via `this.Context.Get<TInterface>()`
- Use `this.Context.Bus` to dispatch additional commands/events

### When Adding Bus Routes
- Check if handler is local (performance priority) or remote (scalability priority)
- Use `AddHandler<TInterface>()` for local processing
- Use `AddCommandProducer/Consumer`, `AddEventProducer/Consumer`, `AddQueryClient/Server` for remote
- Consider concurrency limits when routing high-volume operations

### When Implementing Commands
- Use `ICommand` for fire-and-forget operations
- Use `ICommand<TResult>` when caller needs a response
- Simple commands should be idempotent when used with `DispatchAwaitAsync()`

### When Implementing Events
- Events should represent completed state changes, not intents
- Design for multiple subscribers
- Handle duplicate events gracefully (idempotent handlers)

### When Creating Query Interfaces
- Keep interfaces focused (single responsibility)
- Include `CancellationToken` parameter for cancellation support
- Return `Task<T>` for async, or sync if needed
- Query calls are type-safe and routed via proxy generation
- **Special case**: If return type is `Stream`, the response will be live-streamed from the remote service
