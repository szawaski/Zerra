// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using Zerra.CQRS.Network;

namespace Zerra.Web
{
    /// <summary>
    /// Extension methods for configuring the CQRS API Gateway middleware in the ASP.NET Core pipeline.
    /// </summary>
    public static class CqrsApiGatewayMiddlewareExtensions
    {
        /// <summary>
        /// Adds the CQRS API Gateway middleware to the application pipeline.
        /// </summary>
        /// <remarks>
        /// Exposes the CQRS bus to external HTTP clients at the specified route.
        /// Allows any HTTP client to invoke commands, queries, and events without knowledge of the internal CQRS infrastructure.
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <param name="route">The route path where the API Gateway will listen for requests (default: "/CQRS").</param>
        /// <returns>The application builder for method chaining.</returns>
        public static IApplicationBuilder UseCqrsApiGateway(this IApplicationBuilder builder, string? route = "/CQRS")
        {
            return builder.UseMiddleware<CqrsApiGatewayMiddleware>(route);
        }

        /// <summary>
        /// Adds the CQRS API Gateway middleware with custom authorization to the application pipeline.
        /// </summary>
        /// <remarks>
        /// Exposes the CQRS bus to external HTTP clients at the specified route with custom authentication/authorization.
        /// Allows any HTTP client to invoke commands, queries, and events after passing authorization checks.
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <param name="authorizer">The custom authorizer for validating requests before dispatching to the CQRS bus.</param>
        /// <param name="route">The route path where the API Gateway will listen for requests (default: "/CQRS").</param>
        /// <returns>The application builder for method chaining.</returns>
        public static IApplicationBuilder UseCqrsApiGateway(this IApplicationBuilder builder, ICqrsAuthorizer authorizer, string? route = "/CQRS")
        {
            return builder.UseMiddleware<CqrsApiGatewayMiddleware>(authorizer, route);
        }
    }
}