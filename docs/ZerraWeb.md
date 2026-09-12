[← Back to Documentation](Index.md)

# Zerra.Web - ASP.NET Integration

`Zerra.Web` provides ASP.NET Core integration for Zerra CQRS, enabling your CQRS bus to be hosted within ASP.NET applications. This is essential for IIS-hosted environments (Azure App Services) and for exposing CQRS functionality as an HTTP API Gateway.

## Overview

`Zerra.Web` provides:
- **IIS/Kestrel Hosting** - Run CQRS bus within ASP.NET Core applications
- **API Gateway** - Expose CQRS commands and queries as HTTP endpoints (events are intentionally not accepted from external callers)
- **Azure App Services** - Compatible with IIS-hosted Azure App Services
- **Custom Authorization** - Integrate with ASP.NET authentication/authorization
- **Logging Integration** - Bridge Zerra logging with Microsoft.Extensions.Logging
- **Multiple Content Types** - Support JSON and binary serialization
- **CORS Support** - Built-in cross-origin resource sharing

## Installation

```bash
dotnet add package Zerra.Web
```

## Key Components

### 1. CQRS API Gateway (Main Feature)

The API Gateway exposes your CQRS bus to external HTTP clients, allowing browsers, mobile apps, and other services to invoke commands and queries without knowledge of your internal architecture. Events are not accepted through the gateway; a request whose `MessageType` is not a command is rejected.

#### Basic Setup

```csharp
using Zerra.CQRS;
using Zerra.Serialization;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Web;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Configure CQRS components
ISerializer serializer = new ZerraJsonSerializer(); // Use JSON for external clients
// Fully qualified: ASP.NET implicit usings also bring Microsoft.Extensions.Logging.ILogger into scope
Zerra.Logging.ILogger log = new ConsoleLogger(); // your ILogger implementation (see Logging.md)
IBusLogger busLog = new ConsoleBusLogger();

// Create the CQRS bus
var bus = Bus.New(
    serviceName: "MyService",
    log: log,
    busLog: busLog
);

// Register handlers
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserQueries>(new UserQueryHandler());

// Add Bus and components to DI container - the gateway resolves IBus, ISerializer,
// and optionally Zerra.Logging.ILogger and ICqrsAuthorizer from DI.
// Bus.New returns IBusSetup, so register it explicitly as IBus.
builder.Services.AddSingleton<IBus>(bus);
builder.Services.AddSingleton(serializer);
builder.Services.AddSingleton(log);

var app = builder.Build();

// Enable CQRS API Gateway
app.UseCqrsApiGateway(route: "/api/cqrs");

await app.RunAsync();
```

#### How It Works

The API Gateway:
1. Listens for HTTP POST (and CORS preflight OPTIONS) requests at the specified route (default: `/CQRS`)
2. Calls the registered `ICqrsAuthorizer`, if any
3. Deserializes the request body (`ApiRequestData`) to determine which query or command to invoke
4. Dispatches the message to the CQRS bus
5. Serializes the response and returns it to the client

The request body is an `ApiRequestData` object. The front end scripts and `ApiClient` build it for you; the shapes are shown here for reference.

**Query request** (`ProviderArguments` holds each argument as its own serialized JSON value):
```json
POST /api/cqrs
Content-Type: application/json

{
  "ProviderType": "IUserQueries",
  "ProviderMethod": "GetUserById",
  "ProviderArguments": ["123"],
  "Source": "JavaScript"
}
```

**Command request** (`MessageData` is the command serialized as a JSON string):
```json
POST /api/cqrs
Content-Type: application/json

{
  "MessageType": "CreateUserCommand",
  "MessageData": "{\"Email\":\"user@example.com\",\"Name\":\"John Doe\"}",
  "MessageAwait": true,
  "MessageResult": true,
  "Source": "JavaScript"
}
```

- `MessageAwait` - `false` for fire-and-forget, `true` to wait for the handler to complete
- `MessageResult` - `true` for `ICommand<TResult>` commands; the response body is the serialized result

**Response:** the serialized query result or command result, an empty `200` for commands without a result, or a raw stream for queries that return `Stream`.

### 2. Custom Authorization

Implement `ICqrsAuthorizer` to add custom authentication/authorization:

```csharp
using Zerra.CQRS.Network;
using System.Security;

public class ApiKeyAuthorizer : ICqrsAuthorizer
{
    private readonly string _validApiKey;

    public ApiKeyAuthorizer(string validApiKey)
    {
        _validApiKey = validApiKey;
    }

    // Server-side: Validate incoming requests
    public void Authorize(Dictionary<string, List<string?>> headers)
    {
        if (!headers.TryGetValue("X-API-Key", out var apiKeys) || 
            !apiKeys.Contains(_validApiKey))
        {
            throw new SecurityException("Invalid API Key");
        }
    }

    // Client-side: Add authorization headers
    public Dictionary<string, List<string?>> GetAuthorizationHeaders(
        CancellationToken cancellationToken = default)
    {
        return new Dictionary<string, List<string?>>
        {
            ["X-API-Key"] = new List<string?> { _validApiKey }
        };
    }

    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
        => new(GetAuthorizationHeaders(cancellationToken));
}
```

#### Using Authorization with API Gateway

```csharp
var authorizer = new ApiKeyAuthorizer("my-secret-api-key");

// Add to DI container
builder.Services.AddSingleton<ICqrsAuthorizer>(authorizer);

var app = builder.Build();

// Gateway will automatically use the authorizer from DI
app.UseCqrsApiGateway(route: "/api/cqrs");
```

The middleware will:
- Call `Authorize()` for every incoming POST request, before the message is dispatched
- Return `401 Unauthorized` if `Authorize()` or a handler throws `SecurityException`
- Return `500 Internal Server Error` for other exceptions

### 3. ASP.NET Authentication Integration

Integrate with ASP.NET Core authentication:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

public class JwtCqrsAuthorizer : ICqrsAuthorizer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtCqrsAuthorizer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Authorize(Dictionary<string, List<string?>> headers)
    {
        var context = _httpContextAccessor.HttpContext;

        // Check if user is authenticated
        if (context?.User?.Identity?.IsAuthenticated != true)
        {
            throw new SecurityException("User not authenticated");
        }

        // Check claims/roles
        if (!context.User.IsInRole("ApiUser"))
        {
            throw new SecurityException("Insufficient permissions");
        }
    }

    public Dictionary<string, List<string?>> GetAuthorizationHeaders(
        CancellationToken cancellationToken = default)
    {
        // Get JWT token from current context
        var context = _httpContextAccessor.HttpContext;
        var token = context?.Request.Headers.Authorization.ToString();

        return new Dictionary<string, List<string?>>
        {
            ["Authorization"] = new List<string?> { token }
        };
    }

    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
        => new(GetAuthorizationHeaders(cancellationToken));
}

// Configure in Startup
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT config */ });
builder.Services.AddSingleton<ICqrsAuthorizer, JwtCqrsAuthorizer>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseCqrsApiGateway(route: "/api/cqrs");
```

### 4. Logging Integration

Bridge Zerra logging with ASP.NET Core logging:

```csharp
using Zerra.Logging;
using Zerra.Web;
using Microsoft.Extensions.Logging;

// Create Zerra logger (your implementation)
Zerra.Logging.ILogger zerraLogger = new ConsoleLogger();

// Add Zerra logger to ASP.NET logging
builder.Logging.ClearProviders();
builder.Logging.AddProvider(new ZerraLoggerProvider(zerraLogger));

// Alternatively, on an existing ILoggerFactory: loggerFactory.AddZerraLogger(zerraLogger);

// Now ASP.NET components log through Zerra
var app = builder.Build();
```

This allows:
- ASP.NET middleware to log through Zerra
- Unified logging across CQRS and ASP.NET components
- Consistent log format and destination

## Configuration Options

### API Gateway Route

Customize the endpoint where the gateway listens:

```csharp
// Default route
app.UseCqrsApiGateway(); // Listens at /CQRS

// Custom route
app.UseCqrsApiGateway(route: "/api/v1/gateway");

// Handle POST requests on any path
app.UseCqrsApiGateway(route: null);
```

Every gateway resolves the same `IBus` and `ISerializer` from DI, so multiple routes expose the same bus.

### Content Type Support

The request `Content-Type` must match the `ContentType` of the `ISerializer` registered in DI; a missing or different content type gets `400 Bad Request`:

| Registered serializer | Required `Content-Type` |
|---|---|
| `ZerraJsonSerializer` | `application/json` |
| `ZerraJsonSerializer` with `Nameless = true` | `application/jsonnameless` |
| `ZerraByteSerializer` | `application/octet-stream` |

With a standard JSON serializer, clients may also send `Accept: application/jsonnameless` to receive nameless JSON responses (the front end scripts do this automatically when a model type is supplied). An `Accept` type the gateway can't produce also gets `400 Bad Request`.

### CORS Configuration

The gateway handles CORS itself, including `OPTIONS` preflight requests. By default it allows every origin:

```http
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: *
Access-Control-Allow-Headers: *
```

To restrict browser access, pass `allowOrigins`. Each value can be a full origin (`scheme://host[:port]`) or just a host name; matching is case-insensitive:

```csharp
app.UseCqrsApiGateway(route: "/api/cqrs", allowOrigins: ["https://myapp.com", "mobile.myapp.com"]);
```

With `allowOrigins` set:
- An allowed request `Origin` is echoed back in `Access-Control-Allow-Origin` (with `Vary: Origin`)
- A request whose `Origin` is missing or not allowed gets `401 Unauthorized`, and a disallowed preflight gets no `Access-Control-Allow-Origin` header
- `ApiClient` sends the gateway's host name as its `Origin` (as `HttpCqrsClient` and `KestrelCqrsClient` do for their servers), so include that host to keep .NET clients working, e.g. `allowOrigins: ["https://myapp.com", "api.myapp.com"]`
- Passing `null`, an empty array, or `"*"` allows all origins

`HttpCqrsServer` and `KestrelCqrsServerMiddleware` apply the same rules to their `allowOrigins`.

CORS only constrains browsers; any other client can send any `Origin`. Use `ICqrsAuthorizer` and authentication to control who can call the gateway. Because the gateway writes its own CORS headers, configure origins with `allowOrigins` rather than an ASP.NET CORS policy.

## Usage Examples

### IIS/Azure App Services Hosting

For Azure App Services (which use IIS):

```csharp
using Zerra.CQRS;
using Zerra.Serialization;
using Zerra.Web;

var builder = WebApplication.CreateBuilder(args);

// Configure CQRS
ISerializer serializer = new ZerraJsonSerializer();
var bus = Bus.New("MyService");
bus.AddHandler<IMyCommands>(new MyCommandHandler());
bus.AddHandler<IMyQueries>(new MyQueryHandler());

builder.Services.AddSingleton<IBus>(bus);
builder.Services.AddSingleton(serializer);

var app = builder.Build();

// Expose CQRS via HTTP
app.UseCqrsApiGateway(route: "/api/cqrs");

// IIS/Azure App Services will manage the Kestrel lifetime
await app.RunAsync();
```

**Azure App Service Configuration:**
- Set `ASPNETCORE_ENVIRONMENT` to `Production`
- Configure application settings in Azure Portal
- The app runs in IIS with Kestrel as the web server

### Front End Scripts (JavaScript/TypeScript)

Zerra provides pre-built JavaScript and TypeScript utilities in the **Front End Scripts** solution folder to simplify browser integration with the CQRS API Gateway.

#### Included Files

**JavaScript:**
- [`Bus.js`](../Front%20End%20Scripts/JavaScript/Bus.js) - CQRS client with JSON and Nameless JSON deserialization
- [`BusRoutes.js`](../Front%20End%20Scripts/JavaScript/BusRoutes.js) - Route configuration
- [`JavaScriptModels.tt`](../Front%20End%20Scripts/JavaScript/JavaScriptModels.tt) - T4 template to generate client-side models from .NET types

**TypeScript:**
- [`Bus.ts`](../Front%20End%20Scripts/TypeScript/Bus.ts) - Typed CQRS client with full IntelliSense support
- [`BusConfig.ts`](../Front%20End%20Scripts/TypeScript/BusConfig.ts) - Type-safe route configuration
- [`TypeScriptModels.tt`](../Front%20End%20Scripts/TypeScript/TypeScriptModels.tt) - T4 template to generate TypeScript interfaces

**Binaries (Required for T4):**
- [`Zerra.T4.dll`](../Front%20End%20Scripts/Binaries/Zerra.T4.dll) - Pre-built T4 code generation library
- [`Zerra.T4.pdb`](../Front%20End%20Scripts/Binaries/Zerra.T4.pdb) - Debug symbols (optional)
- [`Zerra.T4.xml`](../Front%20End%20Scripts/Binaries/Zerra.T4.xml) - XML documentation (optional)

> **Note**: The `Zerra.T4.dll` is automatically built from the `Zerra.T4` project and copied to this folder. These binaries are always up-to-date with the latest framework build. Copy the entire `Binaries` folder to your solution to use the T4 templates.

#### Auto-Generate Client Models with T4

The T4 templates automatically generate JavaScript/TypeScript models from your .NET CQRS types.

**Step 1: Copy Files to Your Solution**

Copy the Front End Scripts files to your solution:
```
YourSolution/
├── Scripts/
│   ├── Binaries/
│   │   ├── Zerra.T4.dll
│   │   ├── Zerra.T4.pdb
│   │   └── Zerra.T4.xml
│   └── TypeScriptModels.tt (or JavaScriptModels.tt)
└── YourWebProject/
    └── src/services/ (output location)
```

**Step 2: Update T4 Template Assembly Path**

Edit the `.tt` file to reference the correct path to `Zerra.T4.dll`:

```csharp
<#@ assembly name="Scripts\Binaries\Zerra.T4.dll" #>
```

Or use an absolute path if needed:
```csharp
<#@ assembly name="C:\MyProject\Scripts\Binaries\Zerra.T4.dll" #>
```

**Shipped template reference (expects `Zerra.T4.dll` to be resolvable next to the template):**
```csharp
<#@ assembly name="Zerra.T4.dll" #>
```

**Step 3: Configure Project Build**

Add to your web project `.csproj` (see [`Help-ProjectBuildT4.txt`](../Front%20End%20Scripts/JavaScript/Help-ProjectBuildT4.txt)):

```xml
<!--Section to Build T4 into the UI project-->
<Import Project="$(MSBuildExtensionsPath)\Microsoft\VisualStudio\v17.0\TextTemplating\Microsoft.TextTemplating.targets" />
<PropertyGroup>
    <TransformOnBuild>true</TransformOnBuild>
    <OverwriteReadOnlyOutputFiles>true</OverwriteReadOnlyOutputFiles>
    <TransformOutOfDateOnly>false</TransformOutOfDateOnly>
</PropertyGroup>
<ItemGroup>
    <None Include="..\Scripts\TypeScriptModels.tt">
        <Generator>TextTemplatingFileGenerator</Generator>
        <OutputFilePath>..\MyWebApp\src\services</OutputFilePath>
        <LastGenOutput>TypeScriptModels.ts</LastGenOutput>
    </None>
</ItemGroup>
```

**Step 4: Build Your Solution**

The T4 template will automatically run during build and generate the TypeScript/JavaScript models based on your CQRS types (queries, commands, events, and DTOs).

#### Using the JavaScript Bus

```html
<!-- Include jQuery (required dependency) -->
<script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>

<!-- Include generated models and Bus -->
<script src="JavaScriptModels.js"></script>
<script src="Bus.js"></script>
<script src="BusRoutes.js"></script>

<script>
// Configure routes
BusRoutes["Gateway"] = "https://myapp.azurewebsites.net/api/cqrs";

// Set custom headers (e.g., API key)
Bus.setHeader("X-API-Key", "my-secret-key");

// Example 1: Simple query call
IUserQueries.GetUser("12345", function(user) {
    console.log("User:", user);
    document.getElementById("userName").innerText = user.Name;
}, function(errorText) {
    console.error("Error:", errorText);
});

// Example 2: Query returning a list
IUserQueries.GetAllUsers(function(users) {
    console.log("Found " + users.length + " users");
    users.forEach(function(user) {
        console.log(user.Name + " - " + user.Email);
    });
}, function(errorText) {
    console.error("Error loading users:", errorText);
});

// Example 3: Dispatch a command and wait for its result (DispatchAwait)
const createCommand = new CreateUserCommand({
    Email: "user@example.com",
    Name: "John Doe"
});

Bus.DispatchAwait(createCommand, function(result) {
    console.log("User created with ID:", result.UserId);
}, function(errorText) {
    console.error("Failed to create user:", errorText);
});

// Example 4: Dispatch a command without waiting for it to complete (fire and forget)
const updateCommand = new UpdateUserCommand({
    UserId: "12345",
    Name: "Jane Doe"
});

Bus.Dispatch(updateCommand);

// Example 5: Using Bus.Call directly for more control
Bus.Call(
    "IUserQueries",
    "SearchUsers",
    ["john", 10, 0],  // searchTerm, pageSize, offset
    UserModelType,
    true,  // hasMany = true for arrays
    function(users) {
        console.log("Search results:", users);
    },
    function(errorText) {
        console.error("Search failed:", errorText);
    }
);

</script>
```

For a global error handler, edit the `BusFail` function declared in `BusRoutes.js` (it is a `const`, so it cannot be reassigned from page script):

```javascript
// BusRoutes.js
const BusFail = function (message, url) {
    console.error("CQRS Error at " + url + ": " + message);
    alert("An error occurred. Please try again.");
};
```

The `onFail` callback on each call receives a single error message string.

#### Using the TypeScript Bus

`Bus.ts` exposes `Bus.Call(provider, method, args, modelType, hasMany)`, `Bus.DispatchAsync(command)` (fire and forget), `Bus.DispatchAwaitAsync(command)` (wait, and return the result for commands with results), and `Bus.SetHeader(header, value)`. All return promises. `TypeScriptModels.tt` generates a typed static class per query interface and a class per command, so you rarely call `Bus.Call` directly.

```typescript
import { Bus } from "./Bus";
import { SetBusRoute, SetBusFailCallback } from "./BusConfig";
import { IUserQueries, CreateUserCommand, UpdateUserSettingsCommand } from "./TypeScriptModels";

// Configure routes
SetBusRoute("Gateway", "https://myapp.azurewebsites.net/api/cqrs");

// Set custom headers (e.g., API key)
Bus.SetHeader("X-API-Key", "my-secret-key");

// Set global error handler
SetBusFailCallback((message: string) => {
    console.error("Bus error:", message);
    alert(`An error occurred: ${message}`);
});

// Example 1: Simple query with parameters (generated, typed proxy)
try {
    const user = await IUserQueries.GetUser("12345");
    console.log(`User: ${user.Name} (${user.Email})`); // Full IntelliSense support
} catch (error) {
    console.error("Failed to load user:", error);
}

// Example 2: Query returning array
try {
    const users = await IUserQueries.GetAllUsers();
    console.log(`Loaded ${users.length} users`);
    users.forEach(u => console.log(`${u.Name} - ${u.Email}`));
} catch (error) {
    console.error("Failed to load users:", error);
}

// Example 3: Dispatch command and wait for its result
const createCommand = new CreateUserCommand({
    Email: "user@example.com",
    Name: "John Doe"
});

try {
    const result = await Bus.DispatchAwaitAsync(createCommand);
    console.log(`User created with ID: ${result.UserId}`);
} catch (error) {
    console.error("Failed to create user:", error);
}

// Example 4: Fire-and-forget command with nested objects
const updateCommand = new UpdateUserSettingsCommand({
    UserId: "12345",
    Settings: {
        FirstName: "Jane",
        LastName: "Doe",
        TimeZone: "America/New_York",
        EmailNotifications: true,
        Theme: "dark"
    }
});

try {
    await Bus.DispatchAsync(updateCommand);
    console.log("Settings update sent");
} catch (error) {
    console.error("Failed to send settings update:", error);
}
```

**TypeScript Settings Page Example:**

```typescript
// settings.ts - Type-safe settings page

import { Bus } from "./Bus";
import { 
    UserSettings,
    IUserQueries,
    UpdateUserSettingsCommand
} from "./TypeScriptModels";

class SettingsPage {
    private currentUserId: string;
    private originalSettings: UserSettings | null = null;

    constructor(userId: string) {
        this.currentUserId = userId;
    }

    async loadSettings(): Promise<void> {
        try {
            this.showLoading(true);

            this.originalSettings = await IUserQueries.GetSettings(this.currentUserId);

            this.populateForm(this.originalSettings);
            this.showForm(true);
        } catch (error) {
            console.error("Failed to load settings:", error);
            this.showError("Failed to load settings. Please refresh the page.");
        } finally {
            this.showLoading(false);
        }
    }

    async saveSettings(): Promise<void> {
        const settings = this.getFormData();

        if (!this.validateForm(settings)) {
            return;
        }

        try {
            this.setSaveButtonState(true, "Saving...");

            const command = new UpdateUserSettingsCommand({
                UserId: this.currentUserId,
                FirstName: settings.FirstName,
                LastName: settings.LastName,
                Email: settings.Email,
                TimeZone: settings.TimeZone,
                EmailNotifications: settings.EmailNotifications,
                Theme: settings.Theme
            });

            const result = await Bus.DispatchAwaitAsync(command);

            if (result.Success) {
                this.showSuccess("Settings saved successfully!");
                await this.loadSettings(); // Reload fresh data
            } else {
                alert(`Error: ${result.ErrorMessage}`);
            }
        } catch (error) {
            console.error("Failed to save settings:", error);
            alert("Failed to save settings. Please try again.");
        } finally {
            this.setSaveButtonState(false, "Save Changes");
        }
    }

    resetForm(): void {
        if (this.originalSettings && confirm("Discard all changes?")) {
            this.populateForm(this.originalSettings);
        }
    }

    private getFormData(): UserSettings {
        return {
            UserId: this.currentUserId,
            FirstName: (document.getElementById("txtFirstName") as HTMLInputElement).value.trim(),
            LastName: (document.getElementById("txtLastName") as HTMLInputElement).value.trim(),
            Email: (document.getElementById("txtEmail") as HTMLInputElement).value.trim(),
            TimeZone: (document.getElementById("ddlTimeZone") as HTMLSelectElement).value,
            EmailNotifications: (document.getElementById("chkEmailNotifications") as HTMLInputElement).checked,
            Theme: (document.getElementById("ddlTheme") as HTMLSelectElement).value
        };
    }

    private validateForm(settings: UserSettings): boolean {
        if (!settings.FirstName || !settings.LastName) {
            alert("First name and last name are required.");
            return false;
        }

        if (!settings.Email || !settings.Email.includes("@")) {
            alert("Please enter a valid email address.");
            return false;
        }

        return true;
    }

    private populateForm(settings: UserSettings): void {
        (document.getElementById("txtFirstName") as HTMLInputElement).value = settings.FirstName;
        (document.getElementById("txtLastName") as HTMLInputElement).value = settings.LastName;
        (document.getElementById("txtEmail") as HTMLInputElement).value = settings.Email;
        (document.getElementById("ddlTimeZone") as HTMLSelectElement).value = settings.TimeZone;
        (document.getElementById("chkEmailNotifications") as HTMLInputElement).checked = settings.EmailNotifications;
        (document.getElementById("ddlTheme") as HTMLSelectElement).value = settings.Theme;
    }

    private showLoading(show: boolean): void {
        document.getElementById("loading")!.style.display = show ? "block" : "none";
    }

    private showForm(show: boolean): void {
        document.getElementById("settingsForm")!.style.display = show ? "block" : "none";
    }

    private showError(message: string): void {
        const errorDiv = document.getElementById("error")!;
        errorDiv.textContent = message;
        errorDiv.style.display = "block";
    }

    private showSuccess(message: string): void {
        const successDiv = document.getElementById("successMessage")!;
        successDiv.textContent = message;
        successDiv.style.display = "block";

        setTimeout(() => {
            successDiv.style.display = "none";
        }, 3000);
    }

    private setSaveButtonState(disabled: boolean, text: string): void {
        const btn = document.getElementById("btnSave") as HTMLButtonElement;
        btn.disabled = disabled;
        btn.textContent = text;
    }
}

// Initialize page
const settingsPage = new SettingsPage("12345"); // From auth/session
document.addEventListener("DOMContentLoaded", () => {
    settingsPage.loadSettings();

    document.getElementById("btnSave")!.addEventListener("click", () => settingsPage.saveSettings());
    document.getElementById("btnReset")!.addEventListener("click", () => settingsPage.resetForm());
});
```

#### Nameless JSON Support

The Bus utilities request Nameless JSON with an `Accept: application/jsonnameless` header whenever a model type is known, and decode the response based on its `Content-Type`. They handle either response format.

**Server Configuration:**

Keep the standard JSON serializer. The front end scripts always send `Content-Type: application/json`, and the gateway rejects requests whose content type does not match the registered serializer, so do not register a `Nameless = true` serializer for browser clients.

```csharp
builder.Services.AddSingleton<ISerializer>(new ZerraJsonSerializer());
app.UseCqrsApiGateway();
```

When the request's `Accept` header is `application/jsonnameless`, the gateway deserializes the request with the registered JSON serializer and writes the response as nameless JSON (`Content-Type: application/jsonnameless`). Error responses are always standard JSON so browser code can read the exception.

**Client Code:**
```javascript
// The generated JavaScriptModels.js includes type definitions like:
const UserModelType = {
    UserId: "string",
    Name: "string",
    Email: "string",
    CreatedDate: "Date"
};

// Bus automatically detects "application/jsonnameless" content type
// and deserializes compact arrays back to objects using the model type
IUserQueries.GetAllUsers(function(users) {
    // Server sends compact JSON: [["123","John","john@example.com","2024-01-15"],["456","Jane","jane@example.com","2024-01-16"]]
    // Bus deserializes to: [{ UserId: "123", Name: "John", Email: "john@example.com", CreatedDate: Date }, ...]

    users.forEach(function(user) {
        console.log(user.Name + " (" + user.Email + ")");
        console.log("Created:", user.CreatedDate.toLocaleDateString());
    });
}, function(errorText) {
    console.error("Error:", errorText);
});

// For custom queries with Bus.Call, specify the model type:
Bus.Call(
    "MyApp.Queries.IUserQueries",
    "SearchUsers",
    ["john"],
    UserModelType,  // Required for nameless deserialization
    true,           // hasMany = true for arrays
    function(users) {
        console.log("Found users:", users);
    },
    function(errorText) {
        console.error("Search failed:", errorText);
    }
);
```

#### Real-World Example: User Settings Page

Here's a complete example showing how to build a settings page with data loading, validation, and command dispatching:

```javascript
// settings.js - Complete user settings page example

// Model generated by JavaScriptModels.tt
const UserSettingsModelType = {
    UserId: "string",
    Email: "string",
    FirstName: "string",
    LastName: "string",
    TimeZone: "string",
    EmailNotifications: "boolean",
    Theme: "string"
};

// Initialize page
var currentUserId = "12345"; // From session or auth
var originalSettings = null;

function loadSettings() {
    // Show loading indicator
    $("#loading").show();
    $("#settingsForm").hide();

    // Load user settings from query
    IUserQueries.GetSettings(currentUserId, function(settings) {
        originalSettings = settings;

        // Populate form fields
        $("#txtFirstName").val(settings.FirstName);
        $("#txtLastName").val(settings.LastName);
        $("#txtEmail").val(settings.Email);
        $("#ddlTimeZone").val(settings.TimeZone);
        $("#chkEmailNotifications").prop("checked", settings.EmailNotifications);
        $("#ddlTheme").val(settings.Theme);

        // Hide loading, show form
        $("#loading").hide();
        $("#settingsForm").show();
    }, function(errorText) {
        console.error("Failed to load settings:", errorText);
        $("#loading").hide();
        $("#error").text("Failed to load settings. Please refresh the page.").show();
    });
}

function saveSettings() {
    // Validate form
    var firstName = $("#txtFirstName").val().trim();
    var lastName = $("#txtLastName").val().trim();
    var email = $("#txtEmail").val().trim();

    if (!firstName || !lastName) {
        alert("First name and last name are required.");
        return;
    }

    if (!email || !email.includes("@")) {
        alert("Please enter a valid email address.");
        return;
    }

    // Build command
    var updateCommand = new UpdateUserSettingsCommand({
        UserId: currentUserId,
        FirstName: firstName,
        LastName: lastName,
        Email: email,
        TimeZone: $("#ddlTimeZone").val(),
        EmailNotifications: $("#chkEmailNotifications").is(":checked"),
        Theme: $("#ddlTheme").val()
    });

    // Show saving indicator
    $("#btnSave").prop("disabled", true).text("Saving...");

    // Dispatch command
    Bus.Dispatch(updateCommand, function(result) {
        $("#btnSave").prop("disabled", false).text("Save Changes");

        if (result.Success) {
            $("#successMessage").text("Settings saved successfully!").show();
            setTimeout(function() {
                $("#successMessage").fadeOut();
            }, 3000);

            // Reload to get fresh data
            loadSettings();
        } else {
            alert("Error: " + result.ErrorMessage);
        }
    }, function(errorText) {
        $("#btnSave").prop("disabled", false).text("Save Changes");
        console.error("Failed to save settings:", errorText);
        alert("Failed to save settings. Please try again.");
    });
}

function resetForm() {
    if (originalSettings && confirm("Discard all changes?")) {
        $("#txtFirstName").val(originalSettings.FirstName);
        $("#txtLastName").val(originalSettings.LastName);
        $("#txtEmail").val(originalSettings.Email);
        $("#ddlTimeZone").val(originalSettings.TimeZone);
        $("#chkEmailNotifications").prop("checked", originalSettings.EmailNotifications);
        $("#ddlTheme").val(originalSettings.Theme);
    }
}

// Load data when page loads
$(document).ready(function() {
    loadSettings();

    // Wire up button handlers
    $("#btnSave").click(saveSettings);
    $("#btnReset").click(resetForm);
});
```

**Corresponding HTML:**
```html
<div id="loading" style="display:none;">
    <p>Loading settings...</p>
</div>

<div id="error" style="display:none; color:red;"></div>

<form id="settingsForm" style="display:none;">
    <div class="form-group">
        <label for="txtFirstName">First Name:</label>
        <input type="text" id="txtFirstName" class="form-control" />
    </div>

    <div class="form-group">
        <label for="txtLastName">Last Name:</label>
        <input type="text" id="txtLastName" class="form-control" />
    </div>

    <div class="form-group">
        <label for="txtEmail">Email:</label>
        <input type="email" id="txtEmail" class="form-control" />
    </div>

    <div class="form-group">
        <label for="ddlTimeZone">Time Zone:</label>
        <select id="ddlTimeZone" class="form-control">
            <option value="UTC">UTC</option>
            <option value="America/New_York">Eastern Time</option>
            <option value="America/Chicago">Central Time</option>
            <option value="America/Denver">Mountain Time</option>
            <option value="America/Los_Angeles">Pacific Time</option>
        </select>
    </div>

    <div class="form-group">
        <label>
            <input type="checkbox" id="chkEmailNotifications" />
            Enable email notifications
        </label>
    </div>

    <div class="form-group">
        <label for="ddlTheme">Theme:</label>
        <select id="ddlTheme" class="form-control">
            <option value="light">Light</option>
            <option value="dark">Dark</option>
            <option value="auto">Auto</option>
        </select>
    </div>

    <div id="successMessage" style="display:none; color:green;"></div>

    <button type="button" id="btnSave" class="btn btn-primary">Save Changes</button>
    <button type="button" id="btnReset" class="btn btn-secondary">Reset</button>
</form>

<script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
<script src="JavaScriptModels.js"></script>
<script src="Bus.js"></script>
<script src="BusRoutes.js"></script>
<script src="settings.js"></script>
```

The Bus utilities handle:
- ✅ Date serialization/deserialization (ISO 8601 with timezone)
- ✅ Nested object deserialization
- ✅ Array property deserialization
- ✅ Automatic Nameless JSON detection and decoding
- ✅ Custom header support (authentication, API keys)
- ✅ Global error handling

### C# Client Applications (.NET, Xamarin, MAUI)

For .NET applications (console, desktop, mobile, or server-to-server), use the `ApiClient` class to connect to the CQRS API Gateway.

#### Basic ApiClient Setup

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
bus.AddQueryClient<IUserQueries>(apiClient);

// Use the bus to call remote services
try
{
    // Call a query
    var user = await bus.Call<IUserQueries>().GetUser("12345", CancellationToken.None);
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

#### ApiClient with Authorization

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

#### Console Application Example

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
        bus.AddQueryClient<IUserQueries>(apiClient);

        try
        {
            Console.WriteLine("Fetching users...");
            var users = await bus.Call<IUserQueries>().GetAllUsers(CancellationToken.None);

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

#### Xamarin/MAUI Mobile App Example

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
        _bus.AddQueryClient<IUserQueries>(_apiClient);
    }

    public async Task<UserModel> GetUserAsync(string userId)
    {
        try
        {
            return await _bus.Call<IUserQueries>().GetUser(userId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Handle error (logging, user notification, etc.)
            throw new ApplicationException("Failed to load user", ex);
        }
    }

    public async Task<List<UserModel>> GetAllUsersAsync()
    {
        return await _bus.Call<IUserQueries>().GetAllUsers(CancellationToken.None);
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

#### ASP.NET Core Service-to-Service Communication

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
    bus.AddQueryClient<IUserQueries>(apiClient);

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
        var users = await _bus.Call<IUserQueries>().GetAllUsers(cancellationToken);
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

**Key Points:**
- `ApiClient` connects to HTTP/HTTPS API Gateway endpoints
- Uses JSON serialization for cross-platform compatibility
- Supports custom authorization via `ICqrsAuthorizer`
- Works with any .NET application: Console, WPF, Xamarin, MAUI, ASP.NET
- Integrates seamlessly with the CQRS bus pattern
- Dispose the ApiClient when done to release resources

### Microservices Gateway

Use as a gateway for microservices communication:

```csharp
// Gateway Service (ASP.NET)
var builder = WebApplication.CreateBuilder(args);

ISerializer serializer = new ZerraJsonSerializer();
var bus = Bus.New("GatewayService");

// Connect to backend services
var userServiceClient = new TcpCqrsClient("user-service:9001", serializer, null, null);
bus.AddCommandProducer<IUserCommands>(userServiceClient);
bus.AddQueryClient<IUserQueries>(userServiceClient);

var orderServiceClient = new TcpCqrsClient("order-service:9002", serializer, null, null);
bus.AddCommandProducer<IOrderCommands>(orderServiceClient);
bus.AddQueryClient<IOrderQueries>(orderServiceClient);

builder.Services.AddSingleton<IBus>(bus);
builder.Services.AddSingleton(serializer);

var app = builder.Build();

// Expose unified API to external clients
app.UseCqrsApiGateway(route: "/api");

await app.RunAsync();
```

Clients call the gateway, which routes to appropriate backend services.

## Best Practices

### 1. Use JSON for External Clients

```csharp
// ✅ Good - JSON is interoperable with browsers/mobile
ISerializer serializer = new ZerraJsonSerializer();
app.UseCqrsApiGateway();
```

### 2. Always Use Authorization for Public APIs

```csharp
// ✅ Good - protect your API
builder.Services.AddSingleton<ICqrsAuthorizer>(new ApiKeyAuthorizer("secret"));
app.UseCqrsApiGateway();

// ❌ Bad - no authorization
app.UseCqrsApiGateway(); // Anyone can call any command!
```

### 3. Use HTTPS in Production

```csharp
// appsettings.Production.json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443"
      }
    }
  }
}
```

### 4. Restrict Browser Origins in Production

```csharp
// ✅ Good - only your front ends can call the gateway from a browser, plus ApiClient callers (the gateway's host)
app.UseCqrsApiGateway(route: "/api/cqrs", allowOrigins: ["https://myapp.com", "https://mobile.myapp.com", "api.myapp.com"]);
```

CORS restricts browsers only (see [CORS Configuration](#cors-configuration)), so still use `ICqrsAuthorizer` and authentication to control access.

### 5. Use Rate Limiting

```csharp
using System.Threading.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    // The gateway is middleware rather than an endpoint, so use a global limiter instead of a named policy
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 100 }));
});

var app = builder.Build();
app.UseRateLimiter();
app.UseCqrsApiGateway();
```

### 6. Log Gateway Activity

```csharp
Zerra.Logging.ILogger log = new ConsoleLogger();
IBusLogger busLog = new ConsoleBusLogger();

var bus = Bus.New("MyService", log: log, busLog: busLog);

// The bus logger tracks the commands and queries received through the gateway
```

### 7. Use Front End Scripts for Browser Clients

```csharp
// ✅ Good - use provided Bus.js/Bus.ts utilities
// Located in solution folder: "Front End Scripts"
// - Handles date serialization
// - Supports Nameless JSON
// - T4 templates generate type-safe models
// - Simplifies error handling

// Copy Bus.js/Bus.ts and generated models to your web project
// See examples in "Front End Scripts (JavaScript/TypeScript)" section
```

## When to Use Zerra.Web

Choose `Zerra.Web` when:

- ✅ **IIS Hosting** - Deploying to Azure App Services or IIS
- ✅ **External Clients** - Browsers, mobile apps, or third-party services need access
- ✅ **API Gateway Pattern** - Single entry point for multiple backend services
- ✅ **ASP.NET Integration** - Using ASP.NET authentication/authorization
- ✅ **Existing ASP.NET App** - Adding CQRS to an existing web application
- ✅ **HTTP/HTTPS Required** - Standard web protocols for compatibility

Don't use when:

- ❌ Internal services only - Use `TcpCqrsServer` or message brokers instead
- ❌ High-performance internal communication - Binary transports are faster
- ❌ Message broker patterns - Use Kafka, RabbitMQ, or Azure Service Bus

## Troubleshooting

### Gateway Returns 400 or 500

**Problem**: API Gateway rejects requests

**Solutions**:
- A `400` means the `Content-Type` is missing or doesn't match the registered serializer, the `Accept` type can't be produced (see [Content Type Support](#content-type-support)), or the body had neither `ProviderType` (query) nor `MessageType` (command)
- A `401` without an authorizer failure means the request's `Origin` is missing or isn't in `allowOrigins` (for `ApiClient`, add the gateway's host name)
- Verify `MessageType` names a command type the gateway can resolve (events are rejected) and that the bus has a handler or producer for it
- Verify `ProviderType` names a query interface registered with the bus

### Authorization Fails

**Problem**: Requests fail authorization

**Solutions**:
- Verify `ICqrsAuthorizer` is registered in DI container
- Check authorization headers are included in client requests
- Ensure `Authorize()` method doesn't throw exceptions for valid requests
- Throw `SecurityException` (not other exception types) from `Authorize()` so the gateway responds with `401` rather than `500`
- Use a debugger to inspect the headers received

### CORS Errors in Browser

**Problem**: Browser shows CORS policy errors

**Solutions**:
- If you set `allowOrigins`, verify the page's origin (or its host name) is in the list
- Verify the request path matches the gateway `route`; requests to other paths are passed on and don't get the gateway's CORS headers
- Check that another CORS middleware isn't adding a conflicting `Access-Control-Allow-Origin` header
- Check that preflight OPTIONS requests reach the gateway (it answers them automatically)

### Performance Issues

**Problem**: Gateway is slow under load

**Solutions**:
- Use `ZerraByteSerializer` if clients support binary (faster than JSON)
- Enable response compression in ASP.NET
- Configure Kestrel limits appropriately
- Consider using message brokers for high-volume scenarios
- Add rate limiting to prevent abuse

## See Also

- [Server Setup](ServerSetup.md) - Configure CQRS servers
- [Client Setup](ClientSetup.md) - Configure CQRS clients
- [JsonSerializer](JsonSerializer.md) - JSON serialization for external clients
- [Encryptors](Encryptors.md) - Secure message encryption
- [Logging](Logging.md) - Implement logging for gateway activity
