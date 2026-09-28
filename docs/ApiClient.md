[← Back to Documentation](Index.md)

# ApiClient

`ApiClient` connects a .NET application (console, desktop, mobile, or another server) to the CQRS API gateway hosted by [Zerra.Web](ZerraWeb.md). It is registered on the application's bus like any other query client and command producer, uses JSON, and takes an optional `ICqrsAuthorizer` for authorization headers. Dispose it when the application is done with it.

## Basic ApiClient Setup

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Logging;

// Configure components
ISerializer serializer = new ZerraJsonSerializer();
ILogger logger = new ConsoleLogger();

// Create API client
var apiClient = new ApiClient(
    endpoint: "https://myapp.azurewebsites.net",
    serializer: serializer,
    log: logger,
    authorizer: null,  // Add ICqrsAuthorizer if needed
    route: "/api/cqrs"
);

// Create bus and register the API client
var bus = Bus.New("ClientApp", log: logger);
bus.AddCommandProducer<IUserCommandHandler>(apiClient);
bus.AddQueryClient<IUserQueryHandler>(apiClient);

// Use the bus to call remote services
try
{
    // Call a query
    var user = await bus.Call<IUserQueryHandler>().GetUser("12345", CancellationToken.None);
    Console.WriteLine($"User: {user.Name} ({user.Email})");

    // Dispatch a command
    await bus.DispatchAwaitAsync(new CreateUserCommand
    {
        Email = "newuser@example.com",
        Name = "John Doe"
    });
    Console.WriteLine("User created successfully");
}
catch (Exception ex)
{
    logger.Error("Error calling API", ex);
}
finally
{
    apiClient.Dispose();
}
```

## ApiClient with Authorization

```csharp
using Zerra.CQRS.Network;
using System.Security;

public class ApiKeyAuthorizer : ICqrsAuthorizer
{
    private readonly string _apiKey;

    public ApiKeyAuthorizer(string apiKey)
    {
        _apiKey = apiKey;
    }

    public void Authorize(Dictionary<string, List<string?>> headers)
    {
        // Server-side validation (not used in ApiClient)
        throw new NotImplementedException();
    }

    public Dictionary<string, List<string?>> GetAuthorizationHeaders(
        CancellationToken cancellationToken = default)
    {
        return new Dictionary<string, List<string?>>
        {
            ["X-API-Key"] = new List<string?> { _apiKey }
        };
    }

    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
        => new(GetAuthorizationHeaders(cancellationToken));
}

// Use the authorizer with ApiClient
var authorizer = new ApiKeyAuthorizer("my-secret-api-key");
var apiClient = new ApiClient(
    endpoint: "https://myapp.azurewebsites.net",
    serializer: new ZerraJsonSerializer(),
    log: logger,
    authorizer: authorizer,
    route: "/api/cqrs"
);
```

## Console Application Example

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        // Configuration
        var endpoint = "https://myapp.azurewebsites.net";
        var apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? "dev-key";

        // Setup
        ISerializer serializer = new ZerraJsonSerializer();
        ILogger logger = new ConsoleLogger();
        var authorizer = new ApiKeyAuthorizer(apiKey);

        var apiClient = new ApiClient(endpoint, serializer, logger, authorizer, "/api/cqrs");
        var bus = Bus.New("ConsoleClient", log: logger);

        bus.AddCommandProducer<IUserCommandHandler>(apiClient);
        bus.AddQueryClient<IUserQueryHandler>(apiClient);

        try
        {
            Console.WriteLine("Fetching users...");
            var users = await bus.Call<IUserQueryHandler>().GetAllUsers(CancellationToken.None);

            foreach (var user in users)
            {
                Console.WriteLine($"- {user.Name} ({user.Email})");
            }

            Console.WriteLine("\nCreating new user...");
            await bus.DispatchAwaitAsync(new CreateUserCommand
            {
                Email = "newuser@example.com",
                Name = "Jane Smith"
            });
            Console.WriteLine("User created!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            apiClient.Dispose();
        }
    }
}
```

## Xamarin/MAUI Mobile App Example

```csharp
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;
using Zerra.Logging;

public class UserService
{
    private readonly IBus _bus;
    private readonly ApiClient _apiClient;

    public UserService()
    {
        // Mobile app configuration
        var endpoint = "https://myapp.azurewebsites.net";
        var apiKey = "mobile-app-key"; // Store securely in SecureStorage

        ISerializer serializer = new ZerraJsonSerializer();
        ILogger logger = new MobileLogger(); // Custom logger for mobile

        var authorizer = new ApiKeyAuthorizer(apiKey);
        _apiClient = new ApiClient(endpoint, serializer, logger, authorizer, "/api/cqrs");

        _bus = Bus.New("MobileApp", log: logger);
        _bus.AddCommandProducer<IUserCommandHandler>(_apiClient);
        _bus.AddQueryClient<IUserQueryHandler>(_apiClient);
    }

    public async Task<UserModel> GetUserAsync(string userId)
    {
        try
        {
            return await _bus.Call<IUserQueryHandler>().GetUser(userId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Handle error (logging, user notification, etc.)
            throw new ApplicationException("Failed to load user", ex);
        }
    }

    public async Task<List<UserModel>> GetAllUsersAsync()
    {
        return await _bus.Call<IUserQueryHandler>().GetAllUsers(CancellationToken.None);
    }

    public async Task CreateUserAsync(string email, string name)
    {
        await _bus.DispatchAwaitAsync(new CreateUserCommand
        {
            Email = email,
            Name = name
        });
    }

    public void Dispose()
    {
        _apiClient?.Dispose();
    }
}

// Usage in a ViewModel or Page
public class UsersViewModel
{
    private readonly UserService _userService;

    public ObservableCollection<UserModel> Users { get; } = new();

    public UsersViewModel()
    {
        _userService = new UserService();
    }

    public async Task LoadUsersAsync()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }
        }
        catch (Exception ex)
        {
            // Show error to user
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    public async Task CreateUserAsync(string email, string name)
    {
        try
        {
            await _userService.CreateUserAsync(email, name);
            await LoadUsersAsync(); // Refresh list
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
```

## ASP.NET Core Service-to-Service Communication

```csharp
using Microsoft.Extensions.DependencyInjection;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;

// Program.cs - Configure ApiClient in DI
builder.Services.AddSingleton<ApiClient>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<ApiClient>>();

    var endpoint = configuration["ExternalApi:Endpoint"];
    var apiKey = configuration["ExternalApi:ApiKey"];

    var serializer = new ZerraJsonSerializer();
    var authorizer = new ApiKeyAuthorizer(apiKey);

    return new ApiClient(endpoint, serializer, null, authorizer, "/api/cqrs");
});

builder.Services.AddSingleton<IBus>(serviceProvider =>
{
    var apiClient = serviceProvider.GetRequiredService<ApiClient>();
    var bus = Bus.New("WebService");

    bus.AddCommandProducer<IUserCommandHandler>(apiClient);
    bus.AddQueryClient<IUserQueryHandler>(apiClient);

    return bus;
});

// Controller usage
[ApiController]
[Route("api/[controller]")]
public class ProxyController : ControllerBase
{
    private readonly IBus _bus;

    public ProxyController(IBus bus)
    {
        _bus = bus;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _bus.Call<IUserQueryHandler>().GetAllUsers(cancellationToken);
        return Ok(users);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        await _bus.DispatchAwaitAsync(new CreateUserCommand
        {
            Email = request.Email,
            Name = request.Name
        });
        return Ok();
    }
}
```

## See Also

- [Zerra.Web](ZerraWeb.md) - The CQRS API gateway, authorization, and CORS
- [Security](Security.md) - Claims from the caller to the handler
- [Client Setup](ClientSetup.md) - Calling services directly over TCP, HTTP, or brokers
