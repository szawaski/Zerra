// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS.Network;

namespace Zerra.Serialization
{
    public interface ISerializer
    {
        ContentType ContentType { get; }

        string SerializeString(object? obj);
        string SerializeString(object? obj, Type type);
        string SerializeString<T>(T? obj);
        object? Deserialize(ReadOnlySpan<char> bytes, Type type);
        T? Deserialize<T>(ReadOnlySpan<char> bytes);

        byte[] SerializeBytes(object? obj);
        byte[] SerializeBytes(object? obj, Type type);
        byte[] SerializeBytes<T>(T? obj);
        object? Deserialize(ReadOnlySpan<byte> bytes, Type type);
        T? Deserialize<T>(ReadOnlySpan<byte> bytes);

        void Serialize(Stream stream, object? obj);
        void Serialize(Stream stream, object? obj, Type type);
        void Serialize<T>(Stream stream, T? obj);
        object? Deserialize(Stream stream, Type type);
        T? Deserialize<T>(Stream stream);

        Task SerializeAsync(Stream stream, object? obj, CancellationToken cancellationToken);
        Task SerializeAsync(Stream stream, object? obj, Type type, CancellationToken cancellationToken);
        Task SerializeAsync<T>(Stream stream, T? obj, CancellationToken cancellationToken);
        Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken);
        Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken);
    }
}
