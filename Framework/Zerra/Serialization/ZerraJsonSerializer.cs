// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS.Network;
using Zerra.Serialization.Json;

namespace Zerra.Serialization
{
    /// <summary>
    /// Human-readable JSON serializer implementation.
    /// </summary>
    /// <remarks>
    /// Provides serialization to and from JSON format with optional customization.
    /// All operations delegate to the JsonSerializer for actual serialization logic.
    /// </remarks>
    public sealed class ZerraJsonSerializer : ISerializer
    {
        private readonly JsonSerializerOptions? options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraJsonSerializer"/> class.
        /// </summary>
        /// <param name="options">Optional JSON serialization options for customizing output format and behavior.</param>
        public ZerraJsonSerializer(JsonSerializerOptions? options = null)
        {
            this.options = options;
        }

        /// <inheritdoc />
        public ContentType ContentType => ContentType.Json;

        /// <inheritdoc />
        public byte[] SerializeBytes(object? obj) => JsonSerializer.SerializeBytes(obj, options);

        /// <inheritdoc />
        public byte[] SerializeBytes(object? obj, Type type) => JsonSerializer.SerializeBytes(obj, type, options);

        /// <inheritdoc />
        public byte[] SerializeBytes<T>(T? obj) => JsonSerializer.SerializeBytes<T>(obj, options);

        /// <inheritdoc />
        public object? Deserialize(ReadOnlySpan<byte> bytes, Type type) => JsonSerializer.Deserialize(bytes, type, options);

        /// <inheritdoc />
        public T? Deserialize<T>(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<T>(bytes, options);

        /// <inheritdoc />
        public void Serialize(Stream stream, object? obj) => JsonSerializer.Serialize(stream, obj, options);

        /// <inheritdoc />
        public void Serialize(Stream stream, object? obj, Type type) => JsonSerializer.Serialize(stream, obj, type, options);

        /// <inheritdoc />
        public void Serialize<T>(Stream stream, T? obj) => JsonSerializer.Serialize<T>(stream, obj, options);

        /// <inheritdoc />
        public object? Deserialize(Stream stream, Type type) => JsonSerializer.Deserialize(stream, type, options);

        /// <inheritdoc />
        public T? Deserialize<T>(Stream stream) => JsonSerializer.Deserialize<T>(stream, options);

        /// <inheritdoc />
        public Task SerializeAsync(Stream stream, object? obj, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync(stream, obj, options,null, cancellationToken);

        /// <inheritdoc />
        public Task SerializeAsync(Stream stream, object? obj, Type type, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync(stream, obj, type, options, null, cancellationToken);

        /// <inheritdoc />
        public Task SerializeAsync<T>(Stream stream, T? obj, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync<T>(stream, obj, options, null, cancellationToken);

        /// <inheritdoc />
        public Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken) => JsonSerializer.DeserializeAsync(stream, type, options, null, cancellationToken);

        /// <inheritdoc />
        public Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken) => JsonSerializer.DeserializeAsync<T>(stream, options, null, cancellationToken);
    }
}
