# Security

## Trust Model

Zerra distinguishes between two categories of service:

- **Internal services** — services that communicate directly via TCP, HTTP, or a message broker without going through the API gateway. These are considered trusted callers. The framework does not authenticate the sender; it simply accepts and reconstructs whatever `ClaimsPrincipal` is included in the message envelope. It is your responsibility to ensure these services are only reachable from within a trusted network boundary (e.g. a private VNet, container network, or firewall rule).

- **Gateway-facing services** — services that receive requests through the [Zerra.Web](ZerraWeb.md) API gateway. These are the public entry points and should be secured in one of two ways: implement `ICqrsAuthorizer` to validate headers and set `Thread.CurrentPrincipal` directly, or place standard ASP.NET Core authentication middleware (`UseAuthentication` / `UseAuthorization`) before `UseCqrsApiGateway` and use a minimal `ICqrsAuthorizer` that simply copies `HttpContext.User` onto `Thread.CurrentPrincipal`. See the [Zerra.Web API Gateway](#zerraweb-api-gateway--icqrsauthorizer) section below.

### Encryption as a Trust Mechanism

For internal services, supplying an `IEncryptor` with a shared key is a lightweight way to enforce that only callers who possess the key can send valid messages. Any message that cannot be decrypted is rejected before it reaches a handler. This does not replace network-level isolation but provides an additional layer of assurance against unauthorized senders.

```csharp
var encryptor = new ZerraEncryptor("shared-internal-secret", SymmetricAlgorithmType.AESwithPrefix);
```

See [Encryptors](Encryptors.md) for setup details.

---

Zerra automatically propagates the security claims from the calling thread's `ClaimsPrincipal` to the remote service for all message types — Queries, Commands, and Events. This means any claims established on the client (e.g., from JWT authentication or cookie-based identity) are carried transparently across service boundaries without any additional configuration.

---

## How It Works

When a message is dispatched, Zerra reads `Thread.CurrentPrincipal` on the calling thread and, if it is a `ClaimsPrincipal`, serializes its claims into the message envelope. On the receiving server, Zerra deserializes those claims, reconstructs the `ClaimsPrincipal`, and sets it back on `Thread.CurrentPrincipal` before invoking the handler. The handler therefore runs under the same security identity as the original caller.

```
Client Thread                            Server Handler Thread
─────────────────────────────────────    ─────────────────────────────────────
Thread.CurrentPrincipal                  Thread.CurrentPrincipal
  ClaimsPrincipal                 ──►      ClaimsPrincipal (reconstructed)
    Claim("sub",  "user-123")               Claim("sub",  "user-123")
    Claim("role", "Admin")                  Claim("role", "Admin")
```

This propagation happens for every transport:

| Transport | Queries | Commands | Events |
|-----------|:-------:|:--------:|:------:|
| TCP (direct) | ✅ | ✅ | ✅ |
| HTTP | ✅ | ✅ | ✅ |
| Azure Service Bus | — | ✅ | ✅ |
| Kafka | — | ✅ | ✅ |
| RabbitMQ | — | ✅ | ✅ |

> Queries are only supported over the direct TCP and HTTP transports; message-broker transports handle commands and events.

---

## Reading Claims in a Handler

Because `Thread.CurrentPrincipal` is set before the handler is called, you can read claims anywhere inside it using standard .NET APIs.

```csharp
public class OrderCommandHandler : ICommandHandler<PlaceOrderCommand>
{
    public async ValueTask Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
        {
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = principal.IsInRole("Admin");
            // use userId / isAdmin …
        }
    }
}
```

The same pattern works identically in query handlers and event handlers.

---

## No Claims / Unauthenticated Callers

If `Thread.CurrentPrincipal` on the calling thread is `null` or is not a `ClaimsPrincipal`, no claims are included in the message. On the server side, `Thread.CurrentPrincipal` is explicitly set to `null` in that case, so handlers never accidentally inherit a stale identity from a previous request on the same thread.

---

## Authorization

Zerra does not enforce authorization automatically — that is intentional so you keep full control. Throw a `SecurityException` inside a handler to signal an authorization failure. The framework catches `SecurityException` and returns an appropriate error response to the caller (e.g., `401 Unauthorized` when using the HTTP transport or Zerra.Web API gateway).

```csharp
public async ValueTask Handle(DeleteUserCommand command, CancellationToken cancellationToken)
{
    if (Thread.CurrentPrincipal is not ClaimsPrincipal principal || !principal.IsInRole("Admin"))
        throw new SecurityException("Only admins can delete users.");

    // …
}
```

---

## Zerra.Web API Gateway — `ICqrsAuthorizer`

When the [Zerra.Web](ZerraWeb.md) API gateway receives an inbound HTTP request it calls `ICqrsAuthorizer.Authorize` **before** dispatching the message. This is the correct place to inspect request headers, validate tokens or API keys, and establish the caller's identity. The implementation **must** set `Thread.CurrentPrincipal` to a `ClaimsPrincipal` — that is what Zerra reads when it serializes claims into the outgoing message envelope for propagation to the remote handler.

### How the gateway calls the authorizer

Because `UseCqrsApiGateway` is registered as standard ASP.NET Core middleware, it runs **after** any middleware that precedes it in the pipeline — including `UseAuthentication` and `UseAuthorization`. By the time `ICqrsAuthorizer.Authorize` is called, ASP.NET has already validated any token and populated `HttpContext.User`.

```
Inbound HTTP request
        │
        ▼
app.UseAuthentication()              ← ASP.NET validates token, sets HttpContext.User
        │
        ▼
app.UseAuthorization()               ← ASP.NET enforces [Authorize] policies
        │
        ▼
app.UseCqrsApiGateway(…)
        │
        ▼
ICqrsAuthorizer.Authorize(headers)   ← validate headers; MUST set Thread.CurrentPrincipal
        │  SecurityException ──► 401 Unauthorized
        ▼
Zerra serializes Thread.CurrentPrincipal claims into the message envelope
        │
        ▼
Remote handler receives message with claims set on Thread.CurrentPrincipal
```

### Relying on ASP.NET Middleware

If you are already using ASP.NET Core authentication (`AddAuthentication` / `UseAuthentication`), you do not need to re-validate the token inside `ICqrsAuthorizer`. ASP.NET will have already validated it and populated `HttpContext.User` before the gateway middleware runs. Your authorizer can simply read the user from `IHttpContextAccessor` and set `Thread.CurrentPrincipal`.

```csharp
using System.Security;
using System.Security.Claims;
using Zerra.CQRS.Network;

public class AspNetCqrsAuthorizer : ICqrsAuthorizer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AspNetCqrsAuthorizer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // ASP.NET has already validated the token by the time this is called.
    // Just check the user and set Thread.CurrentPrincipal for claim propagation.
    public void Authorize(Dictionary<string, List<string?>> headers)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            throw new SecurityException("User is not authenticated");

        // Optionally enforce roles or policies
        if (!user.IsInRole("ApiUser"))
            throw new SecurityException("Insufficient permissions");

        // Set the principal so Zerra propagates claims to the remote handler
        Thread.CurrentPrincipal = user;
    }

    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
    {
        // Return whatever headers the client should send (e.g. Bearer token)
        return new ValueTask<Dictionary<string, List<string?>>>(new Dictionary<string, List<string?>>());
    }
}
```

Register it alongside ASP.NET authentication:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* configure issuer, audience, signing key … */ });
builder.Services.AddSingleton<ICqrsAuthorizer, AspNetCqrsAuthorizer>();

var app = builder.Build();

// Order matters — authentication and authorization run before the gateway
app.UseAuthentication();
app.UseAuthorization();
app.UseCqrsApiGateway(route: "/api/cqrs");
```

### Implementing `ICqrsAuthorizer` for JWT Bearer (manual validation)

The most common pattern is to validate the `Authorization` header, build a `ClaimsPrincipal` from the token, and assign it to `Thread.CurrentPrincipal`. Zerra then picks up those claims automatically when it sends the message.

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Zerra.CQRS.Network;

public class JwtCqrsAuthorizer : ICqrsAuthorizer
{
    private readonly TokenValidationParameters _validationParameters;

    public JwtCqrsAuthorizer(TokenValidationParameters validationParameters)
    {
        _validationParameters = validationParameters;
    }

    // Called by the gateway for every inbound request.
    // Validate the token and set Thread.CurrentPrincipal so claims are propagated.
    public void Authorize(Dictionary<string, List<string?>> headers)
    {
        if (!headers.TryGetValue("Authorization", out var values))
            throw new SecurityException("Missing Authorization header");

        var bearer = values.FirstOrDefault(v => v?.StartsWith("Bearer ") == true);
        if (bearer is null)
            throw new SecurityException("Invalid Authorization header");

        var token = bearer["Bearer ".Length..];

        var handler = new JwtSecurityTokenHandler();
        ClaimsPrincipal principal;
        try
        {
            principal = handler.ValidateToken(token, _validationParameters, out _);
        }
        catch (Exception ex)
        {
            throw new SecurityException($"Token validation failed: {ex.Message}");
        }

        // Set the principal — Zerra reads this when building the message envelope
        Thread.CurrentPrincipal = principal;
    }

    // Called by the client side when sending outbound requests through the gateway.
    // Return the headers that carry the caller's credentials.
    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
    {
        // Obtain the token from wherever the client stores it (e.g. a token cache)
        var token = TokenCache.GetCurrentToken();

        var headers = new Dictionary<string, List<string?>>
        {
            ["Authorization"] = new List<string?> { $"Bearer {token}" }
        };
        return new ValueTask<Dictionary<string, List<string?>>>(headers);
    }
}
```

Register the authorizer and wire up the gateway:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* configure issuer, audience, signing key … */ });

// Provide the same validation parameters used by AddJwtBearer
builder.Services.AddSingleton<ICqrsAuthorizer>(sp =>
{
    var parameters = new TokenValidationParameters { /* … */ };
    return new JwtCqrsAuthorizer(parameters);
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseCqrsApiGateway(route: "/api/cqrs");
```

The gateway middleware will:
- Call `Authorize(headers)` for every inbound request.
- Return `401 Unauthorized` if a `SecurityException` is thrown.
- Return `500 Internal Server Error` for any other exception.

### Implementing `ICqrsAuthorizer` for API Keys

A simpler approach when JWT is not required:

```csharp
public class ApiKeyCqrsAuthorizer : ICqrsAuthorizer
{
    private readonly string _validKey;

    public ApiKeyCqrsAuthorizer(string validKey) => _validKey = validKey;

    public void Authorize(Dictionary<string, List<string?>> headers)
    {
        if (!headers.TryGetValue("X-API-Key", out var keys) || !keys.Contains(_validKey))
            throw new SecurityException("Invalid or missing API key");

        // Optionally build a minimal ClaimsPrincipal to carry a service identity
        var identity = new ClaimsIdentity(new[] { new Claim("client", "trusted-service") }, "ApiKey");
        Thread.CurrentPrincipal = new ClaimsPrincipal(identity);
    }

    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
    {
        var headers = new Dictionary<string, List<string?>>
        {
            ["X-API-Key"] = new List<string?> { _validKey }
        };
        return new ValueTask<Dictionary<string, List<string?>>>(headers);
    }
}
```

For further details on the gateway setup see [Zerra.Web](ZerraWeb.md).
