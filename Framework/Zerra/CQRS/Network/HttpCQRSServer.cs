// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net.Sockets;
using System.Security.Claims;
using Zerra.Compression;
using Zerra.Encryption;
using Zerra.Buffers;
using Zerra.Logging;
using Zerra.Serialization;
using Zerra.Reflection;
using Zerra.CQRS.Reflection;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A CQRS Server using basic HTTP communication.
    /// </summary>
    public sealed class HttpCqrsServer : CqrsServerBase
    {
        private readonly ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ICompressor? compressor;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly string[]? allowOrigins;

        /// <summary>
        /// Creates a new HTTP CQRS server.
        /// </summary>
        /// <param name="serverUrl">The URL of the server.</param>
        /// <param name="serializer">The serializer for request and response data.</param>
        /// <param name="encryptor">Optional encryption provider for encrypting request and response data.</param>
        /// <param name="compressor">Optional compressor for message compression, applied before encryption. If null, messages are not compressed.</param>
        /// <param name="authorizer">Optional authorizer for validating requests.</param>
        /// <param name="allowOrigins">Optional CORS HTTP headers for allowed origins.</param>
        /// <param name="log">Optional logging provider.</param>
        public HttpCqrsServer(string serverUrl, ISerializer serializer, IEncryptor? encryptor, ICompressor? compressor, ICqrsAuthorizer? authorizer, string[]? allowOrigins, ILogger? log = null)
            : base(serverUrl, log)
        {
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.compressor = compressor;
            this.authorizer = authorizer;
            if (allowOrigins is not null && allowOrigins.Length > 0 && !allowOrigins.Contains("*"))
                this.allowOrigins = allowOrigins;
            else
                this.allowOrigins = null;
        }

        /// <inheritdoc />
        protected override async Task Handle(Socket socket, CancellationToken cancellationToken)
        {
            if (throttle is null)
            {
                socket.Dispose();
                throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
            }

            var stream = new NetworkStream(socket, false); //one stream for the connection instead of one per request
            try
            {
                for (; ; )
                {
                    HttpRequestHeader? requestHeader = null;
                    var responseStarted = false;

                    var bufferOwner = ArrayPoolHelper<byte>.Rent(HttpCommon.BufferLength);
                    var buffer = bufferOwner.AsMemory();

                    Stream? requestBodyStream = null;
                    Stream? responseBodyStream = null;
                    Stream? resultStream = null; //the handler's stream, disposed once sent
                    CryptoFlushStream? responseBodyCryptoStream = null;
                    Stream? responseBodyCompressStream = null;
                    var isCommand = false;

                    var requestBodyRead = false;
                    var inHandlerContext = false;
                    var throttleUsed = false;
                    var commandCounterUsedContinuation = false;
                    var monitorIsCancellationRequested = false;
                    var requestBegun = false;
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

                            //waiting for the next request ends when closing, once its first bytes arrive nothing about shutting down cancels it
                            var readToken = requestBegun ? CancellationToken.None : cancellationToken;
#if NETSTANDARD2_0
                            var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, readToken);
#else
                            var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength), readToken);
#endif

                            if (bytesRead == 0)
                                return; //not an abort if we haven't started receiving, simple socket disconnect
                            requestBegun = true;
                            headerLength += bytesRead;

                            headerEnd = HttpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                        }
                        requestHeader = HttpCommon.ReadHeader(buffer.Slice(0, headerLength), headerPosition, authorizer is not null); //all headers only for the authorizer

                        if (requestHeader.ContentType.HasValue && requestHeader.ContentType != serializer.ContentType)
                        {
                            log?.Error($"{nameof(HttpCqrsServer)} Received Invalid Content Type {requestHeader.ContentType}, Expected {serializer.ContentType}");
                            throw new CqrsNetworkException($"Invalid Content Type {requestHeader.ContentType}, Expected {serializer.ContentType}");
                        }

                        //browsers send the origin as scheme://host[:port] and HttpCqrsClient sends the host, an allowed value can be either, case doesn't matter
                        var originAllowed = true;
                        if (allowOrigins is not null)
                        {
                            originAllowed = false;
                            if (requestHeader.Origin is not null)
                            {
                                var originHost = Uri.TryCreate(requestHeader.Origin, UriKind.Absolute, out var originUri) ? originUri.Host : null;
                                foreach (var allowOrigin in allowOrigins)
                                {
                                    if (String.Equals(allowOrigin, requestHeader.Origin, StringComparison.OrdinalIgnoreCase) || (originHost is not null && String.Equals(allowOrigin, originHost, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        originAllowed = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (requestHeader.Preflight)
                        {
                            log?.Trace($"{nameof(HttpCqrsServer)} Received Preflight {socket.RemoteEndPoint}");

                            var preflightLength = HttpCommon.BufferPreflightResponse(buffer, requestHeader.Origin, originAllowed);
#if NETSTANDARD2_0
                            await stream.WriteAsync(bufferOwner, 0, preflightLength, CancellationToken.None);
#else
                            await stream.WriteAsync(buffer.Slice(0, preflightLength), CancellationToken.None);
#endif

                            await stream.FlushAsync(CancellationToken.None);
                            continue;
                        }

                        if (!requestHeader.ContentType.HasValue)
                        {
                            log?.Error($"{nameof(HttpCqrsServer)} Received Invalid Content Type {requestHeader.ContentType}");
                            throw new CqrsNetworkException("Invalid Content Type");
                        }

                        if (!originAllowed)
                        {
                            log?.Warn($"{nameof(HttpCqrsServer)} Origin Not Allowed {requestHeader.Origin}");

                            //the unused body is read so the connection stays usable, the client reuses it after an error
                            var unusedBodyStream = new HttpProtocolBodyStream(requestHeader.Chuncked ? null : (requestHeader.ContentLength ?? 0), stream, requestHeader.BodyStartBuffer, false, true);
                            requestBodyStream = unusedBodyStream;
                            await unusedBodyStream.DiscardReadAsync(CancellationToken.None);
#if NETSTANDARD2_0
                            requestBodyStream.Dispose();
#else
                            await requestBodyStream.DisposeAsync();
#endif
                            requestBodyStream = null;
                            requestBodyRead = true;

                            var unauthorizedLength = HttpCommon.BufferUnauthorizedResponseHeader(buffer);
#if NETSTANDARD2_0
                            await stream.WriteAsync(bufferOwner, 0, unauthorizedLength, CancellationToken.None);
#else
                            await stream.WriteAsync(buffer.Slice(0, unauthorizedLength), CancellationToken.None);
#endif
                            await stream.FlushAsync(CancellationToken.None);
                            continue;
                        }

                        //Read Request Body
                        //------------------------------------------------------------------------------------------------------------

                        //chunked takes precedence, without either length the body is empty
                        var requestBodyProtocolStream = new HttpProtocolBodyStream(requestHeader.Chuncked ? null : (requestHeader.ContentLength ?? 0), stream, requestHeader.BodyStartBuffer, false, true);
                        requestBodyStream = requestBodyProtocolStream;

                        if (encryptor is not null)
                            requestBodyStream = encryptor.Decrypt(requestBodyStream, false);
                        if (compressor is not null)
                            requestBodyStream = compressor.Decompress(requestBodyStream, false);

                        CqrsRequestData? data;
                        if (requestHeader.IsUpload)
                        {
                            //the request data is framed, the rest of the body is the stream left for the handler to read
                            using (var uploadDataStream = new TcpProtocolBodyStream(requestBodyStream, null, false, true))
                            {
                                data = await serializer.DeserializeAsync<CqrsRequestData>(uploadDataStream, CancellationToken.None);
                                //the framing.s ending is read and checked, the handler reads the stream after it
                                await uploadDataStream.FinishReadAsync(CancellationToken.None);
                            }
                            if (data is null)
                                throw new CqrsNetworkException("Empty request body");
                            if (String.IsNullOrWhiteSpace(data.ProviderType))
                                throw new CqrsNetworkException("Invalid Request"); //only queries take a stream
                        }
                        else
                        {
                            data = await serializer.DeserializeAsync<CqrsRequestData>(requestBodyStream, CancellationToken.None);
                            //the ending is read and checked, a decompressor stops at its own end without reading it
                            await requestBodyProtocolStream.FinishReadAsync(CancellationToken.None);
                            if (data is null)
                                throw new CqrsNetworkException("Empty request body");

#if NETSTANDARD2_0
                            requestBodyStream.Dispose();
#else
                            await requestBodyStream.DisposeAsync();
#endif
                            requestBodyStream = null;
                            requestBodyRead = true;
                        }

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

                        await throttle.WaitAsync(CancellationToken.None);
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

                            var providerType = TypeFinder.GetTypeFromName(data.ProviderType);

                            if (!this.types.Contains(providerType))
                                throw new CqrsNetworkException($"Unhandled Provider Type {providerType.FullName}");

                            inHandlerContext = true;
                            RemoteQueryCallResponse result;
                            if (requestBodyStream is not null)
                            {
                                //no abort monitor, it would read the upload from the connection, an abandoned upload ends with the connection instead
                                try
                                {
                                    result = await this.providerHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments, new LeaveOpenStream(requestBodyStream), data.Source, serializer, CancellationToken.None);
                                }
                                finally
                                {
                                    //whatever the handler didn't read is read so the connection is ready for the next request
                                    await requestBodyProtocolStream.DiscardReadAsync(CancellationToken.None);
#if NETSTANDARD2_0
                                    requestBodyStream.Dispose();
#else
                                    await requestBodyStream.DisposeAsync();
#endif
                                    requestBodyStream = null;
                                    requestBodyRead = true;
                                }
                            }
                            else
                            {
                                var monitor = new SocketAbortMonitor(socket, CancellationToken.None);
                                try
                                {
                                    result = await this.providerHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments, null, data.Source, serializer, monitor.Token);
                                }
                                finally
                                {
                                    monitorIsCancellationRequested = await monitor.DisposeAndGetIsCancellationRequestedAsync();
                                }
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
                            var responseHeaderLength = HttpCommon.BufferOkResponseHeader(buffer, requestHeader.Origin, requestHeader.ProviderType, requestHeader.ContentType.Value, null);

                            //Response Body, the header goes out with it
                            responseBodyStream = new HttpProtocolBodyStream(null, stream, null, true, true, buffer.Slice(0, responseHeaderLength));

                            int bytesRead;
                            if (result.Stream is not null)
                            {
                                Stream responseBodyWriteStream = responseBodyStream;
                                if (encryptor is not null)
                                    responseBodyWriteStream = responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);
                                if (compressor is not null)
                                    responseBodyWriteStream = responseBodyCompressStream = compressor.Compress(responseBodyWriteStream, true);

#if NETSTANDARD2_0
                                while ((bytesRead = await result.Stream.ReadAsync(bufferOwner, 0, bufferOwner.Length, CancellationToken.None)) > 0)
                                    await responseBodyWriteStream.WriteAsync(bufferOwner, 0, bytesRead, CancellationToken.None);
#else
                                while ((bytesRead = await result.Stream.ReadAsync(buffer, CancellationToken.None)) > 0)
                                    await responseBodyWriteStream.WriteAsync(buffer.Slice(0, bytesRead), CancellationToken.None);
#endif
                                if (responseBodyCompressStream is not null)
                                {
#if NETSTANDARD2_0
                                    responseBodyCompressStream.Dispose();
#else
                                    await responseBodyCompressStream.DisposeAsync();
#endif
                                    responseBodyCompressStream = null;
                                }
                                if (responseBodyCryptoStream is not null)
                                {
#if !NETSTANDARD2_0
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(CancellationToken.None);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                }
                                else
                                {
                                    await responseBodyStream.FlushAsync(CancellationToken.None);
                                }
                                continue;
                            }
                            else
                            {
                                Stream responseBodyWriteStream = responseBodyStream;
                                if (encryptor is not null)
                                    responseBodyWriteStream = responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);
                                if (compressor is not null)
                                    responseBodyWriteStream = responseBodyCompressStream = compressor.Compress(responseBodyWriteStream, true);

                                await serializer.SerializeAsync(responseBodyWriteStream, result.Model, CancellationToken.None);
                                if (responseBodyCompressStream is not null)
                                {
#if NETSTANDARD2_0
                                    responseBodyCompressStream.Dispose();
#else
                                    await responseBodyCompressStream.DisposeAsync();
#endif
                                    responseBodyCompressStream = null;
                                }
                                if (responseBodyCryptoStream is not null)
                                {
#if !NETSTANDARD2_0
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(CancellationToken.None);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                }
                                else
                                {
                                    await responseBodyStream.FlushAsync(CancellationToken.None);
                                }
                                continue;
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
                                if (commandCounter is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
                                isCommand = true;

                                if (!commandCounter.BeginReceive())
                                    throw new CqrsNetworkException("Cannot receive any more commands");

                                var command = (ICommand?)serializer.Deserialize(data.MessageData, messageType);
                                if (command is null)
                                    throw new Exception($"Invalid {nameof(data.MessageData)}");

                                inHandlerContext = true;
                                if (data.MessageResult == true)
                                {
                                    if (commandHandlerWithResultAwaitAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
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
                                else if(data.MessageAwait == true)
                                {
                                    if (commandHandlerAwaitAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
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
                                    if (commandHandlerAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
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
                                if (eventHandlerAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
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
                            var responseHeaderLength = HttpCommon.BufferOkResponseHeader(buffer, requestHeader.Origin, requestHeader.ProviderType, serializer.ContentType, null, hasResult);

                            if (hasResult)
                            {
                                //Response Body, the header goes out with it
                                responseBodyStream = new HttpProtocolBodyStream(null, stream, null, true, true, buffer.Slice(0, responseHeaderLength));
                                Stream responseBodyWriteStream = responseBodyStream;
                                if (encryptor is not null)
                                    responseBodyWriteStream = responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);
                                if (compressor is not null)
                                    responseBodyWriteStream = responseBodyCompressStream = compressor.Compress(responseBodyWriteStream, true);

                                await serializer.SerializeAsync(responseBodyWriteStream, result, CancellationToken.None);
                                if (responseBodyCompressStream is not null)
                                {
#if NETSTANDARD2_0
                                    responseBodyCompressStream.Dispose();
#else
                                    await responseBodyCompressStream.DisposeAsync();
#endif
                                    responseBodyCompressStream = null;
                                }
                                if (responseBodyCryptoStream is not null)
                                {
#if !NETSTANDARD2_0
                                    await responseBodyCryptoStream.FlushFinalBlockAsync(CancellationToken.None);
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                }
                                else
                                {
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
                            //Response Header
                            var responseHeaderLength = HttpCommon.BufferErrorResponseHeader(buffer, requestHeader.Origin);

                            //Response Body, the header goes out with it
                            responseBodyStream = new HttpProtocolBodyStream(null, stream, null, true, true, buffer.Slice(0, responseHeaderLength));
                            Stream responseBodyWriteStream = responseBodyStream;
                            if (encryptor is not null)
                                responseBodyWriteStream = responseBodyCryptoStream = encryptor.Encrypt(responseBodyStream, true);
                            if (compressor is not null)
                                responseBodyWriteStream = responseBodyCompressStream = compressor.Compress(responseBodyWriteStream, true);

                            await ExceptionSerializer.SerializeAsync(serializer, responseBodyWriteStream, ex, CancellationToken.None);
                            if (responseBodyCompressStream is not null)
                            {
#if NETSTANDARD2_0
                                responseBodyCompressStream.Dispose();
#else
                                await responseBodyCompressStream.DisposeAsync();
#endif
                                responseBodyCompressStream = null;
                            }
                            if (responseBodyCryptoStream is not null)
                            {
#if !NETSTANDARD2_0
                                await responseBodyCryptoStream.FlushFinalBlockAsync(CancellationToken.None);
#else
                                responseBodyCryptoStream.FlushFinalBlock();
#endif
                            }
                            else
                            {
                                await responseBodyStream.FlushAsync(CancellationToken.None);
                            }
                        }
                        catch (Exception ex2)
                        {
                            log?.Error($"{nameof(HttpCqrsServer)} Error {socket.RemoteEndPoint}", ex2);
                            return; //the error response did not complete
                        }
                    }
                    finally
                    {
                        if (responseBodyCompressStream is not null)
                        {
                            try
                            {
                                //disposing writes its end into the crypto or response stream, which can fail the same as the response did
#if NETSTANDARD2_0
                                responseBodyCompressStream.Dispose();
#else
                                await responseBodyCompressStream.DisposeAsync();
#endif
                            }
                            catch { }
                        }
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
                        if (throttleUsed && !commandCounterUsedContinuation)
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