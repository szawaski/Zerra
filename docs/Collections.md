[← Back to Documentation](Index.md)

# Collections

`Zerra.Collections` has thread-safe versions of the standard collections. Each implements the usual .NET interfaces and behaves like the type it's based on.

| Collection | Based on | Locking | Notes |
|---|---|---|---|
| `ConcurrentFactoryDictionary<TKey, TValue>` | `ConcurrentDictionary` | striped, on adds only | `GetOrAdd` runs the factory **once** per key; other callers wait for it |
| `ConcurrentList<T>` | `List<T>` | single lock | |
| `ConcurrentHashSet<T>` | `HashSet<T>` | single lock | |
| `ConcurrentSortedDictionary<TKey, TValue>` | `SortedDictionary` | single lock | keys kept in order |
| `ConcurrentReadWriteList<T>` | `List<T>` | reader/writer | concurrent reads, exclusive writes |
| `ConcurrentReadWriteHashSet<T>` | `HashSet<T>` | reader/writer | concurrent reads, exclusive writes |
| `ConcurrentSortedReadWriteDictionary<TKey, TValue>` | `SortedDictionary` | reader/writer | concurrent reads, exclusive writes |
| `AsyncConcurrentQueue<T>` | `Queue<T>` | single lock | `DequeueAsync` waits for an item; `IAsyncEnumerable<T>` |
| `ReadOnlyStack<T>` | `Stack<T>` | none | can only be peeked and popped after construction |

## ConcurrentFactoryDictionary

`ConcurrentDictionary.GetOrAdd` can run the factory several times for the same key when callers race, keeping only one result. `ConcurrentFactoryDictionary` runs it exactly once, so it suits factories that are expensive or have side effects:

```csharp
using Zerra.Collections;

private static readonly ConcurrentFactoryDictionary<Type, TypeDetail> cache = new();

public static TypeDetail Get(Type type) => cache.GetOrAdd(type, static t => BuildDetail(t));   // BuildDetail runs once per type
```

Reads of existing keys take no lock. A factory must not add to the same dictionary, which throws `InvalidOperationException`. When running the factory twice is harmless, `ConcurrentDictionary` is faster.

## Reader/Writer Collections

The `ReadWrite` collections let any number of threads read at once, while writes are exclusive. They pay off when reads far outnumber writes; otherwise the single-lock versions are faster.

They hold a `ReaderWriterLockSlim`, so they implement `IDisposable`. Dispose one that's an instance field when its owner is disposed. A static field doesn't need disposing.

## AsyncConcurrentQueue

A producer-consumer queue that waits for items without polling:

```csharp
private readonly AsyncConcurrentQueue<Func<Task>> work = new();

public void Enqueue(Func<Task> item) => work.Enqueue(item);

public async Task RunAsync(CancellationToken cancellationToken)
{
    await foreach (var item in work.WithCancellation(cancellationToken))
        await item();
}

// or one at a time
var next = await work.DequeueAsync(cancellationToken);
```

## Enumerating

Enumerate a snapshot, from `ToArray()`, when other threads may change the collection during the loop.

## ReadOnlyStack

```csharp
var stack = new ReadOnlyStack<string>(new[] { "a", "b", "c" });
string top = stack.Peek();   // "a"
string item = stack.Pop();   // "a", removed
```
