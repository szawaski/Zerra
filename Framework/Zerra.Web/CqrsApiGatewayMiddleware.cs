// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using System.Security;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Logging;
using Zerra.Serialization;

namespace Zerra.Web
{
    /// <summary>
    /// ASP.NET Core middleware that exposes a CQRS bus to external HTTP clients as an API Gateway.
    /// </summary>
    /// <remarks>
    /// Allows any HTTP client to invoke commands, queries, and events on the CQRS bus without knowledge of the internal architecture.
    /// Provides automatic routing, serialization/deserialization, CORS support, and optional authentication/authorization.
    /// Supports multiple content types (JSON, binary) and both request/response and streaming response patterns.
    /// </remarks>
    public sealed class CqrsApiGatewayMiddleware
    {
        private readonly RequestDelegate requestDelegate;
        private readonly IBus bus;
        private readonly ISerializer serializer;
        private readonly ILogger? log;
        private readonly string? route;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly string[]? allowOrigins;

        private ISerializer? namelessSerializer = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="CqrsApiGatewayMiddleware"/> class with authorization support.
        /// </summary>
        /// <param name="requestDelegate">The next middleware in the pipeline.</param>
        /// <param name="bus">The CQRS bus for dispatching commands, queries, and events.</param>
        /// <param name="serializer">The serializer for request/response serialization and deserialization.</param>
        /// <param name="log">Optional logger for diagnostic information and errors.</param>
        /// <param name="authorizer">Optional authorizer for custom request authentication and authorization. If null, no authorization is performed.</param>
        /// <param name="route">Optional route path to restrict the middleware to specific requests (e.g., "/cqrs"). If null, all POST/OPTIONS requests are processed.</param>
        /// <param name="allowOrigins">Optional CORS origins allowed to call the gateway, as scheme://host[:port] or host. If null, empty, or containing "*", all origins are allowed.</param>
        public CqrsApiGatewayMiddleware(RequestDelegate requestDelegate, IBus bus, ISerializer serializer, ILogger? log = null, ICqrsAuthorizer? authorizer = null, string? route = null, string[]? allowOrigins = null)
        {
            this.requestDelegate = requestDelegate;
            this.bus = bus;
            this.serializer = serializer;
            this.log = log;
            this.authorizer = authorizer;
            this.route = route;
            this.allowOrigins = allowOrigins is null || allowOrigins.Length == 0 || allowOrigins.Contains("*") ? null : allowOrigins;
        }

        /// <summary>
        /// Invokes the middleware to process HTTP requests and dispatch them as CQRS messages.
        /// </summary>
        /// <remarks>
        /// Handles POST requests for CQRS message dispatch and OPTIONS requests for CORS preflight.
        /// Validates content type, deserializes API requests, dispatches to the bus, and serializes responses.
        /// Automatically sets CORS headers and handles both synchronous and streaming responses.
        /// Non-matching requests are passed to the next middleware in the pipeline.
        /// </remarks>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Invoke(HttpContext context)
        {
            if ((!String.IsNullOrWhiteSpace(route) && context.Request.Path != route) || (context.Request.Method != "POST" && context.Request.Method != "OPTIONS"))
            {
                await requestDelegate(context);
                return;
            }

            //browsers accept one origin or * so an allowed request origin is echoed, a disallowed one gets none
            //with allowed origins a request must have an origin like the other CQRS servers, ApiClient sends the host of the gateway
            string? origin = context.Request.Headers[HttpCommon.OriginHeader];
            var originAllowed = true;
            if (allowOrigins is null)
            {
                context.Response.Headers.Append(HttpCommon.AccessControlAllowOriginHeader, "*");
            }
            else
            {
                context.Response.Headers.Append(HttpCommon.VaryHeader, HttpCommon.OriginHeader);
                originAllowed = false;
                if (origin is not null)
                {
                    //browsers send the origin as scheme://host[:port] and ApiClient sends the host, an allowed value can be either, case doesn't matter
                    var originHost = Uri.TryCreate(origin, UriKind.Absolute, out var originUri) ? originUri.Host : null;
                    foreach (var allowOrigin in allowOrigins)
                    {
                        if (String.Equals(allowOrigin, origin, StringComparison.OrdinalIgnoreCase) || (originHost is not null && String.Equals(allowOrigin, originHost, StringComparison.OrdinalIgnoreCase)))
                        {
                            originAllowed = true;
                            break;
                        }
                    }

                    if (originAllowed)
                        context.Response.Headers.Append(HttpCommon.AccessControlAllowOriginHeader, origin);
                }
            }
            context.Response.Headers.Append(HttpCommon.AccessControlAllowMethodsHeader, "*");
            context.Response.Headers.Append(HttpCommon.AccessControlAllowHeadersHeader, "*");

            if (context.Request.Method == "OPTIONS")
                return;

            if (!originAllowed)
            {
                log?.Warn($"{nameof(CqrsApiGatewayMiddleware)} Origin Not Allowed {origin}");
                context.Response.StatusCode = 401;
                return;
            }

            var requestContentType = context.Request.ContentType;
            ContentType contentType;
            if (requestContentType is not null && requestContentType.StartsWith("application/octet-stream"))
                contentType = ContentType.Bytes;
            else if (requestContentType is not null && requestContentType.StartsWith("application/jsonnameless"))
                contentType = ContentType.JsonNameless;
            else if (requestContentType is not null && requestContentType.StartsWith("application/json"))
                contentType = ContentType.Json;
            else
            {
                context.Response.StatusCode = 400;
                return;
            }

            if (contentType != serializer.ContentType)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var accepts = (string?)context.Request.Headers.Accept;
            ContentType? acceptContentType;
            if (accepts is not null)
            {
                if (accepts.StartsWith("application/octet-stream"))
                    acceptContentType = ContentType.Bytes;
                else if (accepts.StartsWith("application/jsonnameless"))
                    acceptContentType = ContentType.JsonNameless;
                else if (accepts.StartsWith("application/json"))
                    acceptContentType = ContentType.Json;
                else
                    acceptContentType = null;
            }
            else
            {
                acceptContentType = null;
            }

            ISerializer acceptSerializer = serializer;
            if (acceptContentType.HasValue && acceptContentType.Value != serializer.ContentType)
            {
                if (acceptContentType == ContentType.JsonNameless && serializer.ContentType == ContentType.Json)
                {
                    //special case
                    namelessSerializer ??= new ZerraJsonSerializer(new Serialization.Json.JsonSerializerOptions()
                    {
                        Nameless = true
                    });
                    acceptSerializer = namelessSerializer;
                }
                else
                {
                    context.Response.StatusCode = 400;
                    return;
                }
            }

            var inHandlerContext = false;
            try
            {
                //inside the try so an authorization SecurityException returns a 401 like the other CQRS servers
                if (authorizer is not null)
                {
                    var headers = context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToList());
                    authorizer.Authorize(headers);
                }

                var data = await serializer.DeserializeAsync<ApiRequestData>(context.Request.Body, context.RequestAborted);
                if (data is null)
                    throw new Exception("Invalid Request");

                inHandlerContext = true;
                var response = await ApiServerHandler.HandleRequestAsync(bus, serializer, acceptSerializer, data, context.RequestAborted);
                inHandlerContext = false;

                if (response is null)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                if (response.Void)
                    return;

                if (response.Bytes is not null)
                {
                    context.Response.ContentType = acceptSerializer.ContentType switch
                    {
                        ContentType.Bytes => "application/octet-stream",
                        ContentType.JsonNameless => "application/jsonnameless; charset=utf-8",
                        ContentType.Json => "application/json; charset=utf-8",
                        _ => throw new NotImplementedException(),
                    };

                    context.Response.ContentLength = response.Bytes.Length;
                    await context.Response.Body.WriteAsync(response.Bytes.AsMemory(0, response.Bytes.Length), context.RequestAborted);
                }
                else if (response.Stream is not null)
                {
                    //the stream may hold a pooled connection to another service, it's only released on dispose
                    await using (response.Stream)
                    {
                        context.Response.ContentType = "application/octet-stream";

                        await response.Stream.CopyToAsync(context.Response.Body, context.RequestAborted);
                        await context.Response.Body.FlushAsync(context.RequestAborted);
                    }
                }
                else
                {
                    throw new NotImplementedException("Should not happen");
                }
            }
            catch (OperationCanceledException ex)
            {
                log?.Error(ex);
            }
            catch (Exception ex)
            {
                if (!inHandlerContext)
                    log?.Error(ex);

                ex = ex.GetBaseException();

                //a SecurityException from a handler in another service arrives as a RemoteServiceException
                if (ex is SecurityException || (ex is RemoteServiceException remote && remote.ErrorType == nameof(SecurityException)))
                    context.Response.StatusCode = 401;
                else
                    context.Response.StatusCode = 500;

                context.Response.ContentType = serializer.ContentType switch
                {
                    ContentType.Bytes => "application/octet-stream",
                    //ContentType.JsonNameless => "application/jsonnameless; charset=utf-8", can't deserialize nameless in JavaScript without knowing Exception model
                    ContentType.Json or ContentType.JsonNameless => "application/json; charset=utf-8",
                    _ => throw new NotImplementedException(),
                };

                await ExceptionSerializer.SerializeAsync(serializer, context.Response.Body, ex, context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        }
    }
}