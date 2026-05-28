// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// Helper for Exception type serialization
    /// </summary>
    public static class ExceptionSerializer
    {
        /// <summary>
        /// Serializes an Exception using the specified serializer.
        /// </summary>
        /// <param name="serializer">The serializer to use for serialization.</param>
        /// <param name="stream">The destination stream of the bytes.</param>
        /// <param name="ex">The Exception to be serialized.</param>
        public static void Serialize(ISerializer serializer, Stream stream, Exception ex)
        {
            var baseException = ex.GetBaseException();

            var content = new ExceptionContent()
            {
                ErrorMessage = baseException.Message,
                ErrorType = baseException.GetType().Name,
                StackTrace = baseException.StackTrace
            };

            serializer.Serialize(stream, content);
        }

        /// <summary>
        /// Serializes an Exception using the specified serializer.
        /// </summary>
        /// <param name="serializer">The serializer to use for serialization.</param>
        /// <param name="ex">The Exception to be serialized.</param>
        /// <returns>The serialized exception as a byte array.</returns>
        public static byte[] Serialize(ISerializer serializer, Exception ex)
        {
            var baseException = ex.GetBaseException();

            var content = new ExceptionContent()
            {
                ErrorMessage = baseException.Message,
                ErrorType = baseException.GetType().Name,
                StackTrace = baseException.StackTrace
            };

            return serializer.SerializeBytes(content);
        }

        /// <summary>
        /// Serializes an Exception using the specified serializer.
        /// </summary>
        /// <param name="serializer">The serializer to use for serialization.</param>
        /// <param name="errorMessage">The error message describing the failure.</param>
        /// <returns>The serialized exception as a byte array.</returns>
        public static byte[] Serialize(ISerializer serializer, string errorMessage)
        {
            var content = new ExceptionContent()
            {
                ErrorMessage = errorMessage,
                ErrorType = "Unknown",
                StackTrace = null
            };

            return serializer.SerializeBytes(content);
        }

        /// <summary>
        /// Deserializes an Exception using the specified serializer.
        /// </summary>
        /// <param name="serializer">The serializer to use for deserialization.</param>
        /// <param name="stream">The source stream of the bytes.</param>
        /// <returns>The deserialized Exception.</returns>
        public static Exception Deserialize(ISerializer serializer, Stream stream)
        {
            var content = serializer.Deserialize<ExceptionContent>(stream);
            if (content is null)
                throw new RemoteServiceException("Failed to deserialize exception content from remote service");

            return new RemoteServiceException(content.ErrorType, content.ErrorMessage, content.StackTrace);
        }

        /// <summary>
        /// Deserializes an Exception using the specified serializer.
        /// </summary>
        /// <param name="serializer">The serializer to use for deserialization.</param>
        /// <param name="bytes">The source byte array of the serialized exception.</param>
        /// <returns>The deserialized Exception.</returns>
        public static Exception Deserialize(ISerializer serializer, byte[] bytes)
        {
            var content = serializer.Deserialize<ExceptionContent>(bytes);
            if (content is null)
                throw new RemoteServiceException("Failed to deserialize exception content from remote service");

            return new RemoteServiceException(content.ErrorType, content.ErrorMessage, content.StackTrace);
        }

        /// <summary>
        /// Asynchronously serializes an Exception using the specified serializer.
        /// </summary>
        /// <param name="serializer">The serializer to use for serialization.</param>
        /// <param name="stream">The destination stream of the bytes.</param>
        /// <param name="ex">The Exception to be serialized.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous serialization operation.</returns>
        public static Task SerializeAsync(ISerializer serializer, Stream stream, Exception ex, CancellationToken cancellationToken)
        {
            var baseException = ex.GetBaseException();

            var content = new ExceptionContent()
            {
                ErrorMessage = baseException.Message,
                ErrorType = baseException.GetType().Name,
                StackTrace = baseException.StackTrace
            };

            return serializer.SerializeAsync(stream, content, cancellationToken);
        }
        /// <summary>
        /// Asynchronously deserializes an Exception using the specified serializer.
        /// </summary>
        /// <param name="serializer">The serializer to use for deserialization.</param>
        /// <param name="stream">The source stream of the bytes.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous deserialization operation that returns the deserialized Exception.</returns>
        public static async Task<Exception> DeserializeAsync(ISerializer serializer, Stream stream, CancellationToken cancellationToken)
        {
            var content = await serializer.DeserializeAsync<ExceptionContent>(stream, cancellationToken);
            if (content is null)
                throw new RemoteServiceException("Failed to deserialize exception content from remote service");

            return new RemoteServiceException(content.ErrorType, content.ErrorMessage, content.StackTrace);
        }
    }
}