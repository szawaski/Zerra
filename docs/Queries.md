[← Back to Documentation](Index.md)

# Queries

Queries represent read operations that retrieve data without modifying state. Zerra implements queries through type-safe interfaces with automatic proxy generation for remote calls.

## Overview

Queries in Zerra:
- Represent synchronous request-response operations
- Return data without modifying state
- Defined as interface methods (not explicit types like commands/events)
- Support both local and remote invocation
- Automatically propagate `CancellationToken` to remote services
- Can return any serializable type, including `Stream` for large payloads
- Can take one `Stream` argument, uploaded to the remote service as it's read
- Can be synchronous or asynchronous (async recommended)

## Defining Query Interfaces

### Basic Query Interface

```csharp
using Zerra.CQRS;
using MyApp.Models;

public interface IUserQueryHandler : IQueryHandler
{
    Task<User> GetUserById(int id, CancellationToken cancellationToken);
    Task<List<User>> GetActiveUsers(CancellationToken cancellationToken);
    Task<int> GetUserCount(CancellationToken cancellationToken);
}
```

### Requirements

- Must inherit from `IQueryHandler` (marker interface)
- Methods can return `Task<T>`, `Task`, or synchronous types
- Parameters must be serializable
- CancellationToken should be the last parameter

## Implementing Query Handlers

### Basic Handler Implementation

Handlers inherit from `BaseHandler` to access the bus context:

```csharp
using Zerra.CQRS;
using MyApp.Models;

public class UserQueryHandler : BaseHandler, IUserQueryHandler
{
    public async Task<User> GetUserById(int id, CancellationToken cancellationToken)
    {
        // Access services via Context
        var repository = Context.GetService<IUserRepository>();

        // Log if needed
        Log?.Debug($"Fetching user {id}");

        // Retrieve and return data
        var user = await repository.GetByIdAsync(id, cancellationToken);

        if (user == null)
            throw new KeyNotFoundException($"User {id} not found");

        return user;
    }

    public async Task<List<User>> GetActiveUsers(CancellationToken cancellationToken)
    {
        var repository = Context.GetService<IUserRepository>();
        return await repository.GetActiveAsync(cancellationToken);
    }

    public async Task<int> GetUserCount(CancellationToken cancellationToken)
    {
        var repository = Context.GetService<IUserRepository>();
        return await repository.GetCountAsync(cancellationToken);
    }
}
```

### Accessing BusContext

The `BaseHandler` base class provides access to:

```csharp
public class UserQueryHandler : BaseHandler, IUserQueryHandler
{
    public async Task<User> GetUserById(int id, CancellationToken cancellationToken)
    {
        // Access the bus for dispatching other messages
        IBus bus = this.Bus;  // or Context.Bus

        // Access logger
        ILogger? logger = this.Log;  // or Context.Log

        // Get service name
        string serviceName = Context.ServiceName;

        // Retrieve injected services
        var repository = Context.GetService<IUserRepository>();
        var cache = Context.GetService<ICacheService>();

        // Try get optional service
        if (Context.TryGetService<IEmailService>(out var emailService))
        {
            // Use email service if available
        }

        return await repository.GetByIdAsync(id, cancellationToken);
    }
}
```

## Calling Queries

### Local Query Calls

When handlers are registered locally, queries execute in-process:

```csharp
// Register handler locally
bus.AddHandler<IUserQueryHandler>(new UserQueryHandler());

// Call query - executes locally
var user = await bus.Call<IUserQueryHandler>().GetUserById(123, cancellationToken);
var users = await bus.Call<IUserQueryHandler>().GetActiveUsers(cancellationToken);
```

### Remote Query Calls

When a query client is registered, queries are sent to a remote server:

```csharp
// Server side - register handler and server
bus.AddHandler<IUserQueryHandler>(new UserQueryHandler());
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, logger);
bus.AddQueryServer<IUserQueryHandler>(server);

// Client side - register client
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, logger);
bus.AddQueryClient<IUserQueryHandler>(client);

// Call query - executes remotely
var user = await bus.Call<IUserQueryHandler>().GetUserById(123, cancellationToken);
```

### Call Syntax

```csharp
// Standard call with CancellationToken
var result = await bus.Call<IQueryInterface>().MethodName(args, cancellationToken);

// Without CancellationToken (not recommended)
var result = await bus.Call<IQueryInterface>().MethodName(args);

// Synchronous call (not recommended, use async when possible)
var result = bus.Call<IQueryInterface>().SyncMethod(args);
```

## CancellationToken Propagation

CancellationTokens automatically propagate to remote services:

```csharp
// Client side - create cancellation token
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    // Token propagates to remote server
    var users = await bus.Call<IUserQueryHandler>().GetActiveUsers(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Query was cancelled");
}
catch (TimeoutException)
{
    Console.WriteLine("Query timed out");
}

// Server side - receives and respects the token
public async Task<List<User>> GetActiveUsers(CancellationToken cancellationToken)
{
    var repository = Context.GetService<IUserRepository>();

    // Token is propagated from client
    return await repository.GetActiveAsync(cancellationToken);
}
```

## Streaming Queries

Queries can return `Stream` for large payloads or continuous data:

### Define Streaming Query

```csharp
public interface IFileQueryHandler : IQueryHandler
{
    Task<Stream> DownloadFile(string filePath, CancellationToken cancellationToken);
    Task<Stream> GetLogFile(DateTime date, CancellationToken cancellationToken);
}
```

### Implement Streaming Handler

```csharp
public class FileQueryHandler : BaseHandler, IFileQueryHandler
{
    public Task<Stream> DownloadFile(string filePath, CancellationToken cancellationToken)
    {
        Log?.Info($"Streaming file: {filePath}");

        // Validate file exists
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        // Return file stream directly - no buffering
        // Stream is read on-demand as client consumes it
        return Task.FromResult<Stream>(File.OpenRead(filePath));
    }

    public Task<Stream> GetLogFile(DateTime date, CancellationToken cancellationToken)
    {
        var logDirectory = Context.GetService<IConfiguration>()["LogDirectory"];
        var logFile = Path.Combine(logDirectory, $"app-{date:yyyy-MM-dd}.log");

        if (!File.Exists(logFile))
            throw new FileNotFoundException($"Log file not found for date: {date:yyyy-MM-dd}");

        // Stream file directly without loading into memory
        return Task.FromResult<Stream>(File.OpenRead(logFile));
    }
}
```

### Call Streaming Query

```csharp
// Client side - receive stream without buffering entire content
using var stream = await bus.Call<IFileQueryHandler>().DownloadFile("data/large-file.bin", cancellationToken);

// Save to local file - streams directly without loading all into memory
using var outputFile = File.Create("downloaded-file.bin");
await stream.CopyToAsync(outputFile, cancellationToken);

Console.WriteLine("File downloaded successfully");

// Or process log file line-by-line
using var logStream = await bus.Call<IFileQueryHandler>().GetLogFile(DateTime.Today, cancellationToken);
using var reader = new StreamReader(logStream);

// Read line-by-line without loading entire file into memory
while (!reader.EndOfStream)
{
    var line = await reader.ReadLineAsync(cancellationToken);
    if (line.Contains("ERROR"))
        Console.WriteLine(line);
}
```

### Upload a Stream

A query can also take a `Stream` argument, at most one. Over TCP, HTTP, Kestrel, and the API gateway the other arguments are sent first, then the stream's bytes follow in the same request body until the caller's stream ends, so neither side holds the whole upload in memory.

```csharp
public interface IFileQueryHandler : IQueryHandler
{
    Task<ImportPreviewModel> PreviewImport(string fileName, Stream content, CancellationToken cancellationToken);
}

public class FileQueryHandler : BaseHandler, IFileQueryHandler
{
    public async Task<ImportPreviewModel> PreviewImport(string fileName, Stream content, CancellationToken cancellationToken)
    {
        //reads the upload as it arrives
        using var reader = new StreamReader(content);
        var preview = new ImportPreviewModel();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
            preview.Add(line);
        return preview;
    }
}

//Client side, the caller owns and disposes its stream
await using var file = File.OpenRead("import.csv");
var preview = await bus.Call<IFileQueryHandler>().PreviewImport("import.csv", file, cancellationToken);
```

- The stream is only valid until the handler's task completes. Whatever the handler doesn't read is read and discarded by the server so the connection can be reused, and disposing it (as `StreamReader` does) is fine.
- A `null` stream is passed to the handler as `null` and sends no upload.
- An upload can't be retried on another connection once the stream has been read, so the TCP and HTTP clients open a new connection for each upload instead of reusing a pooled one.
- The TCP and HTTP servers don't watch for the caller cancelling while it uploads, since the handler is reading the same connection. A caller that gives up closes the connection, which ends the handler's reads with an error. Kestrel and the gateway pass `RequestAborted` as usual.
- A query can take a `Stream` and return one.
- Uploading is a read like any other query argument. A handler that changes state with it belongs in a command, which takes no stream, so store the upload somewhere first and send the command with where it is.

### Stream Best Practices

```csharp
// ✅ Good - Direct file stream, no buffering
public Task<Stream> DownloadFile(string filePath, CancellationToken cancellationToken)
{
    return Task.FromResult<Stream>(File.OpenRead(filePath));
}

// ✅ Good - Dispose stream in caller
using var stream = await bus.Call<IFileQueryHandler>().DownloadFile(path, cancellationToken);
await stream.CopyToAsync(outputFile, cancellationToken);

// ✅ Good - Process large streams incrementally
using var stream = await bus.Call<IFileQueryHandler>().GetLogFile(date, cancellationToken);
using var reader = new StreamReader(stream);
while (!reader.EndOfStream)
{
    var line = await reader.ReadLineAsync(cancellationToken);
    ProcessLine(line);
}

// ❌ Bad - Loading entire stream into memory
using var stream = await bus.Call<IFileQueryHandler>().DownloadFile(path, cancellationToken);
var allBytes = new byte[stream.Length]; // Allocates entire file in memory!
await stream.ReadAsync(allBytes, cancellationToken);

// ❌ Bad - Buffering content before returning
public async Task<Stream> GetData(CancellationToken cancellationToken)
{
    var memoryStream = new MemoryStream();
    // ... write all data to memory stream
    memoryStream.Position = 0;
    return memoryStream; // Entire dataset buffered in memory
}
```

## Synchronous Queries (Not Recommended)

While Zerra supports synchronous queries, they are not recommended:

### Synchronous Definition

```csharp
public interface IUserQueryHandler : IQueryHandler
{
    // ⚠️ Not recommended - blocks thread
    void GetVoid();
    int GetCount();
    User GetUserById(int id);

    // ✅ Recommended - async
    Task<int> GetCountAsync(CancellationToken cancellationToken);
    Task<User> GetUserByIdAsync(int id, CancellationToken cancellationToken);
}
```

### Why Async is Better

```csharp
// ❌ Synchronous - blocks calling thread during network call
var count = bus.Call<IUserQueryHandler>().GetCount();

// ✅ Asynchronous - doesn't block, better scalability
var count = await bus.Call<IUserQueryHandler>().GetCountAsync(cancellationToken);
```

## Complex Return Types

### Returning Collections

```csharp
public interface IUserQueryHandler : IQueryHandler
{
    Task<List<User>> GetUsers(CancellationToken cancellationToken);
    Task<User[]> GetUsersArray(CancellationToken cancellationToken);
    Task<IEnumerable<User>> GetUsersEnumerable(CancellationToken cancellationToken);
    Task<Dictionary<int, User>> GetUsersDictionary(CancellationToken cancellationToken);
}
```

### Returning Complex Objects

```csharp
public class UserSearchResult
{
    public List<User> Users { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
}

public interface IUserQueryHandler : IQueryHandler
{
    Task<UserSearchResult> SearchUsers(
        string searchTerm, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken);
}
```

### Returning Nullable Types

```csharp
public interface IUserQueryHandler : IQueryHandler
{
    Task<User?> FindUserByEmail(string email, CancellationToken cancellationToken);
    Task<int?> GetOptionalValue(int id, CancellationToken cancellationToken);
}

// Implementation
public async Task<User?> FindUserByEmail(string email, CancellationToken cancellationToken)
{
    var repository = Context.GetService<IUserRepository>();
    return await repository.FindByEmailAsync(email, cancellationToken);
}

// Calling
var user = await bus.Call<IUserQueryHandler>().FindUserByEmail("test@example.com", cancellationToken);
if (user != null)
{
    Console.WriteLine($"Found user: {user.Name}");
}
```

## Error Handling

### Server-Side Error Handling

```csharp
public class UserQueryHandler : BaseHandler, IUserQueryHandler
{
    public async Task<User> GetUserById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var repository = Context.GetService<IUserRepository>();
            var user = await repository.GetByIdAsync(id, cancellationToken);

            if (user == null)
            {
                Log?.Warn($"User {id} not found");
                throw new KeyNotFoundException($"User {id} not found");
            }

            return user;
        }
        catch (Exception ex)
        {
            Log?.Error($"Error fetching user {id}", ex);
            throw;
        }
    }
}
```

### Client-Side Error Handling

When a query is handled by a remote service, any exception thrown in the handler is captured and returned to the caller as a `RemoteServiceException`. The exception carries the original error message (`Message`), the original exception type name (`ErrorType`), the query or command that caused it (`Source`), and the remote stack trace (`StackTrace`).

```csharp
try
{
    var user = await bus.Call<IUserQueryHandler>().GetUserById(123, cancellationToken);
}
catch (RemoteServiceException ex)
{
    // Server handler threw an exception
    Console.WriteLine($"Source:     {ex.Source}");    // e.g. "IUserQueryHandler.GetUserById"
    Console.WriteLine($"Error type: {ex.ErrorType}"); // e.g. "KeyNotFoundException"
    Console.WriteLine($"Message:    {ex.Message}");
    Console.WriteLine($"Stack trace:\n{ex.StackTrace}");
}
catch (TimeoutException ex)
{
    // Handle timeout
    Console.WriteLine("Query timed out");
}
catch (OperationCanceledException)
{
    // Handle cancellation
    Console.WriteLine("Query was cancelled");
}
catch (Exception ex)
{
    // Network or connection error
    Console.WriteLine($"Query failed: {ex.Message}");
}
```

## Performance Optimization

### Caching Pattern

```csharp
public class UserQueryHandler : BaseHandler, IUserQueryHandler
{
    public async Task<User> GetUserById(int id, CancellationToken cancellationToken)
    {
        var cache = Context.GetService<ICacheService>();
        var cacheKey = $"user:{id}";

        // Try cache first
        var cachedUser = await cache.GetAsync<User>(cacheKey, cancellationToken);
        if (cachedUser != null)
        {
            Log?.Debug($"User {id} retrieved from cache");
            return cachedUser;
        }

        // Cache miss - get from repository
        var repository = Context.GetService<IUserRepository>();
        var user = await repository.GetByIdAsync(id, cancellationToken);

        if (user != null)
        {
            // Cache for future requests
            await cache.SetAsync(cacheKey, user, TimeSpan.FromMinutes(5), cancellationToken);
        }

        return user;
    }
}
```

### Pagination

```csharp
public interface IUserQueryHandler : IQueryHandler
{
    Task<PagedResult<User>> GetUsersPage(
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken);
}

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

// Implementation
public async Task<PagedResult<User>> GetUsersPage(
    int pageNumber, 
    int pageSize, 
    CancellationToken cancellationToken)
{
    var repository = Context.GetService<IUserRepository>();

    var totalCount = await repository.GetCountAsync(cancellationToken);
    var items = await repository.GetPageAsync(pageNumber, pageSize, cancellationToken);

    return new PagedResult<User>
    {
        Items = items,
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
```

## Best Practices

### 1. Always Use CancellationToken

```csharp
// ✅ Good - includes cancellation token
Task<User> GetUserById(int id, CancellationToken cancellationToken);

// ❌ Poor - no cancellation support
Task<User> GetUserById(int id);
```

### 2. Return Appropriate Types

```csharp
// ✅ Good - returns null for not found
Task<User?> FindUserByEmail(string email, CancellationToken cancellationToken);

// ❌ Poor - throws exception for not found (expensive for expected scenarios)
Task<User> FindUserByEmail(string email, CancellationToken cancellationToken); // throws if not found
```

### 3. Use Async/Await

```csharp
// ✅ Good - async
public async Task<List<User>> GetActiveUsers(CancellationToken cancellationToken)
{
    var repository = Context.GetService<IUserRepository>();
    return await repository.GetActiveAsync(cancellationToken);
}

// ❌ Poor - synchronous (blocks thread)
public List<User> GetActiveUsers()
{
    var repository = Context.GetService<IUserRepository>();
    return repository.GetActive();
}
```

### 4. Log Appropriately

```csharp
// ✅ Good - contextual logging
public async Task<User> GetUserById(int id, CancellationToken cancellationToken)
{
    Log?.Trace($"GetUserById called with id={id}");

    var repository = Context.GetService<IUserRepository>();
    var user = await repository.GetByIdAsync(id, cancellationToken);

    if (user == null)
        Log?.Warn($"User {id} not found");
    else
        Log?.Debug($"User {id} retrieved successfully");

    return user;
}
```

### 5. Handle Errors Gracefully

```csharp
// ✅ Good - clear error messages
public async Task<User> GetUserById(int id, CancellationToken cancellationToken)
{
    if (id <= 0)
        throw new ArgumentException("User ID must be positive", nameof(id));

    var repository = Context.GetService<IUserRepository>();
    var user = await repository.GetByIdAsync(id, cancellationToken);

    if (user == null)
        throw new KeyNotFoundException($"User with ID {id} was not found");

    return user;
}
```

## See Also

- [Commands](Commands.md) - Execute state-changing operations
- [Events](Events.md) - Publish state change notifications
- [Service Injection](ServiceInjection.md) - Access services in handlers
- [Server Setup](ServerSetup.md) - Configure query servers
- [Client Setup](ClientSetup.md) - Configure query clients
- [Logging](Logging.md) - Implement logging in handlers
