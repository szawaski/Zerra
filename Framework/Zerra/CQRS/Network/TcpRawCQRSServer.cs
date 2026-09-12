// Copyright © KaKush LLC
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
    /// A CQRS Server using Custom TCP communication.
    /// </summary>
    public sealed class TcpRawCqrsServer : CqrsServerBase
    {
        private readonly ContentType? contentType;
        private readonly SymmetricConfig? symmetricConfig;

        /// <summary>
        /// Creates a new TCP Server
        /// </summary>
        /// <param name="contentType">The format of the body of the request and response.</param>
        /// <param name="serverUrl">The url of the server.</param>
        /// <param name="symmetricConfig">If provided, information to encrypt the data.</param>
        public TcpRawCqrsServer(ContentType? contentType, string serverUrl, SymmetricConfig? symmetricConfig)
            : base(serverUrl)
        {
            this.contentType = contentType;
            this.symmetricConfig = symmetricConfig;
        }

        /// <inheritdoc />
        protected override async Task Handle(Socket socket, CancellationToken cancellationToken)
        {
            if (throttle is null)
            {
                socket.Dispose();
                throw new InvalidOperationException($"{nameof(TcpRawCqrsServer)} is not setup");
            }

            var stream = new NetworkStream(socket, false); //one stream for the connection instead of one per request
            try
            {
                for (; ; )
                {
                    TcpRequestHeader? requestHeader = null;
                    var responseStarted = false;

                    var bufferOwner = ArrayPoolHelper<byte>.Rent(TcpRawCommon.BufferLength);
                    var buffer = bufferOwner.AsMemory();

                    Stream? requestBodyStream = null;
                    Stream? responseBodyStream = null;
                    Stream? resultStream = null; //the handler's stream, disposed once sent
                    CryptoFlushStream? responseBodyCryptoStream = null;
                    var isCommand = false;

                    var requestBodyRead = false;
                    var inHandlerContext = false;
                    var throttlerUsed = false;
                    var monitorIsCancellationRequested = false;
                    try
                    {
                        //Read Request Header
                        //------------------------------------------------------------------------------------------------------------
                        var headerPosition = 0;
                        var headerLength = 0;
                        var requestHeaderEnd = false;
                        while (!requestHeaderEnd)
                        {
                            if (headerLength == buffer.Length)
                                throw new CqrsNetworkException($"{nameof(TcpRawCqrsServer)} Header Too Long");

#if NETSTANDARD2_0
                            var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, cancellationToken);
#else
                            var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);
#endif

                            if (bytesRead == 0)
                                return; //not an abort if we haven't started receiving, simple socket disconnect
                            headerLength += bytesRead;

                            requestHeaderEnd = TcpRawCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                        }
                        requestHeader = TcpRawCommon.ReadHeader(buffer.Slice(0, headerLength), headerPosition);

                        if (!requestHeader.ContentType.HasValue || (contentType.HasValue && requestHeader.ContentType.HasValue && requestHeader.ContentType != contentType))
                        {
                            _ = Log.ErrorAsync($"{nameof(TcpRawCqrsServer)} Received Invalid Content Type {requestHeader.ContentType}");
                            throw new CqrsNetworkException("Invalid Content Type");
                        }

                        //Read Request Body
                        //------------------------------------------------------------------------------------------------------------

                        requestBodyStream = new TcpRawProtocolBodyStream(stream, requestHeader.BodyStartBuffer, false, true);

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
                        requestBodyRead = true;

                        //Authroize
                        //------------------------------------------------------------------------------------------------------------
                        if (data.Claims is not null)
                        {
                            var claimsIdentity = new ClaimsIdentity(data.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                            Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                        }
                        else
                        {
                            Thread.CurrentPrincipal = null;
                        }

                        await throttle.WaitAsync(cancellationToken);
                        throttlerUsed = true;

                        //Process and Respond
                        //----------------------------------------------------------------------------------------------------
                        if (!String.IsNullOrWhiteSpace(data.ProviderType))
                        {
                            if (providerHandlerAsync is null) throw new InvalidOperationException($"{nameof(TcpRawCqrsServer)} is not setup");

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
                                result = await this.providerHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments, data.Source, false, monitor.Token);
                            }
                            finally
                            {
                                monitorIsCancellationRequested = await monitor.DisposeAndGetIsCancellationRequestedAsync();
                            }
                            inHandlerContext = false;
                            resultStream = result.Stream;

                            if (monitorIsCancellationRequested)
                            {
                                _ = Log.ErrorAsync(new OperationCanceledException());
                                continue;
                            }

                            responseStarted = true;

                            //Response Header
                            var responseHeaderLength = TcpRawCommon.BufferHeader(buffer, data.ProviderType, requestHeader.ContentType.Value);

                            //Response Body, the header goes out with it
                            responseBodyStream = new TcpRawProtocolBodyStream(stream, null, true, true, buffer.Slice(0, responseHeaderLength));

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
                                if (commandCounter is null) throw new InvalidOperationException($"{nameof(TcpRawCqrsServer)} is not setup");
                                isCommand = true;

                                if (!commandCounter.BeginReceive())
                                    throw new CqrsNetworkException("Cannot receive any more commands");

                                var command = (ICommand?)ContentTypeSerializer.Deserialize(requestHeader.ContentType.Value, messageType, data.MessageData);
                                if (command is null)
                                    throw new Exception($"Invalid {nameof(data.MessageData)}");

                                inHandlerContext = true;
                                if (data.MessageResult)
                                {
                                    if (commandHandlerWithResultAwaitAsync is null) throw new InvalidOperationException($"{nameof(TcpRawCqrsServer)} is not setup");
                                    var monitor = new SocketAbortMonitor(socket, cancellationToken);
                                    try
                                    {
                                        result = await commandHandlerWithResultAwaitAsync(command, data.Source, false, monitor.Token);
                                    }
                                    finally
                                    {
                                        monitorIsCancellationRequested = await monitor.DisposeAndGetIsCancellationRequestedAsync();
                                    }
                                    hasResult = true;
                                }
                                else if (data.MessageAwait)
                                {
                                    if (commandHandlerAwaitAsync is null) throw new InvalidOperationException($"{nameof(TcpRawCqrsServer)} is not setup");
                                    var monitor = new SocketAbortMonitor(socket, cancellationToken);
                                    try
                                    {
                                        await commandHandlerAwaitAsync(command, data.Source, false, monitor.Token);
                                    }
                                    finally
                                    {
                                        monitorIsCancellationRequested = await monitor.DisposeAndGetIsCancellationRequestedAsync();
                                    }
                                    hasResult = false;
                                }
                                else
                                {
                                    if (commandHandlerAsync is null) throw new InvalidOperationException($"{nameof(TcpRawCqrsServer)} is not setup");
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
                                if (eventHandlerAsync is null)
                                    throw new InvalidOperationException($"{nameof(TcpRawCqrsServer)} is not setup");
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
                            var responseHeaderLength = TcpRawCommon.BufferHeader(buffer, data.MessageType, requestHeader.ContentType.Value);

                            //Response Body, the header goes out with it
                            responseBodyStream = new TcpRawProtocolBodyStream(stream, null, true, true, buffer.Slice(0, responseHeaderLength));

                            if (hasResult)
                            {
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
                                    await responseBodyStream.FlushAsync(cancellationToken); //the serializer doesn't end the body
                                }
                            }
                            else
                            {
                                //Response Body Empty, the ending goes out with the header
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

                        //the connection can only be reused if the request was completely read and nothing of the response was sent
                        if (!requestBodyRead || responseStarted || requestHeader is null || !requestHeader.ContentType.HasValue || !socket.Connected)
                        {
                            _ = Log.ErrorAsync(ex);
                            return; //aborted, network error, or unreadable request
                        }

                        //rejected before the handler, the server logs it and the caller gets the error
                        if (!inHandlerContext)
                            _ = Log.ErrorAsync(ex);

                        try
                        {
                            //Response Header for Error
                            var responseHeaderLength = TcpRawCommon.BufferErrorHeader(buffer, requestHeader.ProviderType, requestHeader.ContentType.Value);

                            //Response Body, the header goes out with it
                            responseBodyStream = new TcpRawProtocolBodyStream(stream, null, true, true, buffer.Slice(0, responseHeaderLength));
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
                                await responseBodyStream.FlushAsync(cancellationToken); //the serializer doesn't end the body
                            }
                        }
                        catch (Exception ex2)
                        {
                            _ = Log.ErrorAsync($"{nameof(TcpRawCqrsServer)} Error {socket.RemoteEndPoint}", ex2);
                            return; //the error response did not complete
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
                        if (resultStream is not null)
                        {
#if NETSTANDARD2_0
                            resultStream.Dispose();
#else
                            await resultStream.DisposeAsync();
#endif
                        }
                        ArrayPoolHelper<byte>.Return(bufferOwner);
                        if (throttlerUsed)
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
#if NETSTANDARD2_0
                stream.Dispose();
#else
                await stream.DisposeAsync();
#endif
                socket.Dispose();
            }
        }
    }
}