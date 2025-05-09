﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Net.Sockets;
using System.Threading;
using System.Security.Claims;
using System.Linq;
using Zerra.Reflection;
using Zerra.Logging;
using Zerra.Encryption;
using System.IO;
using System.Threading.Tasks;
using Zerra.Buffers;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A CQRS Server using basic HTTP communication.
    /// </summary>
    public sealed class HttpCqrsServer : CqrsServerBase
    {
        private readonly ContentType? contentType;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly string[]? allowOrigins;

        /// <summary>
        /// Creates a new HTTP Server
        /// </summary>
        /// <param name="contentType">The format of the body of the request and response.</param>
        /// <param name="serverUrl">The url of the server.</param>
        /// <param name="symmetricConfig">If provided, information to encrypt the data.</param>
        /// <param name="authorizer">An authorizer for the server to validate requests.</param>
        /// <param name="allowOrigins">CORS HTTP headers for Allow-Origins</param>
        public HttpCqrsServer(ContentType? contentType, string serverUrl, SymmetricConfig? symmetricConfig, ICqrsAuthorizer? authorizer, string[]? allowOrigins)
            : base(serverUrl)
        {
            this.contentType = contentType;
            this.symmetricConfig = symmetricConfig;
            this.authorizer = authorizer;
            if (allowOrigins is not null && !allowOrigins.Contains("*"))
                this.allowOrigins = allowOrigins.Select(x => x.ToLower()).ToArray();
            else
                allowOrigins = null;
        }

        /// <inheritdoc />
        protected override async Task Handle(Socket socket, CancellationToken cancellationToken)
        {
            if (throttle is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");

            try
            {
                for (; ; )
                {
                    HttpRequestHeader? requestHeader = null;
                    var responseStarted = false;

                    var bufferOwner = ArrayPoolHelper<byte>.Rent(HttpCommon.BufferLength);
                    var buffer = bufferOwner.AsMemory();
                    var stream = new NetworkStream(socket, false);

                    Stream? requestBodyStream = null;
                    Stream? responseBodyStream = null;
                    CryptoFlushStream? responseBodyCryptoStream = null;
                    var isCommand = false;

                    var inHandlerContext = false;
                    var throttleUsed = false;
                    var monitorIsCancellationRequested = false;
                    try
                    {
                        //Read Request Header
                        //------------------------------------------------------------------------------------------------------------
                        var headerPosition = 0;
                        var headerLength = 0;
                        var headerEnd = false;
                        while (!headerEnd)
                        {
                            if (headerLength == buffer.Length)
                                throw new CqrsNetworkException($"{nameof(HttpCqrsServer)} Header Too Long");

#if NETSTANDARD2_0
                            var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, cancellationToken);
#else
                            var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);
#endif

                            if (bytesRead == 0)
                                return; //not an abort if we haven't started receiving, simple socket disconnect
                            headerLength += bytesRead;

                            headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                        }
                        requestHeader = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);

                        if (contentType.HasValue && requestHeader.ContentType.HasValue && requestHeader.ContentType != contentType)
                        {
                            _ = Log.ErrorAsync($"{nameof(HttpCqrsServer)} Received Invalid Content Type {requestHeader.ContentType}");
                            throw new CqrsNetworkException("Invalid Content Type");
                        }

                        if (requestHeader.Preflight)
                        {
                            _ = Log.TraceAsync($"{nameof(HttpCqrsServer)} Received Preflight {socket.RemoteEndPoint}");

                            var preflightLength = HttpCommon.BufferPreflightResponse(buffer, requestHeader.Origin);
#if NETSTANDARD2_0
                            await stream.WriteAsync(bufferOwner, 0, preflightLength, cancellationToken);
#else
                            await stream.WriteAsync(buffer.Slice(0, preflightLength), cancellationToken);
#endif

                            await stream.FlushAsync(cancellationToken);
                            continue;
                        }

                        if (!requestHeader.ContentType.HasValue)
                        {
                            _ = Log.ErrorAsync($"{nameof(HttpCqrsServer)} Received Invalid Content Type {requestHeader.ContentType}");
                            throw new CqrsNetworkException("Invalid Content Type");
                        }

                        if (allowOrigins is not null && allowOrigins.Length > 0)
                        {
                            if (allowOrigins.Contains(requestHeader.Origin))
                            {
                                throw new CqrsNetworkException($"Origin Not Allowed {requestHeader.Origin}");
                            }
                        }

                        //Read Request Body
                        //------------------------------------------------------------------------------------------------------------

                        requestBodyStream = new HttpProtocolBodyStream(requestHeader.ContentLength, stream, requestHeader.BodyStartBuffer, true);

                        if (symmetricConfig is not null)
                            requestBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, requestBodyStream, false);

                        var data = await ContentTypeSerializer.DeserializeAsync<CqrsRequestData>(requestHeader.ContentType.Value, requestBodyStream, cancellationToken);
                        if (data is null)
                            throw new CqrsNetworkException("Empty request body");

#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                        requestBodyStream = null;

                        //Authorize
                        //------------------------------------------------------------------------------------------------------------
                        if (this.authorizer is not null)
                        {
                            if (requestHeader.Headers is not null)
                                this.authorizer.Authorize(requestHeader.Headers);
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

                        await throttle.WaitAsync(cancellationToken);
                        throttleUsed = true;

                        //Process and Respond
                        //----------------------------------------------------------------------------------------------------
                        if (!String.IsNullOrWhiteSpace(data.ProviderType))
                        {
                            if (providerHandlerAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");

                            if (String.IsNullOrWhiteSpace(data.ProviderMethod)) throw new Exception("Invalid Request");
                            if (data.ProviderArguments is null) throw new Exception("Invalid Request");
                            if (String.IsNullOrWhiteSpace(data.Source)) throw new Exception("Invalid Request");
                            if (requestHeader.ProviderType != data.ProviderType) throw new Exception("Invalid Request");

                            var providerType = Discovery.GetTypeFromName(data.ProviderType);
                            var typeDetail = TypeAnalyzer.GetTypeDetail(providerType);

                            if (!this.types.Contains(providerType))
                                throw new CqrsNetworkException($"Unhandled Provider Type {providerType.FullName}");

                            inHandlerContext = true;
                            RemoteQueryCallResponse result;
                            var monitor = new SocketAbortMonitor(socket, cancellationToken);
                            try
                            {
                                result = await this.providerHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments, socket.AddressFamily.ToString(), false, monitor.Token);
                            }
                            finally
                            {
                                monitorIsCancellationRequested = monitor.DisposeAndGetIsCancellationRequested();
                            }
                            inHandlerContext = false;

                            if (monitorIsCancellationRequested)
                            {
                                _ = Log.ErrorAsync(new OperationCanceledException());
                                continue;
                            }

                            responseStarted = true;

                            //Response Header
                            var responseHeaderLength = HttpCommon.BufferOkResponseHeader(buffer, requestHeader.Origin, requestHeader.ProviderType, requestHeader.ContentType.Value, null);
#if NETSTANDARD2_0
                            await stream.WriteAsync(bufferOwner, 0, responseHeaderLength);
#else
                            await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                            //Response Body
                            responseBodyStream = new HttpProtocolBodyStream(null, stream, null, false);

                            int bytesRead;
                            if (result.Stream is not null)
                            {
                                if (symmetricConfig is not null)
                                {
                                    responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

#if NETSTANDARD2_0
                                    while ((bytesRead = await result.Stream.ReadAsync(bufferOwner, 0, bufferOwner.Length, cancellationToken)) > 0)
                                        await responseBodyCryptoStream.WriteAsync(bufferOwner, 0, bytesRead, cancellationToken);
#else
                                    while ((bytesRead = await result.Stream.ReadAsync(buffer, cancellationToken)) > 0)
                                        await responseBodyCryptoStream.WriteAsync(buffer.Slice(0, bytesRead), cancellationToken);
#endif
#if NET5_0_OR_GREATER
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                    continue;
                                }
                                else
                                {
#if NETSTANDARD2_0
                                    while ((bytesRead = await result.Stream.ReadAsync(bufferOwner, 0, bufferOwner.Length, cancellationToken)) > 0)
                                        await responseBodyStream.WriteAsync(bufferOwner, 0, bytesRead, cancellationToken);
#else
                                    while ((bytesRead = await result.Stream.ReadAsync(buffer, cancellationToken)) > 0)
                                        await responseBodyStream.WriteAsync(buffer.Slice(0, bytesRead), cancellationToken);
#endif
                                    await responseBodyStream.FlushAsync(cancellationToken);
                                    continue;
                                }
                            }
                            else
                            {
                                if (symmetricConfig is not null)
                                {
                                    responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

                                    await ContentTypeSerializer.SerializeAsync(requestHeader.ContentType.Value, responseBodyCryptoStream, result.Model, cancellationToken);
#if NET5_0_OR_GREATER
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                    continue;
                                }
                                else
                                {
                                    await ContentTypeSerializer.SerializeAsync(requestHeader.ContentType.Value, responseBodyStream, result.Model, cancellationToken);
                                    await responseBodyStream.FlushAsync(cancellationToken);
                                    continue;
                                }
                            }
                        }
                        else if (!String.IsNullOrWhiteSpace(data.MessageType))
                        {
                            if (data.MessageData is null) throw new Exception("Invalid Request");
                            if (String.IsNullOrWhiteSpace(data.Source)) throw new Exception("Invalid Request");
                            if (requestHeader.ProviderType != data.MessageType) throw new Exception("Invalid Request");

                            var messageType = Discovery.GetTypeFromName(data.MessageType);
                            var typeDetail = TypeAnalyzer.GetTypeDetail(messageType);

                            if (!this.types.Contains(messageType))
                                throw new CqrsNetworkException($"Unhandled Message Type {messageType.FullName}");

                            bool hasResult;
                            object? result = null;

                            if (typeDetail.Interfaces.Contains(typeof(ICommand)))
                            {
                                if (commandCounter is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
                                isCommand = true;

                                if (!commandCounter.BeginReceive())
                                    throw new CqrsNetworkException("Cannot receive any more commands");

                                var command = (ICommand?)ContentTypeSerializer.Deserialize(requestHeader.ContentType.Value, messageType, data.MessageData);
                                if (command is null)
                                    throw new Exception($"Invalid {nameof(data.MessageData)}");

                                inHandlerContext = true;
                                if (data.MessageResult == true)
                                {
                                    if (commandHandlerWithResultAwaitAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
                                    var monitor = new SocketAbortMonitor(socket, cancellationToken);
                                    try
                                    {
                                        result = await commandHandlerWithResultAwaitAsync(command, data.Source, false, monitor.Token);
                                    }
                                    finally
                                    {
                                        monitorIsCancellationRequested = monitor.DisposeAndGetIsCancellationRequested();
                                    }
                                    hasResult = true;
                                }
                                if (data.MessageAwait == true)
                                {
                                    if (commandHandlerAwaitAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
                                    var monitor = new SocketAbortMonitor(socket, cancellationToken);
                                    try
                                    {
                                        await commandHandlerAwaitAsync(command, data.Source, false, monitor.Token);
                                    }
                                    finally
                                    {
                                        monitorIsCancellationRequested = monitor.DisposeAndGetIsCancellationRequested();
                                    }
                                    hasResult = false;
                                }
                                else
                                {
                                    if (commandHandlerAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
                                    _ = Task.Run(() => commandHandlerAsync(command, data.Source, false, default));
                                    hasResult = false;
                                }
                                inHandlerContext = false;
                            }
                            else if (typeDetail.Interfaces.Contains(typeof(IEvent)))
                            {
                                var @event = (IEvent?)ContentTypeSerializer.Deserialize(requestHeader.ContentType.Value, messageType, data.MessageData);
                                if (@event is null)
                                    throw new Exception($"Invalid {nameof(data.MessageData)}");

                                inHandlerContext = true;
                                if (eventHandlerAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
                                _ = Task.Run(() => eventHandlerAsync(@event, data.Source, false));
                                hasResult = false;
                                inHandlerContext = false;
                            }
                            else
                            {
                                throw new CqrsNetworkException($"Unhandled Message Type {messageType.FullName}");
                            }

                            if (monitorIsCancellationRequested)
                            {
                                _ = Log.ErrorAsync(new OperationCanceledException());
                                continue;
                            }

                            responseStarted = true;

                            //Response Header
                            var responseHeaderLength = HttpCommon.BufferOkResponseHeader(buffer, requestHeader.Origin, requestHeader.ProviderType, contentType, null);
#if NETSTANDARD2_0
                            await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                            await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                            if (hasResult)
                            {
                                //Response Body
                                responseBodyStream = new HttpProtocolBodyStream(null, stream, null, false);
                                if (symmetricConfig is not null)
                                {
                                    responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

                                    await ContentTypeSerializer.SerializeAsync(requestHeader.ContentType.Value, responseBodyCryptoStream, result, cancellationToken);
#if NET5_0_OR_GREATER
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                }
                                else
                                {
                                    await ContentTypeSerializer.SerializeAsync(requestHeader.ContentType.Value, responseBodyStream, result, cancellationToken);
                                }
                            }
                            else
                            {
                                //Response Body Empty
                                responseBodyStream = new HttpProtocolBodyStream(null, stream, null, false);
                                await responseBodyStream.FlushAsync(cancellationToken);
                            }

                            continue;
                        }

                        throw new CqrsNetworkException("Invalid Request");
                    }
                    catch (Exception ex)
                    {
                        if (inHandlerContext && monitorIsCancellationRequested)
                        {
                            _ = Log.ErrorAsync(new OperationCanceledException());
                            continue;
                        }

                        if (!inHandlerContext || !socket.Connected)
                        {
                            _ = Log.ErrorAsync(ex);
                            return; //aborted or network error
                        }

                        if (!responseStarted && requestHeader is not null && requestHeader.ContentType.HasValue)
                        {
                            try
                            {
                                //Response Header
                                var responseHeaderLength = HttpCommon.BufferErrorResponseHeader(buffer, requestHeader.Origin);
#if NETSTANDARD2_0
                                await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                                await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                                //Response Body
                                responseBodyStream = new HttpProtocolBodyStream(null, stream, null, false);
                                if (symmetricConfig is not null)
                                {
                                    responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

                                    await ContentTypeSerializer.SerializeExceptionAsync(requestHeader.ContentType.Value, responseBodyCryptoStream, ex, cancellationToken);
#if NET5_0_OR_GREATER
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                }
                                else
                                {
                                    await ContentTypeSerializer.SerializeExceptionAsync(requestHeader.ContentType.Value, responseBodyStream, ex, cancellationToken);
                                }
                            }
                            catch (Exception ex2)
                            {
                                _ = Log.ErrorAsync($"{nameof(HttpCqrsServer)} Error {socket.RemoteEndPoint}", ex2);
                            }
                        }
                    }
                    finally
                    {
                        if (responseBodyCryptoStream is not null)
                        {
#if NETSTANDARD2_0
                            responseBodyCryptoStream.Dispose();
#else
                            await responseBodyCryptoStream.DisposeAsync();
#endif
                        }
                        if (responseBodyStream is not null)
                        {
#if NETSTANDARD2_0
                            responseBodyStream.Dispose();
#else
                            await responseBodyStream.DisposeAsync();
#endif
                        }
                        if (requestBodyStream is not null)
                        {
#if NETSTANDARD2_0
                            requestBodyStream.Dispose();
#else
                            await requestBodyStream.DisposeAsync();
#endif
                        }
                        if (stream is not null)
                        {
#if NETSTANDARD2_0
                            stream.Dispose();
#else
                            await stream.DisposeAsync();
#endif
                        }
                        ArrayPoolHelper<byte>.Return(bufferOwner);
                        if (throttleUsed)
                        {
                            if (isCommand && commandCounter is not null)
                                commandCounter.CompleteReceive(throttle);
                            else
                                throttle.Release();
                        }
                    }
                }
            }
            finally
            {
                socket.Dispose();
            }
        }
    }
}