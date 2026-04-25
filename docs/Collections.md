[← Back to Documentation](Index.md)

# Collection Classes

Zerra provides specialized thread-safe collection classes designed for high-performance concurrent scenarios. All collections implement standard .NET collection interfaces and operate identically to their underlying types with added thread-safety guarantees.

## Overview

The collection classes in Zerra offer:
- **Thread-safe operations** - All operations are safe for concurrent access
- **High performance** - Optimized for common concurrent patterns
- **Familiar APIs** - Identical to standard .NET collections
- **Factory guarantees** - Ensures single execution per key in concurrent scenarios
- **Read-Write locking** - Optimized concurrent read access with exclusive writes
- **Async operations** - Support for asynchronous queue patterns

## ConcurrentFactoryDictionary<TKey, TValue>

Thread-safe dictionary based on `ConcurrentDictionary<TKey, TValue>` that guarantees each factory method is executed **only once per key**, even under high concurrency. Unlike standard `ConcurrentDictionary`, which may execute the same factory multiple times concurrently, this implementation ensures single execution with other threads waiting for completion.

**Key Difference:** Factory side effects execute exactly once per key.

```csharp
// Guaranteed single execution - other threads wait
string value = dictionary.GetOrAdd(key, k => {
    // This executes only once, even if 100 threads call simultaneously
    return ExpensiveOperation(k);
});
```

### Use Case: Type Cache

```csharp
private static readonly ConcurrentFactoryDictionary<Type, TypeDetail> typeCache = new();

public static TypeDetail GetTypeDetail(Type type)
{
    return typeCache.GetOrAdd(type, t => 
        TypeDetailGenerator.Generate(t) // Guaranteed once per type
    );
}
```

### Use Case: Resource Pool

```csharp
private static readonly ConcurrentFactoryDictionary<string, DbConnection> connections = new();

public static DbConnection GetConnection(string connectionString)
{
    return connections.GetOrAdd(connectionString, connStr => 
    {
        var conn = new SqlConnection(connStr);
        conn.Open();
        return conn; // Connection created only once per connection string
    });
}
```

## ConcurrentList<T>

Thread-safe list based on `List<T>` with locking for concurrent access. Operates identically to `List<T>` but safe for concurrent use.

**Key Difference:** All operations are thread-safe.

### Use Case: Event Handlers

```csharp
private readonly ConcurrentList<Action<EventArgs>> handlers = new();

public void RaiseEvent(EventArgs args)
{
    // Safe to iterate even if handlers are being added/removed
    var snapshot = handlers.ToArray();
    foreach (var handler in snapshot)
        handler(args);
}
```

### Use Case: Log Buffer

```csharp
private readonly ConcurrentList<LogEntry> logBuffer = new();
private const int MaxBufferSize = 1000;

public void Log(LogEntry entry)
{
    logBuffer.Add(entry);
    if (logBuffer.Count > MaxBufferSize)
        logBuffer.RemoveAt(0);
}
```

## AsyncConcurrentQueue<T>

Thread-safe async queue based on `Queue<T>` with async/await support for waiting when empty. Perfect for producer-consumer patterns.

**Key Difference:** Async `DequeueAsync()` waits for items instead of throwing, and implements `IAsyncEnumerable<T>`.

```csharp
// Waits for items without polling
var item = await queue.DequeueAsync(cancellationToken);

// Or use async enumeration
await foreach (var item in queue.WithCancellation(cancellationToken))
{
    await ProcessItemAsync(item);
}
```

### Use Case: Background Task Queue

```csharp
private readonly AsyncConcurrentQueue<Func<Task>> taskQueue = new();

public async Task ProcessTasksAsync(CancellationToken cancellationToken)
{
    await foreach (var task in taskQueue.WithCancellation(cancellationToken))
    {
        try { await task(); }
        catch (Exception ex) { Log.Error(ex, "Task failed"); }
    }
}
```

## ConcurrentHashSet<T>

Thread-safe hash set based on `HashSet<T>` with locking for concurrent access. Operates identically to `HashSet<T>` but safe for concurrent use.

**Key Difference:** All operations are thread-safe.

### Use Case: Processed Items Tracker

```csharp
private readonly ConcurrentHashSet<Guid> processedIds = new();

public bool TryMarkAsProcessed(Guid id)
{
    return processedIds.Add(id); // Returns false if already processed
}
```

## ConcurrentReadWriteHashSet<T>

Thread-safe hash set based on `HashSet<T>` with read-write locking for optimized concurrent read access. Best for read-heavy scenarios.

**Key Difference:** Multiple concurrent readers, exclusive writers. Better read performance than `ConcurrentHashSet<T>`.

> **Important:** Implements `IDisposable`. Must be disposed when not used as a static field to release the internal `ReaderWriterLockSlim`.

### Use Case: Active Connection Tracker

```csharp
// Static - no disposal needed
private static readonly ConcurrentReadWriteHashSet<string> activeConnections = new();

// Instance - must dispose
public class ConnectionManager : IDisposable
{
    private readonly ConcurrentReadWriteHashSet<string> activeConnections = new();

    public bool IsActive(string connectionId)
    {
        // Many threads can check simultaneously
        return activeConnections.Contains(connectionId);
    }

    public void Dispose()
    {
        activeConnections.Dispose();
    }
}
```

## ConcurrentReadWriteList<T>

Thread-safe list based on `List<T>` with read-write locking for optimized concurrent read access. Best for read-heavy scenarios.

**Key Difference:** Multiple concurrent readers, exclusive writers. Better read performance than `ConcurrentList<T>`.

> **Important:** Implements `IDisposable`. Must be disposed when not used as a static field to release the internal `ReaderWriterLockSlim`.

### Use Case: Configuration Cache

```csharp
// Static - no disposal needed
private static readonly ConcurrentReadWriteList<ConfigEntry> configCache = new();

// Instance - must dispose
public class ConfigManager : IDisposable
{
    private readonly ConcurrentReadWriteList<ConfigEntry> configCache = new();

    public ConfigEntry? GetConfigEntry(string key)
    {
        // Many threads can read simultaneously
        for (int i = 0; i < configCache.Count; i++)
            if (configCache[i].Key == key)
                return configCache[i];
        return null;
    }

    public void Dispose()
    {
        configCache.Dispose();
    }
}
```

## ConcurrentSortedDictionary<TKey, TValue>

Thread-safe sorted dictionary based on `SortedDictionary<TKey, TValue>` with locking for concurrent access. Keys maintained in sorted order.

**Key Difference:** All operations are thread-safe. Keys automatically sorted.

### Use Case: Time-Series Data

```csharp
private readonly ConcurrentSortedDictionary<DateTime, double> readings = new();

public IEnumerable<KeyValuePair<DateTime, double>> GetReadingsSince(DateTime start)
{
    // Keys are sorted chronologically
    return readings.Where(kvp => kvp.Key >= start).ToArray();
}
```

## ConcurrentSortedReadWriteDictionary<TKey, TValue>

Thread-safe sorted dictionary based on `SortedDictionary<TKey, TValue>` with read-write locking for optimized concurrent read access. Keys maintained in sorted order.

**Key Difference:** Multiple concurrent readers, exclusive writers. Better read performance than `ConcurrentSortedDictionary<TKey, TValue>`.

> **Important:** Implements `IDisposable`. Must be disposed when not used as a static field to release the internal `ReaderWriterLockSlim`.

### Use Case: Leaderboard

```csharp
// Static - no disposal needed
private static readonly ConcurrentSortedReadWriteDictionary<int, Player> leaderboard = new(
    Comparer<int>.Create((a, b) => b.CompareTo(a)) // Descending
);

// Instance - must dispose
public class Leaderboard : IDisposable
{
    private readonly ConcurrentSortedReadWriteDictionary<int, Player> scores = new(
        Comparer<int>.Create((a, b) => b.CompareTo(a))
    );

    public IEnumerable<Player> GetTopPlayers(int count)
    {
        // Many threads can read concurrently
        return scores.Take(count).Select(kvp => kvp.Value).ToArray();
    }

    public void Dispose()
    {
        scores.Dispose();
    }
}
```

## ReadOnlyStack<T>

Read-only stack based on `Stack<T>` that allows only peeking and popping. No items can be added after initialization.

**Key Difference:** Immutable-like behavior. Cannot add items, only consume them.

```csharp
var stack = new ReadOnlyStack<string>(new[] { "a", "b", "c" });

string top = stack.Peek(); // "a"
string item = stack.Pop(); // "a" - removes from stack
```

### Use Case: Undo Stack View

```csharp
public class UndoManager
{
    private readonly List<Action> undoActions = new();

    public ReadOnlyStack<Action> GetUndoStack()
    {
        // Return read-only view that can be popped but not modified
        return new ReadOnlyStack<Action>(undoActions.AsEnumerable().Reverse());
    }
}
```

## When to Use

### Use ConcurrentFactoryDictionary When:
- Factory side effects must occur exactly once per key
- Caching expensive computations
- Managing shared resources or connections
- Multiple threads might request the same key simultaneously

### Use ConcurrentList When:
- You need ordered, indexed access to items
- Multiple threads add/remove from a shared list
- Moderate concurrency with simple locking is acceptable

### Use AsyncConcurrentQueue When:
- Implementing producer-consumer patterns
- You need async/await support for waiting on items
- Background task processing without polling

### Use ConcurrentHashSet When:
- You need unique elements with moderate read/write balance
- Set operations (union, intersect, except)
- Simple locking model is sufficient

### Use ConcurrentReadWriteHashSet When:
- Read-heavy scenarios with unique elements
- Contains checks are very frequent (10:1 or better read:write ratio)

### Use ConcurrentReadWriteList When:
- Read-heavy scenarios with indexed access
- Configuration or reference data that changes occasionally
- Better performance than ConcurrentList for reads (10:1 or better)

### Use ConcurrentSortedDictionary When:
- Keys must be maintained in sorted order
- Moderate read/write balance
- Range queries or ordered enumeration needed

### Use ConcurrentSortedReadWriteDictionary When:
- Keys must be in sorted order with read-heavy access
- Better performance than ConcurrentSortedDictionary for reads (10:1 or better)

### Use ReadOnlyStack When:
- You need a stack that can only be consumed
- Immutable-like stack behavior after initialization
- Undo/history stacks with read-only views

### Don't Use When:
- **ConcurrentFactoryDictionary**: If standard `ConcurrentDictionary` behavior is acceptable (multiple factory calls) - it's faster
- **ConcurrentList/ConcurrentReadWriteList**: Very high-throughput additions - use `ConcurrentBag<T>` or `BlockingCollection<T>`
- **ConcurrentHashSet/ConcurrentReadWriteHashSet**: Ordered elements needed - use sorted dictionaries
- **AsyncConcurrentQueue**: Synchronous blocking is acceptable - `BlockingCollection<T>` may be simpler
- **ReadWrite collections**: Writes are as frequent as reads - standard locking may perform better

## Performance Considerations

### ConcurrentFactoryDictionary
- Uses partitioned locking to reduce contention
- Slower than `ConcurrentDictionary` when factory side effects don't matter
- Factory methods should be reasonably fast

### ConcurrentList
- Single lock for all operations
- Best for moderate concurrency
- `ToArray()` creates efficient snapshot

### AsyncConcurrentQueue
- No busy-waiting - uses `TaskCompletionSource`
- Minimal overhead when items are available
- Efficient for producer-consumer patterns

### HashSet Collections
- **ConcurrentHashSet**: Simple locking, balanced read/write
- **ConcurrentReadWriteHashSet**: Optimized for 10:1+ read:write ratio
- O(1) average case lookups

### List Collections
- **ConcurrentList**: Simple locking, balanced read/write
- **ConcurrentReadWriteList**: Optimized for 10:1+ read:write ratio
- Index access is very fast with read lock

### Sorted Dictionary Collections
- **ConcurrentSortedDictionary**: Simple locking, O(log n)
- **ConcurrentSortedReadWriteDictionary**: Optimized for 10:1+ read:write ratio, O(log n)
- Sorted order maintained automatically

### ReadOnlyStack
- Very lightweight - minimal overhead
- Automatic downsizing after pops
- No locking needed (immutable after construction)

## Best Practices

1. **Disposal** - **ReadWrite collections must be disposed** when used as instance fields. Static fields don't need disposal. ReadWrite collections implement `IDisposable` and must release their internal `ReaderWriterLockSlim`.
2. **Factory Methods** - Keep factory methods fast and side-effect aware
3. **Snapshots** - Use `ToArray()` or `ToList()` for safe enumeration during modifications
4. **Read-Write Choice** - Use ReadWrite variants only when reads significantly outnumber writes (10:1 or better)
5. **Async Patterns** - Use `AsyncConcurrentQueue` for natural async producer-consumer patterns instead of polling
6. **Cancellation** - Always provide cancellation tokens for async operations
7. **Sorted Collections** - Use sorted variants only when order is required - they have O(log n) overhead

### Disposal Pattern for ReadWrite Collections

```csharp
// ✅ Static - no disposal needed
private static readonly ConcurrentReadWriteHashSet<string> staticSet = new();

// ✅ Instance - must dispose
public class MyService : IDisposable
{
    private readonly ConcurrentReadWriteList<Item> items = new();

    public void Dispose()
    {
        items.Dispose(); // Release ReaderWriterLockSlim
    }
}

// ✅ Using statement
using var tempList = new ConcurrentReadWriteList<int>();
tempList.Add(1);
// Disposed automatically
```

## See Also

- [AOT](AOT.md) - Collections work with AOT compilation
- [Reflection](Reflection.md) - TypeAnalyzer uses ConcurrentFactoryDictionary internally
