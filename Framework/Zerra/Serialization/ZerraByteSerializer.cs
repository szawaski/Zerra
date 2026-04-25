using Zerra.CQRS.Network;
using Zerra.Serialization.Bytes;

namespace Zerra.Serialization
{
    /// <summary>
    /// High-performance binary serializer implementation using compact byte format.
    /// </summary>
    /// <remarks>
    /// Provides efficient serialization to and from binary format.
    /// All operations delegate to the ByteSerializer for actual serialization logic.
    /// </remarks>
    public sealed class ZerraByteSerializer : ISerializer
    {
        private readonly ByteSerializerOptions? options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraByteSerializer"/> class.
        /// </summary>
        /// <param name="options">Optional serialization options for customizing binary output format and behavior.</param>
        public ZerraByteSerializer(ByteSerializerOptions? options = null)
        {
            this.options = options;
        }

        /// <inheritdoc />
        public ContentType ContentType => ContentType.Bytes;

        /// <inheritdoc />
        public byte[] SerializeBytes(object? obj) => ByteSerializer.Serialize(obj, options);

        /// <inheritdoc />
        public byte[] SerializeBytes(object? obj, Type type) => ByteSerializer.Serialize(obj, type, options);

        /// <inheritdoc />
        public byte[] SerializeBytes<T>(T? obj) => ByteSerializer.Serialize<T>(obj, options);

        /// <inheritdoc />
        public object? Deserialize(ReadOnlySpan<byte> bytes, Type type) => ByteSerializer.Deserialize(bytes, type, options);

        /// <inheritdoc />
        public T? Deserialize<T>(ReadOnlySpan<byte> bytes) => ByteSerializer.Deserialize<T>(bytes, options);

        /// <inheritdoc />
        public void Serialize(Stream stream, object? obj) => ByteSerializer.Serialize(stream, obj, options);

        /// <inheritdoc />
        public void Serialize(Stream stream, object? obj, Type type) => ByteSerializer.Serialize(stream, obj, type, options);

        /// <inheritdoc />
        public void Serialize<T>(Stream stream, T? obj) => ByteSerializer.Serialize<T>(stream, obj, options);

        /// <inheritdoc />
        public object? Deserialize(Stream stream, Type type) => ByteSerializer.Deserialize(stream, type, options);

        /// <inheritdoc />
        public T? Deserialize<T>(Stream stream) => ByteSerializer.Deserialize<T>(stream, options);

        /// <inheritdoc />
        public Task SerializeAsync(Stream stream, object? obj, CancellationToken cancellationToken) => ByteSerializer.SerializeAsync(stream, obj, options, cancellationToken);

        /// <inheritdoc />
        public Task SerializeAsync(Stream stream, object? obj, Type type, CancellationToken cancellationToken) => ByteSerializer.SerializeAsync(stream, obj, type, options, cancellationToken);

        /// <inheritdoc />
        public Task SerializeAsync<T>(Stream stream, T? obj, CancellationToken cancellationToken) => ByteSerializer.SerializeAsync<T>(stream, obj, options, cancellationToken);

        /// <inheritdoc />
        public Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken) => ByteSerializer.DeserializeAsync(stream, type, options, cancellationToken);

        /// <inheritdoc />
        public Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken) => ByteSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
    }
}
