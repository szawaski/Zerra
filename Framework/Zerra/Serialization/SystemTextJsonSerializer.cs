// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Zerra.CQRS.Network;

namespace Zerra.Serialization
{
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    public sealed class SystemTextJsonSerializer : ISerializer
    {
        private readonly JsonSerializerOptions? options;
        public SystemTextJsonSerializer(JsonSerializerOptions? options = null)
        {
            this.options = options;
        }

        public ContentType ContentType => ContentType.Json;

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
        public string SerializeString(object? obj) => JsonSerializer.Serialize(obj, options);
        public string SerializeString(object? obj, Type type) => JsonSerializer.Serialize(obj, type, options);
        public string SerializeString<T>(T? obj) => JsonSerializer.Serialize<T>(obj, options);
        public object? Deserialize(ReadOnlySpan<char> str, Type type) => JsonSerializer.Deserialize(str, type, options);
        public T? Deserialize<T>(ReadOnlySpan<char> str) => JsonSerializer.Deserialize<T>(str, options);

        public byte[] SerializeBytes(object? obj) => JsonSerializer.SerializeToUtf8Bytes(obj, options);
        public byte[] SerializeBytes(object? obj, Type type) => JsonSerializer.SerializeToUtf8Bytes(obj, type, options);
        public byte[] SerializeBytes<T>(T? obj) => JsonSerializer.SerializeToUtf8Bytes<T>(obj, options);
        public object? Deserialize(ReadOnlySpan<byte> bytes, Type type) => JsonSerializer.Deserialize(bytes, type, options);
        public T? Deserialize<T>(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<T>(bytes, options);

        public void Serialize(Stream stream, object? obj) => JsonSerializer.Serialize(stream, obj, options);
        public void Serialize(Stream stream, object? obj, Type type) => JsonSerializer.Serialize(stream, obj, type, options);
        public void Serialize<T>(Stream stream, T? obj) => JsonSerializer.Serialize<T>(stream, obj, options);
        public object? Deserialize(Stream stream, Type type) => JsonSerializer.Deserialize(stream, type, options);
        public T? Deserialize<T>(Stream stream) => JsonSerializer.Deserialize<T>(stream, options);

        public Task SerializeAsync(Stream stream, object? obj, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync(stream, obj, options, cancellationToken);
        public Task SerializeAsync(Stream stream, object? obj, Type type, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync(stream, obj, type, options, cancellationToken);
        public Task SerializeAsync<T>(Stream stream, T? obj, CancellationToken cancellationToken) => JsonSerializer.SerializeAsync<T>(stream, obj, options, cancellationToken);
        public Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken) => JsonSerializer.DeserializeAsync(stream, type, options, cancellationToken).AsTask();
        public Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken) => JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken).AsTask();
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
    }
}
