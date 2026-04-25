// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// Data for an API response.
    /// This many be a serialized byte array, a stream, or a void response.
    /// Used by <see cref="ApiServerHandler"/>
    /// </summary>
    public sealed class ApiResponseData
    {
        /// <summary>
        /// The bytes for the API response
        /// </summary>
        public byte[]? Bytes { get; }
        /// <summary>
        /// The stream of the API resonse.
        /// </summary>
        public Stream? Stream { get; }
        /// <summary>
        /// Indicates if there a void response from the API.
        /// </summary>
        public bool Void { get { return Bytes is null && Stream is null; } }
        /// <summary>
        /// Creates an API response that is void.
        /// </summary>
        public ApiResponseData()
        {
            this.Bytes = null;
            this.Stream = null;
        }
        /// <summary>
        /// Creates an API response with serialized bytes
        /// </summary>
        /// <param name="bytes">The serialized bytes for the response</param>
        public ApiResponseData(byte[] bytes)
        {
            this.Bytes = bytes;
            this.Stream = null;
        }
        /// <summary>
        /// Creates an API response with a stream.
        /// </summary>
        /// <param name="stream">The stream for the response.</param>
        public ApiResponseData(Stream stream)
        {
            this.Bytes = null;
            this.Stream = stream;
        }
    }
}