[← Back to Documentation](Index.md)

# Queries

A query reads data without changing state. Queries are methods on an interface, not message types. Callers get a generated proxy, so a remote query reads like a local method call.

## Defining Query Interfaces

```csharp
using Zerra.CQRS;

public interface IUserQueryHandler : IQueryHandler
{
    Task<User> GetUserById(int id, CancellationToken cancellationToken);
    Task<User?> FindUserByEmail(string email, CancellationToken cancellationToken);
    Task<PagedResult<User>> SearchUsers(string searchTerm, int page, int pageSize, CancellationToken cancellationToken);
}
```

- The interface derives from `IQueryHandler`.
- Methods return `Task<T>`, `Task`, or a plain synchronous type. Prefer async. A synchronous call blocks the caller's thread for the whole network round trip, and on .NET Standard 2.0 `ApiClient` and `KestrelCqrsClient` don't support synchronous calls at all.
- Parameters and results must be serializable: models, collections, dictionaries, and nullable types all work. A method can also take or return a `Stream` (see [Streams](#streams)).
- Put the `CancellationToken` last.

Return `null` for "not found" when that is an expected answer, as `FindUserByEmail` does. Throwing is for errors.

## Implementing Query Handlers

```csharp
public class UserQueryHandler : BaseHandler, IUserQueryHandler
{
    public async Task<User> GetUserById(int id, CancellationToken cancellationToken)
    {
        var user = await Context.GetService<IUserRepository>().GetByIdAsync(id, cancellationToken);
        return user ?? throw new KeyNotFoundException($"User {id} not found");
    }

    public Task<User?> FindUserByEmail(string email, CancellationToken cancellationToken)
        => Context.GetService<IUserRepository>().FindByEmailAsync(email, cancellationToken);

    public Task<PagedResult<User>> SearchUsers(string searchTerm, int page, int pageSize, CancellationToken cancellationToken)
        => Context.GetService<IUserRepository>().SearchAsync(searchTerm, page, pageSize, cancellationToken);
}
```

`BaseHandler` provides `Bus`, `Log`, and `Context`, as described in [Commands](Commands.md#handlers). A query handler can call other queries through `Bus.Call<T>()`.

## Calling Queries

```csharp
var user = await bus.Call<IUserQueryHandler>().GetUserById(123, cancellationToken);
```

Where the query runs depends only on registration:

```csharp
// Local: runs in this process
bus.AddHandler<IUserQueryHandler>(new UserQueryHandler());

// Remote: server side
bus.AddHandler<IUserQueryHandler>(new UserQueryHandler());
bus.AddQueryServer<IUserQueryHandler>(new TcpCqrsServer("localhost:9001", serializer, encryptor, log));

// Remote: client side
bus.AddQueryClient<IUserQueryHandler>(new TcpCqrsClient("localhost:9001", serializer, encryptor, log));
```

Queries always travel directly over TCP or HTTP, never through a message broker. See [Server Setup](ServerSetup.md) and [Client Setup](ClientSetup.md).

### Cancellation and Timeouts

Cancelling the caller's `CancellationToken` cancels the call, including the handler's token on the remote service. This differs from commands, whose tokens don't propagate.

A query that runs longer than the bus's `defaultCallTimeout` throws `TimeoutException` (see [Client Setup](ClientSetup.md#timeout-configuration)).

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
try
{
    var users = await bus.Call<IUserQueryHandler>().SearchUsers("john", 1, 20, cts.Token);
}
catch (OperationCanceledException) { /* cancelled */ }
catch (TimeoutException) { /* timed out */ }
```

## Errors

An exception thrown by a remote handler is returned to the caller as a `RemoteServiceException` carrying the original `Message`, the original type name in `ErrorType`, the query in `Source` (such as `IUserQueryHandler.GetUserById`), and the remote `StackTrace`. There is no `InnerException`, so branch on `ErrorType`:

```csharp
try
{
    var user = await bus.Call<IUserQueryHandler>().GetUserById(123, cancellationToken);
}
catch (RemoteServiceException ex) when (ex.ErrorType == nameof(KeyNotFoundException))
{
    // not found
}
```

Network and connection failures surface as their own exceptions, such as `IOException`.

## Streams

### Returning a Stream

A query can return a `Stream`. It is streamed to the caller as it is read, so neither side holds the whole payload in memory. Return the source stream directly, such as a file stream, rather than copying it into a `MemoryStream` first.

```csharp
public interface IFileQueryHandler : IQueryHandler
{
    Task<Stream> DownloadFile(string filePath, CancellationToken cancellationToken);
}

public Task<Stream> DownloadFile(string filePath, CancellationToken cancellationToken)
    => Task.FromResult<Stream>(File.OpenRead(filePath));

// Caller: disposes the stream when done
await using var stream = await bus.Call<IFileQueryHandler>().DownloadFile("data/large-file.bin", cancellationToken);
await using var output = File.Create("downloaded-file.bin");
await stream.CopyToAsync(output, cancellationToken);
```

### Upload a Stream

A query can also take one `Stream` argument. Over TCP, HTTP, Kestrel, and the API gateway, the other arguments are sent first, then the stream's bytes follow in the same request until the caller's stream ends.

```csharp
public interface IFileQueryHandler : IQueryHandler
{
    Task<ImportPreviewModel> PreviewImport(string fileName, Stream content, CancellationToken cancellationToken);
}

public async Task<ImportPreviewModel> PreviewImport(string fileName, Stream content, CancellationToken cancellationToken)
{
    using var reader = new StreamReader(content); // reads the upload as it arrives
    var preview = new ImportPreviewModel();
    string? line;
    while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        preview.Add(line);
    return preview;
}

// Caller: owns and disposes its stream
await using var file = File.OpenRead("import.csv");
var preview = await bus.Call<IFileQueryHandler>().PreviewImport("import.csv", file, cancellationToken);
```

- The stream is valid only until the handler's task completes. Whatever the handler doesn't read is read and discarded by the server so the connection can be reused. Disposing it, as `StreamReader` does, is fine.
- A `null` stream reaches the handler as `null` and sends no upload.
- An upload can't be retried on another connection once the stream has been read, so the TCP and HTTP clients open a new connection for each upload instead of reusing a pooled one.
- The TCP and HTTP servers don't watch for the caller cancelling during an upload, since the handler is reading the same connection. A caller that gives up closes the connection, which ends the handler's reads with an error. Kestrel and the gateway pass `RequestAborted` as usual.
- A query can take a `Stream` and return one.
- An upload is a read like any other query argument. To change state with it, store it first and send a command saying where it is, since commands don't take streams.

## See Also

- [Commands](Commands.md) - State-changing operations
- [Events](Events.md) - State change notifications
- [Server Setup](ServerSetup.md) - Query servers
- [Client Setup](ClientSetup.md) - Query clients
