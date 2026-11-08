[← Back to Documentation](Index.md)

# Security

## Trust Model

- **Services** talk to each other over TCP, HTTP, or a broker inside a private network. Zerra doesn't authenticate the sender: it accepts the claims in the message. The network boundary is what makes these callers trusted, so keep servers and brokers off the public internet.
- **Outside callers** come through the [Zerra.Web](ZerraWeb.md) API gateway. Register an `ICqrsAuthorizer` there to check every request, or run ASP.NET Core authentication in front of it.

Message [encryption](Encryptors.md) keeps messages private and, with `AES_GCM` or `AES_CBC_HMAC`, rejects changed ones. It doesn't prove who sent a message, and a captured message can be sent again. See [Running in Production](Production.md#security) for TLS and key rotation.

## Claims Travel With Every Message

When a message is sent, the caller's `Thread.CurrentPrincipal`, if it's a `ClaimsPrincipal`, goes into the message. The handler runs with the same principal on its own `Thread.CurrentPrincipal`, however many services away. There is nothing to configure.

```csharp
public async Task Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
{
    if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = principal.IsInRole("Admin");
    }
}
```

Without a `ClaimsPrincipal` on the caller, the handler's `Thread.CurrentPrincipal` is `null`, never a stale identity from an earlier message.

## Authorization

Zerra enforces nothing on its own. Check the claims in the handler and throw `SecurityException`. The caller gets it as a `CqrsServiceException` with `ErrorType` `SecurityException`, and the gateway answers `401 Unauthorized`:

```csharp
if (Thread.CurrentPrincipal is not ClaimsPrincipal principal || !principal.IsInRole("Admin"))
    throw new SecurityException("Only admins can delete users.");
```

## The Gateway Authorizer

The gateway calls `ICqrsAuthorizer.Authorize(headers)` before every request is dispatched. Throw `SecurityException` to refuse it, and set `Thread.CurrentPrincipal` to the caller's `ClaimsPrincipal` so the handlers receive it. The same class supplies the headers on the calling side, for `ApiClient` and `HttpCqrsClient`, through `GetAuthorizationHeaders`.

```csharp
public interface ICqrsAuthorizer
{
    void Authorize(Dictionary<string, List<string?>> headers);
    ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default);
    Dictionary<string, List<string?>> GetAuthorizationHeaders(CancellationToken cancellationToken = default);
}
```

Register it in DI:

```csharp
builder.Services.AddSingleton<ICqrsAuthorizer, JwtCqrsAuthorizer>();
app.UseCqrsApiGateway("/CQRS");
```

### With ASP.NET Core Authentication

`UseAuthentication` runs before the gateway, so the token is already validated. The authorizer only copies `HttpContext.User` across:

```csharp
public class AspNetCqrsAuthorizer(IHttpContextAccessor httpContextAccessor) : ICqrsAuthorizer
{
    public void Authorize(Dictionary<string, List<string?>> headers)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            throw new SecurityException("User is not authenticated");

        Thread.CurrentPrincipal = user;
    }

    public Dictionary<string, List<string?>> GetAuthorizationHeaders(CancellationToken cancellationToken = default) => new();
    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default) => new(GetAuthorizationHeaders(cancellationToken));
}
```

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => { /* issuer, audience, signing key */ });
builder.Services.AddSingleton<ICqrsAuthorizer, AspNetCqrsAuthorizer>();

var app = builder.Build();
app.UseAuthentication();   // before the gateway
app.UseAuthorization();
app.UseCqrsApiGateway("/CQRS");
```

### JWT, Validated by the Authorizer

```csharp
public class JwtCqrsAuthorizer(TokenValidationParameters validationParameters) : ICqrsAuthorizer
{
    public void Authorize(Dictionary<string, List<string?>> headers)
    {
        if (!headers.TryGetValue("Authorization", out var values))
            throw new SecurityException("Missing Authorization header");

        var bearer = values.FirstOrDefault(v => v?.StartsWith("Bearer ") == true)
            ?? throw new SecurityException("Invalid Authorization header");

        try
        {
            Thread.CurrentPrincipal = new JwtSecurityTokenHandler().ValidateToken(bearer["Bearer ".Length..], validationParameters, out _);
        }
        catch (Exception ex)
        {
            throw new SecurityException($"Token validation failed: {ex.Message}");
        }
    }

    // the calling side: the headers to send
    public Dictionary<string, List<string?>> GetAuthorizationHeaders(CancellationToken cancellationToken = default)
        => new() { ["Authorization"] = [$"Bearer {TokenCache.GetCurrentToken()}"] };

    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default) => new(GetAuthorizationHeaders(cancellationToken));
}
```

### API Key

```csharp
public class ApiKeyCqrsAuthorizer(string validKey) : ICqrsAuthorizer
{
    public void Authorize(Dictionary<string, List<string?>> headers)
    {
        if (!headers.TryGetValue("X-API-Key", out var keys) || !keys.Contains(validKey))
            throw new SecurityException("Invalid or missing API key");

        Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("client", "trusted-service")], "ApiKey"));
    }

    public Dictionary<string, List<string?>> GetAuthorizationHeaders(CancellationToken cancellationToken = default)
        => new() { ["X-API-Key"] = [validKey] };

    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(CancellationToken cancellationToken = default) => new(GetAuthorizationHeaders(cancellationToken));
}
```

## See Also

- [Zerra.Web](ZerraWeb.md) - The gateway, CORS, and the production checklist
- [Encryptors](Encryptors.md) - What encryption does and doesn't protect
- [Running in Production](Production.md#security)
