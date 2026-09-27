// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// Data for an API response.
    /// This may be a model to serialize, a stream, or a void response.
    /// Used by <see cref="ApiServerHandler"/>
    /// </summary>
    public sealed class ApiResponseData
    {
        /// <summary>
        /// The stream of the API resonse.
        /// </summary>
        public Stream? Stream { get; }
        /// <summary>
        /// The model of the API response, serialized with <see cref="Serializer"/> straight to the response. It may be null, which is serialized too.
        /// </summary>
        public object? Model { get; }
        /// <summary>
        /// The serializer for <see cref="Model"/>, null when the response has no model.
        /// </summary>
        public ISerializer? Serializer { get; }
        /// <summary>
        /// Indicates if there a void response from the API.
        /// </summary>
        public bool Void { get { return Stream is null && Serializer is null; } }
        /// <summary>
        /// Creates an API response that is void.
        /// </summary>
        public ApiResponseData()
        {
        }
        /// <summary>
        /// Creates an API response with a stream.
        /// </summary>
        /// <param name="stream">The stream for the response.</param>
        public ApiResponseData(Stream stream)
        {
            this.Stream = stream;
        }
        /// <summary>
        /// Creates an API response with a model to serialize to the response.
        /// </summary>
        /// <param name="model">The model for the response, which may be null.</param>
        /// <param name="serializer">The serializer that writes the model to the response.</param>
        public ApiResponseData(object? model, ISerializer serializer)
        {
            this.Model = model;
            this.Serializer = serializer;
        }
    }
}
