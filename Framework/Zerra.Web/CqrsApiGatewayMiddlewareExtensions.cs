// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Serialization;

namespace Zerra.Web
{
    /// <summary>
    /// Extension methods for configuring the CQRS API Gateway middleware in the ASP.NET Core pipeline.
    /// </summary>
    public static class CqrsApiGatewayMiddlewareExtensions
    {
        /// <summary>
        /// Adds the CQRS API Gateway middleware with custom authorization to the application pipeline.
        /// </summary>
        /// <remarks>
        /// Exposes the CQRS bus to external HTTP clients at the specified route with custom authentication/authorization.
        /// Allows any HTTP client to invoke queries and commands after passing authorization checks.
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <param name="route">The route path where the API Gateway will listen for requests (default: "/CQRS").</param>
        /// <param name="allowOrigins">Optional CORS origins allowed to call the gateway, as scheme://host[:port] or host. If null, all origins are allowed.</param>
        /// <param name="allowAnonymous">Allows the gateway to run without an <see cref="ICqrsAuthorizer"/> registered in DI, for a gateway behind ASP.NET Core authentication or one that is meant to be open. Without it and without an authorizer, this method throws.</param>
        /// <returns>The application builder for method chaining.</returns>
        /// <exception cref="InvalidOperationException">No <see cref="ICqrsAuthorizer"/> is registered and <paramref name="allowAnonymous"/> is false.</exception>
        public static IApplicationBuilder UseCqrsApiGateway(this IApplicationBuilder builder, string? route = "/CQRS", string[]? allowOrigins = null, bool allowAnonymous = false)
        {
            if (!allowAnonymous && builder.ApplicationServices.GetService(typeof(ICqrsAuthorizer)) is null)
                throw new InvalidOperationException($"{nameof(UseCqrsApiGateway)} needs an {nameof(ICqrsAuthorizer)} registered in DI, or allowAnonymous: true for a gateway behind ASP.NET Core authentication or one meant to be open");

            //UseMiddleware picks the constructor by argument type and a null argument matches none, so only non-null arguments are passed and the constructor's null defaults cover the rest
            var args = new List<object>(2);
            if (route is not null)
                args.Add(route);
            if (allowOrigins is not null)
                args.Add(allowOrigins);
            return builder.UseMiddleware<CqrsApiGatewayMiddleware>(args.ToArray());
        }
    }
}