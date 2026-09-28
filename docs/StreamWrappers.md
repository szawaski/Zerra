[← Back to Documentation](Index.md)

# Stream Wrappers

`Zerra.IO` has two base classes for streams that sit on top of another stream: `StreamWrapper`, to observe or adjust individual operations, and `StreamTransform`, to change the bytes as they pass through. Both take a `leaveOpen` flag: with `false`, disposing the wrapper disposes the wrapped stream too.

## StreamWrapper

Every member forwards to the wrapped stream and can be overridden. Override only what you need, and call `base` for the rest of the behavior:

```csharp
using Zerra.IO;

public class ProgressStream : StreamWrapper
{
    private readonly long totalSize;
    private readonly IProgress<double> progress;
    private long processed;

    public ProgressStream(Stream stream, long totalSize, IProgress<double> progress, bool leaveOpen = false)
        : base(stream, leaveOpen)
    {
        this.totalSize = totalSize;
        this.progress = progress;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = base.Read(buffer, offset, count);
        progress.Report((processed += read) * 100.0 / totalSize);
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var read = await base.ReadAsync(buffer, offset, count, cancellationToken);
        progress.Report((processed += read) * 100.0 / totalSize);
        return read;
    }
}

using var file = File.OpenRead("largefile.dat");
using var tracked = new ProgressStream(file, file.Length, new Progress<double>(p => Console.WriteLine($"{p:F1}%")));
```

Callers may use any `Read` or `Write` overload: array, span, async, or single byte. A `StreamWrapper` has to override each one it wants to intercept, including the span and `Memory` overloads, which are the fastest on modern runtimes.

## StreamTransform

`StreamTransform` routes every `Read` and `Write` overload through four span and memory methods, so a transformation is written once. Because a transformation can change sizes and offsets, it also implements `Length`, `Position`, `Seek`, and `SetLength`. The wrapped stream is the protected `stream` field.

```csharp
public class XorStreamTransform : StreamTransform
{
    private readonly byte key;

    public XorStreamTransform(Stream stream, byte key, bool leaveOpen = false) : base(stream, leaveOpen) => this.key = key;

    public override long Length => stream.Length;
    public override long Position { get => stream.Position; set => stream.Position = value; }
    public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);
    public override void SetLength(long value) => stream.SetLength(value);

    protected override int InternalRead(Span<byte> buffer)
    {
        var read = stream.Read(buffer);
        for (var i = 0; i < read; i++)
            buffer[i] ^= key;
        return read;
    }

    protected override async ValueTask<int> InternalReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var read = await stream.ReadAsync(buffer, cancellationToken);
        var span = buffer.Span;
        for (var i = 0; i < read; i++)
            span[i] ^= key;
        return read;
    }

    protected override void InternalWrite(ReadOnlySpan<byte> buffer)
    {
        var transformed = new byte[buffer.Length];
        for (var i = 0; i < buffer.Length; i++)
            transformed[i] = (byte)(buffer[i] ^ key);
        stream.Write(transformed);
    }

    protected override ValueTask InternalWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        var transformed = new byte[buffer.Length];
        var span = buffer.Span;
        for (var i = 0; i < span.Length; i++)
            transformed[i] = (byte)(span[i] ^ key);
        return stream.WriteAsync(transformed, cancellationToken);
    }
}
```

A transform that can't seek, such as compression, throws `NotSupportedException` from `Seek`, `SetLength`, and the `Position` setter, and tracks `Position` itself. Override `Dispose(bool)` to dispose anything the transform created.

## StreamExtensions

```csharp
using Zerra.IO;

byte[] data = stream.ToArray();                                  // read to the end
byte[] data2 = await stream.ToArrayAsync(cancellationToken);

// read until the buffer is full or the stream ends; returns the bytes read
int read = stream.ReadToSpan(buffer.AsSpan());
int read2 = await stream.ReadToMemoryAsync(buffer.AsMemory(), cancellationToken);
```

## See Also

- [Encryptors](Encryptors.md) - Encrypting streams
- [Queries](Queries.md#streams) - Streaming query results and uploads
