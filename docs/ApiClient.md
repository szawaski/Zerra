[← Back to Documentation](Index.md)

# ApiClient

`ApiClient` (in `Zerra`, namespace `Zerra.CQRS.Network`) connects a .NET application, such as a console, desktop, mobile, or server app, to the CQRS API gateway hosted by [Zerra.Web](ZerraWeb.md). It is a query client and command producer, registered on the application's bus like any other.

## Setup

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;

var apiClient = new ApiClient(
    endpoint: "https://myapp.example.com",
    serializer: new ZerraJsonSerializer(),   // must match the gateway's registered serializer
    log: log,
    authorizer: authorizer,                  // optional ICqrsAuthorizer supplying request headers
    route: "/CQRS"                           // the gateway's route
);

var bus = Bus.New("ClientApp", log);
bus.AddQueryClient<IUserQueryHandler>(apiClient);
bus.AddCommandProducer<IUserCommandHandler>(apiClient);

var user = await bus.Call<IUserQueryHandler>().GetUser("12345", cancellationToken);
await bus.DispatchAwaitAsync(new CreateUserCommand { Email = "user@example.com", Name = "John Doe" });
```

When the application is done with it, stop the bus with `bus.StopServicesAsync()`, which disposes the client, or dispose the client directly.

In ASP.NET Core or a MAUI app, build the client and bus once at startup and register the bus as a singleton `IBus`.

## Notes

- **Authorization:** the authorizer's `GetAuthorizationHeadersAsync` supplies headers such as an API key or bearer token for each request. [Security](Security.md) has complete API key and JWT authorizers.
- **Origin:** `ApiClient` sends the gateway's host name as its `Origin`. If the gateway sets `allowOrigins`, include that host, or the gateway answers `401`. See [CORS Configuration](ZerraWeb.md#cors-configuration).
- **Serializer:** the gateway accepts only its registered serializer's content type, usually JSON. A gateway registered with `ZerraByteSerializer` serves .NET callers only.
- **Synchronous queries:** on .NET Standard 2.0 they throw `PlatformNotSupportedException`, because `HttpClient` has no synchronous send there. Use async query methods.
- **Errors:** a handler's exception arrives as a `RemoteServiceException`, as over any other transport. See [Commands](Commands.md#errors).

## See Also

- [Zerra.Web](ZerraWeb.md) - The gateway, authorization, and CORS
- [Client Setup](ClientSetup.md) - Calling services directly over TCP, HTTP, or brokers
