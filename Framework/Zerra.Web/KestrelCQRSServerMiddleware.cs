// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using Zerra.Buffers;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Serialization;
using Zerra.Serialization.Json;
using Zerra.SourceGeneration;

namespace Zerra.Web
{
    public sealed class KestrelCqrsServerMiddleware : IDisposable
    {
        private readonly RequestDelegate requestDelegate;
        private readonly ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ILogger? log;
        private readonly KestrelCqrsServerLinkedSettings settings;

        public KestrelCqrsServerMiddleware(RequestDelegate requestDelegate, ISerializer serializer, IEncryptor? encryptor, ILogger? log, KestrelCqrsServerLinkedSettings settings)
        {
            this.requestDelegate = requestDelegate;
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.log = log;
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
                    log?.Warn($"{nameof(KestrelCqrsServerMiddleware)} Origin Not Allowed {originRequestHeader}");
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
                    if (encryptor is not null)
                        body = encryptor.Decrypt(body, false);

                    data = await serializer.DeserializeAsync<CqrsRequestData>(body, context.RequestAborted);

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

                    var providerType = TypeHelper.GetTypeFromName(data.ProviderType);

                    if (!settings.Types.TryGetValue(providerType, out throttle))
                        throw new Exception($"{providerType.GetNiceName()} is not registered with {nameof(KestrelCqrsServerMiddleware)}");

                    await throttle.WaitAsync(context.RequestAborted);

                    inHandlerContext = true;
                    var result = await settings.ProviderHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments, data.Source, false, serializer, context.RequestAborted);
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
                            if (encryptor is not null)
                            {
                                CryptoFlushStream? responseBodyCryptoStream = null;
                                try
                                {
                                    responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);

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
                        }
                        await context.Response.Body.FlushAsync(context.RequestAborted);

                        return;
                    }
                    else
                    {
                        if (encryptor is not null)
                        {
                            CryptoFlushStream? responseBodyCryptoStream = null;
                            try
                            {
                                responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);

                                await serializer.SerializeAsync(responseBodyCryptoStream, result.Model, context.RequestAborted);
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
                            await serializer.SerializeAsync(responseBodyStream, result.Model, context.RequestAborted);
                            await responseBodyStream.FlushAsync(context.RequestAborted);
                            return;
                        }
                    }
                }
                else if (!String.IsNullOrWhiteSpace(data.MessageType))
                {
                    if (data.MessageData is null) throw new Exception("Invalid Request");
                    if (String.IsNullOrWhiteSpace(data.Source)) throw new Exception("Invalid Request");

                    var messageType = TypeHelper.GetTypeFromName(data.MessageType);
                    var typeDetail = TypeAnalyzer.GetTypeDetail(messageType);

                    if (!settings.Types.TryGetValue(messageType, out throttle))
                        throw new Exception($"{messageType.GetNiceName()} is not registered with {nameof(KestrelCqrsServerMiddleware)}");

                    await throttle.WaitAsync(context.RequestAborted);

                    bool hasResult;
                    object? result = null;

                    if (typeDetail.Interfaces.Contains(typeof(ICommand)))
                    {
                        if (settings.CommandCounter is null) throw new InvalidOperationException($"{nameof(KestrelCqrsServerCommandConsumer)} is not setup");
                        isCommand = true;

                        if (!settings.CommandCounter.BeginReceive())
                            throw new Exception("Cannot receive any more commands");

                        var command = (ICommand?)JsonSerializer.Deserialize(data.MessageData, messageType);
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
                    else if (typeDetail.Interfaces.Contains(typeof(ICommand)))
                    {
                        var @event = (IEvent?)JsonSerializer.Deserialize(data.MessageData, messageType);
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
                        if (encryptor is not null)
                        {
                            var responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);
                            await serializer.SerializeAsync(responseBodyCryptoStream, result, context.RequestAborted);
                            await responseBodyCryptoStream.FlushFinalBlockAsync(context.RequestAborted);
                            await responseBodyCryptoStream.DisposeAsync();
                        }
                        else
                        {
                            await serializer.SerializeAsync(responseBodyStream, result, context.RequestAborted);
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
                log?.Error(ex);
            }
            catch (Exception ex)
            {
                if (!inHandlerContext)
                    log?.Error(ex);

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
                if (encryptor is not null)
                {
                    var responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);
                    await ExceptionSerializer.SerializeAsync(serializer, responseBodyCryptoStream, ex, context.RequestAborted);
                    await responseBodyCryptoStream.FlushFinalBlockAsync();
                    await responseBodyCryptoStream.DisposeAsync();
                }
                else
                {
                    await ExceptionSerializer.SerializeAsync(serializer, responseBodyStream, ex, context.RequestAborted);
                    await responseBodyStream.FlushAsync(context.RequestAborted);
                }
            }
            finally
            {
                if (throttle is not null)
                {
                    if (isCommand && settings.CommandCounter is not null)
                        settings.CommandCounter.CompleteReceive(throttle);
                    else
                        _ = throttle.Release();
                }
            }
        }
    }
}