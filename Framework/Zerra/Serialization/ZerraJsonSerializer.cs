// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS.Network;
using Zerra.Serialization.Json;

namespace Zerra.Serialization
{
    public sealed class ZerraJsonSerializer : ISerializer
    {
        private readonly JsonSerializerOptions? options;
        public ZerraJsonSerializer(JsonSerializerOptions? options = null)
        {
            this.options = options;
        }

        public ContentType ContentType => ContentType.Json;

        public string SerializeString(object? obj) => JsonSerializer.Serialize(obj, options);
        public string SerializeString(object? obj, Type type) => JsonSerializer.Serialize(obj, type, options);
        public string SerializeString<T>(T? obj) => JsonSerializer.Serialize<T>(obj, options);
        public object? Deserialize(ReadOnlySpan<char> str, Type type) => JsonSerializer.Deserialize(str, type, options);
        public T? Deserialize<T>(ReadOnlySpan<char> str) => JsonSerializer.Deserialize<T>(str, options);

        public byte[] SerializeBytes(object? obj) => JsonSerializer.SerializeBytes(obj, options);
        public byte[] SerializeBytes(object? obj, Type type) => JsonSerializer.SerializeBytes(obj, type, options);
        public byte[] SerializeBytes<T>(T? obj) => JsonSerializer.SerializeBytes<T>(obj, options);
        public object? Deserialize(ReadOnlySpan<byte> bytes, Type type) => JsonSerializer.Deserialize(bytes, type, options);
        public T? Deserialize<T>(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<T>(bytes, options);

        public void Serialize(Stream stream, object? obj) => JsonSerializer.Serialize(stream, obj, options);
        public void Serialize(Stream stream, object? obj, Type type) => JsonSerializer.Serialize(stream, obj, type, options);
        public void Serialize<T>(Stream stream, T? obj) => JsonSerializer.Serialize<T>(stream, obj, options);
        public object? Deserialize(Stream stream, Type type) => JsonSerializer.Deserialize(stream, type, options);
        public T? Deserialize<T>(Stream stream) => JsonSerializer.Deserialize<T>(stream, options);

        public Task SerializeAsync(Stream stream, object? obj, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync(stream, obj, options,null, cancellationToken);
        public Task SerializeAsync(Stream stream, object? obj, Type type, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync(stream, obj, type, options, null, cancellationToken);
        public Task SerializeAsync<T>(Stream stream, T? obj, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync<T>(stream, obj, options, null, cancellationToken);
        public Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken) => JsonSerializer.DeserializeAsync(stream, type, options, null, cancellationToken);
        public Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken) => JsonSerializer.DeserializeAsync<T>(stream, options, null, cancellationToken);
    }
}
