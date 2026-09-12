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
                    var isCommand = false;

                    var requestBodyRead = false;
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

                            headerEnd = HttpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                        }
                        requestHeader = HttpCommon.ReadHeader(buffer.Slice(0, headerLength), headerPosition, authorizer is not null); //all headers only for the authorizer

                        if (contentType.HasValue && requestHeader.ContentType.HasValue && requestHeader.ContentType != contentType)
                        {
                            _ = Log.ErrorAsync($"{nameof(HttpCqrsServer)} Received Invalid Content Type {requestHeader.ContentType}");
                            throw new CqrsNetworkException("Invalid Content Type");
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
                            _ = Log.TraceAsync($"{nameof(HttpCqrsServer)} Received Preflight {socket.RemoteEndPoint}");

                            var preflightLength = HttpCommon.BufferPreflightResponse(buffer, requestHeader.Origin, originAllowed);
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

                        if (!originAllowed)
                        {
                            _ = Log.WarnAsync($"{nameof(HttpCqrsServer)} Origin Not Allowed {requestHeader.Origin}");

                            //the unused body is read so the connection stays usable, the client reuses it after an error
                            requestBodyStream = new HttpProtocolBodyStream(requestHeader.Chuncked ? null : (requestHeader.ContentLength ?? 0), stream, requestHeader.BodyStartBuffer, false, true);
                            await requestBodyStream.CopyToAsync(Stream.Null, 81920, cancellationToken);
#if NETSTANDARD2_0
                            requestBodyStream.Dispose();
#else
                            await requestBodyStream.DisposeAsync();
#endif
                            requestBodyStream = null;
                            requestBodyRead = true;

                            var unauthorizedLength = HttpCommon.BufferUnauthorizedResponseHeader(buffer);
#if NETSTANDARD2_0
                            await stream.WriteAsync(bufferOwner, 0, unauthorizedLength, cancellationToken);
#else
                            await stream.WriteAsync(buffer.Slice(0, unauthorizedLength), cancellationToken);
#endif
                            await stream.FlushAsync(cancellationToken);
                            continue;
                        }

                        //Read Request Body
                        //------------------------------------------------------------------------------------------------------------

                        //chunked takes precedence, without either length the body is empty
                        requestBodyStream = new HttpProtocolBodyStream(requestHeader.Chuncked ? null : (requestHeader.ContentLength ?? 0), stream, requestHeader.BodyStartBuffer, false, true);

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
                            var responseHeaderLength = HttpCommon.BufferOkResponseHeader(buffer, requestHeader.Origin, requestHeader.ProviderType, requestHeader.ContentType.Value, null);

                            //Response Body, the header goes out with it
                            responseBodyStream = new HttpProtocolBodyStream(null, stream, null, true, true, buffer.Slice(0, responseHeaderLength), true); //wide chunk lengths so 5.3 clients do not stall on a small body

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
                            if (requestHeader.ProviderType is not null && requestHeader.ProviderType != data.MessageType) throw new Exception("Invalid Request"); //5.3 clients don't send it for messages

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
                                        monitorIsCancellationRequested = await monitor.DisposeAndGetIsCancellationRequestedAsync();
                                    }
                                    hasResult = true;
                                }
                                else if (data.MessageAwait == true)
                                {
                                    if (commandHandlerAwaitAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
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
                            var responseHeaderLength = HttpCommon.BufferOkResponseHeader(buffer, requestHeader.Origin, requestHeader.ProviderType, requestHeader.ContentType.Value, null, hasResult);

                            if (hasResult)
                            {
                                //Response Body, the header goes out with it
                                responseBodyStream = new HttpProtocolBodyStream(null, stream, null, true, true, buffer.Slice(0, responseHeaderLength), true); //wide chunk lengths so 5.3 clients do not stall on a small body
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
                                //Response Body Empty, the header says Content-Length 0 so there is no chunked ending
#if NETSTANDARD2_0
                                await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                                await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif
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
                            //the error body is built first so the header can give its length, see BufferErrorResponseHeader
                            var errorBody = new MemoryStream();
                            if (symmetricConfig is not null)
                            {
                                var errorBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, errorBody, true, true); //the body is still needed after, leave it open
                                await ContentTypeSerializer.SerializeExceptionAsync(requestHeader.ContentType.Value, errorBodyCryptoStream, ex, cancellationToken);
#if NET5_0_OR_GREATER
                                await errorBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                                errorBodyCryptoStream.FlushFinalBlock();
#endif
#if NETSTANDARD2_0
                                errorBodyCryptoStream.Dispose();
#else
                                await errorBodyCryptoStream.DisposeAsync();
#endif
                            }
                            else
                            {
                                await ContentTypeSerializer.SerializeExceptionAsync(requestHeader.ContentType.Value, errorBody, ex, cancellationToken);
                            }

                            //Response Header
                            var responseHeaderLength = HttpCommon.BufferErrorResponseHeader(buffer, requestHeader.Origin, (int)errorBody.Length);
#if NETSTANDARD2_0
                            await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
                            await stream.WriteAsync(errorBody.GetBuffer(), 0, (int)errorBody.Length, cancellationToken);
#else
                            await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
                            await stream.WriteAsync(errorBody.GetBuffer().AsMemory(0, (int)errorBody.Length), cancellationToken);
#endif
                            await stream.FlushAsync(cancellationToken);
                        }
                        catch (Exception ex2)
                        {
                            _ = Log.ErrorAsync($"{nameof(HttpCqrsServer)} Error {socket.RemoteEndPoint}", ex2);
                        }
                        return; //the connection closes after an error, a 5.3 client reads the error body as a result and would leave the connection out of step
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