using Zerra.CQRS.Network;
using Zerra.Serialization.Bytes;

namespace Zerra.Serialization
{
    public sealed class ZerraByteSerializer : ISerializer
    {
        private readonly ByteSerializerOptions? options;
        public ZerraByteSerializer(ByteSerializerOptions? options = null)
        {
            this.options = options;
        }

        public ContentType ContentType => ContentType.Bytes;

        public string SerializeString(object? obj) => Convert.ToBase64String(ByteSerializer.Serialize(obj, options));
        public string SerializeString(object? obj, Type type) => Convert.ToBase64String(ByteSerializer.Serialize(obj, type, options));
        public string SerializeString<T>(T? obj) => Convert.ToBase64String(ByteSerializer.Serialize<T>(obj, options));
        public object? Deserialize(ReadOnlySpan<char> str, Type type) => ByteSerializer.Deserialize(Convert.FromBase64String(str.ToString()), type, options);
        public T? Deserialize<T>(ReadOnlySpan<char> str) => ByteSerializer.Deserialize<T>(Convert.FromBase64String(str.ToString()), options);

        public byte[] SerializeBytes(object? obj) => ByteSerializer.Serialize(obj, options);
        public byte[] SerializeBytes(object? obj, Type type) => ByteSerializer.Serialize(obj, type, options);
        public byte[] SerializeBytes<T>(T? obj) => ByteSerializer.Serialize<T>(obj, options);
        public object? Deserialize(ReadOnlySpan<byte> bytes, Type type) => ByteSerializer.Deserialize(bytes, type, options);
        public T? Deserialize<T>(ReadOnlySpan<byte> bytes) => ByteSerializer.Deserialize<T>(bytes, options);

        public void Serialize(Stream stream, object? obj) => ByteSerializer.Serialize(stream, obj, options);
        public void Serialize(Stream stream, object? obj, Type type) => ByteSerializer.Serialize(stream, obj, type, options);
        public void Serialize<T>(Stream stream, T? obj) => ByteSerializer.Serialize<T>(stream, obj, options);
        public object? Deserialize(Stream stream, Type type) => ByteSerializer.Deserialize(stream, type, options);
        public T? Deserialize<T>(Stream stream) => ByteSerializer.Deserialize<T>(stream, options);

        public Task SerializeAsync(Stream stream, object? obj, CancellationToken cancellationToken) => ByteSerializer.SerializeAsync(stream, obj, options, cancellationToken);
        public Task SerializeAsync(Stream stream, object? obj, Type type, CancellationToken cancellationToken) => ByteSerializer.SerializeAsync(stream, obj, type, options, cancellationToken);
        public Task SerializeAsync<T>(Stream stream, T? obj, CancellationToken cancellationToken) => ByteSerializer.SerializeAsync<T>(stream, obj, options, cancellationToken);
        public Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken) => ByteSerializer.DeserializeAsync(stream, type, options, cancellationToken);
        public Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken) => ByteSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
    }
}
