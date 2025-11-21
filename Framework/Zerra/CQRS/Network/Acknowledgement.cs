// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.Serialization.Bytes;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A response from a service that a CQRS operation was completed and if it was successful.
    /// This may contain a result or an exception.
    /// </summary>
    public sealed class Acknowledgement
    {
        /// <summary>
        /// Indicates if the acknowledgment was a success.
        /// </summary>
        public bool Success { get; private set; }
        /// <summary>
        /// The error message text if the acknowledgment was a failure.
        /// </summary>
        public string? ErrorMessage { get; private set; }
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
        /// <param name="errorMessage">The error message describing the failure.</param>
        public Acknowledgement(string errorMessage)
        {
            this.Success = false;
            this.ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Creates a acknowledgement response that either has a successful result or failure with an exception.
        /// </summary>
        /// <param name="result">The successful result.</param>
        /// <param name="ex">The exception indicating a failure.</param>
        public Acknowledgement(object? result, Exception? ex)
        {
            this.Success = ex is null;
            if (ex is not null)
            {
                this.Success = false;
                this.ErrorMessage = ex.Message;
                var type = ex.GetType();
                this.DataType = type.FullName;
                this.Data = ByteSerializer.Serialize(ex, type, byteSerializerOptions);
            }
            else if (result is not null)
            {
                this.Success = true;
                var type = result.GetType();
                this.DataType = type.FullName;
                this.Data = ByteSerializer.Serialize(result, type, byteSerializerOptions);
            }
            else
            {
                this.Success = true;
            }
        }

        private static readonly ByteSerializerOptions byteSerializerOptions = new()
        {
            IndexType = ByteSerializerIndexType.MemberNames,
            IgnoreIndexAttribute = true
        };

        /// <summary>
        /// Throws an exception if the acknowledgement indicates a failure.
        /// The inner exception will be the original exception.
        /// If the original exception type is not know to this assembly, the inner exception will be null.
        /// </summary>
        /// <param name="ack">The acknowledgement to check for a failure.</param>
        /// <exception cref="RemoteServiceException"></exception>
        public static void ThrowIfFailed(Acknowledgement? ack)
        {
            if (ack is null)
                throw new RemoteServiceException("Acknowledgement Failed");
            if (ack.Success)
                return;

            Exception? ex = null;
            if (ack.DataType is not null && ack.Data is not null && ack.Data.Length > 0)
            {
                try
                {
                    if (Discovery.TryGetTypeFromName(ack.DataType, out var type))
                        ex = (Exception?)ByteSerializer.Deserialize(ack.Data, type, byteSerializerOptions);
                }
                catch { }
            }
            throw new RemoteServiceException(ack.ErrorMessage, ex);
        }

        /// <summary>
        /// Extracts the result if acknowledgement is successful; otherwise throws an exception.
        /// The inner exception will be the original exception.
        /// If the original exception type is not know to this assembly, the inner exception will be null.
        /// </summary>
        /// <param name="ack">The acknowledgement for the result or failure.</param>
        /// <returns>The result if successful which may be a null.  A failure will throw an exception.</returns>
        /// <exception cref="RemoteServiceException"></exception>
        public static object? GetResultOrThrowIfFailed(Acknowledgement? ack)
        {
            if (ack is null)
                throw new RemoteServiceException("Acknowledgement Failed");

            if (!ack.Success)
            {
                Exception? ex = null;
                if (ack.DataType is not null && ack.Data is not null && ack.Data.Length > 0)
                {
                    try
                    {
                        if (Discovery.TryGetTypeFromName(ack.DataType, out var type))
                            ex = (Exception?)ByteSerializer.Deserialize(ack.Data, type, byteSerializerOptions);
                    }
                    catch { }
                }
                throw new RemoteServiceException(ack.ErrorMessage, ex);
            }

            if (ack.DataType is not null && ack.Data is not null && ack.Data.Length > 0)
            {
                try
                {
                    var type = Discovery.GetTypeFromName(ack.DataType);
                    var result = ByteSerializer.Deserialize(ack.Data, type, byteSerializerOptions);
                    return result;
                }
                catch (Exception ex)
                {
                    throw new RemoteServiceException($"Failed to deserialize result type {ack.DataType}", ex);
                }
            }

            return null;
        }
    }
}
