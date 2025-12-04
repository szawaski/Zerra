// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net.Sockets;
using System.Security.Claims;
using Zerra.Buffers;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Serialization;
using Zerra.SourceGeneration;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A CQRS Server using Custom TCP communication.
    /// </summary>
    public sealed class TcpCqrsServer : CqrsServerBase
    {
        private readonly ISerializer serializer;
        private readonly IEncryptor? encryptor;

        /// <summary>
        /// Creates a new TCP Server
        /// </summary>
        /// <param name="contentType">The format of the body of the request and response.</param>
        /// <param name="serverUrl">The url of the server.</param>
        /// <param name="symmetricConfig">If provided, information to encrypt the data.</param>
        public TcpCqrsServer(string serverUrl, ISerializer serializer, IEncryptor? encryptor, ILog? log)
            : base(serverUrl, log)
        {
            this.serializer = serializer;
            this.encryptor = encryptor;
        }

        /// <inheritdoc />
        protected override async Task Handle(Socket socket, CancellationToken cancellationToken)
        {
            if (throttle is null) throw new InvalidOperationException($"{nameof(TcpCqrsServer)} is not setup");

            try
            {
                for (; ; )
                {
                    TcpRequestHeader? requestHeader = null;
                    var responseStarted = false;

                    var bufferOwner = ArrayPoolHelper<byte>.Rent(TcpCommon.BufferLength);
                    var buffer = bufferOwner.AsMemory();
                    var stream = new NetworkStream(socket, false);

                    Stream? requestBodyStream = null;
                    Stream? responseBodyStream = null;
                    CryptoFlushStream? responseBodyCryptoStream = null;
                    var isCommand = false;

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
                                throw new CqrsNetworkException($"{nameof(TcpCqrsServer)} Header Too Long");

#if NETSTANDARD2_0
                            var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, cancellationToken);
#else
                            var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);
#endif

                            if (bytesRead == 0)
                                return; //not an abort if we haven't started receiving, simple socket disconnect
                            headerLength += bytesRead;

                            requestHeaderEnd = TcpCommon.TryReadToHeaderEnd(buffer[..headerLength], ref headerPosition);
                        }
                        requestHeader = TcpCommon.ReadHeader(buffer[..headerLength], headerPosition);

                        if (requestHeader.ContentType.HasValue && requestHeader.ContentType != serializer.ContentType)
                        {
                            log?.Error($"{nameof(TcpCqrsServer)} Received Invalid Content Type {requestHeader.ContentType}, Expected {serializer.ContentType}");
                            throw new CqrsNetworkException($"Invalid Content Type {requestHeader.ContentType}, Expected {serializer.ContentType}");
                        }

                        //Read Request Body
                        //------------------------------------------------------------------------------------------------------------

                        requestBodyStream = new TcpProtocolBodyStream(stream, requestHeader.BodyStartBuffer, false, true);

                        if (encryptor is not null)
                            requestBodyStream = encryptor.Decrypt(requestBodyStream, false);

                        var data = await serializer.DeserializeAsync<CqrsRequestData>(requestBodyStream, cancellationToken);
                        if (data is null)
                            throw new CqrsNetworkException("Empty request body");

#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                        requestBodyStream = null;

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
                            if (providerHandlerAsync is null) throw new InvalidOperationException($"{nameof(TcpCqrsServer)} is not setup");

                            if (String.IsNullOrWhiteSpace(data.ProviderMethod)) throw new Exception("Invalid Request");
                            if (data.ProviderArguments is null) throw new Exception("Invalid Request");
                            if (String.IsNullOrWhiteSpace(data.Source)) throw new Exception("Invalid Request");
                            if (requestHeader.ProviderType != data.ProviderType) throw new Exception("Invalid Request");

                            var providerType = TypeHelper.GetTypeFromName(data.ProviderType);

                            if (!this.types.Contains(providerType))
                                throw new CqrsNetworkException($"Unhandled Provider Type {providerType.FullName}");

                            inHandlerContext = true;
                            RemoteQueryCallResponse result;
                            var monitor = new SocketAbortMonitor(socket, cancellationToken);
                            try
                            {
                                result = await this.providerHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments, data.Source, false, serializer, monitor.Token);
                            }
                            finally
                            {
                                monitorIsCancellationRequested = monitor.DisposeAndGetIsCancellationRequested();
                            }
                            inHandlerContext = false;

                            if (monitorIsCancellationRequested)
                            {
                                log?.Error(new OperationCanceledException());
                                continue;
                            }

                            responseStarted = true;

                            //Response Header
                            var responseHeaderLength = TcpCommon.BufferHeader(buffer, data.ProviderType, requestHeader.ContentType.Value);
#if NETSTANDARD2_0
                            await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                            await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                            //Response Body
                            responseBodyStream = new TcpProtocolBodyStream(stream, null, true, true);

                            int bytesRead;
                            if (result.Stream is not null)
                            {
                                if (encryptor is not null)
                                {
                                    responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);

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
                                if (encryptor is not null)
                                {
                                    responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);

                                    await serializer.SerializeAsync(responseBodyCryptoStream, result.Model, cancellationToken);
#if NET5_0_OR_GREATER
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                    continue;
                                }
                                else
                                {
                                    await serializer.SerializeAsync(responseBodyStream, result.Model, cancellationToken);
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

                            var messageType = TypeHelper.GetTypeFromName(data.MessageType);

                            if (!this.types.Contains(messageType))
                                throw new CqrsNetworkException($"Unhandled Message Type {messageType.FullName}");

                            //types should have been generated at this point so we don't need to provide types to search
                            var info = BusCommandOrEventInfo.GetByType(messageType, null);

                            bool hasResult;
                            object? result = null;

                            if (info.CommandTypes.Contains(messageType))
                            {
                                if (commandCounter is null) throw new InvalidOperationException($"{nameof(TcpCqrsServer)} is not setup");
                                isCommand = true;

                                if (!commandCounter.BeginReceive())
                                    throw new CqrsNetworkException("Cannot receive any more commands");

                                var command = (ICommand?)serializer.Deserialize(data.MessageData, messageType);
                                if (command is null)
                                    throw new Exception($"Invalid {nameof(data.MessageData)}");

                                inHandlerContext = true;
                                if (data.MessageResult)
                                {
                                    if (commandHandlerWithResultAwaitAsync is null) throw new InvalidOperationException($"{nameof(TcpCqrsServer)} is not setup");
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
                                else if (data.MessageAwait)
                                {
                                    if (commandHandlerAwaitAsync is null) throw new InvalidOperationException($"{nameof(TcpCqrsServer)} is not setup");
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
                                    if (commandHandlerAsync is null) throw new InvalidOperationException($"{nameof(TcpCqrsServer)} is not setup");
                                    _ = Task.Run(() => commandHandlerAsync(command, data.Source, false, default));
                                    hasResult = false;
                                }
                                inHandlerContext = false;
                            }
                            else if (info.EventTypes.Contains(messageType))
                            {
                                var @event = (IEvent?)serializer.Deserialize(data.MessageData, messageType);
                                if (@event is null)
                                    throw new Exception($"Invalid {nameof(data.MessageData)}");

                                inHandlerContext = true;
                                if (eventHandlerAsync is null)
                                    throw new InvalidOperationException($"{nameof(TcpCqrsServer)} is not setup");
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
                                log?.Error(new OperationCanceledException());
                                continue;
                            }

                            responseStarted = true;

                            //Response Header
                            var responseHeaderLength = TcpCommon.BufferHeader(buffer, data.MessageType, requestHeader.ContentType.Value);
#if NETSTANDARD2_0
                            await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                            await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                            if (hasResult)
                            {
                                //Response Body
                                responseBodyStream = new TcpProtocolBodyStream(stream, null, true, true);
                                if (encryptor is not null)
                                {
                                    responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);

                                    await serializer.SerializeAsync(responseBodyCryptoStream, result, cancellationToken);
#if NET5_0_OR_GREATER
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                }
                                else
                                {
                                    await serializer.SerializeAsync(responseBodyStream, result, cancellationToken);
                                }
                            }
                            else
                            {
                                //Response Body Empty
                                await stream.FlushAsync(cancellationToken);
                            }

                            continue;
                        }

                        throw new CqrsNetworkException("Invalid Request");
                    }
                    catch (Exception ex)
                    {
                        if (inHandlerContext && monitorIsCancellationRequested)
                        {
                            log?.Error(new OperationCanceledException());
                            continue;
                        }

                        if (!inHandlerContext || !socket.Connected)
                        {
                            log?.Error(ex);
                            return; //aborted or network error
                        }

                        if (!responseStarted && requestHeader is not null && requestHeader.ContentType.HasValue)
                        {
                            try
                            {
                                //Response Header for Error
                                var responseHeaderLength = TcpCommon.BufferErrorHeader(buffer, requestHeader.ProviderType, requestHeader.ContentType.Value);
#if NETSTANDARD2_0
                                await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                                await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                                //Response Body
                                responseBodyStream = new TcpProtocolBodyStream(stream, null, true, true);
                                if (encryptor is not null)
                                {
                                    responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);

                                    await ExceptionSerializer.SerializeAsync(serializer, responseBodyCryptoStream, ex, cancellationToken);
#if NET5_0_OR_GREATER
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                }
                                else
                                {
                                    await ExceptionSerializer.SerializeAsync(serializer, responseBodyStream, ex, cancellationToken);
                                }
                            }
                            catch (Exception ex2)
                            {
                                log?.Error($"{nameof(TcpCqrsServer)} Error {socket.RemoteEndPoint}", ex2);
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
                socket.Dispose();
            }
        }
    }
}