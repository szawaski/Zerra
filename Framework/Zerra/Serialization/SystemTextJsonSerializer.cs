// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Zerra.CQRS.Network;

namespace Zerra.Serialization
{
    /// <summary>
    /// JSON serializer using System.Text.Json for compatibility with .NET's standard library.
    /// </summary>
    /// <remarks>
    /// Provides serialization to and from JSON using System.Text.Json directly with reflection-based type handling.
    /// Not suitable for trimmed or AOT-compiled applications. For AOT scenarios, configure System.Text.Json source generation
    /// in the <see cref="JsonSerializerOptions"/> using a <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>.
    /// Requires unreferenced code and dynamic code attributes due to reflection-based serialization.
    /// </remarks>
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    public sealed class SystemTextJsonSerializer : ISerializer
    {
        private readonly JsonSerializerOptions? options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemTextJsonSerializer"/> class.
        /// </summary>
        /// <remarks>
        /// This implementation uses reflection-based serialization and is not compatible with AOT compilation.
        /// For AOT-compiled applications, pass a <see cref="JsonSerializerOptions"/> configured with a source-generated 
        /// <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> via the constructor parameter.
        /// </remarks>
        /// <param name="options">Optional JSON serialization options from System.Text.Json for customizing output format and behavior. 
        /// For AOT support, provide options that use a source-generated serializer context.</param>
        public SystemTextJsonSerializer(JsonSerializerOptions? options = null)
        {
            this.options = options;
        }

        /// <inheritdoc />
        public ContentType ContentType => ContentType.Json;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

        /// <inheritdoc />
        public byte[] SerializeBytes(object? obj) => JsonSerializer.SerializeToUtf8Bytes(obj, options);

        /// <inheritdoc />
        public byte[] SerializeBytes(object? obj, Type type) => JsonSerializer.SerializeToUtf8Bytes(obj, type, options);

        /// <inheritdoc />
        public byte[] SerializeBytes<T>(T? obj) => JsonSerializer.SerializeToUtf8Bytes<T>(obj, options);

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
        public Task SerializeAsync(Stream stream, object? obj, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync(stream, obj, options, cancellationToken);

        /// <inheritdoc />
        public Task SerializeAsync(Stream stream, object? obj, Type type, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync(stream, obj, type, options, cancellationToken);

        /// <inheritdoc />
        public Task SerializeAsync<T>(Stream stream, T? obj, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync<T>(stream, obj, options, cancellationToken);

        /// <inheritdoc />
        public Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken) => JsonSerializer.DeserializeAsync(stream, type, options, cancellationToken).AsTask();

        /// <inheritdoc />
        public Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken) => JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken).AsTask();
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
    }
}
