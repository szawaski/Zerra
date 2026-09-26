// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Dynamic;
using System.Net.Sockets;
using System.Security.Claims;
using Zerra.Buffers;
using Zerra.CQRS.Reflection;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Reflection;
using Zerra.Serialization;

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
        /// Creates a new TCP CQRS server.
        /// </summary>
        /// <param name="serverUrl">The URL of the server.</param>
        /// <param name="serializer">The serializer for request and response data.</param>
        /// <param name="encryptor">Optional encryption provider for encrypting request and response data.</param>
        /// <param name="log">Optional logging provider.</param>
        public TcpCqrsServer(string serverUrl, ISerializer serializer, IEncryptor? encryptor, ILogger? log)
            : base(serverUrl, log)
        {
            this.serializer = serializer;
            this.encryptor = encryptor;
        }

        /// <inheritdoc />
        protected override async Task Handle(Socket socket, CancellationToken cancellationToken)
        {
            if (throttle is null)
            {
                socket.Dispose();
                throw new InvalidOperationException($"{nameof(TcpCqrsServer)} is not setup");
            }

            var stream = new NetworkStream(socket, false); //one stream for the connection instead of one per request
            try
            {
                for (; ; )
                {
                    TcpRequestHeader? requestHeader = null;
                    var responseStarted = false;

                    var bufferOwner = ArrayPoolHelper<byte>.Rent(TcpCommon.BufferLength);
                    var buffer = bufferOwner.AsMemory();

                    Stream? requestBodyStream = null;
                    Stream? responseBodyStream = null;
                    Stream? resultStream = null; //the handler's stream, disposed once sent
                    CryptoFlushStream? responseBodyCryptoStream = null;
                    var isCommand = false;

                    var requestBodyRead = false;
                    var inHandlerContext = false;
                    var throttlerUsed = false;
                    var commandCounterUsedContinuation = false;
                    var monitorIsCancellationRequested = false;
                    var requestBegun = false;
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

                            //waiting for the next request ends when closing, once its first bytes arrive nothing about shutting down cancels it
                            var readToken = requestBegun ? CancellationToken.None : cancellationToken;
#if NETSTANDARD2_0
                            var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, readToken);
#else
                            var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), readToken);
#endif

                            if (bytesRead == 0)
                                return; //not an abort if we haven't started receiving, simple socket disconnect
                            requestBegun = true;
                            headerLength += bytesRead;

                            requestHeaderEnd = TcpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                        }
                        requestHeader = TcpCommon.ReadHeader(buffer.Slice(0, headerLength), headerPosition);

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

                        var data = await serializer.DeserializeAsync<CqrsRequestData>(requestBodyStream, CancellationToken.None);
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

                        await throttle.WaitAsync(CancellationToken.None);
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

                            var providerType = TypeFinder.GetTypeFromName(data.ProviderType);

                            if (!this.types.Contains(providerType))
                                throw new CqrsNetworkException($"Unhandled Provider Type {providerType.FullName}");

                            inHandlerContext = true;
                            RemoteQueryCallResponse result;
                            var monitor = new SocketAbortMonitor(socket, CancellationToken.None);
                            try
                            {
                                result = await this.providerHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments, data.Source, serializer, monitor.Token);
                            }
                            finally
                            {
                                monitorIsCancellationRequested = await monitor.DisposeAndGetIsCancellationRequestedAsync();
                            }
                            inHandlerContext = false;
                            resultStream = result.Stream;

                            if (monitorIsCancellationRequested)
                            {
                                log?.Error(new OperationCanceledException());
                                continue;
                            }

                            responseStarted = true;

                            //Response Header
#pragma warning disable CS8629 // Nullable value type may be null. False Positive
                            var responseHeaderLength = TcpCommon.BufferHeader(buffer, data.ProviderType, requestHeader.ContentType.Value);
#pragma warning restore CS8629 // Nullable value type may be null. False Positive

                            //Response Body, the header goes out with it
                            responseBodyStream = new TcpProtocolBodyStream(stream, null, true, true, buffer.Slice(0, responseHeaderLength));

                            int bytesRead;
                            if (result.Stream is not null)
                            {
                                if (encryptor is not null)
                                {
                                    responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);

#if NETSTANDARD2_0
                                    while ((bytesRead = await result.Stream.ReadAsync(bufferOwner, 0, bufferOwner.Length, CancellationToken.None)) > 0)
                                        await responseBodyCryptoStream.WriteAsync(bufferOwner, 0, bytesRead, CancellationToken.None);
#else
                                    while ((bytesRead = await result.Stream.ReadAsync(buffer, CancellationToken.None)) > 0)
                                        await responseBodyCryptoStream.WriteAsync(buffer.Slice(0, bytesRead), CancellationToken.None);
#endif
#if !NETSTANDARD2_0
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(CancellationToken.None);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                    continue;
                                }
                                else
                                {
#if NETSTANDARD2_0
                                    while ((bytesRead = await result.Stream.ReadAsync(bufferOwner, 0, bufferOwner.Length, CancellationToken.None)) > 0)
                                        await responseBodyStream.WriteAsync(bufferOwner, 0, bytesRead, CancellationToken.None);
#else
                                    while ((bytesRead = await result.Stream.ReadAsync(buffer, CancellationToken.None)) > 0)
                                        await responseBodyStream.WriteAsync(buffer.Slice(0, bytesRead), CancellationToken.None);
#endif
                                    await responseBodyStream.FlushAsync(CancellationToken.None);
                                    continue;
                                }
                            }
                            else
                            {
                                if (encryptor is not null)
                                {
                                    responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);

                                    await serializer.SerializeAsync(responseBodyCryptoStream, result.Model, CancellationToken.None);
#if !NETSTANDARD2_0
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(CancellationToken.None);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                    continue;
                                }
                                else
                                {
                                    await serializer.SerializeAsync(responseBodyStream, result.Model, CancellationToken.None);
                                    await responseBodyStream.FlushAsync(CancellationToken.None);
                                    continue;
                                }
                            }
                        }
                        else if (!String.IsNullOrWhiteSpace(data.MessageType))
                        {
                            if (data.MessageData is null) throw new Exception("Invalid Request");
                            if (String.IsNullOrWhiteSpace(data.Source)) throw new Exception("Invalid Request");
                            if (requestHeader.ProviderType != data.MessageType) throw new Exception("Invalid Request");

                            var messageType = TypeFinder.GetTypeFromName(data.MessageType);

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
                                    var monitor = new SocketAbortMonitor(socket, CancellationToken.None);
                                    try
                                    {
                                        result = await commandHandlerWithResultAwaitAsync(command, data.Source, monitor.Token);
                                    }
                                    finally
                                    {
                                        monitorIsCancellationRequested = await monitor.DisposeAndGetIsCancellationRequestedAsync();
                                    }
                                    hasResult = true;
                                }
                                else if (data.MessageAwait)
                                {
                                    if (commandHandlerAwaitAsync is null) throw new InvalidOperationException($"{nameof(TcpCqrsServer)} is not setup");
                                    var monitor = new SocketAbortMonitor(socket, CancellationToken.None);
                                    try
                                    {
                                        await commandHandlerAwaitAsync(command, data.Source, monitor.Token);
                                    }
                                    finally
                                    {
                                        monitorIsCancellationRequested = await monitor.DisposeAndGetIsCancellationRequestedAsync();
                                    }
                                    hasResult = false;
                                }
                                else
                                {
                                    if (commandHandlerAsync is null) throw new InvalidOperationException($"{nameof(TcpCqrsServer)} is not setup");
                                    var commandHandlerTask = Task.Run(() => commandHandlerAsync(command, data.Source, default));
                                    //tracked until the handler finishes so disposing waits for it
                                    _ = running.Add(commandHandlerTask);
                                    _ = commandHandlerTask.ContinueWith(removeRunning, running, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                                    if (commandCounter != null)
                                        _ = commandHandlerTask.ContinueWith(x => commandCounter.CompleteReceive(throttle));
                                    commandCounterUsedContinuation = true;
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
                                var eventHandlerTask = Task.Run(() => eventHandlerAsync(@event, data.Source));
                                //tracked until the handler finishes so disposing waits for it
                                _ = running.Add(eventHandlerTask);
                                _ = eventHandlerTask.ContinueWith(removeRunning, running, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
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
#pragma warning disable CS8629 // Nullable value type may be null. False Positive
                            var responseHeaderLength = TcpCommon.BufferHeader(buffer, data.MessageType, requestHeader.ContentType.Value);
#pragma warning restore CS8629 // Nullable value type may be null. False Positive

                            if (hasResult)
                            {
                                //Response Body, the header goes out with it
                                responseBodyStream = new TcpProtocolBodyStream(stream, null, true, true, buffer.Slice(0, responseHeaderLength));
                                if (encryptor is not null)
                                {
                                    responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);

                                    await serializer.SerializeAsync(responseBodyCryptoStream, result, CancellationToken.None);
#if !NETSTANDARD2_0
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(CancellationToken.None);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                }
                                else
                                {
                                    await serializer.SerializeAsync(responseBodyStream, result, CancellationToken.None);
                                    await responseBodyStream.FlushAsync(CancellationToken.None);
                                }
                            }
                            else
                            {
                                //Response Body Empty
#if NETSTANDARD2_0
                                await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, CancellationToken.None);
#else
                                await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), CancellationToken.None);
#endif
                                await stream.FlushAsync(CancellationToken.None);
                            }

                            continue;
                        }

                        throw new CqrsNetworkException("Invalid Request");
                    }
                    catch (Exception ex)
                    {
                        if (!requestBegun && cancellationToken.IsCancellationRequested)
                            return; //closing while the connection waited for its next request

                        if (inHandlerContext && monitorIsCancellationRequested)
                        {
                            log?.Error(new OperationCanceledException());
                            continue;
                        }

                        //the connection can only be reused if the request was completely read and nothing of the response was sent
                        if (!requestBodyRead || responseStarted || requestHeader is null || !requestHeader.ContentType.HasValue || !socket.Connected)
                        {
                            log?.Error(ex);
                            return; //aborted, network error, or unreadable request
                        }

                        //rejected before the handler, the server logs it and the caller gets the error
                        if (!inHandlerContext)
                            log?.Error(ex);

                        try
                        {
                            //Response Header for Error
                            var responseHeaderLength = TcpCommon.BufferErrorHeader(buffer, requestHeader.ProviderType, requestHeader.ContentType.Value);

                            //Response Body, the header goes out with it
                            responseBodyStream = new TcpProtocolBodyStream(stream, null, true, true, buffer.Slice(0, responseHeaderLength));
                            if (encryptor is not null)
                            {
                                responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);

                                await ExceptionSerializer.SerializeAsync(serializer, responseBodyCryptoStream, ex, CancellationToken.None);
#if !NETSTANDARD2_0
                                await responseBodyCryptoStream.FlushFinalBlockAsync(CancellationToken.None);
#else
                                responseBodyCryptoStream.FlushFinalBlock();
#endif
                            }
                            else
                            {
                                await ExceptionSerializer.SerializeAsync(serializer, responseBodyStream, ex, CancellationToken.None);
                                await responseBodyStream.FlushAsync(CancellationToken.None);
                            }
                        }
                        catch (Exception ex2)
                        {
                            log?.Error($"{nameof(TcpCqrsServer)} Error {socket.RemoteEndPoint}", ex2);
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
                        if (throttlerUsed && !commandCounterUsedContinuation)
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