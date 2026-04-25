// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS.Network;

namespace Zerra.Serialization
{
    /// <summary>
    /// Defines serialization and deserialization operations for converting objects to/from various formats.
    /// </summary>
    /// <remarks>
    /// Provides methods to serialize objects to strings, bytes, or streams, and deserialize them back.
    /// Supports both synchronous and asynchronous operations for stream-based I/O.
    /// Implementations handle specific serialization formats (JSON, binary, etc.).
    /// </remarks>
    public interface ISerializer
    {
        /// <summary>
        /// Gets the content type (serialization format) of this serializer.
        /// </summary>
        ContentType ContentType { get; }

        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <param name="obj">The object to serialize, or null.</param>
        /// <returns>The serialized byte array.</returns>
        byte[] SerializeBytes(object? obj);

        /// <summary>
        /// Serializes an object to a byte array with an explicit type.
        /// </summary>
        /// <param name="obj">The object to serialize, or null.</param>
        /// <param name="type">The type to use for serialization.</param>
        /// <returns>The serialized byte array.</returns>
        byte[] SerializeBytes(object? obj, Type type);

        /// <summary>
        /// Serializes a strongly-typed object to a byte array.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The object to serialize, or null.</param>
        /// <returns>The serialized byte array.</returns>
        byte[] SerializeBytes<T>(T? obj);

        /// <summary>
        /// Deserializes bytes to an object of the specified type.
        /// </summary>
        /// <param name="bytes">The byte span containing serialized data.</param>
        /// <param name="type">The type to deserialize into.</param>
        /// <returns>The deserialized object, or null if deserialization resulted in null.</returns>
        object? Deserialize(ReadOnlySpan<byte> bytes, Type type);

        /// <summary>
        /// Deserializes bytes to a strongly-typed object.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="bytes">The byte span containing serialized data.</param>
        /// <returns>The deserialized object, or null if deserialization resulted in null.</returns>
        T? Deserialize<T>(ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Serializes an object to a stream synchronously.
        /// </summary>
        /// <param name="stream">The stream to write the serialized data to.</param>
        /// <param name="obj">The object to serialize, or null.</param>
        void Serialize(Stream stream, object? obj);

        /// <summary>
        /// Serializes an object to a stream synchronously with an explicit type.
        /// </summary>
        /// <param name="stream">The stream to write the serialized data to.</param>
        /// <param name="obj">The object to serialize, or null.</param>
        /// <param name="type">The type to use for serialization.</param>
        void Serialize(Stream stream, object? obj, Type type);

        /// <summary>
        /// Serializes a strongly-typed object to a stream synchronously.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="stream">The stream to write the serialized data to.</param>
        /// <param name="obj">The object to serialize, or null.</param>
        void Serialize<T>(Stream stream, T? obj);

        /// <summary>
        /// Deserializes a stream to an object of the specified type synchronously.
        /// </summary>
        /// <param name="stream">The stream containing serialized data.</param>
        /// <param name="type">The type to deserialize into.</param>
        /// <returns>The deserialized object, or null if deserialization resulted in null.</returns>
        object? Deserialize(Stream stream, Type type);

        /// <summary>
        /// Deserializes a stream to a strongly-typed object synchronously.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="stream">The stream containing serialized data.</param>
        /// <returns>The deserialized object, or null if deserialization resulted in null.</returns>
        T? Deserialize<T>(Stream stream);

        /// <summary>
        /// Serializes an object to a stream asynchronously.
        /// </summary>
        /// <param name="stream">The stream to write the serialized data to.</param>
        /// <param name="obj">The object to serialize, or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SerializeAsync(Stream stream, object? obj, CancellationToken cancellationToken);

        /// <summary>
        /// Serializes an object to a stream asynchronously with an explicit type.
        /// </summary>
        /// <param name="stream">The stream to write the serialized data to.</param>
        /// <param name="obj">The object to serialize, or null.</param>
        /// <param name="type">The type to use for serialization.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SerializeAsync(Stream stream, object? obj, Type type, CancellationToken cancellationToken);

        /// <summary>
        /// Serializes a strongly-typed object to a stream asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="stream">The stream to write the serialized data to.</param>
        /// <param name="obj">The object to serialize, or null.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SerializeAsync<T>(Stream stream, T? obj, CancellationToken cancellationToken);

        /// <summary>
        /// Deserializes a stream to an object of the specified type asynchronously.
        /// </summary>
        /// <param name="stream">The stream containing serialized data.</param>
        /// <param name="type">The type to deserialize into.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation that yields the deserialized object, or null if deserialization resulted in null.</returns>
        Task<object?> DeserializeAsync(Stream stream, Type type, CancellationToken cancellationToken);

        /// <summary>
        /// Deserializes a stream to a strongly-typed object asynchronously.
        /// </summary>
        /// <typeparam name="T">The type to deserialize into.</typeparam>
        /// <param name="stream">The stream containing serialized data.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation that yields the deserialized object, or null if deserialization resulted in null.</returns>
        Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken);
    }
}
