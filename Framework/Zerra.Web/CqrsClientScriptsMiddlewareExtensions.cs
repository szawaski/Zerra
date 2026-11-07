// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;

namespace Zerra.Web
{
    /// <summary>
    /// Extension methods for serving the CQRS browser client scripts in the ASP.NET Core pipeline.
    /// </summary>
    public static class CqrsClientScriptsMiddlewareExtensions
    {
        /// <summary>
        /// Serves the Bus.js and Bus.ts browser clients and the models, query functions, and commands they call, generated from the bus.
        /// </summary>
        /// <remarks>
        /// Serves GET {route}/Bus.js, {route}/JavaScriptModels.js, {route}/Bus.ts, and {route}/TypeScriptModels.ts.
        /// Anyone who can reach the route sees the names and members of the query interfaces, commands, and models the bus routes.
        /// </remarks>
        /// <param name="builder">The application builder.</param>
        /// <param name="route">The route the scripts are served under (default: "/CQRSClient").</param>
        /// <param name="gatewayRoute">The route or url of the CQRS API gateway the scripts call, as passed to UseCqrsApiGateway (default: "/CQRS").</param>
        /// <returns>The application builder for method chaining.</returns>
        public static IApplicationBuilder UseCqrsClientScripts(this IApplicationBuilder builder, string route = "/CQRSClient", string gatewayRoute = "/CQRS")
            => builder.UseMiddleware<CqrsClientScriptsMiddleware>(route, gatewayRoute);
    }
}
