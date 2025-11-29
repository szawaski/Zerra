// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// A service query response for a remote call to a query server.
    /// This will either contain a model or a stream.
    /// It is used in <see cref="QueryHandlerDelegate"/> as a method in <see cref="IQueryServer"/>
    /// </summary>
    public sealed class RemoteQueryCallResponse
    {
        private readonly object? model;
        private readonly Stream? stream;

        /// <summary>
        /// The model response from the remote service.
        /// </summary>
        public object? Model => model;
        /// <summary>
        /// The stream response from the remote service.
        /// </summary>
        public Stream? Stream => stream;

        /// <summary>
        /// Creates a new reponse that contains a model.
        /// </summary>
        /// <param name="model">The model for the response.</param>
        public RemoteQueryCallResponse(object? model)
        {
            this.model = model;
            this.stream = null;
        }
        /// <summary>
        /// Creates a new response that contains a stream.
        /// </summary>
        /// <param name="stream">The stream for the response.</param>
        public RemoteQueryCallResponse(Stream? stream)
        {
            this.model = null;
            this.stream = stream;
        }
    }
}
