[← Back to Documentation](Index.md)

# Delivery and Failure Handling

How Zerra delivers commands and events across process boundaries, why it works that way, and how handlers fit into it.

## The Delivery Model

By default every transport acknowledges a message when a consumer receives it, then runs the handler. Broker consumers can opt into [resilient commands](#resilient-commands) instead. Receipt is the default by choice:

- **The queue never waits on a slow handler.** A replica busy with a long command doesn't hold a partition, lock, or unacknowledged message that other replicas are waiting behind.
- **A message is never run twice by Zerra.** There is no redelivery after a handler has started, so a handler that charged a card or sent an email isn't run again because a lock expired or a connection blinked.
- **Shutdown finishes everything received.** Stopping the bus stops receiving, then waits for every received message to finish. Handlers are never cancelled. Rolling deployments, scale-in, and `docker stop` lose nothing.

The one case where work is lost is a forced termination, such as an out-of-memory kill or power loss, while a handler is running. Only the messages being handled at that moment are affected; messages not yet received stay in the broker for the next consumer. [Resilient commands](#resilient-commands) cover that case for commands.

### Resilient Commands

Pass `resilientCommands: true` to a `KafkaConsumer`, `RabbitMQConsumer`, or `AzureServiceBusConsumer` and its commands are acknowledged when the handler finishes instead of when they're received. If the process dies while a handler is running, the broker gives that command to the next replica.

```csharp
var consumer = new RabbitMQConsumer("localhost", serializer, encryptor, null, log, "prod", resilientCommands: true);
```

What changes:

- **A command can run more than once.** It runs again if the process dies after the handler's work but before the acknowledgement. Make these handlers [idempotent](Commands.md#idempotency).
- **A failed handler is still not retried.** A handler that throws has handled the command, the same as without the option. Only a crash brings a command back.
- **The broker waits on running handlers.** Kafka commits a command's offset only after every earlier command from that partition has finished, and a replica joining or leaving the group waits for the running commands before the partition moves. Azure Service Bus keeps renewing the message lock while the handler runs.
- **Broker limits apply.** RabbitMQ closes a channel that holds a message unacknowledged longer than its `consumer_timeout` (30 minutes by default). Azure Service Bus moves a command to the dead-letter queue after its `MaxDeliveryCount` (10 by default) deliveries, so a command that crashes the process every time stops coming back.

Events are not affected; they're always acknowledged when received.

### Handler Failures Are Results, Not Retries

A handler that throws has handled the message: the exception is its outcome. A caller using `DispatchAwaitAsync` receives it as a `RemoteServiceException` with the original type and message, and otherwise the handling service's `IBusLogger` records it. Zerra doesn't redeliver it, because a handler failure is usually a business outcome (validation, a missing record, a rule violation) where running again changes nothing or repeats a side effect. The caller knows whether a retry makes sense, so retry policy belongs there. With no redelivery loop there are no poison messages to bound and nothing to dead-letter.

## When Each Message Is Acknowledged

| Transport | Acknowledged | Messages not yet received |
|---|---|---|
| Kafka | the offset is committed as the message is consumed | kept in the topic, read by the group's next consumer |
| RabbitMQ | `BasicAck` as the message is received | kept in the queue (messages are transient, so a broker restart drops them) |
| Azure Service Bus | received in `ReceiveAndDelete` mode | kept in the queue or subscription |
| TCP / HTTP / Kestrel | the server has read and deserialized the request | not applicable: the caller gets an error if the server is unreachable |

| What happens | Result |
|---|---|
| The service stops normally, including a deployment or scale-in | messages already received finish; the rest wait for the next consumer |
| The handler throws | an awaiting caller gets `RemoteServiceException`; the message is done |
| The broker is unreachable | consumers reconnect every 5 seconds; a dispatch fails with an exception or times out |
| The process is forcibly terminated while handling | the messages being handled at that moment are lost, except [resilient commands](#resilient-commands), which go to the next consumer |

`PerReplica` event subscriptions belong to one consumer, so events published while that replica is down aren't held for it, which is what a per-replica notification wants. `PerService` subscriptions hold them. See [Events](Events.md#choosing-per-replica-or-per-service).

## What a Dispatch Returns

| Call | Returns when | A handler exception |
|---|---|---|
| `DispatchAsync` to a broker | Kafka and Service Bus have accepted the message; RabbitMQ's client has published it | recorded by the handling service's `IBusLogger` |
| `DispatchAsync` over TCP or HTTP | the server has received the message and started the handler | recorded by the handling service's `IBusLogger` |
| `DispatchAwaitAsync` | the handler has finished | throws `RemoteServiceException` |
| `Call<T>().Method(...)` (a query) | the handler has returned | throws `RemoteServiceException` |

`DispatchAwaitAsync` waits for that command's handler. Commands and events the handler itself sends with `DispatchAsync` have been sent; to wait for downstream work, the handler awaits the commands it depends on.

### Timeouts

As with any remote call, over HTTP, gRPC, or a broker, a caller that stops waiting can't know whether the work ran. A `TimeoutException` or `OperationCanceledException` means the caller stopped waiting; the command may still run, may have run, or may not have arrived. A command's cancellation token isn't sent to a remote handler, so a command that has started always runs to completion rather than stopping halfway through a state change. A query's token is sent, since a query is safe to stop at any point. See [Commands](Commands.md#cancellation).

### Connection Retries

The TCP and HTTP clients pool connections. If a pooled connection turns out to be closed before the server starts responding, the request is sent once more on a new connection. Nothing is retried once the server has started responding, and a request with an upload stream is never retried.

## Building Handlers for It

Only the handler knows whether repeating its work is safe, so these choices are the application's:

- **Idempotent commands where callers retry.** Give the command an ID the caller creates and have the handler skip an ID it has already stored, or check the target state first, as `ActivateUserCommand` does in [Commands](Commands.md#idempotency).
- **Save, then notify.** Write the state change, then dispatch the event. The reverse order can announce a change that never happened.
- **An outbox where a notification must survive a forced termination.** Store the outgoing message in the same transaction as the state change, and dispatch it from a background job.
- **Retry at the caller.** Wrap `DispatchAwaitAsync` in a retry policy, for example with Polly, for commands whose handlers are idempotent.
- **Compensate across services.** When a handler awaits several commands, undo the earlier ones if a later one fails. See [Coordinating Several Services](Commands.md#coordinating-several-services).
- **Commands for work that must happen once.** Events fan out to every replica unless the subscriber registers `PerService`. See [Events Are Fanned Out to Every Replica](Events.md#events-are-fanned-out-to-every-replica).

## Remote Calls Look Local

`bus.Call<IUserQueryHandler>().GetUserById(id)` reads the same whether the handler is local or remote. The bus gives the caller what a remote call needs:

- **Timeouts.** `defaultCallTimeout`, `defaultDispatchTimeout`, and `defaultDispatchAwaitTimeout` on `Bus.New`, or a `TimeSpan` or `CancellationToken` per call. See [Client Setup](ClientSetup.md#timeout-configuration).
- **Cancellation.** A query's token reaches the remote handler, so a caller that gives up stops the work too. See [Queries](Queries.md#cancellation-and-timeouts).
- **Typed failures.** `RemoteServiceException` when the handler threw, `TimeoutException`, and connection exceptions. See [Client Setup](ClientSetup.md#errors).
- **Bounded concurrency.** `maxConcurrentQueries`, `maxConcurrentCommandsPerTopic`, and `maxConcurrentEventsPerTopic` on `Bus.New` limit how much each client and server runs at once; further messages wait.
- **Per-hop logging.** An `IBusLogger` records each message's source, service, duration, and exception on both ends. See [Logging](Logging.md#ibuslogger).

Registration in `Program.cs` is the one place that says which interfaces are remote, one line per interface.

## How This Is Tested

The behaviors on this page are covered by the transport test suites against real Kafka, RabbitMQ, and Azure Service Bus brokers: finishing work in progress on shutdown, handler errors not being received again, consumers recovering after their topic or queue is deleted, receive-limit hand-off between replicas, and sustained load across replicas within the concurrency limits. See [Testing](Testing.md).

## See Also

- [Commands](Commands.md) - Dispatching, errors, and idempotency
- [Events](Events.md) - Per-replica and per-service delivery
- [Server Setup](ServerSetup.md#shutdown) - Shutdown and work in progress
- [Running in Production](Production.md) - Security, versioning, and operations
