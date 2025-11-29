// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;
using Zerra.SourceGeneration;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// Helper for Exception type serialization
    /// </summary>
    public static class ExceptionSerializer
    {
        /// <summary>
        /// Serializes an Exception selecting a serializer from the specified ContentType
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The destination stream of the bytes.</param>
        /// <param name="ex">The Exception to be serialized.</param>
        public static void Serialize(ISerializer serializer, Stream stream, Exception ex)
        {
            var errorType = ex.GetType();
            var content = new ExceptionContent()
            {
                ErrorMessage = ex.GetBaseException().Message,
                ErrorType = errorType.FullName
            };

            content.ErrorBytes = serializer.SerializeBytes(ex, errorType);
            serializer.Serialize(stream, content);
        }
        /// <summary>
        /// Deserializes an Exception selecting a serializer from the specified ContentType
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The source stream of the bytes.</param>
        /// <returns>The deserialized Exception.</returns>
        public static Exception Deserialize(ISerializer serializer, Stream stream)
        {
            var content = serializer.Deserialize<ExceptionContent>(stream);
            if (content is null)
                throw new RemoteServiceException("Invalid Exception Content");

            Exception? ex = null;
            if (content.ErrorType is not null)
            {
                try
                {
                    var type = TypeHelper.GetTypeFromName(content.ErrorType);

                    if (type == null)
                        return new RemoteServiceException(content?.ErrorMessage, ex);

                    _ = serializer.Deserialize(content.ErrorBytes, type);
                    ex = (Exception?)serializer.Deserialize(content.ErrorBytes, type);
                }
                catch { }
            }

            return new RemoteServiceException(content?.ErrorMessage, ex);
        }

        /// <summary>
        /// Serializes an Exception selecting a serializer from the specified ContentType
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The destination stream of the bytes.</param>
        /// <param name="ex">The Exception to be serialized.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The deserialized Exception.</returns>
        public static Task SerializeAsync(ISerializer serializer, Stream stream, Exception ex, CancellationToken cancellationToken)
        {
            var errorType = ex.GetType();
            var content = new ExceptionContent()
            {
                ErrorMessage = ex.GetBaseException().Message,
                ErrorType = errorType.FullName
            };

            content.ErrorBytes = serializer.SerializeBytes(ex, errorType);
            return serializer.SerializeAsync(stream, content, cancellationToken);
        }
        /// <summary>
        /// Deserializes an Exception selecting a serializer from the specified ContentType
        /// </summary>
        /// <param name="contentType">Determines which serializer will be used for this data format.</param>
        /// <param name="stream">The source stream of the bytes.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The deserialized Exception.</returns>
        public static async Task<Exception> DeserializeAsync(ISerializer serializer, Stream stream, CancellationToken cancellationToken)
        {
            var content = await serializer.DeserializeAsync<ExceptionContent>(stream, cancellationToken);
            if (content is null)
                throw new RemoteServiceException("Invalid Exception Content");

            Exception? ex = null;
            if (content.ErrorType is not null)
            {
                try
                {
                    var type = TypeHelper.GetTypeFromName(content.ErrorType);

                    if (type == null)
                        return new RemoteServiceException(content?.ErrorMessage, ex);

                    ex = (Exception?)serializer.Deserialize(content.ErrorBytes, type);
                }
                catch { }
            }

            return new RemoteServiceException(content?.ErrorMessage, ex);
        }
    }
}