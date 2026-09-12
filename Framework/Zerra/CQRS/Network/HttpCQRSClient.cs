// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Linq;
using Zerra.Buffers;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A CQRS Client using basic HTTP communication.
    /// </summary>
    public sealed class HttpCqrsClient : CqrsClientBase
    {
        private readonly ContentType contentType;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly SocketClientPool socketPool;

        /// <summary>
        /// Creates a new HTTP Client.
        /// </summary>
        /// <param name="contentType">The format of the body of the request and response.</param>
        /// <param name="serviceUrl">The url of the server.</param>
        /// <param name="symmetricConfig">If provided, information to encrypt the data.</param>
        /// <param name="authorizer">An authorizer for adding headers needed for the server to validate requests.</param>
        public HttpCqrsClient(ContentType contentType, string serviceUrl, SymmetricConfig? symmetricConfig, ICqrsAuthorizer? authorizer)
            : base(serviceUrl)
        {
            this.contentType = contentType;
            this.symmetricConfig = symmetricConfig;
            this.authorizer = authorizer;
            this.socketPool = SocketClientPool.Shared;
        }

        /// <inheritdoc />
        protected override TReturn? CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source) where TReturn : default
        {
            throttle.Wait();

            SocketPoolStream? stream = null;
            Stream? requestBodyStream = null;
            CryptoFlushStream? requestBodyCryptoStream = null;
            Stream? responseBodyStream = null;
            var bufferOwner = ArrayPoolHelper<byte>.Rent(HttpCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CqrsRequestData()
                {
                    ProviderType = interfaceType.Name,
                    ProviderMethod = methodName,

                    Claims = claims,
                    Source = source
                };
                data.AddProviderArguments(arguments);

                Dictionary<string, List<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = Task.Run(() => authorizer.GetAuthorizationHeadersAsync().AsTask()).GetAwaiter().GetResult();

                var buffer = bufferOwner.AsMemory();

                var requireNewConnection = false;
                var responseStarted = false; //once the server responds it has the request, retrying on a new connection could run it twice
            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUri, data.ProviderType, contentType, authHeaders);

#if NETSTANDARD2_0
                    stream = socketPool.BeginStream(host, port, ProtocolType.Tcp, bufferOwner, 0, 0, requireNewConnection);
#else
                    stream = socketPool.BeginStream(host, port, ProtocolType.Tcp, ReadOnlySpan<byte>.Empty, requireNewConnection);
#endif

                    requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true, true, buffer.Slice(0, requestHeaderLength)); //the header goes out with the body

                    if (symmetricConfig is not null)
                    {
                        requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true);
                        ContentTypeSerializer.Serialize(contentType, requestBodyCryptoStream, data);
                        requestBodyCryptoStream.FlushFinalBlock();
                        requestBodyCryptoStream.Dispose();
                        requestBodyCryptoStream = null;
                    }
                    else
                    {
                        ContentTypeSerializer.Serialize(contentType, requestBodyStream, data);
                        requestBodyStream.Flush();
                        requestBodyStream.Dispose();
                    }

                    requestBodyStream = null;

                    //Response Header
                    var headerPosition = 0;
                    var headerLength = 0;
                    var headerEnd = false;
                    while (!headerEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(HttpCqrsClient)} Header Too Long");

                        var bytesRead = stream.Read(bufferOwner, headerLength, buffer.Length - headerLength); //after what was read, the position backs off to rescan

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.IsNewConnection || responseStarted)
                            {
                                stream = null;
                                throw new ConnectionAbortedException();
                            }
                            else
                            {
                                stream = null;
                                requireNewConnection = true;
                                goto newconnection;
                            }
                        }
                        headerLength += bytesRead;
                        responseStarted = true;

                        headerEnd = HttpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                    }
                    var responseHeader = HttpCommon.ReadHeader(buffer.Slice(0, headerLength), headerPosition);

                    //Response Body
                    if (isStream)
                        responseBodyStream = new HttpProtocolBodyStream(responseHeader.Chuncked ? null : (responseHeader.ContentLength ?? 0), stream, responseHeader.BodyStartBuffer.ToArray(), false, false);
                    else
                        responseBodyStream = new HttpProtocolBodyStream(responseHeader.Chuncked ? null : (responseHeader.ContentLength ?? 0), stream, responseHeader.BodyStartBuffer, false, false, endOnPeerSilence: true); //a 5.3 server does not end an unencrypted body

                    if (symmetricConfig is not null)
                        responseBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        //an error without a body, such as Kestrel's 401 or a 400 sent before the request is read, has no details to read so the status is the error
                        var responseException = !responseHeader.Chuncked && (responseHeader.ContentLength ?? 0) == 0
                            ? new RemoteServiceException($"Remote service responded {responseHeader.ErrorStatus} without details for {interfaceType.Name}.{methodName}")
                            : ContentTypeSerializer.DeserializeException(contentType, responseBodyStream);
                        isThrowingRemote = true;
                        throw responseException;
                    }

                    if (isStream)
                    {
                        return (TReturn)(object)responseBodyStream; //TODO better way to convert type???
                    }
                    else
                    {
                        var model = ContentTypeSerializer.Deserialize<TReturn>(contentType, responseBodyStream);
                        responseBodyStream.Dispose();
                        return model;
                    }
                }
                catch (Exception ex)
                {
                    //the request streams leave the socket open so they go first, the response stream closes the socket stream so it goes after the socket is handled
                    if (requestBodyCryptoStream is not null)
                    {
                        try
                        {
                            //disposing flushes its final block into the request stream, which can fail the same as the request did
                            requestBodyCryptoStream.Dispose();
                        }
                        catch { }
                    }
                    if (requestBodyStream is not null)
                        requestBodyStream.Dispose();
                    if (isThrowingRemote)
                    {
                        if (stream is not null)
                            stream.Dispose();
                    }
                    else
                    {
                        if (stream is not null)
                        {
                            stream.DisposeSocket();
                            if (!stream.IsNewConnection && !responseStarted)
                            {
                                _ = Log.ErrorAsync(ex);
                                stream = null;
                                requireNewConnection = true;
                                goto newconnection;
                            }
                        }
                    }

                    if (responseBodyStream is not null)
                    {
                        try
                        {
                            //crypto stream can error, we want to throw the actual error
                            responseBodyStream.Dispose();
                        }
                        catch { }
                    }

                    throw;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(bufferOwner);
                throttle.Release();
            }
        }

        /// <inheritdoc />
        protected override async Task<TReturn?> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source, CancellationToken cancellationToken) where TReturn : default
        {
            await throttle.WaitAsync(cancellationToken);

            SocketPoolStream? stream = null;
            Stream? requestBodyStream = null;
            CryptoFlushStream? requestBodyCryptoStream = null;
            Stream? responseBodyStream = null;
            var bufferOwner = ArrayPoolHelper<byte>.Rent(HttpCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CqrsRequestData()
                {
                    ProviderType = interfaceType.Name,
                    ProviderMethod = methodName,

                    Claims = claims,
                    Source = source
                };
                data.AddProviderArguments(arguments);

                Dictionary<string, List<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = await authorizer.GetAuthorizationHeadersAsync();

                var buffer = bufferOwner.AsMemory();

                var requireNewConnection = false;
                var responseStarted = false; //once the server responds it has the request, retrying on a new connection could run it twice
            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUri, data.ProviderType, contentType, authHeaders);

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, 0, requireNewConnection, cancellationToken);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, Memory<byte>.Empty, requireNewConnection, cancellationToken);
#endif

                    requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true, true, buffer.Slice(0, requestHeaderLength)); //the header goes out with the body

                    if (symmetricConfig is not null)
                    {
                        requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true);
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data, cancellationToken);
#if NET5_0_OR_GREATER
                        await requestBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                        requestBodyCryptoStream.FlushFinalBlock();
#endif
#if NETSTANDARD2_0
                        requestBodyCryptoStream.Dispose();
#else
                        await requestBodyCryptoStream.DisposeAsync();
#endif
                        requestBodyCryptoStream = null;
                    }
                    else
                    {
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyStream, data, cancellationToken);
                        await requestBodyStream.FlushAsync(cancellationToken);
#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                    }

                    requestBodyStream = null;

                    //Response Header
                    var headerPosition = 0;
                    var headerLength = 0;
                    var headerEnd = false;
                    while (!headerEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(HttpCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, cancellationToken);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);
#endif

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.IsNewConnection || responseStarted)
                            {
                                stream = null;
                                throw new ConnectionAbortedException();
                            }
                            else
                            {
                                stream = null;
                                requireNewConnection = true;
                                goto newconnection;
                            }
                        }
                        headerLength += bytesRead;
                        responseStarted = true;

                        headerEnd = HttpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                    }
                    var responseHeader = HttpCommon.ReadHeader(buffer.Slice(0, headerLength), headerPosition);

                    //Response Body
                    if (isStream)
                        responseBodyStream = new HttpProtocolBodyStream(responseHeader.Chuncked ? null : (responseHeader.ContentLength ?? 0), stream, responseHeader.BodyStartBuffer.ToArray(), false, false);
                    else
                        responseBodyStream = new HttpProtocolBodyStream(responseHeader.Chuncked ? null : (responseHeader.ContentLength ?? 0), stream, responseHeader.BodyStartBuffer, false, false, endOnPeerSilence: true); //a 5.3 server does not end an unencrypted body

                    if (symmetricConfig is not null)
                        responseBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        //an error without a body, such as Kestrel's 401 or a 400 sent before the request is read, has no details to read so the status is the error
                        var responseException = !responseHeader.Chuncked && (responseHeader.ContentLength ?? 0) == 0
                            ? new RemoteServiceException($"Remote service responded {responseHeader.ErrorStatus} without details for {interfaceType.Name}.{methodName}")
                            : await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream, cancellationToken);
                        isThrowingRemote = true;
                        throw responseException;
                    }

                    if (isStream)
                    {
                        return (TReturn)(object)responseBodyStream; //TODO better way to convert type???
                    }
                    else
                    {
                        var model = await ContentTypeSerializer.DeserializeAsync<TReturn>(contentType, responseBodyStream, cancellationToken);
#if NETSTANDARD2_0
                        responseBodyStream.Dispose();
#else
                        await responseBodyStream.DisposeAsync();
#endif
                        return model;
                    }
                }
                catch (Exception ex)
                {
                    //the request streams leave the socket open so they go first, the response stream closes the socket stream so it goes after the socket is handled
                    if (requestBodyCryptoStream is not null)
                    {
                        try
                        {
                            //disposing flushes its final block into the request stream, which can fail the same as the request did
#if NETSTANDARD2_0
                            requestBodyCryptoStream.Dispose();
#else
                            await requestBodyCryptoStream.DisposeAsync();
#endif
                        }
                        catch { }
                    }
                    if (requestBodyStream is not null)
                    {
#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                    }
                    if (isThrowingRemote)
                    {
                        if (stream is not null)
                            stream.Dispose();
                    }
                    else if (cancellationToken.IsCancellationRequested)
                    {
                        _ = Log.ErrorAsync(ex);
                        if (stream is not null)
                        {
                            //only while the server has the whole request and hasn't responded, mid request the abort byte reads as request data and mid response the server is done with it
                            var abortAcknowledged = requestBodyStream is null && !responseStarted && await SocketAbortMonitor.SendAndAcknowledgeAbortAsync(stream);
                            if (abortAcknowledged)
                                stream?.Dispose();
                            else
                                stream?.DisposeSocket();
                        }
                    }
                    else
                    {
                        if (stream is not null)
                        {
                            stream.DisposeSocket();
                            if (!stream.IsNewConnection && !responseStarted)
                            {
                                _ = Log.ErrorAsync(ex);
                                stream = null;
                                requireNewConnection = true;
                                goto newconnection;
                            }
                        }
                    }

                    if (responseBodyStream is not null)
                    {
#if NETSTANDARD2_0
                        responseBodyStream.Dispose();
#else
                        await responseBodyStream.DisposeAsync();
#endif
                    }

                    throw;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(bufferOwner);
                throttle.Release();
            }
        }

        /// <inheritdoc />
        protected override async Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source, CancellationToken cancellationToken)
        {
            await throttle.WaitAsync(cancellationToken);

            SocketPoolStream? stream = null;
            Stream? requestBodyStream = null;
            CryptoFlushStream? requestBodyCryptoStream = null;
            Stream? responseBodyStream = null;
            var bufferOwner = ArrayPoolHelper<byte>.Rent(HttpCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                var messageTypeName = commandType.GetNiceName();

                var messageData = ContentTypeSerializer.Serialize(contentType, command);

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CqrsRequestData()
                {
                    MessageType = messageTypeName,
                    MessageData = messageData,
                    MessageAwait = messageAwait,
                    MessageResult = false,

                    Claims = claims,
                    Source = source
                };

                Dictionary<string, List<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = await authorizer.GetAuthorizationHeadersAsync();

                var buffer = bufferOwner.AsMemory();

                var requireNewConnection = false;
                var responseStarted = false; //once the server responds it has the request, retrying on a new connection could run it twice
            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUri, data.MessageType, contentType, authHeaders); //the server matches the message type against it

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, 0, requireNewConnection, cancellationToken);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, Memory<byte>.Empty, requireNewConnection, cancellationToken);
#endif

                    requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true, true, buffer.Slice(0, requestHeaderLength)); //the header goes out with the body

                    if (symmetricConfig is not null)
                    {
                        requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true);
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data, cancellationToken);
#if NET5_0_OR_GREATER
                        await requestBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                        requestBodyCryptoStream.FlushFinalBlock();
#endif
#if NETSTANDARD2_0
                        requestBodyCryptoStream.Dispose();
#else
                        await requestBodyCryptoStream.DisposeAsync();
#endif
                        requestBodyCryptoStream = null;
                    }
                    else
                    {
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyStream, data, cancellationToken);
                        await requestBodyStream.FlushAsync(cancellationToken);

#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                    }

                    requestBodyStream = null;

                    //Response Header
                    var headerPosition = 0;
                    var headerLength = 0;
                    var headerEnd = false;
                    while (!headerEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(HttpCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, cancellationToken);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);
#endif

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.IsNewConnection || responseStarted)
                            {
                                stream = null;
                                throw new ConnectionAbortedException();
                            }
                            else
                            {
                                stream = null;
                                goto newconnection;
                            }
                        }
                        headerLength += bytesRead;
                        responseStarted = true;

                        headerEnd = HttpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                    }
                    var responseHeader = HttpCommon.ReadHeader(buffer.Slice(0, headerLength), headerPosition);

                    //Response Body
                    responseBodyStream = new HttpProtocolBodyStream(responseHeader.Chuncked ? null : (responseHeader.ContentLength ?? 0), stream, responseHeader.BodyStartBuffer, false, false, endOnPeerSilence: true); //a 5.3 server does not end an unencrypted body

                    if (symmetricConfig is not null)
                        responseBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        //an error without a body, such as Kestrel's 401 or a 400 sent before the request is read, has no details to read so the status is the error
                        var responseException = !responseHeader.Chuncked && (responseHeader.ContentLength ?? 0) == 0
                            ? new RemoteServiceException($"Remote service responded {responseHeader.ErrorStatus} without details for {messageTypeName}")
                            : await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream, cancellationToken);
                        isThrowingRemote = true;
                        throw responseException;
                    }

#if NETSTANDARD2_0
                    responseBodyStream.Dispose();
#else
                    await responseBodyStream.DisposeAsync();
#endif
                }
                catch (Exception ex)
                {
                    //the request streams leave the socket open so they go first, the response stream closes the socket stream so it goes after the socket is handled
                    if (requestBodyCryptoStream is not null)
                    {
                        try
                        {
                            //disposing flushes its final block into the request stream, which can fail the same as the request did
#if NETSTANDARD2_0
                            requestBodyCryptoStream.Dispose();
#else
                            await requestBodyCryptoStream.DisposeAsync();
#endif
                        }
                        catch { }
                    }
                    if (requestBodyStream is not null)
                    {
#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                    }
                    if (isThrowingRemote)
                    {
                        if (stream is not null)
                            stream.Dispose();
                    }
                    else if (cancellationToken.IsCancellationRequested)
                    {
                        _ = Log.ErrorAsync(ex);
                        if (stream is not null)
                        {
                            //only while the server has the whole request and hasn't responded, mid request the abort byte reads as request data and mid response the server is done with it
                            var abortAcknowledged = requestBodyStream is null && !responseStarted && await SocketAbortMonitor.SendAndAcknowledgeAbortAsync(stream);
                            if (abortAcknowledged)
                                stream?.Dispose();
                            else
                                stream?.DisposeSocket();
                        }
                    }
                    else
                    {
                        if (stream is not null)
                        {
                            stream.DisposeSocket();
                            if (!stream.IsNewConnection && !responseStarted)
                            {
                                _ = Log.ErrorAsync(ex);
                                stream = null;
                                requireNewConnection = true;
                                goto newconnection;
                            }
                        }
                    }

                    if (responseBodyStream is not null)
                    {
#if NETSTANDARD2_0
                        responseBodyStream.Dispose();
#else
                        await responseBodyStream.DisposeAsync();
#endif
                    }

                    throw;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(bufferOwner);
                throttle.Release();
            }
        }
        /// <inheritdoc />
        protected override async Task<TResult> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source, CancellationToken cancellationToken) where TResult : default
        {
            await throttle.WaitAsync(cancellationToken);

            SocketPoolStream? stream = null;
            Stream? requestBodyStream = null;
            CryptoFlushStream? requestBodyCryptoStream = null;
            Stream? responseBodyStream = null;
            var bufferOwner = ArrayPoolHelper<byte>.Rent(HttpCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                var messageTypeName = commandType.GetNiceName();

                var messageData = ContentTypeSerializer.Serialize(contentType, command);

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CqrsRequestData()
                {
                    MessageType = messageTypeName,
                    MessageData = messageData,
                    MessageAwait = true,
                    MessageResult = true,

                    Claims = claims,
                    Source = source
                };

                Dictionary<string, List<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = await authorizer.GetAuthorizationHeadersAsync();

                var buffer = bufferOwner.AsMemory();

                var requireNewConnection = false;
                var responseStarted = false; //once the server responds it has the request, retrying on a new connection could run it twice
            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUri, data.MessageType, contentType, authHeaders); //the server matches the message type against it

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, 0, requireNewConnection, cancellationToken);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, Memory<byte>.Empty, requireNewConnection, cancellationToken);
#endif

                    requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true, true, buffer.Slice(0, requestHeaderLength)); //the header goes out with the body

                    if (symmetricConfig is not null)
                    {
                        requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true);
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data, cancellationToken);
#if NET5_0_OR_GREATER
                        await requestBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                        requestBodyCryptoStream.FlushFinalBlock();
#endif
#if NETSTANDARD2_0
                        requestBodyCryptoStream.Dispose();
#else
                        await requestBodyCryptoStream.DisposeAsync();
#endif
                        requestBodyCryptoStream = null;
                    }
                    else
                    {
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyStream, data, cancellationToken);
                        await requestBodyStream.FlushAsync(cancellationToken);

#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                    }

                    requestBodyStream = null;

                    //Response Header
                    var headerPosition = 0;
                    var headerLength = 0;
                    var headerEnd = false;
                    while (!headerEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(HttpCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, cancellationToken);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);
#endif

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.IsNewConnection || responseStarted)
                            {
                                stream = null;
                                throw new ConnectionAbortedException();
                            }
                            else
                            {
                                stream = null;
                                requireNewConnection = true;
                                goto newconnection;
                            }
                        }
                        headerLength += bytesRead;
                        responseStarted = true;

                        headerEnd = HttpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                    }
                    var responseHeader = HttpCommon.ReadHeader(buffer.Slice(0, headerLength), headerPosition);

                    //Response Body
                    responseBodyStream = new HttpProtocolBodyStream(responseHeader.Chuncked ? null : (responseHeader.ContentLength ?? 0), stream, responseHeader.BodyStartBuffer, false, false, endOnPeerSilence: true); //a 5.3 server does not end an unencrypted body

                    if (symmetricConfig is not null)
                        responseBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        //an error without a body, such as Kestrel's 401 or a 400 sent before the request is read, has no details to read so the status is the error
                        var responseException = !responseHeader.Chuncked && (responseHeader.ContentLength ?? 0) == 0
                            ? new RemoteServiceException($"Remote service responded {responseHeader.ErrorStatus} without details for {messageTypeName}")
                            : await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream, cancellationToken);
                        isThrowingRemote = true;
                        throw responseException;
                    }

                    if (isStream)
                    {
                        return (TResult)(object)responseBodyStream; //TODO better way to convert type???
                    }
                    else
                    {
                        var model = await ContentTypeSerializer.DeserializeAsync<TResult>(contentType, responseBodyStream, cancellationToken);
#if NETSTANDARD2_0
                        responseBodyStream.Dispose();
#else
                        await responseBodyStream.DisposeAsync();
#endif
                        return model!;
                    }
                }
                catch (Exception ex)
                {
                    //the request streams leave the socket open so they go first, the response stream closes the socket stream so it goes after the socket is handled
                    if (requestBodyCryptoStream is not null)
                    {
                        try
                        {
                            //disposing flushes its final block into the request stream, which can fail the same as the request did
#if NETSTANDARD2_0
                            requestBodyCryptoStream.Dispose();
#else
                            await requestBodyCryptoStream.DisposeAsync();
#endif
                        }
                        catch { }
                    }
                    if (requestBodyStream is not null)
                    {
#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                    }
                    if (isThrowingRemote)
                    {
                        if (stream is not null)
                            stream.Dispose();
                    }
                    else if (cancellationToken.IsCancellationRequested)
                    {
                        _ = Log.ErrorAsync(ex);
                        if (stream is not null)
                        {
                            //only while the server has the whole request and hasn't responded, mid request the abort byte reads as request data and mid response the server is done with it
                            var abortAcknowledged = requestBodyStream is null && !responseStarted && await SocketAbortMonitor.SendAndAcknowledgeAbortAsync(stream);
                            if (abortAcknowledged)
                                stream?.Dispose();
                            else
                                stream?.DisposeSocket();
                        }
                    }
                    else
                    {
                        if (stream is not null)
                        {
                            stream.DisposeSocket();
                            if (!stream.IsNewConnection && !responseStarted)
                            {
                                _ = Log.ErrorAsync(ex);
                                stream = null;
                                requireNewConnection = true;
                                goto newconnection;
                            }
                        }
                    }

                    if (responseBodyStream is not null)
                    {
#if NETSTANDARD2_0
                        responseBodyStream.Dispose();
#else
                        await responseBodyStream.DisposeAsync();
#endif
                    }

                    throw;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(bufferOwner);
                throttle.Release();
            }
        }

        /// <inheritdoc />
        protected override async Task DispatchInternal(SemaphoreSlim throttle, Type eventType, IEvent @event, string source, CancellationToken cancellationToken)
        {
            await throttle.WaitAsync(cancellationToken);

            SocketPoolStream? stream = null;
            Stream? requestBodyStream = null;
            CryptoFlushStream? requestBodyCryptoStream = null;
            Stream? responseBodyStream = null;
            var bufferOwner = ArrayPoolHelper<byte>.Rent(HttpCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                var messageTypeName = eventType.GetNiceName();

                var messageData = ContentTypeSerializer.Serialize(contentType, @event);

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CqrsRequestData()
                {
                    MessageType = messageTypeName,
                    MessageData = messageData,
                    MessageAwait = false,
                    MessageResult = false,

                    Claims = claims,
                    Source = source
                };

                Dictionary<string, List<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = await authorizer.GetAuthorizationHeadersAsync();

                var buffer = bufferOwner.AsMemory();

                var requireNewConnection = false;
                var responseStarted = false; //once the server responds it has the request, retrying on a new connection could run it twice
            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUri, data.MessageType, contentType, authHeaders); //the server matches the message type against it

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, 0, requireNewConnection, cancellationToken);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, Memory<byte>.Empty, requireNewConnection, cancellationToken);
#endif

                    requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true, true, buffer.Slice(0, requestHeaderLength)); //the header goes out with the body

                    if (symmetricConfig is not null)
                    {
                        requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true);
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data, cancellationToken);
#if NET5_0_OR_GREATER
                        await requestBodyCryptoStream.FlushFinalBlockAsync(cancellationToken);
#else
                        requestBodyCryptoStream.FlushFinalBlock();
#endif
#if NETSTANDARD2_0
                        requestBodyCryptoStream.Dispose();
#else
                        await requestBodyCryptoStream.DisposeAsync();
#endif
                        requestBodyCryptoStream = null;
                    }
                    else
                    {
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyStream, data, cancellationToken);
                        await requestBodyStream.FlushAsync(cancellationToken);

#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                    }

                    requestBodyStream = null;

                    //Response Header
                    var headerPosition = 0;
                    var headerLength = 0;
                    var headerEnd = false;
                    while (!headerEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(HttpCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, cancellationToken);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);
#endif

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.IsNewConnection || responseStarted)
                            {
                                stream = null;
                                throw new ConnectionAbortedException();
                            }
                            else
                            {
                                stream = null;
                                requireNewConnection = true;
                                goto newconnection;
                            }
                        }
                        headerLength += bytesRead;
                        responseStarted = true;

                        headerEnd = HttpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                    }
                    var responseHeader = HttpCommon.ReadHeader(buffer.Slice(0, headerLength), headerPosition);

                    //Response Body
                    responseBodyStream = new HttpProtocolBodyStream(responseHeader.Chuncked ? null : (responseHeader.ContentLength ?? 0), stream, responseHeader.BodyStartBuffer, false, false, endOnPeerSilence: true); //a 5.3 server does not end an unencrypted body

                    if (symmetricConfig is not null)
                        responseBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        //an error without a body, such as Kestrel's 401 or a 400 sent before the request is read, has no details to read so the status is the error
                        var responseException = !responseHeader.Chuncked && (responseHeader.ContentLength ?? 0) == 0
                            ? new RemoteServiceException($"Remote service responded {responseHeader.ErrorStatus} without details for {messageTypeName}")
                            : await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream, cancellationToken);
                        isThrowingRemote = true;
                        throw responseException;
                    }

#if NETSTANDARD2_0
                    responseBodyStream.Dispose();
#else
                    await responseBodyStream.DisposeAsync();
#endif
                }
                catch (Exception ex)
                {
                    //the request streams leave the socket open so they go first, the response stream closes the socket stream so it goes after the socket is handled
                    if (requestBodyCryptoStream is not null)
                    {
                        try
                        {
                            //disposing flushes its final block into the request stream, which can fail the same as the request did
#if NETSTANDARD2_0
                            requestBodyCryptoStream.Dispose();
#else
                            await requestBodyCryptoStream.DisposeAsync();
#endif
                        }
                        catch { }
                    }
                    if (requestBodyStream is not null)
                    {
#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                    }
                    if (isThrowingRemote)
                    {
                        if (stream is not null)
                            stream.Dispose();
                    }
                    else if (cancellationToken.IsCancellationRequested)
                    {
                        _ = Log.ErrorAsync(ex);
                        if (stream is not null)
                        {
                            //only while the server has the whole request and hasn't responded, mid request the abort byte reads as request data and mid response the server is done with it
                            var abortAcknowledged = requestBodyStream is null && !responseStarted && await SocketAbortMonitor.SendAndAcknowledgeAbortAsync(stream);
                            if (abortAcknowledged)
                                stream?.Dispose();
                            else
                                stream?.DisposeSocket();
                        }
                    }
                    else
                    {
                        if (stream is not null)
                        {
                            stream.DisposeSocket();
                            if (!stream.IsNewConnection && !responseStarted)
                            {
                                _ = Log.ErrorAsync(ex);
                                stream = null;
                                requireNewConnection = true;
                                goto newconnection;
                            }
                        }
                    }

                    if (responseBodyStream is not null)
                    {
#if NETSTANDARD2_0
                        responseBodyStream.Dispose();
#else
                        await responseBodyStream.DisposeAsync();
#endif
                    }

                    throw;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(bufferOwner);
                throttle.Release();
            }
        }
    }
}