// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using Zerra.CQRS;
using Zerra.CQRS.Network;

namespace Zerra.Web
{
    /// <summary>
    /// Configuration settings for the Kestrel CQRS server middleware.
    /// </summary>
    /// <remarks>
    /// Manages registration of CQRS types, handler delegates, authorization, and concurrency throttling.
    /// Shared between middleware and server components to coordinate request processing.
    /// </remarks>
    public sealed class KestrelCqrsServerLinkedSettings : IDisposable
    {
        /// <summary>
        /// Gets the dictionary mapping CQRS types to their concurrency throttle semaphores.
        /// </summary>
        public ConcurrentDictionary<Type, SemaphoreSlim> Types { get; }

        /// <summary>
        /// Gets or sets the command counter for tracking command processing limits.
        /// </summary>
        public CommandCounter? CommandCounter { get; set; }

        /// <summary>
        /// Gets or sets the async delegate for handling query method invocations.
        /// </summary>
        public QueryHandlerDelegate? ProviderHandlerAsync { get; set; }

        /// <summary>
        /// Gets or sets the async delegate for handling fire-and-forget command dispatch.
        /// </summary>
        public HandleRemoteCommandDispatch? CommandHandlerAsync { get; set; }

        /// <summary>
        /// Gets or sets the async delegate for handling command dispatch with await semantics.
        /// </summary>
        public HandleRemoteCommandDispatch? CommandHandlerAwaitAsync { get; set; }

        /// <summary>
        /// Gets or sets the async delegate for handling command dispatch with result return.
        /// </summary>
        public HandleRemoteCommandWithResultDispatch? CommandHandlerWithResultAwaitAsync { get; set; }

        /// <summary>
        /// Gets or sets the async delegate for handling event dispatch.
        /// </summary>
        public HandleRemoteEventDispatch? EventHandlerAsync { get; set; }

        /// <summary>
        /// Gets the optional route path where the server middleware listens for requests.
        /// </summary>
        public string? Route { get; }

        /// <summary>
        /// Gets the optional custom authorizer for request validation.
        /// </summary>
        public ICqrsAuthorizer? Authorizer { get; }

        /// <summary>
        /// Gets the content type (serialization format) for requests and responses.
        /// </summary>
        public ContentType ContentType { get; }

        private string[]? allowOrigins;

        /// <summary>
        /// Gets or sets the list of allowed origin hosts for CORS validation.
        /// </summary>
        /// <remarks>
        /// Setting this to null or an empty array allows all origins ("*").
        /// </remarks>
        public string[]? AllowOrigins
        {
            get
            {
                return allowOrigins;
            }
            set
            {
                allowOrigins = value is null || value.Length == 0 ? null : value;
                allowOriginsString = value is null || value.Length == 0 ? "*" : String.Join(", ", value);
            }
        }

        private string allowOriginsString;

        /// <summary>
        /// Gets the CORS allow origins header value as a formatted string.
        /// </summary>
        public string AllowOriginsString
        {
            get
            {
                return allowOriginsString;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KestrelCqrsServerLinkedSettings"/> class.
        /// </summary>
        /// <param name="route">The optional route path where the server middleware listens.</param>
        /// <param name="authorizer">The optional custom authorizer for request validation.</param>
        /// <param name="contentType">The content type for serialization.</param>
        public KestrelCqrsServerLinkedSettings(string? route, ICqrsAuthorizer? authorizer, ContentType contentType)
        {
            this.Route = route;
            this.Authorizer = authorizer;
            this.ContentType = contentType;

            Types = new();
            Types = new();
            this.allowOriginsString = "*";
        }

        /// <summary>
        /// Releases all resources used by the <see cref="KestrelCqrsServerLinkedSettings"/>.
        /// </summary>
        /// <remarks>
        /// Disposes all concurrency throttle semaphores and clears the type registry.
        /// </remarks>
        public void Dispose()
        {
            foreach (var throttle in Types.Values)
                throttle.Dispose();
            Types.Clear();

            foreach (var throttle in Types.Values)
                throttle.Dispose();
            Types.Clear();
        }
    }
}