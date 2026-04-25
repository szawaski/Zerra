[← Back to Documentation](Index.md)

# Zerra.Web - ASP.NET Integration

`Zerra.Web` provides ASP.NET Core integration for Zerra CQRS, enabling your CQRS bus to be hosted within ASP.NET applications. This is essential for IIS-hosted environments (Azure App Services) and for exposing CQRS functionality as an HTTP API Gateway.

## Overview

`Zerra.Web` provides:
- **IIS/Kestrel Hosting** - Run CQRS bus within ASP.NET Core applications
- **API Gateway** - Expose CQRS commands, queries, and events as HTTP endpoints
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

The API Gateway exposes your CQRS bus to external HTTP clients, allowing browsers, mobile apps, and other services to invoke commands, queries, and events without knowledge of your internal architecture.

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
IEncryptor encryptor = new ZerraEncryptor("mySecurePassword", SymmetricAlgorithmType.AESwithPrefix);
ILogger log = new Logger();
IBusLogger busLog = new BusLogger();

// Create the CQRS bus
var bus = Bus.New(
    service: "MyService",
    log: log,
    busLog: busLog
);

// Register handlers
bus.AddHandler<IUserCommandHandler>(new UserCommandHandler());
bus.AddHandler<IUserQueries>(new UserQueryHandler());

// Add Bus and components to DI container
builder.Services.AddSingleton(bus);
builder.Services.AddSingleton(serializer);
builder.Services.AddSingleton(log);

var app = builder.Build();

// Enable CQRS API Gateway
app.UseCqrsApiGateway(route: "/api/cqrs");

await app.RunAsync();
```

#### How It Works

The API Gateway:
1. Listens for HTTP POST requests at the specified route (default: `/CQRS`)
2. Deserializes the request body to determine which command/query/event to invoke
3. Dispatches the message to the CQRS bus
4. Serializes the response and returns it to the client

**Request Format:**
```json
POST /api/cqrs
Content-Type: application/json

{
  "MessageType": "MyApp.Commands.CreateUserCommand",
  "Message": {
    "Email": "user@example.com",
    "Name": "John Doe"
  }
}
```

**Response Format:**
```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "UserId": "12345"
}
```

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
    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
    {
        var headers = new Dictionary<string, List<string?>>
        {
            ["X-API-Key"] = new List<string?> { _validApiKey }
        };
        return new ValueTask<Dictionary<string, List<string?>>>(headers);
    }
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
- Call `Authorize()` for every incoming request
- Return `401 Unauthorized` if `SecurityException` is thrown
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

    public ValueTask<Dictionary<string, List<string?>>> GetAuthorizationHeadersAsync(
        CancellationToken cancellationToken = default)
    {
        // Get JWT token from current context
        var context = _httpContextAccessor.HttpContext;
        var token = context?.Request.Headers.Authorization.ToString();

        var headers = new Dictionary<string, List<string?>>
        {
            ["Authorization"] = new List<string?> { token }
        };
        return new ValueTask<Dictionary<string, List<string?>>>(headers);
    }
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

// Create Zerra logger
ILogger zerraLogger = new Logger();

// Add Zerra logger to ASP.NET logging
builder.Logging.ClearProviders();
builder.Logging.AddZerraLogger(zerraLogger);

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

// Multiple gateways (different buses)
app.UseCqrsApiGateway(route: "/api/users");
app.UseCqrsApiGateway(route: "/api/orders");
```

### Content Type Support

The API Gateway automatically supports multiple content types:

```csharp
// Client specifies content type
Content-Type: application/json              // Standard JSON
Content-Type: application/jsonnameless     // Compact nameless JSON
Content-Type: application/octet-stream     // Binary (ZerraByteSerializer)

// Gateway uses the same serializer for request and response
```

### CORS Configuration

CORS is automatically enabled with permissive defaults:

```http
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: *
Access-Control-Allow-Headers: *
```

For production, configure ASP.NET CORS middleware before the gateway:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy", policy =>
    {
        policy.WithOrigins("https://myapp.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("MyPolicy");
app.UseCqrsApiGateway(route: "/api/cqrs");
```

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

builder.Services.AddSingleton(bus);
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

**Original template reference (example):**
```csharp
<#@ assembly name="Framework\Zerra.T4.TestDev\bin\Release\net48\Zerra.T4.dll" #>
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
}, function(jqXHR, textStatus, errorThrown) {
    console.error("Error:", textStatus, errorThrown);
});

// Example 2: Query returning a list
IUserQueries.GetAllUsers(function(users) {
    console.log("Found " + users.length + " users");
    users.forEach(function(user) {
        console.log(user.Name + " - " + user.Email);
    });
}, function(jqXHR, textStatus, errorThrown) {
    console.error("Error loading users:", textStatus);
});

// Example 3: Dispatch a command with result
const createCommand = new CreateUserCommand({
    Email: "user@example.com",
    Name: "John Doe"
});

Bus.Dispatch(createCommand, function(result) {
    console.log("User created with ID:", result.UserId);
}, function(jqXHR, textStatus, errorThrown) {
    console.error("Failed to create user:", textStatus);
});

// Example 4: Dispatch command without awaiting result (fire and forget)
const updateCommand = new UpdateUserCommand({
    UserId: "12345",
    Name: "Jane Doe"
});

Bus.DispatchAwait(updateCommand);

// Example 5: Using Bus.Call directly for more control
Bus.Call(
    "MyApp.Queries.IUserQueries",
    "SearchUsers",
    ["john", 10, 0],  // searchTerm, pageSize, offset
    UserModelType,
    true,  // hasMany = true for arrays
    function(users) {
        console.log("Search results:", users);
    },
    function(jqXHR, textStatus, errorThrown) {
        console.error("Search failed:", textStatus);
    }
);

// Global error handler
BusFail = function(message, url) {
    console.error("CQRS Error at " + url + ": " + message);
    alert("An error occurred. Please try again.");
};
</script>
```

#### Using the TypeScript Bus

```typescript
import { Bus } from "./Bus";
import { SetBusRoute, SetBusFailCallback } from "./BusConfig";
import { User, UserSettings, GetUserQuery, GetAllUsersQuery, CreateUserCommand, UpdateUserSettingsCommand } from "./TypeScriptModels";

// Configure routes
SetBusRoute("Gateway", "https://myapp.azurewebsites.net/api/cqrs");

// Set global error handler
SetBusFailCallback((message: string) => {
    console.error("Bus error:", message);
    alert(`An error occurred: ${message}`);
});

// Example 1: Simple query with parameters
const query = new GetUserQuery();
query.UserId = "12345";

try {
    const user = await Bus.Call<User>(
        "MyApp.Queries.IUserQueries",
        "GetUser",
        [query],
        User,    // Type for IntelliSense and deserialization
        false    // hasMany = false for single result
    );

    console.log(`User: ${user.Name} (${user.Email})`); // Full IntelliSense support
} catch (error) {
    console.error("Failed to load user:", error);
}

// Example 2: Query returning array
try {
    const users = await Bus.Call<User[]>(
        "MyApp.Queries.IUserQueries",
        "GetAllUsers",
        [],
        User,
        true     // hasMany = true for arrays
    );

    console.log(`Loaded ${users.length} users`);
    users.forEach(u => console.log(`${u.Name} - ${u.Email}`));
} catch (error) {
    console.error("Failed to load users:", error);
}

// Example 3: Dispatch command with result
const createCommand = new CreateUserCommand();
createCommand.Email = "user@example.com";
createCommand.Name = "John Doe";

try {
    const result = await Bus.Dispatch(createCommand);
    console.log(`User created with ID: ${result.UserId}`);
} catch (error) {
    console.error("Failed to create user:", error);
}

// Example 4: Complex command with nested objects
const updateCommand = new UpdateUserSettingsCommand();
updateCommand.UserId = "12345";
updateCommand.Settings = {
    FirstName: "Jane",
    LastName: "Doe",
    TimeZone: "America/New_York",
    EmailNotifications: true,
    Theme: "dark"
};

try {
    await Bus.Dispatch(updateCommand);
    console.log("Settings updated successfully");
} catch (error) {
    console.error("Failed to update settings:", error);
}
```

**TypeScript Settings Page Example:**

```typescript
// settings.ts - Type-safe settings page

import { Bus } from "./Bus";
import { 
    UserSettings, 
    GetSettingsQuery, 
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

            const query = new GetSettingsQuery();
            query.UserId = this.currentUserId;

            this.originalSettings = await Bus.Call<UserSettings>(
                "MyApp.Queries.IUserQueries",
                "GetSettings",
                [query],
                UserSettings,
                false
            );

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

            const command = new UpdateUserSettingsCommand();
            command.UserId = this.currentUserId;
            command.FirstName = settings.FirstName;
            command.LastName = settings.LastName;
            command.Email = settings.Email;
            command.TimeZone = settings.TimeZone;
            command.EmailNotifications = settings.EmailNotifications;
            command.Theme = settings.Theme;

            const result = await Bus.Dispatch(command);

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
```

#### Nameless JSON Support

The Bus utilities automatically detect and deserialize Nameless JSON responses:

**Server Configuration:**
```csharp
var options = new JsonSerializerOptions { Nameless = true };
var serializer = new ZerraJsonSerializer(options);
app.UseCqrsApiGateway();
```

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
}, function(jqXHR, textStatus, errorThrown) {
    console.error("Error:", textStatus);
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
    function(jqXHR, textStatus, errorThrown) {
        console.error("Search failed:", textStatus);
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
    }, function(jqXHR, textStatus, errorThrown) {
        console.error("Failed to load settings:", textStatus, errorThrown);
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
    }, function(jqXHR, textStatus, errorThrown) {
        $("#btnSave").prop("disabled", false).text("Save Changes");
        console.error("Failed to save settings:", textStatus, errorThrown);
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

builder.Services.AddSingleton(bus);
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

### 4. Configure CORS Properly

```csharp
// ✅ Good - specific origins in production
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://myapp.com", "https://mobile.myapp.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### 5. Use Rate Limiting

```csharp
using Microsoft.AspNetCore.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });
});

var app = builder.Build();
app.UseRateLimiter();
app.UseCqrsApiGateway();
```

### 6. Log Gateway Activity

```csharp
ILogger log = new Logger();
IBusLogger busLog = new BusLogger();

var bus = Bus.New("MyService", log: log, busLog: busLog);

// BusLogger will track all commands/queries/events through the gateway
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

### Gateway Returns 400 Bad Request

**Problem**: API Gateway returns 400 for valid requests

**Solutions**:
- Verify `MessageType` is the fully-qualified type name (e.g., `MyApp.Commands.CreateUserCommand`)
- Check that the message type is registered with the bus
- Ensure request body matches the command/query/event structure
- Verify `Content-Type` header matches the serializer

### Authorization Fails

**Problem**: API Gateway returns 401 Unauthorized

**Solutions**:
- Verify `ICqrsAuthorizer` is registered in DI container
- Check authorization headers are included in client requests
- Ensure `Authorize()` method doesn't throw exceptions for valid requests
- Use a debugger to inspect the headers received

### CORS Errors in Browser

**Problem**: Browser shows CORS policy errors

**Solutions**:
- Configure ASP.NET CORS middleware before the gateway
- Verify `Access-Control-Allow-Origin` includes your client domain
- Check that preflight OPTIONS requests are handled (gateway does this automatically)
- Ensure client sends correct `Origin` header

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
