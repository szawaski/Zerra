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
using Zerra.Serialization.Json;

namespace Zerra.Web
{
    public sealed class KestrelCQRSServerMiddleware : IDisposable
    {
        private readonly RequestDelegate requestDelegate;
        private readonly SymmetricConfig symmetricConfig;
        private readonly KestrelCQRSServerLinkedSettings settings;

        public KestrelCQRSServerMiddleware(RequestDelegate requestDelegate, SymmetricConfig symmetricConfig, KestrelCQRSServerLinkedSettings settings)
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
                context.Response.Headers.Append(HttpCommon.AccessControlAllowOriginHeader, settings.AllowOriginsString);
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
                if (!context.Request.Headers.TryGetValue(HttpCommon.OriginHeader, out var originRequestHeaderValue))
                {
                    context.Response.StatusCode = 401;
                    return;
                }
                originRequestHeader = originRequestHeaderValue;

                if (settings.AllowOrigins.Contains(originRequestHeader))
                {
                    _ = Log.WarnAsync($"{nameof(KestrelCQRSServerMiddleware)} Origin Not Allowed {originRequestHeader}");
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

                    data = await ContentTypeSerializer.DeserializeAsync<CqrsRequestData>(contentType.Value, body);

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
                    var headers = context.Request.Headers.ToDictionary<KeyValuePair<string, StringValues>, string, IList<string?>>(x => x.Key, x => x.Value.ToArray());
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
                    if (settings.ProviderHandlerAsync is null) throw new InvalidOperationException($"{nameof(KestrelCQRSServerMiddleware)} is not setup");

                    if (String.IsNullOrWhiteSpace(data.ProviderMethod)) throw new Exception("Invalid Request");
                    if (data.ProviderArguments is null) throw new Exception("Invalid Request");
                    if (String.IsNullOrWhiteSpace(data.Source)) throw new Exception("Invalid Request");

                    var providerType = Discovery.GetTypeFromName(data.ProviderType);

                    if (!settings.InterfaceTypes.TryGetValue(providerType, out throttle))
                        throw new Exception($"{providerType.GetNiceName()} is not registered with {nameof(KestrelCQRSServerMiddleware)}");

                    await throttle.WaitAsync();

                    inHandlerContext = true;
                    var result = await settings.ProviderHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments, data.Source, false);
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
                                        await responseBodyCryptoStream.WriteAsync(buffer.Slice(0, bytesRead));
                                    await responseBodyCryptoStream.FlushFinalBlockAsync();
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
                                    await responseBodyStream.WriteAsync(buffer.Slice(0, bytesRead));
                                await responseBodyStream.FlushAsync();
                            }
                        }
                        finally
                        {
                            ArrayPoolHelper<byte>.Return(bufferOwner);
                        }
                        await context.Response.Body.FlushAsync();

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

                                await ContentTypeSerializer.SerializeAsync(contentType.Value, responseBodyCryptoStream, result.Model);
                                await responseBodyCryptoStream.FlushFinalBlockAsync();
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
                            await ContentTypeSerializer.SerializeAsync(contentType.Value, responseBodyStream, result.Model);
                            await responseBodyStream.FlushAsync();
                            return;
                        }
                    }
                }
                else if (!String.IsNullOrWhiteSpace(data.MessageType))
                {
                    if (settings.ReceiveCounter is null) throw new InvalidOperationException($"{nameof(KestrelCQRSServerMiddleware)} is not setup");
                    if (data.MessageData is null) throw new Exception("Invalid Request");
                    if (String.IsNullOrWhiteSpace(data.Source)) throw new Exception("Invalid Request");

                    isCommand = true;
                    if (!settings.ReceiveCounter.BeginReceive())
                        throw new Exception("Cannot receive any more commands");

                    var messageType = Discovery.GetTypeFromName(data.MessageType);

                    if (!settings.CommandTypes.TryGetValue(messageType, out throttle))
                        throw new Exception($"{messageType.GetNiceName()} is not registered with {nameof(KestrelCQRSServerMiddleware)}");

                    await throttle.WaitAsync();

                    _ = settings.ReceiveCounter.BeginReceive();

                    var command = (ICommand?)JsonSerializer.Deserialize(messageType, data.MessageData);
                    if (command is null)
                        throw new Exception("Invalid Request");

                    bool hasResult;
                    object? result = null;

                    inHandlerContext = true;
                    if (data.MessageResult == true)
                    {
                        if (settings.HandlerWithResultAwaitAsync is null) throw new InvalidOperationException($"{nameof(KestrelCQRSServerMiddleware)} is not setup");
                        result = await settings.HandlerWithResultAwaitAsync(command, data.Source, false);
                        hasResult = true;
                    }
                    else if (data.MessageAwait == true)
                    {
                        if (settings.HandlerAwaitAsync is null) throw new InvalidOperationException($"{nameof(KestrelCQRSServerMiddleware)} is not setup");
                        await settings.HandlerAwaitAsync(command, data.Source, false);
                        hasResult = false;
                    }
                    else
                    {
                        if (settings.HandlerAsync is null) throw new InvalidOperationException($"{nameof(KestrelCQRSServerMiddleware)} is not setup");
                        await settings.HandlerAsync(command, data.Source, false);
                        hasResult = false;
                    }
                    inHandlerContext = false;

                    //Response Header
                    context.Response.Headers.Append(HttpCommon.ProviderTypeHeader, data.ProviderType);
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

                        var responseBodyStream = context.Response.Body;
                        if (symmetricConfig is not null)
                        {
                            var responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);
                            await ContentTypeSerializer.SerializeAsync(contentType.Value, responseBodyCryptoStream, result);
                            await responseBodyCryptoStream.FlushFinalBlockAsync();
                            await responseBodyCryptoStream.DisposeAsync();
                        }
                        else
                        {
                            await ContentTypeSerializer.SerializeAsync(contentType.Value, responseBodyStream, result);
                            await responseBodyStream.FlushAsync();
                        }
                    }

                    return;
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
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

                var responseBodyStream = context.Response.Body;
                if (symmetricConfig is not null)
                {
                    var responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);
                    await ContentTypeSerializer.SerializeExceptionAsync(contentType.Value, responseBodyCryptoStream, ex);
                    await responseBodyCryptoStream.FlushFinalBlockAsync();
                    await responseBodyCryptoStream.DisposeAsync();
                }
                else
                {
                    await ContentTypeSerializer.SerializeExceptionAsync(contentType.Value, responseBodyStream, ex);
                    await responseBodyStream.FlushAsync();
                }
            }
            finally
            {
                if (throttle is not null)
                {
                    if (isCommand)
                    {
                        settings.ReceiveCounter!.CompleteReceive(throttle);
                    }
                    else
                    {
                        throttle.Release();
                    }
                }
            }
        }
    }
}