// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Buffers;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Reflection;

namespace Zerra.Web
{
    public sealed class KestrelCqrsServerMiddleware : IDisposable
    {
        private readonly RequestDelegate requestDelegate;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly KestrelCqrsServerLinkedSettings settings;

        public KestrelCqrsServerMiddleware(RequestDelegate requestDelegate, SymmetricConfig? symmetricConfig, KestrelCqrsServerLinkedSettings settings)
        {
            this.requestDelegate = requestDelegate;
            this.symmetricConfig = symmetricConfig;
            this.settings = settings;
        }

        public void Dispose()
        {
            settings.Dispose();
        }

        public async Task Invoke(HttpContext context)
        {
            if ((!String.IsNullOrWhiteSpace(settings.Route) && context.Request.Path != settings.Route) || (context.Request.Method != "POST" && context.Request.Method != "OPTIONS"))
            {
                await requestDelegate(context);
                return;
            }

            if (context.Request.Method == "OPTIONS")
            {
                //browsers accept one origin or * so an allowed request origin is echoed, a disallowed one gets none
                if (settings.AllowOrigins is null)
                {
                    context.Response.Headers.Append(HttpCommon.AccessControlAllowOriginHeader, "*");
                }
                else
                {
                    context.Response.Headers.Append(HttpCommon.VaryHeader, HttpCommon.OriginHeader);
                    string? preflightOrigin = context.Request.Headers[HttpCommon.OriginHeader];
                    if (preflightOrigin is not null)
                    {
                        //browsers send the origin as scheme://host[:port] and KestrelCqrsClient sends the host, an allowed value can be either, case doesn't matter
                        var preflightOriginHost = Uri.TryCreate(preflightOrigin, UriKind.Absolute, out var preflightOriginUri) ? preflightOriginUri.Host : null;
                        foreach (var allowOrigin in settings.AllowOrigins)
                        {
                            if (String.Equals(allowOrigin, preflightOrigin, StringComparison.OrdinalIgnoreCase) || (preflightOriginHost is not null && String.Equals(allowOrigin, preflightOriginHost, StringComparison.OrdinalIgnoreCase)))
                            {
                                context.Response.Headers.Append(HttpCommon.AccessControlAllowOriginHeader, preflightOrigin);
                                break;
                            }
                        }
                    }
                }
                context.Response.Headers.Append(HttpCommon.AccessControlAllowMethodsHeader, "*");
                context.Response.Headers.Append(HttpCommon.AccessControlAllowHeadersHeader, "*");
                return;
            }

            var requestContentType = context.Request.ContentType;
            ContentType? contentType;
            if (requestContentType is not null)
            {
                if (requestContentType.StartsWith("application/octet-stream"))
                    contentType = ContentType.Bytes;
                else if (requestContentType.StartsWith("application/jsonnameless"))
                    contentType = ContentType.JsonNameless;
                else if (requestContentType.StartsWith("application/json"))
                    contentType = ContentType.Json;
                else
                    contentType = null;
            }
            else
            {
                contentType = null;
            }

            if (!contentType.HasValue)
            {
                context.Response.StatusCode = 400;
                return;
            }

            if (contentType != settings.ContentType)
            {
                context.Response.StatusCode = 400;
                return;
            }

            string? providerTypeRequestHeader;
            if (!context.Request.Headers.TryGetValue(HttpCommon.ProviderTypeHeader, out var providerTypeRequestHeaderValue))
            {
                context.Response.StatusCode = 400;
                return;
            }
            providerTypeRequestHeader = providerTypeRequestHeaderValue;

            string? originRequestHeader;
            if (settings.AllowOrigins is not null)
            {
                //the allow origin echoes the request origin so caches must vary by it
                context.Response.Headers.Append(HttpCommon.VaryHeader, HttpCommon.OriginHeader);

                if (!context.Request.Headers.TryGetValue(HttpCommon.OriginHeader, out var originRequestHeaderValue))
                {
                    context.Response.StatusCode = 401;
                    return;
                }
                originRequestHeader = originRequestHeaderValue;

                //browsers send the origin as scheme://host[:port] and KestrelCqrsClient sends the host, an allowed value can be either, case doesn't matter
                var originAllowed = false;
                if (originRequestHeader is not null)
                {
                    var originHost = Uri.TryCreate(originRequestHeader, UriKind.Absolute, out var originUri) ? originUri.Host : null;
                    foreach (var allowOrigin in settings.AllowOrigins)
                    {
                        if (String.Equals(allowOrigin, originRequestHeader, StringComparison.OrdinalIgnoreCase) || (originHost is not null && String.Equals(allowOrigin, originHost, StringComparison.OrdinalIgnoreCase)))
                        {
                            originAllowed = true;
                            break;
                        }
                    }
                }

                if (!originAllowed)
                {
                    _ = Log.WarnAsync($"{nameof(KestrelCqrsServerMiddleware)} Origin Not Allowed {originRequestHeader}");
                    context.Response.StatusCode = 401;
                    return;
                }
            }
            else
            {
                originRequestHeader = "*";
            }

            var isCommand = false;
            var inHandlerContext = false;
            SemaphoreSlim? throttle = null;
            try
            {
                Stream body = context.Request.Body;
                CqrsRequestData? data;
                try
                {
                    if (symmetricConfig is not null)
                        body = SymmetricEncryptor.Decrypt(symmetricConfig, body, false);

                    data = await ContentTypeSerializer.DeserializeAsync<CqrsRequestData>(contentType.Value, body, context.RequestAborted);

                    if (data is null)
                        throw new Exception("Invalid Request");
                }
                finally
                {
                    body.Dispose();
                }

                //Authorize
                //------------------------------------------------------------------------------------------------------------
                if (settings.Authorizer is not null)
                {
                    var headers = context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToList());
                    settings.Authorizer.Authorize(headers);
                }
                else
                {
                    if (data.Claims is not null)
                    {
                        var claimsIdentity = new ClaimsIdentity(data.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                        Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                    }
                    else
                    {
                        Thread.CurrentPrincipal = null;
                    }
                }

                //Process and Respond
                //----------------------------------------------------------------------------------------------------
                if (!String.IsNullOrWhiteSpace(data.ProviderType))
                {
                    if (settings.ProviderHandlerAsync is null) throw new InvalidOperationException($"{nameof(KestrelCqrsServerMiddleware)} is not setup");

                    if (String.IsNullOrWhiteSpace(data.ProviderMethod)) throw new Exception("Invalid Request");
                    if (data.ProviderArguments is null) throw new Exception("Invalid Request");
                    if (String.IsNullOrWhiteSpace(data.Source)) throw new Exception("Invalid Request");

                    var providerType = Discovery.GetTypeFromName(data.ProviderType);

                    if (!settings.Types.TryGetValue(providerType, out var providerThrottle))
                        throw new Exception($"{providerType.GetNiceName()} is not registered with {nameof(KestrelCqrsServerMiddleware)}");

                    await providerThrottle.WaitAsync(context.RequestAborted);
                    throttle = providerThrottle; //only released once taken, a request canceled while waiting has nothing to release

                    inHandlerContext = true;
                    var result = await settings.ProviderHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments, data.Source, false, context.RequestAborted);
                    inHandlerContext = false;

                    //Response Header
                    context.Response.Headers.Append(HttpCommon.AccessControlAllowOriginHeader, originRequestHeader);
                    context.Response.Headers.Append(HttpCommon.AccessControlAllowMethodsHeader, "*");
                    context.Response.Headers.Append(HttpCommon.AccessControlAllowHeadersHeader, "*");
                    switch (contentType.Value)
                    {
                        case ContentType.Bytes:
                            context.Response.Headers.Append(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeBytes);
                            break;
                        case ContentType.Json:
                            context.Response.Headers.Append(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeJson);
                            break;
                        case ContentType.JsonNameless:
                            context.Response.Headers.Append(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeJsonNameless);
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    //Response Body
                    var responseBodyStream = context.Response.Body;
                    int bytesRead;
                    if (result.Stream is not null)
                    {
                        var bufferOwner = ArrayPoolHelper<byte>.Rent(HttpCommon.BufferLength);
                        var buffer = bufferOwner.AsMemory();

                        try
                        {
                            if (symmetricConfig is not null)
                            {
                                CryptoFlushStream? responseBodyCryptoStream = null;
                                try
                                {
                                    responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

                                    while ((bytesRead = await result.Stream.ReadAsync(buffer)) > 0)
                                        await responseBodyCryptoStream.WriteAsync(buffer.Slice(0, bytesRead), context.RequestAborted);
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(context.RequestAborted);
                                }
                                finally
                                {
                                    if (responseBodyCryptoStream is not null)
                                    {
                                        await responseBodyCryptoStream.DisposeAsync();
                                    }
                                }
                            }
                            else
                            {
                                while ((bytesRead = await result.Stream.ReadAsync(buffer)) > 0)
                                    await responseBodyStream.WriteAsync(buffer.Slice(0, bytesRead), context.RequestAborted);
                                await responseBodyStream.FlushAsync(context.RequestAborted);
                            }
                        }
                        finally
                        {
                            ArrayPoolHelper<byte>.Return(bufferOwner);
                            await result.Stream.DisposeAsync(); //the handler's stream is done once sent
                        }
                        await context.Response.Body.FlushAsync(context.RequestAborted);

                        return;
                    }
                    else
                    {
                        if (symmetricConfig is not null)
                        {
                            CryptoFlushStream? responseBodyCryptoStream = null;
                            try
                            {
                                responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

                                await ContentTypeSerializer.SerializeAsync(contentType.Value, responseBodyCryptoStream, result.Model, context.RequestAborted);
                                await responseBodyCryptoStream.FlushFinalBlockAsync(context.RequestAborted);
                                return;
                            }
                            finally
                            {
                                if (responseBodyCryptoStream is not null)
                                {
                                    await responseBodyCryptoStream.DisposeAsync();
                                }
                            }
                        }
                        else
                        {
                            await ContentTypeSerializer.SerializeAsync(contentType.Value, responseBodyStream, result.Model, context.RequestAborted);
                            await responseBodyStream.FlushAsync(context.RequestAborted);
                            return;
                        }
                    }
                }
                else if (!String.IsNullOrWhiteSpace(data.MessageType))
                {
                    if (data.MessageData is null) throw new Exception("Invalid Request");
                    if (String.IsNullOrWhiteSpace(data.Source)) throw new Exception("Invalid Request");

                    var messageType = Discovery.GetTypeFromName(data.MessageType);
                    var typeDetail = TypeAnalyzer.GetTypeDetail(messageType);

                    if (!settings.Types.TryGetValue(messageType, out var messageThrottle))
                        throw new Exception($"{messageType.GetNiceName()} is not registered with {nameof(KestrelCqrsServerMiddleware)}");

                    await messageThrottle.WaitAsync(context.RequestAborted);
                    throttle = messageThrottle; //only released once taken, a request canceled while waiting has nothing to release

                    bool hasResult;
                    object? result = null;

                    if (typeDetail.Interfaces.Contains(typeof(ICommand)))
                    {
                        if (settings.CommandCounter is null) throw new InvalidOperationException($"{nameof(KestrelCqrsServerCommandConsumer)} is not setup");
                        isCommand = true;

                        if (!settings.CommandCounter.BeginReceive())
                            throw new Exception("Cannot receive any more commands");

                        var command = (ICommand?)ContentTypeSerializer.Deserialize(contentType.Value, messageType, data.MessageData); //the client serializes with the request content type
                        if (command is null)
                            throw new Exception("Invalid Request");

                        inHandlerContext = true;
                        if (data.MessageResult == true)
                        {
                            if (settings.CommandHandlerWithResultAwaitAsync is null) throw new InvalidOperationException($"{nameof(KestrelCqrsServerMiddleware)} is not setup");
                            result = await settings.CommandHandlerWithResultAwaitAsync(command, data.Source, false, context.RequestAborted);
                            hasResult = true;
                        }
                        else if (data.MessageAwait == true)
                        {
                            if (settings.CommandHandlerAwaitAsync is null) throw new InvalidOperationException($"{nameof(KestrelCqrsServerMiddleware)} is not setup");
                            await settings.CommandHandlerAwaitAsync(command, data.Source, false, context.RequestAborted);
                            hasResult = false;
                        }
                        else
                        {
                            if (settings.CommandHandlerAsync is null) throw new InvalidOperationException($"{nameof(KestrelCqrsServerMiddleware)} is not setup");
                            await settings.CommandHandlerAsync(command, data.Source, false, default);
                            hasResult = false;
                        }
                        inHandlerContext = false;
                    }
                    else if (typeDetail.Interfaces.Contains(typeof(IEvent)))
                    {
                        var @event = (IEvent?)ContentTypeSerializer.Deserialize(contentType.Value, messageType, data.MessageData);
                        if (@event is null)
                            throw new Exception("Invalid Request");

                        inHandlerContext = true;
                        if (settings.EventHandlerAsync is null) throw new InvalidOperationException($"{nameof(KestrelCqrsServerMiddleware)} is not setup");
                        await settings.EventHandlerAsync(@event, data.Source, false);
                        hasResult = false;
                        inHandlerContext = false;
                    }
                    else
                    {
                        throw new CqrsNetworkException($"Unhandled Message Type {messageType.FullName}");
                    }

                    //Response Header
                    context.Response.Headers.Append(HttpCommon.ProviderTypeHeader, data.MessageType);
                    context.Response.Headers.Append(HttpCommon.AccessControlAllowOriginHeader, originRequestHeader);
                    context.Response.Headers.Append(HttpCommon.AccessControlAllowMethodsHeader, "*");
                    context.Response.Headers.Append(HttpCommon.AccessControlAllowHeadersHeader, "*");
                    switch (contentType.Value)
                    {
                        case ContentType.Bytes:
                            context.Response.Headers.Append(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeBytes);
                            break;
                        case ContentType.Json:
                            context.Response.Headers.Append(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeJson);
                            break;
                        case ContentType.JsonNameless:
                            context.Response.Headers.Append(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeJsonNameless);
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    if (hasResult)
                    {
                        var responseBodyStream = context.Response.Body;
                        if (symmetricConfig is not null)
                        {
                            var responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);
                            await ContentTypeSerializer.SerializeAsync(contentType.Value, responseBodyCryptoStream, result, context.RequestAborted);
                            await responseBodyCryptoStream.FlushFinalBlockAsync(context.RequestAborted);
                            await responseBodyCryptoStream.DisposeAsync();
                        }
                        else
                        {
                            await ContentTypeSerializer.SerializeAsync(contentType.Value, responseBodyStream, result, context.RequestAborted);
                            await responseBodyStream.FlushAsync(context.RequestAborted);
                        }
                    }

                    return;
                }
                else
                {
                    context.Response.StatusCode = 400;
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

                context.Response.StatusCode = 500;

                //Response Header
                context.Response.Headers.Append(HttpCommon.AccessControlAllowOriginHeader, originRequestHeader);
                context.Response.Headers.Append(HttpCommon.AccessControlAllowMethodsHeader, "*");
                context.Response.Headers.Append(HttpCommon.AccessControlAllowHeadersHeader, "*");
                switch (contentType.Value)
                {
                    case ContentType.Bytes:
                        context.Response.Headers.Append(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeBytes);
                        break;
                    case ContentType.Json:
                        context.Response.Headers.Append(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeJson);
                        break;
                    case ContentType.JsonNameless:
                        context.Response.Headers.Append(HttpCommon.ContentTypeHeader, HttpCommon.ContentTypeJsonNameless);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                //the error body is built first so it can be sent with its length and the connection closed after
                //5.3 clients never see the error status and read the body as a result, this makes them fail on it instead of returning whatever it decodes to
                var errorBody = new MemoryStream();
                if (symmetricConfig is not null)
                {
                    var errorBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, errorBody, true, true); //the body is still needed after, leave it open
                    await ContentTypeSerializer.SerializeExceptionAsync(contentType.Value, errorBodyCryptoStream, ex, context.RequestAborted);
                    await errorBodyCryptoStream.FlushFinalBlockAsync();
                    await errorBodyCryptoStream.DisposeAsync();
                }
                else
                {
                    await ContentTypeSerializer.SerializeExceptionAsync(contentType.Value, errorBody, ex, context.RequestAborted);
                }

                context.Response.ContentLength = errorBody.Length;
                context.Response.Headers.Connection = "close";
                await context.Response.Body.WriteAsync(errorBody.GetBuffer().AsMemory(0, (int)errorBody.Length), context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
            }
            finally
            {
                if (throttle is not null)
                {
                    if (isCommand && settings.CommandCounter is not null)
                        settings.CommandCounter.CompleteReceive(throttle);
                    else
                        throttle.Release();
                }
            }
        }
    }
}