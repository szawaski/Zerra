// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Serialization;

namespace Zerra.Web
{
    /// <summary>
    /// Extension methods for adding the Kestrel CQRS server middleware to the ASP.NET Core pipeline.
    /// </summary>
    public static class KestrelCqrsServerMiddlewareExtensions
    {
        /// <summary>
        /// Adds the Kestrel CQRS server middleware to the application pipeline.
        /// </summary>
        /// <remarks>
        /// Use the same <paramref name="settings"/> for the <see cref="KestrelCqrsServerQueryServer"/>, <see cref="KestrelCqrsServerCommandConsumer"/>,
        /// and <see cref="KestrelCqrsServerEventConsumer"/> so the middleware serves the types they register.
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <param name="serializer">The serializer for request/response serialization and deserialization.</param>
        /// <param name="encryptor">Optional encryptor/decryptor for message encryption. If null, messages are unencrypted.</param>
        /// <param name="log">Optional logger for diagnostic information and errors.</param>
        /// <param name="settings">The server settings shared with the query server and consumers.</param>
        /// <returns>The application builder for method chaining.</returns>
        public static IApplicationBuilder UseKestrelCqrsServer(this IApplicationBuilder builder, ISerializer serializer, IEncryptor? encryptor, ILogger? log, KestrelCqrsServerLinkedSettings settings)
        {
            //built here instead of UseMiddleware, which picks the constructor by argument type and a null encryptor or log matches none
            return builder.Use(next => new KestrelCqrsServerMiddleware(next, serializer, encryptor, log, settings).Invoke);
        }
    }
}
