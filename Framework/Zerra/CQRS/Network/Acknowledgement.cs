// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A response from a service that a CQRS operation was completed and if it was successful.
    /// This may contain a result or an exception.
    /// </summary>
    public sealed class Acknowledgement
    {
        /// <summary>
        /// The serialized exception content if the acknowledgment was a failure.
        /// </summary>
        public byte[]? Exception { get; private set; }
        /// <summary>
        /// The data type of the result or the exception.
        /// </summary>
        public string? DataType { get; private set; }
        /// <summary>
        /// The serialized data of the result or the exception.
        /// </summary>
        public byte[]? Data { get; private set; }

        /// <summary>
        /// Creates a acknowledgement response that indicates a failure.
        /// </summary>
        /// <param name="serializer">The serializer to use for serializing the exception.</param>
        /// <param name="errorMessage">The error message describing the failure.</param>
        public Acknowledgement(ISerializer serializer, string errorMessage)
        {
            this.Exception = ExceptionSerializer.Serialize(serializer, errorMessage);
        }

        /// <summary>
        /// Creates a acknowledgement response that either has a successful result or failure with an exception.
        /// </summary>
        /// <param name="serializer">The serializer to use for serializing the result or exception.</param>
        /// <param name="result">The successful result.</param>
        /// <param name="ex">The exception indicating a failure.</param>
        public Acknowledgement(ISerializer serializer, object? result, Exception? ex)
        {
            if (ex is not null)
            {
                this.Exception = ExceptionSerializer.Serialize(serializer, ex);
            }
            else if (result is not null)
            {
                var type = result.GetType();
                this.DataType = type.AssemblyQualifiedName;
                this.Data = serializer.SerializeBytes(result, type);
            }
        }

        /// <summary>
        /// Throws an exception if the acknowledgement indicates a failure.
        /// The inner exception will be the original exception.
        /// If the original exception type is not know to this assembly, the inner exception will be null.
        /// </summary>
        /// <param name="serializer">The serializer to use for deserializing the result or exception.</param>
        /// <param name="ack">The acknowledgement to check for a failure.</param>
        /// <exception cref="RemoteServiceException"></exception>
        public static void ThrowIfFailed(ISerializer serializer, Acknowledgement? ack)
        {
            if (ack is null)
                throw new RemoteServiceException( "Failed to deserialize acknowledgement from remote service");

            if (ack.Exception == null)
                return;

            var ex = ExceptionSerializer.Deserialize(serializer, ack.Exception);
            throw ex;
        }

        /// <summary>
        /// Extracts the result if acknowledgement is successful; otherwise throws an exception.
        /// The inner exception will be the original exception.
        /// If the original exception type is not know to this assembly, the inner exception will be null.
        /// </summary>
        /// <param name="serializer">The serializer to use for deserializing the result or exception.</param>
        /// <param name="ack">The acknowledgement for the result or failure.</param>
        /// <returns>The result if successful which may be a null.  A failure will throw an exception.</returns>
        /// <exception cref="RemoteServiceException"></exception>
        public static object? GetResultOrThrowIfFailed(ISerializer serializer, Acknowledgement? ack)
        {
            if (ack is null)
                throw new RemoteServiceException("Failed to deserialize acknowledgement from remote service");

            if (ack.Exception != null)
            {
                var ex = ExceptionSerializer.Deserialize(serializer, ack.Exception);
                throw ex;
            }

            if (ack.DataType is not null && ack.Data is not null && ack.Data.Length > 0)
            {
                try
                {
                    var type = TypeFinder.GetTypeFromName(ack.DataType);
                    var result = serializer.Deserialize(ack.Data, type);
                    return result;
                }
                catch (Exception ex)
                {
                    throw new RemoteServiceException($"Failed to deserialize acknowledgement from remote service of type {ack.DataType}");
                }
            }

            return null;
        }
    }
}
