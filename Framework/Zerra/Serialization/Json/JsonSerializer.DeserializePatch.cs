// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Json.State;
using Zerra.Buffers;
using Zerra.Serialization.Json.Converters;
using Zerra.Reflection;

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
#if NETSTANDARD2_0
        /// <summary>
        /// Deserializes a JSON string to an object of the specified type as a patch, returning both the deserialized object and the associated graph. Used for partial updates of existing objects.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="str">The JSON string containing the patch data. If null or empty, returns the default value for the type.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A tuple containing the deserialized object (or default value if the string is null or empty) and the associated graph for tracking object relationships.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static (T?, Graph?) DeserializePatch<T>(string? str, JsonSerializerOptions? options = null, Graph? graph = null)
            => DeserializePatch<T>(str.AsSpan(), options, graph);

        /// <summary>
        /// Deserializes a JSON string to an object of the specified type as a patch, returning both the deserialized object and the associated graph. Used for partial updates of existing objects.
        /// </summary>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="str">The JSON string containing the patch data. If null or empty, returns null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A tuple containing the deserialized object (or null if the string is null or empty) and the associated graph for tracking object relationships.</returns>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static (object?, Graph?) DeserializePatch(Type type, string? str, JsonSerializerOptions? options = null, Graph? graph = null)
            => DeserializePatch(str.AsSpan(), type, options, graph);
#endif

        /// <summary>
        /// Deserializes a JSON character span to an object of the specified type as a patch, returning both the deserialized object and the associated graph. Used for partial updates of existing objects.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="chars">The character span containing the patch data. If empty, returns the default value for the type or an empty string if T is string.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A tuple containing the deserialized object (or default value if the span is empty) and the associated graph for tracking object relationships.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static (T?, Graph?) DeserializePatch<T>(ReadOnlySpan<char> chars, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (chars.Length == 0)
            {
                if (typeof(T) == typeof(string))
                    return ((T)(object)String.Empty, null);
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

            var state = new ReadState(options, graph, true, true);

            T? result;

            _ = Read(converter, chars, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

            return (result, state.Current.ReturnGraph);
        }
        /// <summary>
        /// Deserializes a JSON character span to an object of the specified type as a patch, returning both the deserialized object and the associated graph. Used for partial updates of existing objects.
        /// </summary>
        /// <param name="chars">The character span containing the patch data. If empty, returns null or an empty string if type is string.</param>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A tuple containing the deserialized object (or null if the span is empty) and the associated graph for tracking object relationships.</returns>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static (object?, Graph?) DeserializePatch(ReadOnlySpan<char> chars, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (chars.Length == 0)
            {
                if (type == typeof(string))
                    return (String.Empty, null);
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var state = new ReadState(options, graph, true, true);

            object? result;

            _ = ReadBoxed(converter, chars, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

            return (result, state.Current.ReturnGraph);
        }

        /// <summary>
        /// Deserializes a UTF-8 byte span to an object of the specified type as a patch, returning both the deserialized object and the associated graph. Used for partial updates of existing objects.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="bytes">The byte span containing the patch data. If empty, returns the default value for the type or an empty string if T is string.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A tuple containing the deserialized object (or default value if the span is empty) and the associated graph for tracking object relationships.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static (T?, Graph?) DeserializePatch<T>(ReadOnlySpan<byte> bytes, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (bytes.Length == 0)
            {
                if (typeof(T) == typeof(string))
                    return ((T)(object)String.Empty, null);
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

            var state = new ReadState(options, graph, true, true);

            T? result;

            _ = Read(converter, bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

            return (result, state.Current.ReturnGraph);
        }
        /// <summary>
        /// Deserializes a UTF-8 byte span to an object of the specified type as a patch, returning both the deserialized object and the associated graph. Used for partial updates of existing objects.
        /// </summary>
        /// <param name="bytes">The byte span containing the patch data. If empty, returns null or an empty string if type is string.</param>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A tuple containing the deserialized object (or null if the span is empty) and the associated graph for tracking object relationships.</returns>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static (object?, Graph?) DeserializePatch(ReadOnlySpan<byte> bytes, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (bytes.Length == 0)
            {
                if (type == typeof(string))
                    return (String.Empty, null);
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var state = new ReadState(options, graph, true, true);

            object? result;

            _ = ReadBoxed(converter, bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

            return (result, state.Current.ReturnGraph);
        }

        /// <summary>
        /// Deserializes data from a stream to an object of the specified type as a patch, returning both the deserialized object and the associated graph. Used for partial updates of existing objects.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="stream">The stream containing the patch data. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A tuple containing the deserialized object (or default value if the stream is empty) and the associated graph for tracking object relationships.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static (T?, Graph?) DeserializePatch<T>(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

            var isFinalBlock = false;
            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var length = 0;
                var read = -1;
                while (length < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = stream.Read(buffer, length, buffer.Length - length);
#else
                    read = stream.Read(buffer.AsSpan(length));
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    length += read;
                }

                if (length == 0)
                {
                    if (typeof(T) == typeof(string))
                        return ((T)(object)String.Empty, null);
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock, true);

                T? result;

                for (; ; )
                {
                    var bytesUsed = Read(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    BufferShift(buffer, bytesUsed);
                    length -= bytesUsed;

                    if (length + state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.SizeNeeded);

                    while (length < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = stream.Read(buffer, length, buffer.Length - length);
#else
                        read = stream.Read(buffer.AsSpan(length));
#endif

                        if (read == 0)
                        {
                            state.IsFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < state.SizeNeeded)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    state.SizeNeeded = 0;
                }

                return (result, state.Current.ReturnGraph);
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
        /// <summary>
        /// Deserializes data from a stream to an object of the specified type as a patch, returning both the deserialized object and the associated graph. Used for partial updates of existing objects.
        /// </summary>
        /// <param name="stream">The stream containing the patch data. Must not be null.</param>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A tuple containing the deserialized object (or null if the stream is empty) and the associated graph for tracking object relationships.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream or type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static (object?, Graph?) DeserializePatch(Stream stream, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var isFinalBlock = false;
            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var length = 0;
                var read = -1;
                while (length < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = stream.Read(buffer, length, buffer.Length - length);
#else
                    read = stream.Read(buffer.AsSpan(length));
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    length += read;
                }

                if (length == 0)
                {
                    if (type == typeof(string))
                        return (String.Empty, null);
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock, true);

                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    BufferShift(buffer, bytesUsed);
                    length -= bytesUsed;

                    if (length + state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.SizeNeeded);

                    while (length < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = stream.Read(buffer, length, buffer.Length - length);
#else
                        read = stream.Read(buffer.AsSpan(length));
#endif

                        if (read == 0)
                        {
                            state.IsFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < state.SizeNeeded)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    state.SizeNeeded = 0;
                }

                return (result, state.Current.ReturnGraph);
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

        /// <summary>
        /// Asynchronously deserializes data from a stream to an object of the specified type as a patch, returning both the deserialized object and the associated graph. Used for partial updates of existing objects.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="stream">The stream containing the patch data. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous deserialization operation and returns a tuple containing the deserialized object (or default value if the stream is empty) and the associated graph for tracking object relationships.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static async Task<(T?, Graph?)> DeserializePatchAsync<T>(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

            var isFinalBlock = false;
            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var length = 0;
                var read = -1;
                while (length < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = await stream.ReadAsync(buffer, length, buffer.Length - length, cancellationToken);
#else
                    read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    length += read;
                }

                if (length == 0)
                {
                    if (typeof(T) == typeof(string))
                        return ((T)(object)String.Empty, null);
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock, true);

                T? result;

                for (; ; )
                {
                    var bytesUsed = Read(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    BufferShift(buffer, bytesUsed);
                    length -= bytesUsed;

                    if (length + state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.SizeNeeded);

                    while (length < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = await stream.ReadAsync(buffer, length, buffer.Length - length, cancellationToken);
#else
                        read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
#endif

                        if (read == 0)
                        {
                            state.IsFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < state.SizeNeeded)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    state.SizeNeeded = 0;
                }

                return (result, state.Current.ReturnGraph);
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
        /// <summary>
        /// Asynchronously deserializes data from a stream to an object of the specified type as a patch, returning both the deserialized object and the associated graph. Used for partial updates of existing objects.
        /// </summary>
        /// <param name="stream">The stream containing the patch data. Must not be null.</param>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous deserialization operation and returns a tuple containing the deserialized object (or null if the stream is empty) and the associated graph for tracking object relationships.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream or type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static async Task<(object?, Graph?)> DeserializePatchAsync(Stream stream, Type type, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var isFinalBlock = false;
            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var length = 0;
                var read = -1;
                while (length < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = await stream.ReadAsync(buffer, length, buffer.Length - length, cancellationToken);
#else
                    read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    length += read;
                }

                if (length == 0)
                {
                    if (type == typeof(string))
                        return (String.Empty, null);
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock, true);

                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    BufferShift(buffer, bytesUsed);
                    length -= bytesUsed;

                    if (length + state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.SizeNeeded);

                    while (length < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = await stream.ReadAsync(buffer, length, buffer.Length - length, cancellationToken);
#else
                        read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
#endif

                        if (read == 0)
                        {
                            state.IsFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < state.SizeNeeded)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    state.SizeNeeded = 0;
                }

                return (result, state.Current.ReturnGraph);
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
    }
}
