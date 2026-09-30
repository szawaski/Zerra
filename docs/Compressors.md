[← Back to Documentation](Index.md)

# Compressors

An `ICompressor` compresses every message a server, client, producer, or consumer sends, and decompresses what it receives. `ZerraCompressor` is the built-in implementation, using the compression streams in `System.IO.Compression`. Pass `null` instead of a compressor to send messages uncompressed.

```csharp
using Zerra.Compression;

ICompressor compressor = new ZerraCompressor(CompressionAlgorithmType.Deflate);

var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, compressor, log);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, compressor, log);
```

The compressor is the argument right after the encryptor. Like serializers and encryptors, compressors are passed to each server, client, producer, and consumer, not to `Bus.New`. Both ends must use the same algorithm, or neither end a compressor.

## Order with Encryption

Compression runs between the serializer and the encryptor:

- **Sending:** serialize → compress → encrypt
- **Receiving:** decrypt → decompress → deserialize

Encrypted bytes look random and don't compress, so compressing first is what makes the payload smaller. The encryptor and compressor are independent; either can be used without the other.

## Configuration

```csharp
public ZerraCompressor(
    CompressionAlgorithmType algorithm,
    CompressionLevel level = CompressionLevel.Fastest)    // only affects compressing
```

## Algorithms

| `CompressionAlgorithmType` | Format | Notes |
|---|---|---|
| `Deflate` | RFC 1951 | raw, no header or checksum |
| `GZip` | RFC 1952 | Deflate with a header and CRC-32 |
| `ZLib` | RFC 1950 | Deflate with a small header and Adler-32, not on .NET Standard 2.0 |
| `Brotli` | RFC 7932 | usually the smallest, more CPU, not on .NET Standard 2.0 |

On .NET Standard 2.0, `ZLib` and `Brotli` throw `PlatformNotSupportedException` from the constructor.

## When to Use It

Compression costs CPU on both ends, so it only pays off for large payloads. Small messages barely shrink and can grow. On a fast network, such as between services in one data center, compressing can take longer than sending the extra bytes.

Message brokers with large commands benefit the most: each message crosses the network twice and is stored by the broker, and smaller messages stay under its size limit. For TCP and HTTP, compress only services that return large results or stream files.

Deflate is the best default. GZip and ZLib cost about the same, and Brotli is somewhat smaller but uses noticeably more CPU.

## Custom Compressors

Implement `ICompressor`:

```csharp
public interface ICompressor
{
    byte[] Compress(byte[] bytes);
    byte[] Decompress(byte[] bytes);
    Span<byte> Compress(ReadOnlySpan<byte> bytes);          // not on .NET Standard 2.0
    Span<byte> Decompress(ReadOnlySpan<byte> bytes);
    Stream Compress(Stream stream, bool leaveOpen);         // write to it to compress into stream
    Stream Decompress(Stream stream, bool leaveOpen);       // read from it to decompress from stream
}
```

The brokers use the byte array overloads. TCP and HTTP use the stream overloads:

- The stream from `Compress` has to finish writing the compressed data when it's disposed. With `leaveOpen` true, it must leave `stream` open.
- The stream from `Decompress` can stop at the end of the compressed data; it doesn't need to read `stream` any further.

## See Also

- [Encryptors](Encryptors.md) - Encrypting what the compressor produces
- [Serializers](Serializers.md) - What gets compressed
