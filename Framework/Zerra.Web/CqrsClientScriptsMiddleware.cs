// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using Zerra.CQRS;

namespace Zerra.Web
{
    /// <summary>
    /// ASP.NET Core middleware that serves the Bus.js and Bus.ts browser clients and the models, query functions, and commands they call.
    /// </summary>
    /// <remarks>
    /// Serves GET {route}/Bus.js, {route}/JavaScriptModels.js, {route}/Bus.ts, and {route}/TypeScriptModels.ts.
    /// Bus.js and Bus.ts call the CQRS API gateway at the gateway route unless the page sets another with Bus.setRoute.
    /// The models are generated from the query interfaces and commands added to the bus, the same ones the gateway routes.
    /// Each script is built on its first request and kept.
    /// </remarks>
    public sealed class CqrsClientScriptsMiddleware
    {
        private const string defaultGatewayRouteLine = "\"Gateway\": \"/CQRS\"";

        private readonly RequestDelegate requestDelegate;
        private readonly IBus bus;
        private readonly string gatewayRoute;
        private readonly PathString busJavaScriptPath;
        private readonly PathString javaScriptPath;
        private readonly PathString busTypeScriptPath;
        private readonly PathString typeScriptPath;

        private string? busJavaScript = null;
        private string? javaScript = null;
        private string? busTypeScript = null;
        private string? typeScript = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="CqrsClientScriptsMiddleware"/> class.
        /// </summary>
        /// <param name="requestDelegate">The next middleware in the pipeline.</param>
        /// <param name="bus">The CQRS bus whose query interfaces and commands the scripts call.</param>
        /// <param name="route">The route the scripts are served under (default: "/CQRSClient").</param>
        /// <param name="gatewayRoute">The route or url of the CQRS API gateway the scripts call (default: "/CQRS").</param>
        public CqrsClientScriptsMiddleware(RequestDelegate requestDelegate, IBus bus, string route = "/CQRSClient", string gatewayRoute = "/CQRS")
        {
            if (String.IsNullOrWhiteSpace(route))
                throw new ArgumentException("The route is required", nameof(route));
            if (String.IsNullOrWhiteSpace(gatewayRoute))
                throw new ArgumentException("The gateway route is required", nameof(gatewayRoute));
            if (gatewayRoute.IndexOfAny(['"', '\\', '\r', '\n']) >= 0)
                throw new ArgumentException("The gateway route can't have quotes, backslashes, or line breaks", nameof(gatewayRoute));

            this.requestDelegate = requestDelegate;
            this.bus = bus;
            this.gatewayRoute = gatewayRoute;
            route = route.TrimEnd('/');
            this.busJavaScriptPath = new PathString(route + "/Bus.js");
            this.javaScriptPath = new PathString(route + "/JavaScriptModels.js");
            this.busTypeScriptPath = new PathString(route + "/Bus.ts");
            this.typeScriptPath = new PathString(route + "/TypeScriptModels.ts");
        }

        /// <summary>
        /// Invokes the middleware to serve a client script, passing other requests to the next middleware.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task Invoke(HttpContext context)
        {
            if (context.Request.Method == "GET")
            {
                var path = context.Request.Path;
                if (path.Equals(busJavaScriptPath))
                {
                    busJavaScript ??= ReadBusScript("Zerra.Web.Bus.js");
                    context.Response.ContentType = "text/javascript; charset=utf-8";
                    return context.Response.WriteAsync(busJavaScript, context.RequestAborted);
                }
                if (path.Equals(javaScriptPath))
                {
                    javaScript ??= CqrsClientScriptGenerator.GenerateJavaScript(bus);
                    context.Response.ContentType = "text/javascript; charset=utf-8";
                    return context.Response.WriteAsync(javaScript, context.RequestAborted);
                }
                if (path.Equals(busTypeScriptPath))
                {
                    busTypeScript ??= ReadBusScript("Zerra.Web.Bus.ts");
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    return context.Response.WriteAsync(busTypeScript, context.RequestAborted);
                }
                if (path.Equals(typeScriptPath))
                {
                    typeScript ??= CqrsClientScriptGenerator.GenerateTypeScript(bus);
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    return context.Response.WriteAsync(typeScript, context.RequestAborted);
                }
            }

            return requestDelegate(context);
        }

        private string ReadBusScript(string resourceName)
        {
            using var stream = typeof(CqrsClientScriptsMiddleware).Assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"{resourceName} is missing from Zerra.Web");
            using var reader = new StreamReader(stream);
            var script = reader.ReadToEnd();
            if (script.IndexOf(defaultGatewayRouteLine, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException($"{resourceName} has no gateway route to set");
            return script.Replace(defaultGatewayRouteLine, $"\"Gateway\": \"{gatewayRoute}\"");
        }
    }
}
