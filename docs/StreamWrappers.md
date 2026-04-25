[← Back to Documentation](Index.md)

# IO Stream Wrappers

Zerra provides flexible stream wrapper classes that enable interception, transformation, and monitoring of stream operations without modifying the underlying stream.

## Overview

The IO stream wrappers provide:
- **Transparent wrapping** - Wrap any stream without changing its behavior
- **Interception points** - Override methods to intercept read/write operations
- **Transformation support** - Transform bytes as they flow through the stream
- **Leave-open semantics** - Control whether wrapped stream is closed
- **Full stream API** - Support for sync, async, and span-based operations

## StreamWrapper

`StreamWrapper` is a base class for creating custom stream wrappers where each method can be overridden to intercept operations.

### Basic Usage

```csharp
using Zerra.IO;

public class LoggingStreamWrapper : StreamWrapper
{
    public LoggingStreamWrapper(Stream stream, bool leaveOpen = false)
        : base(stream, leaveOpen)
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        Console.WriteLine($"Reading up to {count} bytes");
        int bytesRead = base.Read(buffer, offset, count);
        Console.WriteLine($"Read {bytesRead} bytes");
        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Console.WriteLine($"Writing {count} bytes");
        base.Write(buffer, offset, count);
    }
}

// Usage
using var fileStream = File.OpenRead("data.bin");
using var logger = new LoggingStreamWrapper(fileStream, leaveOpen: false);

byte[] buffer = new byte[1024];
logger.Read(buffer, 0, buffer.Length); // Logs read operation
```

### Leave Open Behavior

```csharp
var baseStream = new MemoryStream();

// Wrapper closes the base stream when disposed
using (var wrapper = new StreamWrapper(baseStream, leaveOpen: false))
{
    // Use wrapper
}
// baseStream is now closed

var baseStream2 = new MemoryStream();

// Wrapper leaves base stream open when disposed
using (var wrapper = new StreamWrapper(baseStream2, leaveOpen: true))
{
    // Use wrapper
}
// baseStream2 is still open and usable
baseStream2.Dispose(); // Must dispose manually
```

### Override Points

```csharp
public class CustomStreamWrapper : StreamWrapper
{
    public CustomStreamWrapper(Stream stream, bool leaveOpen)
        : base(stream, leaveOpen)
    {
    }

    // Synchronous read
    public override int Read(byte[] buffer, int offset, int count)
    {
        // Pre-read logic
        int result = base.Read(buffer, offset, count);
        // Post-read logic
        return result;
    }

    // Asynchronous read
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        // Pre-read logic
        int result = await base.ReadAsync(buffer, offset, count, cancellationToken);
        // Post-read logic
        return result;
    }

    // Synchronous write
    public override void Write(byte[] buffer, int offset, int count)
    {
        // Pre-write logic
        base.Write(buffer, offset, count);
        // Post-write logic
    }

    // Asynchronous write
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        // Pre-write logic
        await base.WriteAsync(buffer, offset, count, cancellationToken);
        // Post-write logic
    }

    // Seek operation
    public override long Seek(long offset, SeekOrigin origin)
    {
        // Custom seek logic
        return base.Seek(offset, origin);
    }

    // Flush operation
    public override void Flush()
    {
        // Pre-flush logic
        base.Flush();
        // Post-flush logic
    }
}
```

## StreamTransform

`StreamTransform` is a specialized wrapper for transforming bytes as they're read from or written to the stream. Unlike `StreamWrapper`, it requires implementing Length, Position, Seek, and SetLength as the transformation may affect these values.

### Abstract Members

Classes deriving from `StreamTransform` must implement:

```csharp
public abstract class MyTransform : StreamTransform
{
    public MyTransform(Stream stream, bool leaveOpen)
        : base(stream, leaveOpen)
    {
    }

    // Must implement these abstract members
    public override abstract long Length { get; }
    public override abstract long Position { get; set; }
    public override abstract long Seek(long offset, SeekOrigin origin);
    public override abstract void SetLength(long value);

    // Override transformation methods
    protected abstract int Transform(Span<byte> buffer, bool isRead);
}
```

### Example: XOR Encryption Transform

```csharp
public class XorStreamTransform : StreamTransform
{
    private readonly byte key;
    private long position;

    public XorStreamTransform(Stream stream, byte key, bool leaveOpen = false)
        : base(stream, leaveOpen)
    {
        this.key = key;
    }

    public override long Length => stream.Length;

    public override long Position
    {
        get => position;
        set
        {
            position = value;
            stream.Position = value;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        position = stream.Seek(offset, origin);
        return position;
    }

    public override void SetLength(long value)
    {
        stream.SetLength(value);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = stream.Read(buffer, offset, count);

        // XOR each byte
        for (int i = 0; i < bytesRead; i++)
        {
            buffer[offset + i] ^= key;
        }

        position += bytesRead;
        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        // Create temporary buffer for XOR'd data
        byte[] transformed = new byte[count];

        for (int i = 0; i < count; i++)
        {
            transformed[i] = (byte)(buffer[offset + i] ^ key);
        }

        stream.Write(transformed, 0, count);
        position += count;
    }
}

// Usage
using var fileStream = File.Open("encrypted.dat", FileMode.OpenOrCreate);
using var xorStream = new XorStreamTransform(fileStream, key: 0x42);

// Write encrypted data
byte[] data = Encoding.UTF8.GetBytes("Secret Message");
xorStream.Write(data, 0, data.Length);
```

## StreamExtensions

Utility extension methods for streams:

```csharp
using Zerra.IO;

// Read entire stream into byte array
byte[] data = stream.ToArray();

// Copy stream with custom buffer size
await sourceStream.CopyToAsync(destStream, bufferSize: 81920, cancellationToken);
```

## Common Use Cases

### Logging Wrapper

```csharp
public class LoggingStream : StreamWrapper
{
    private readonly ILogger logger;
    private long totalBytesRead;
    private long totalBytesWritten;

    public LoggingStream(Stream stream, ILogger logger, bool leaveOpen = false)
        : base(stream, leaveOpen)
    {
        this.logger = logger;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var sw = Stopwatch.StartNew();
        int bytesRead = base.Read(buffer, offset, count);
        sw.Stop();

        totalBytesRead += bytesRead;
        logger.LogDebug($"Read {bytesRead} bytes in {sw.ElapsedMilliseconds}ms (total: {totalBytesRead})");

        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var sw = Stopwatch.StartNew();
        base.Write(buffer, offset, count);
        sw.Stop();

        totalBytesWritten += count;
        logger.LogDebug($"Wrote {count} bytes in {sw.ElapsedMilliseconds}ms (total: {totalBytesWritten})");
    }
}
```

### Rate Limiting Wrapper

```csharp
public class ThrottledStream : StreamWrapper
{
    private readonly int bytesPerSecond;
    private readonly Stopwatch stopwatch = new();
    private long totalBytes;

    public ThrottledStream(Stream stream, int bytesPerSecond, bool leaveOpen = false)
        : base(stream, leaveOpen)
    {
        this.bytesPerSecond = bytesPerSecond;
        stopwatch.Start();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrottleIfNeeded(count);
        int bytesRead = base.Read(buffer, offset, count);
        totalBytes += bytesRead;
        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrottleIfNeeded(count);
        base.Write(buffer, offset, count);
        totalBytes += count;
    }

    private void ThrottleIfNeeded(int bytes)
    {
        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        double expectedSeconds = totalBytes / (double)bytesPerSecond;
        double waitSeconds = expectedSeconds - elapsedSeconds;

        if (waitSeconds > 0)
        {
            Thread.Sleep(TimeSpan.FromSeconds(waitSeconds));
        }
    }
}
```

### Progress Reporting Wrapper

```csharp
public class ProgressStream : StreamWrapper
{
    private readonly long totalSize;
    private readonly IProgress<double> progress;
    private long bytesProcessed;

    public ProgressStream(Stream stream, long totalSize, IProgress<double> progress, bool leaveOpen = false)
        : base(stream, leaveOpen)
    {
        this.totalSize = totalSize;
        this.progress = progress;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = base.Read(buffer, offset, count);
        UpdateProgress(bytesRead);
        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        base.Write(buffer, offset, count);
        UpdateProgress(count);
    }

    private void UpdateProgress(int bytes)
    {
        bytesProcessed += bytes;
        double percentage = (double)bytesProcessed / totalSize * 100.0;
        progress?.Report(percentage);
    }
}

// Usage
var progress = new Progress<double>(percent => 
{
    Console.WriteLine($"Progress: {percent:F2}%");
});

using var fileStream = File.OpenRead("largefile.dat");
using var progressStream = new ProgressStream(fileStream, fileStream.Length, progress);

// Read operations report progress
byte[] buffer = new byte[8192];
while (progressStream.Read(buffer, 0, buffer.Length) > 0)
{
    // Process data
}
```

### Compression Wrapper

```csharp
public class CompressionStream : StreamTransform
{
    private readonly GZipStream gzipStream;
    private long position;

    public CompressionStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
        : base(stream, leaveOpen)
    {
        gzipStream = new GZipStream(stream, mode, leaveOpen: true);
    }

    public override long Length => stream.Length;
    public override long Position
    {
        get => position;
        set => throw new NotSupportedException("Compression streams don't support seeking");
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("Compression streams don't support seeking");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("Compression streams don't support SetLength");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = gzipStream.Read(buffer, offset, count);
        position += bytesRead;
        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        gzipStream.Write(buffer, offset, count);
        position += count;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gzipStream.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

## Best Practices

1. **Always call base methods** - Unless you're completely replacing functionality
2. **Handle disposal correctly** - Respect `leaveOpen` parameter
3. **Maintain position tracking** - Keep position accurate when transforming
4. **Support async** - Override async methods for better performance
5. **Document behavior** - Clearly document what your wrapper does
6. **Buffer management** - Be careful with buffer ownership and pooling
7. **Exception safety** - Ensure cleanup happens even on errors

## Performance Considerations

- **Minimal overhead** - Wrappers add minimal overhead when not overriding methods
- **Async efficiency** - Override async methods to avoid sync-over-async
- **Buffer pooling** - Consider using ArrayPool for temporary buffers
- **Stack allocation** - StreamTransform uses stack allocation where possible
- **Virtual calls** - Override costs one virtual call per operation

## Limitations

- **Seeking** - Some wrappers (compression, transformation) may not support seeking
- **Length** - Transformed streams may have different length than base stream
- **Position** - Position may not correspond 1:1 with base stream
- **Span support** - Override span-based methods for best performance on modern runtimes

## See Also

- [Serializers](Serializers.md) - Serializers can work with wrapped streams
- [Encryptors](Encryptors.md) - Can be combined with stream wrappers
