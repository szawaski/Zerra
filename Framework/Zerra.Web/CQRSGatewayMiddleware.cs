// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using System;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Zerra.CQRS.Network;

namespace Zerra.Web
{
    public class CQRSGatewayMiddleware
    {
        private readonly RequestDelegate requestDelegate;
        private readonly string route;

        public CQRSGatewayMiddleware(RequestDelegate requestDelegate, string route)
        {
            this.requestDelegate = requestDelegate;
            this.route = route;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path != route || context.Request.Method != "POST")
            {
                await requestDelegate(context);
                return;
            }

            var requestContentType = context.Request.ContentType;
            ContentType contentType;
            if (requestContentType.StartsWith("application/octet-stream"))
                contentType = ContentType.Bytes;
            else if (requestContentType.StartsWith("application/jsonnameless"))
                contentType = ContentType.JsonNameless;
            else if (requestContentType.StartsWith("application/json"))
                contentType = ContentType.Json;
            else
            {
                context.Response.StatusCode = 400;
                return;
            }

            var accepts = (string)context.Request.Headers["Accept"];
            ContentType? acceptContentType;
            if (accepts.StartsWith("application/octet-stream"))
                acceptContentType = ContentType.Bytes;
            else if (accepts.StartsWith("application/jsonnameless"))
                acceptContentType = ContentType.JsonNameless;
            else if (accepts.StartsWith("application/json"))
                acceptContentType = ContentType.Json;
            else
                acceptContentType = null;

            try
            {
                var data = await ContentTypeSerializer.DeserializeAsync<CQRSRequestData>(contentType, context.Request.Body);

                var response = await ApiServerHandler.HandleRequestAsync(acceptContentType, data);
                if (response == null)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                if (response.Void)
                {
                    return;
                }

                switch (acceptContentType)
                {
                    case ContentType.Bytes: context.Response.ContentType = "application/octet-stream"; break;
                    case ContentType.JsonNameless: context.Response.ContentType = "application/jsonnameless; charset=utf-8"; break;
                    case ContentType.Json: context.Response.ContentType = "application/json; charset=utf-8"; break;
                    default: throw new NotImplementedException();
                };

                if (response.Bytes != null)
                {
                    context.Response.ContentLength = response.Bytes.Length;
#if NETSTANDARD2_0
                    await context.Response.Body.WriteAsync(response.Bytes, 0, response.Bytes.Length);
#else
                    await context.Response.Body.WriteAsync(response.Bytes.AsMemory(0, response.Bytes.Length));
#endif
                }
                else if (response.Stream != null)
                {
                    await response.Stream.CopyToAsync(context.Response.Body);
                }
                else
                {
                    throw new NotImplementedException("Should not happen");
                }

                await context.Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                ex = ex.GetBaseException();

                if (ex is SecurityException)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "text/plain";
                    var errorBytes = Encoding.UTF8.GetBytes(!String.IsNullOrEmpty(ex.Message) ? ex.Message : "Unauthorized");
                    context.Response.ContentLength = errorBytes.Length;
#if NETSTANDARD2_0
                    await context.Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
#else
                    await context.Response.Body.WriteAsync(errorBytes.AsMemory(0, errorBytes.Length));
#endif
                    await context.Response.Body.FlushAsync();
                }
                else
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/plain";
                    var errorBytes = Encoding.UTF8.GetBytes(!String.IsNullOrEmpty(ex.Message) ? ex.Message : ex.GetType().Name);
                    context.Response.ContentLength = errorBytes.Length;
#if NETSTANDARD2_0
                    await context.Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
#else
                    await context.Response.Body.WriteAsync(errorBytes.AsMemory(0, errorBytes.Length));
#endif
                    await context.Response.Body.FlushAsync();
                }
            }
        }
    }
}