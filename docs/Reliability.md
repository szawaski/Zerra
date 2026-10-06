[← Back to Documentation](Index.md)

# Delivery and Failure Handling

This page states what Zerra guarantees when messages cross a process boundary, and what it leaves to your handlers. Read it before putting a payment, an email, or a stock movement behind a remote command.

## The Short Version

- **Commands and events are delivered at most once.** Every transport acknowledges a message when a consumer receives it, before the handler runs. A message is never handled twice by Zerra's own doing, but one in flight when its process is forcibly terminated is lost. Acknowledging on receipt keeps the queue or partition moving for the other replicas instead of holding it until handlers finish.
- **Zerra does not retry a failed handler.** A handler's exception goes back to a caller awaiting it, or to the logs. There is no automatic redelivery, dead-letter queue, or poison-message handling.
- **Graceful shutdown loses nothing.** Stopping the bus stops receiving, then waits for every message already received to finish. Handlers are never cancelled.
- **Duplicates come from callers.** A caller that retries after a `TimeoutException` may send a command that already ran, so handlers that can be retried should be idempotent.

## When Each Message Is Acknowledged

| Transport | Acknowledged | Messages not yet received |
|---|---|---|
| Kafka | the offset is committed as the message is consumed | kept in the topic, read by the group's next consumer |
| RabbitMQ | `BasicAck` as the message is received | kept in the queue (messages are transient, so a broker restart drops them) |
| Azure Service Bus | received in `ReceiveAndDelete` mode | kept in the queue or subscription |
| TCP / HTTP / Kestrel | the server has read and deserialized the request | not applicable: the caller gets an error if the server is unreachable |

So for every transport:

| What happens | Result |
|---|---|
| The consumer is stopped normally | messages already received finish; the rest wait for the next consumer |
| The process crashes or is killed while handling | that message is lost |
| The handler throws | the message is not redelivered; an awaiting caller gets `RemoteServiceException` |
| The broker is unreachable | consumers retry every 5 seconds; a dispatch fails with an exception or times out |

`PerReplica` event subscriptions belong to one consumer, so events published while that replica is down are not held for it. See [Events](Events.md#choosing-per-replica-or-per-service).

## What a Dispatch Returns

| Call | Returns when | A handler exception |
|---|---|---|
| `DispatchAsync` to a broker | Kafka and Service Bus have accepted the message; RabbitMQ's client has published it | never reaches the caller |
| `DispatchAsync` over TCP or HTTP | the server has received the message and started the handler | never reaches the caller |
| `DispatchAwaitAsync` | the handler has finished | throws `RemoteServiceException` |
| `Call<T>().Method(...)` (a query) | the handler has returned | throws `RemoteServiceException` |

`DispatchAwaitAsync` waits for **that** handler only. Commands and events the handler sends with `DispatchAsync` have been sent, not handled, so it says nothing about other services' subscribers. To wait for downstream work, the handler must itself `DispatchAwaitAsync` the commands it depends on.

A `TimeoutException` or `OperationCanceledException` means the caller stopped waiting. The command may still run, may have run, or may never have arrived. A command's cancellation token isn't sent to a remote handler, so it always runs to completion once received. See [Commands](Commands.md#cancellation).

### Connection Retries

The TCP and HTTP clients pool connections. If a pooled connection turns out to be closed before the server starts responding, the request is sent once more on a new connection. Nothing is retried once the server has started responding, and a request with an upload stream is never retried. Zerra makes no other retries.

## Making Handlers Safe

Zerra leaves retry policy to you because only the handler knows whether repeating its work is safe. The usual patterns:

- **Make retried commands idempotent.** Give the command an ID the caller creates, and have the handler skip an ID it has already stored, or check the target state first, as `ActivateUserCommand` does in [Commands](Commands.md#idempotency).
- **Save before you notify.** Write the state change, then dispatch the event. If the process dies between the two, the state is right and only the notification is missing, which a reconciliation job or the next change can repair. The reverse order announces changes that never happened.
- **Retry at the caller, deliberately.** Wrap `DispatchAwaitAsync` in your own retry policy, for example with Polly, only for commands whose handlers are idempotent.
- **Use an outbox when a notification must not be lost.** Zerra has no built-in transactional outbox. Store outgoing messages in the same transaction as the state change, and have a background job dispatch them and mark them sent.
- **Compensate across services.** When a handler awaits several commands, undo the earlier ones if a later one fails. See [Coordinating Several Services](Commands.md#coordinating-several-services).
- **Choose commands for work that must happen once.** Events fan out to every replica unless the subscriber registers `PerService`. See [Events Are Fanned Out to Every Replica](Events.md#events-are-fanned-out-to-every-replica).

## Remote Calls Look Local

`bus.Call<IUserQueryHandler>().GetUserById(id)` reads the same whether the handler is local or remote, but a remote call can be slow, fail, or time out. Plan for that at the call site:

- **Set timeouts.** None are set by default. Set `defaultCallTimeout`, `defaultDispatchTimeout`, and `defaultDispatchAwaitTimeout` on `Bus.New`, or pass a `TimeSpan` or `CancellationToken` per call. See [Client Setup](ClientSetup.md#timeout-configuration).
- **Pass the cancellation token.** A query's token reaches the remote handler, so a caller that gives up stops the work too. See [Queries](Queries.md#cancellation-and-timeouts).
- **Catch the remote failures.** `RemoteServiceException` (the handler threw), `TimeoutException`, and connection exceptions. See [Client Setup](ClientSetup.md#errors).
- **Bound concurrency.** `maxConcurrentQueries`, `maxConcurrentCommandsPerTopic`, and `maxConcurrentEventsPerTopic` on `Bus.New` limit how much each client and server runs at once; further messages wait.
- **Log every hop.** An `IBusLogger` records each message's source, service, duration, and exception on both ends. See [Logging](Logging.md#ibuslogger).

Registration is the one place that says which interfaces are remote. Keeping it in `Program.cs`, with one line per interface, makes that easy to review.

## Testing Failures

Before relying on a transport in production, exercise the cases above against your own handlers:

- kill a consumer while it handles a long command, and confirm what your reconciliation or retry does
- stop the broker during a dispatch, and confirm callers get an exception and consumers reconnect
- dispatch the same command twice, and confirm the handler's idempotency
- stop the service normally under load, and confirm in-flight work finishes within the orchestrator's grace period (see [Shutdown](ServerSetup.md#shutdown))

`Demo/Store` falls back to direct TCP or in-memory stores when a broker or database isn't running, which makes these experiments quick to set up.

## See Also

- [Commands](Commands.md) - Dispatching, errors, and idempotency
- [Events](Events.md) - Per-replica and per-service delivery
- [Server Setup](ServerSetup.md#shutdown) - Shutdown and work in progress
- [Production Checklist](Production.md) - Security, versioning, and operations
