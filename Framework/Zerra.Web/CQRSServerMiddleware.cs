// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.IO;
using Zerra.Logging;
using Zerra.Reflection;

namespace Zerra.Web
{
    public class CQRSServerMiddleware
    {
        private readonly RequestDelegate requestDelegate;
        private readonly string route;
        private readonly NetworkType networkType;
        private readonly ContentType? contentType;
        private readonly IHttpApiAuthorizer apiAuthorizer;
        private readonly string[] allowOrigins;
        private readonly string allowOriginsString;

        private readonly Func<Type, string, string[], Task<RemoteQueryCallResponse>> providerHandlerAsync;
        private readonly Func<ICommand, Task> handlerAsync = null;
        private readonly Func<ICommand, Task> handlerAwaitAsync = null;

        public CQRSServerMiddleware(RequestDelegate requestDelegate, string route, NetworkType networkType, ContentType? contentType, IHttpApiAuthorizer apiAuthorizer, string[] allowOrigins)
        {
            this.requestDelegate = requestDelegate;
            this.route = route;
            this.networkType = networkType;
            this.contentType = contentType;

            this.apiAuthorizer = apiAuthorizer;
            if (allowOrigins != null && !allowOrigins.Contains("*"))
            {
                this.allowOrigins = allowOrigins.Select(x => x.ToLower()).ToArray();
                this.allowOriginsString = this.allowOrigins != null ? String.Join(", ", this.allowOrigins) : "*";
            }
            else
            {
                allowOrigins = null;
            }

            this.providerHandlerAsync = Bus.HandleRemoteQueryCallAsync;
            this.handlerAsync = Bus.HandleRemoteCommandDispatchAsync;
            this.handlerAwaitAsync = Bus.HandleRemoteCommandDispatchAwaitAsync;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path != route || context.Request.Method != "POST" || context.Request.Method != "OPTIONS")
            {
                await requestDelegate(context);
                return;
            }

            if (context.Request.Method != "OPTIONS")
            {
                context.Response.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, allowOriginsString);
                context.Response.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");
                context.Response.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                return;
            }

            string originRequestHeader;
            if (!context.Request.Headers.TryGetValue(HttpCommon.OriginHeader, out StringValues originRequestHeaderValue))
            {
                context.Response.StatusCode = 400;
                return;
            }
            originRequestHeader = originRequestHeaderValue;

            string providerTypeRequestHeader;
            if (!context.Request.Headers.TryGetValue(HttpCommon.ProviderTypeHeader, out StringValues providerTypeRequestHeaderValue))
            {
                context.Response.StatusCode = 400;
                return;
            }
            providerTypeRequestHeader = providerTypeRequestHeaderValue;

            try
            {

                _ = Log.TraceAsync($"{nameof(CQRSServerMiddleware)} Received {providerTypeRequestHeaderValue}");
                if (allowOrigins != null && allowOrigins.Length > 0)
                {
                    if (allowOrigins.Contains(originRequestHeader))
                    {
                        throw new Exception($"Origin Not Allowed {originRequestHeader}");
                    }
                }

                if (!Enum.TryParse(context.Request.ContentType, out ContentType contentTypeRequestHeader))
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                if (contentType.HasValue && !context.Request.ContentLength.HasValue || context.Request.ContentLength.Value == 0)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                if (contentTypeRequestHeader != contentType)
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                var data = await ContentTypeSerializer.DeserializeAsync<CQRSRequestData>(contentTypeRequestHeader, context.Request.Body);

                //Authorize
                //------------------------------------------------------------------------------------------------------------
                switch (networkType)
                {
                    case NetworkType.Internal:
                        if (data.Claims != null)
                        {
                            var claimsIdentity = new ClaimsIdentity(data.Claims.Select(x => new Claim(x.Type, x.Value)), "CQRS");
                            Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                        }
                        else
                        {
                            Thread.CurrentPrincipal = null;
                        }
                        break;
                    case NetworkType.Api:
                        if (this.apiAuthorizer != null)
                        {
                            var headers = context.Request.Headers.ToDictionary<KeyValuePair<string, StringValues>, string, IList<string>>(x => x.Key, x => x.Value.ToArray());
                            this.apiAuthorizer.Authorize(headers);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

                //Process and Respond
                //----------------------------------------------------------------------------------------------------
                if (!String.IsNullOrWhiteSpace(data.ProviderType))
                {
                    var providerType = Discovery.GetTypeFromName(data.ProviderType);
                    var typeDetail = TypeAnalyzer.GetTypeDetail(providerType);

                    //if (!this.interfaceTypes.Contains(providerType))
                    //    throw new Exception($"Unhandled Provider Type {providerType.FullName}");

                    bool exposed = typeDetail.Attributes.Any(x => x is ServiceExposedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == networkType))
                        && !typeDetail.Attributes.Any(x => x is ServiceBlockedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == networkType));
                    if (!exposed)
                        throw new Exception($"Provider {data.MessageType} is not exposed to {networkType}");

                    _ = Log.TraceAsync($"Received Call: {providerType.GetNiceName()}.{data.ProviderMethod}");

                    var result = await this.providerHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments);

                    //Response Header
                    context.Response.Headers.Add(HttpCommon.ProviderTypeHeader, data.ProviderType);
                    context.Response.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, originRequestHeader);
                    context.Response.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");
                    context.Response.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                    switch (contentTypeRequestHeader)
                    {
                        case ContentType.Bytes:
                            context.Response.Headers.Add(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeBytes);
                            break;
                        case ContentType.Json:
                            context.Response.Headers.Add(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeJson);
                            break;
                        case ContentType.JsonNameless:
                            context.Response.Headers.Add(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeJsonNameless);
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    int bytesRead;
                    if (result.Stream != null)
                    {
                        var bufferOwner = BufferArrayPool<byte>.Rent(HttpCommon.BufferLength);
                        var buffer = bufferOwner.AsMemory();
                        try
                        {
#if NETSTANDARD2_0
                            while ((bytesRead = await result.Stream.ReadAsync(bufferOwner, 0, bufferOwner.Length)) > 0)
                                await context.Response.Body.WriteAsync(bufferOwner, 0, bytesRead);
#else
                            while ((bytesRead = await result.Stream.ReadAsync(buffer)) > 0)
                                await context.Response.Body.WriteAsync(buffer.Slice(0, bytesRead));
#endif
                        }
                        finally
                        {
                            BufferArrayPool<byte>.Return(bufferOwner);
                        }
                        await context.Response.Body.FlushAsync();

                        return;
                    }
                    else
                    {
                        await ContentTypeSerializer.SerializeAsync(contentTypeRequestHeader, context.Response.Body, result.Model);
                        await context.Response.Body.FlushAsync();
                        return;
                    }
                }
                else if (!String.IsNullOrWhiteSpace(data.MessageType))
                {
                    var commandType = Discovery.GetTypeFromName(data.MessageType);
                    var typeDetail = TypeAnalyzer.GetTypeDetail(commandType);

                    if (!typeDetail.Interfaces.Contains(typeof(ICommand)))
                        throw new Exception($"Type {data.MessageType} is not a command");

                    bool exposed = typeDetail.Attributes.Any(x => x is ServiceExposedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == networkType))
                        && !typeDetail.Attributes.Any(x => x is ServiceBlockedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == networkType));
                    if (!exposed)
                        throw new Exception($"Command {data.MessageType} is not exposed to {networkType}");

                    var command = (ICommand)System.Text.Json.JsonSerializer.Deserialize(data.MessageData, commandType);

                    if (data.MessageAwait)
                        await handlerAwaitAsync(command);
                    else
                        await handlerAsync(command);

                    //Response Header
                    context.Response.Headers.Add(HttpCommon.ProviderTypeHeader, data.ProviderType);
                    context.Response.Headers.Add(HttpCommon.AccessControlAllowOriginHeader, originRequestHeader);
                    context.Response.Headers.Add(HttpCommon.AccessControlAllowMethodsHeader, "*");
                    context.Response.Headers.Add(HttpCommon.AccessControlAllowHeadersHeader, "*");
                    switch (contentTypeRequestHeader)
                    {
                        case ContentType.Bytes:
                            context.Response.Headers.Add(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeBytes);
                            break;
                        case ContentType.Json:
                            context.Response.Headers.Add(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeJson);
                            break;
                        case ContentType.JsonNameless:
                            context.Response.Headers.Add(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeJsonNameless);
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    //Response Body Empty

                    return;
                }

                context.Response.StatusCode = 400;
                return;
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync(null, ex);
                throw;
            }
        }
    }
}