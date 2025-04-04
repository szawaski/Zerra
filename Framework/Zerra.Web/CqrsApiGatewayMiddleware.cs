// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Zerra.CQRS.Network;
using Zerra.Logging;

namespace Zerra.Web
{
    public sealed class CqrsApiGatewayMiddleware
    {
        private readonly RequestDelegate requestDelegate;
        private readonly string? route;
        private readonly ICqrsAuthorizer? authorizer;

        public CqrsApiGatewayMiddleware(RequestDelegate requestDelegate, string? route)
        {
            this.route = route;
            this.requestDelegate = requestDelegate;
        }
        public CqrsApiGatewayMiddleware(RequestDelegate requestDelegate, ICqrsAuthorizer? authorizer, string? route)
        {
            this.authorizer = authorizer;
            this.route = route;
            this.requestDelegate = requestDelegate;
        }

        public async Task Invoke(HttpContext context)
        {
            if ((!String.IsNullOrWhiteSpace(route) && context.Request.Path != route) || (context.Request.Method != "POST" && context.Request.Method != "OPTIONS"))
            {
                await requestDelegate(context);
                return;
            }

            context.Response.Headers.Append(HttpCommon.AccessControlAllowOriginHeader, "*");
            context.Response.Headers.Append(HttpCommon.AccessControlAllowMethodsHeader, "*");
            context.Response.Headers.Append(HttpCommon.AccessControlAllowHeadersHeader, "*");

            if (context.Request.Method == "OPTIONS")
                return;

            var requestContentType = context.Request.ContentType;
            ContentType contentType;
            if (requestContentType is not null)
            {
                if (requestContentType.StartsWith("application/octet-stream"))
                    contentType = ContentType.Bytes;
                else if (requestContentType.StartsWith("application/jsonnameless"))
                    contentType = ContentType.JsonNameless;
                else if (requestContentType.StartsWith("application/json"))
                    contentType = ContentType.Json;
                else
                    throw new Exception("Invalid Request");
            }
            else
            {
                throw new Exception("Invalid Request");
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

            if (authorizer is not null)
            {
                var headers = context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToList());
                authorizer.Authorize(headers);
            }

            var inHandlerContext = false;
            try
            {
                var data = await ContentTypeSerializer.DeserializeAsync<ApiRequestData>(contentType, context.Request.Body, context.RequestAborted);
                if (data is null)
                    throw new Exception("Invalid Request");

                inHandlerContext = true;
                var response = await ApiServerHandler.HandleRequestAsync(acceptContentType, data, context.RequestAborted);
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
                    context.Response.ContentType = acceptContentType switch
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
                    context.Response.ContentType = "application/octet-stream";

                    await response.Stream.CopyToAsync(context.Response.Body, context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                }
                else
                {
                    throw new NotImplementedException("Should not happen");
                }
            }
            catch (OperationCanceledException ex)
            {
                _ = Log.ErrorAsync(ex);
            }
            catch (Exception ex)
            {
                if (!inHandlerContext)
                    _ = Log.ErrorAsync(ex);

                ex = ex.GetBaseException();

                if (ex is SecurityException)
                    context.Response.StatusCode = 401;
                else
                    context.Response.StatusCode = 500;

                context.Response.ContentType = acceptContentType switch
                {
                    ContentType.Bytes => "application/octet-stream",
                    //ContentType.JsonNameless => "application/jsonnameless; charset=utf-8",
                    //can't deserialize nameless in JavaScript without knowing Exception model
                    ContentType.Json or ContentType.JsonNameless or null => "application/json; charset=utf-8",
                    _ => throw new NotImplementedException(),
                };

                await ContentTypeSerializer.SerializeExceptionAsync(acceptContentType ?? ContentType.Json, context.Response.Body, ex, context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
        }
    }
}