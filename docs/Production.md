[← Back to Documentation](Index.md)

# Running in Production

How a Zerra deployment is laid out, secured, versioned, and observed. Each section links to the page with the details.

## Delivery

Commands and events are acknowledged on receipt and handled once; shutdown finishes everything received; a handler's exception is its result and isn't retried. [Delivery and Failure Handling](Reliability.md) explains the model and the reasoning behind it.

- Put work that must happen once in a command, or in an event the subscriber registers `PerService` ([Events](Events.md#the-rule)).
- Make commands idempotent where callers retry them ([Commands](Commands.md#idempotency)).
- Set timeouts on `Bus.New` or per call ([Client Setup](ClientSetup.md#timeout-configuration)).
- Set `shutdownTimeout` a few seconds shorter than the orchestrator's grace period ([Server Setup](ServerSetup.md#shutdown)).

## Security

### Trust Boundaries

Zerra separates public traffic from service-to-service traffic:

- **Public callers** reach the [Zerra.Web](ZerraWeb.md) API gateway, which authenticates and authorizes every request through `ICqrsAuthorizer` or ASP.NET Core authentication, restricts browser origins, and exposes only the interfaces its bus registers. Events are never accepted from outside.
- **Services** talk to each other directly over TCP, HTTP, or a broker inside a private network, the same trust model as most service meshes and internal gRPC. The caller's claims travel with each message, so a handler sees the identity of the user who started the request, several hops away. Message encryption with a shared key means only services holding the key can send a valid message.

So internal servers and brokers belong on a private network, the gateway has an `ICqrsAuthorizer`, and handlers check the claims they depend on. See [Security](Security.md).

### Encryption

| Connection | Encryption |
|---|---|
| `TcpCqrsServer`, `HttpCqrsServer` | message encryption on every message with an `IEncryptor` such as `ZerraEncryptor` (AES) |
| Kestrel (`Zerra.Web`) | HTTPS, configured in ASP.NET Core, plus message encryption |
| RabbitMQ | TLS with an `amqps://` URI ([RabbitMQ Setup](RabbitMQSetup.md#connection-settings)), plus message encryption |
| Azure Service Bus | TLS, plus message encryption |
| Kafka | TLS with `useTls: true`, as `SSL` or `SASL_SSL` ([Kafka Setup](KafkaSetup.md#connection-settings)), plus message encryption |

Message encryption protects message bodies end to end, including while they sit on a broker's disk. Read the key from configuration or a secret store, with one key per environment ([Encryptors](Encryptors.md#configuration)). Both ends of an endpoint use the same key, so a key is rotated by updating the services on that endpoint together, or by bringing up the new key under a new environment prefix or port and moving callers over.

### Error Details

A handler's exception reaches the caller with its type name, message, and stack trace, including across several service hops and through the gateway. Failures are easy to diagnose anywhere in a call chain, and validation messages reach the user as written. Write exception messages for whoever can call, without secrets, connection strings, or personal data.

### Request Limits

- The gateway runs inside ASP.NET Core, so Kestrel's request size limit and ASP.NET rate limiting apply ([Zerra.Web](ZerraWeb.md#production-checklist)). Stream uploads are let through on purpose.
- Every client and server is bounded by `maxConcurrentQueries` and the per-topic command and event limits on `Bus.New`; further messages wait ([Client Setup](ClientSetup.md#bus-options)).

## Versioning and Rolling Deployments

Services that deploy independently run different versions of a contract for a while:

- **Contract identity.** Messages and query interfaces are identified by their assembly-qualified type name. To rename a contract, add the new one and retire the old one.
- **Binary contracts.** `ZerraByteSerializer` matches members by declaration order by default, the most compact form. Give contracts that will change a `[SerializerIndex]` per member and add members rather than reordering ([ByteSerializer](ByteSerializer.md#versioning)). `ZerraJsonSerializer` matches by name.
- **Deployment order.** A receiver on the new version reads an old sender's messages, giving missing members their defaults. With indexes, deploy the receiving service before the services that send a new member. With `ByteSerializerIndexType.MemberNames`, `UseTypes = true`, or `ZerraJsonSerializer`, unknown members are skipped and either order works.
- **Major versions.** Zerra 5 and 6 use different wire formats ([Upgrade Guide](UpgradeV5ToV6.md)), so services that share messages move to a new major version together.

## Observability

Zerra plugs into the logging and tracing you already run instead of bundling its own ([Logging](Logging.md)):

- **`ILogger`** receives framework events, such as a lost broker connection or a message that couldn't be read.
- **`IBusLogger`** sees every query, command, and event on both the sending and the handling side, with the source service, duration, and exception. Implement it to start OpenTelemetry activities, record metrics, or write structured logs; because every service logs both ends, the logs together trace a request across services.

Pass the `ILogger` to `Bus.New` and to every server, client, producer, and consumer, and alert on repeated broker connection errors and handler exceptions.

## Testing an Application

- Handlers are tested in memory, registered on a bus with `AddHandler` and an in-memory repository, as the `Store.*.Test` projects do ([Store demo](../Demo/Store/README.md)).
- `Demo/Store` falls back to direct TCP and in-memory stores when a broker or database isn't running, so the same services run on a laptop and against real infrastructure.
- Publish and run a Native AOT build if you deploy with AOT ([AOT](AOT.md)).

How Zerra itself is tested is described in [Testing](Testing.md).

## Repository

`Zerra.Repository` is experimental. The CQRS packages don't depend on it, so it's adopted separately. See [Repository](Repository.md).

## See Also

- [Delivery and Failure Handling](Reliability.md)
- [Security](Security.md)
- [Testing](Testing.md)
- [Server Setup](ServerSetup.md) and [Client Setup](ClientSetup.md)
- [Store demo](../Demo/Store/README.md) - A complete multi-service application
