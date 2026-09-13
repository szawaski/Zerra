// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net.Sockets;
using Zerra.Encryption;
using Zerra.Logging;
using System.Security.Claims;
using Zerra.Buffers;
using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    /// <summary>
    /// A CQRS Client using custom TCP communication.
    /// </summary>
    public sealed class TcpCqrsClient : CqrsClientBase
    {
        private readonly ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly SocketClientPool socketPool;

        /// <summary>
        /// Creates a new TCP CQRS client.
        /// </summary>
        /// <param name="serviceUrl">The URL of the server.</param>
        /// <param name="serializer">The serializer for request and response data.</param>
        /// <param name="encryptor">Optional encryption provider for encrypting request and response data.</param>
        /// <param name="log">Optional logging provider.</param>
        public TcpCqrsClient(string serviceUrl, ISerializer serializer, IEncryptor? encryptor, ILogger? log)
            : base(serviceUrl, log)
        {
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.socketPool = SocketClientPool.Shared;
        }

        /// <inheritdoc />
        protected override TReturn CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source)
        {
            throttle.Wait();

            SocketPoolStream? stream = null;
            Stream? requestBodyStream = null;
            CryptoFlushStream? requestBodyCryptoStream = null;
            Stream? responseBodyStream = null;
            var bufferOwner = ArrayPoolHelper<byte>.Rent(TcpCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CqrsRequestData()
                {
                    ProviderType = interfaceType.AssemblyQualifiedName ?? throw new ArgumentException("Handler interface must have AssemblyQualifiedName"),
                    ProviderMethod = methodName,

                    Claims = claims,
                    Source = source
                };

                //a trailing CancellationToken isn't sent, the server passes its own in its place
                var serializeCount = argumentTypes.Count > 0 && argumentTypes[argumentTypes.Count - 1] == typeof(CancellationToken) ? argumentTypes.Count - 1 : argumentTypes.Count;
                data.ProviderArguments = new byte[argumentTypes.Count][];
                for (var i = 0; i < serializeCount; i++)
                    data.ProviderArguments[i] = serializer.SerializeBytes(arguments[i], argumentTypes[i]);

                var buffer = bufferOwner.AsMemory();

                var requireNewConnection = false;
                var responseStarted = false; //once the server responds it has the request, retrying on a new connection could run it twice
            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = TcpCommon.BufferHeader(buffer, data.ProviderType, serializer.ContentType);

                    stream = socketPool.BeginStream(host, port, ProtocolType.Tcp, ReadOnlySpan<byte>.Empty, requireNewConnection, CancellationToken.None);

                    requestBodyStream = new TcpProtocolBodyStream(stream, null, true, true, buffer.Slice(0, requestHeaderLength)); //the header goes out with the body

                    if (encryptor is not null)
                    {
                        requestBodyCryptoStream = encryptor.Encrypt(requestBodyStream, true);
                        serializer.Serialize(requestBodyCryptoStream, data);
                        requestBodyCryptoStream.FlushFinalBlock();
                        requestBodyCryptoStream.Dispose();
                        requestBodyCryptoStream = null;
                    }
                    else
                    {
                        serializer.Serialize(requestBodyStream, data);
                        requestBodyStream.Flush();
                        requestBodyStream.Dispose();
                    }

                    requestBodyStream = null;

                    //Response Header
                    var headerPosition = 0;
                    var headerLength = 0;
                    var requestHeaderEnd = false;
                    while (!requestHeaderEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(TcpCqrsClient)} Header Too Long");

                        var bytesRead = stream.Read(buffer.Span.Slice(headerPosition, buffer.Length - headerPosition));

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

                        requestHeaderEnd = TcpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                    }
                    var responseHeader = TcpCommon.ReadHeader(buffer[..headerLength], headerPosition);

                    //Response Body
                    if (isStream)
                        responseBodyStream = new TcpProtocolBodyStream(stream, responseHeader.BodyStartBuffer.ToArray(), false, false);
                    else
                        responseBodyStream = new TcpProtocolBodyStream(stream, responseHeader.BodyStartBuffer, false, false);

                    if (encryptor is not null)
                        responseBodyStream = encryptor.Decrypt(responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        var responseException = ExceptionSerializer.Deserialize($"{interfaceType.Name}.{methodName}", serializer, responseBodyStream);
                        isThrowingRemote = true;
                        throw responseException;
                    }

                    if (isStream)
                    {
                        return (TReturn)(object)responseBodyStream; //TODO better way to convert type???
                    }
                    else
                    {
                        var model = serializer.Deserialize<TReturn>(responseBodyStream);
                        responseBodyStream.Dispose();
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
                                log?.Error(ex);
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

                    if (isThrowingRemote)
                        throw;
                    else
                        throw new Exception($"Call failed for {interfaceType.Name}.{methodName} - {ex.GetBaseException().Message}");
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(bufferOwner);
                throttle.Release();
            }
        }

        /// <inheritdoc />
        protected override async Task<TReturn> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source, CancellationToken cancellationToken) where TReturn : default
        {
            await throttle.WaitAsync(cancellationToken);

            SocketPoolStream? stream = null;
            Stream? requestBodyStream = null;
            CryptoFlushStream? requestBodyCryptoStream = null;
            Stream? responseBodyStream = null;
            var bufferOwner = ArrayPoolHelper<byte>.Rent(TcpCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CqrsRequestData()
                {
                    ProviderType = interfaceType.AssemblyQualifiedName ?? throw new ArgumentException("Handler interface must have AssemblyQualifiedName"),
                    ProviderMethod = methodName,

                    Claims = claims,
                    Source = source
                };

                //a trailing CancellationToken isn't sent, the server passes its own in its place
                var serializeCount = argumentTypes.Count > 0 && argumentTypes[argumentTypes.Count - 1] == typeof(CancellationToken) ? argumentTypes.Count - 1 : argumentTypes.Count;
                data.ProviderArguments = new byte[argumentTypes.Count][];
                for (var i = 0; i < serializeCount; i++)
                    data.ProviderArguments[i] = serializer.SerializeBytes(arguments[i], argumentTypes[i]);

                var buffer = bufferOwner.AsMemory();

                var requireNewConnection = false;
                var responseStarted = false; //once the server responds it has the request, retrying on a new connection could run it twice
            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = TcpCommon.BufferHeader(buffer, data.ProviderType, serializer.ContentType);

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, 0, requireNewConnection, cancellationToken);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, Memory<byte>.Empty, requireNewConnection, cancellationToken);
#endif

                    requestBodyStream = new TcpProtocolBodyStream(stream, null, true, true, buffer.Slice(0, requestHeaderLength)); //the header goes out with the body

                    if (encryptor is not null)
                    {
                        requestBodyCryptoStream = encryptor.Encrypt(requestBodyStream, true);
                        await serializer.SerializeAsync(requestBodyCryptoStream, data, cancellationToken);
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
                        await serializer.SerializeAsync(requestBodyStream, data, cancellationToken);
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
                    var requestHeaderEnd = false;
                    while (!requestHeaderEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(TcpCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition, cancellationToken);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition), cancellationToken);
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

                        requestHeaderEnd = TcpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                    }
                    var responseHeader = TcpCommon.ReadHeader(buffer[..headerLength], headerPosition);

                    //Response Body
                    if (isStream)
                        responseBodyStream = new TcpProtocolBodyStream(stream, responseHeader.BodyStartBuffer.ToArray(), false, false);
                    else
                        responseBodyStream = new TcpProtocolBodyStream(stream, responseHeader.BodyStartBuffer, false, false);

                    if (encryptor is not null)
                        responseBodyStream = encryptor.Decrypt(responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        var responseException = await ExceptionSerializer.DeserializeAsync($"{interfaceType.Name}.{methodName}", serializer, responseBodyStream, cancellationToken);
                        isThrowingRemote = true;
                        throw responseException;
                    }

                    if (isStream)
                    {
                        return (TReturn)(object)responseBodyStream; //TODO better way to convert type???
                    }
                    else
                    {
                        var model = await serializer.DeserializeAsync<TReturn>(responseBodyStream, cancellationToken);
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
                        log?.Error(ex);
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
                                log?.Error(ex);
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
#if NETSTANDARD2_0
                            responseBodyStream.Dispose();
#else
                            await responseBodyStream.DisposeAsync();
#endif
                        }
                        catch { }
                    }

                    if (isThrowingRemote || cancellationToken.IsCancellationRequested)
                        throw;
                    else
                        throw new Exception($"Call failed for {interfaceType.Name}.{methodName} - {ex.GetBaseException().Message}");
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
            var bufferOwner = ArrayPoolHelper<byte>.Rent(TcpCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                var messageTypeName = commandType.AssemblyQualifiedName ?? throw new ArgumentException("Command Type must have Assembly Qualified Name");

                var messageData = serializer.SerializeBytes(command, commandType);

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

                var buffer = bufferOwner.AsMemory();

                var requireNewConnection = false;
                var responseStarted = false; //once the server responds it has the request, retrying on a new connection could run it twice
            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = TcpCommon.BufferHeader(buffer, data.MessageType, serializer.ContentType);

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, 0, requireNewConnection, cancellationToken);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, Memory<byte>.Empty, requireNewConnection, cancellationToken);
#endif

                    requestBodyStream = new TcpProtocolBodyStream(stream, null, true, true, buffer.Slice(0, requestHeaderLength)); //the header goes out with the body

                    if (encryptor is not null)
                    {
                        requestBodyCryptoStream = encryptor.Encrypt(requestBodyStream, true);
                        await serializer.SerializeAsync(requestBodyCryptoStream, data, cancellationToken);
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
                        await serializer.SerializeAsync(requestBodyStream, data, cancellationToken);
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
                    var requestHeaderEnd = false;
                    while (!requestHeaderEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(TcpCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition, cancellationToken);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition), cancellationToken);
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

                        requestHeaderEnd = TcpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                    }
                    var responseHeader = TcpCommon.ReadHeader(buffer[..headerLength], headerPosition);

                    //Response Body
                    responseBodyStream = new TcpProtocolBodyStream(stream, responseHeader.BodyStartBuffer, false, false);

                    if (encryptor is not null)
                        responseBodyStream = encryptor.Decrypt(responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        var responseException = await ExceptionSerializer.DeserializeAsync(commandType.Name, serializer, responseBodyStream, cancellationToken);
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
                            stream?.Dispose();
                    }
                    else if (cancellationToken.IsCancellationRequested)
                    {
                        log?.Error(ex);
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
                                log?.Error(ex);
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

                    if (isThrowingRemote || cancellationToken.IsCancellationRequested)
                        throw;
                    else
                        throw new Exception($"Dispatch failed for {commandType.Name} - {ex.GetBaseException().Message}");
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
            var bufferOwner = ArrayPoolHelper<byte>.Rent(TcpCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                var messageTypeName = commandType.AssemblyQualifiedName ?? throw new ArgumentException("Command Type must have Assembly Qualified Name");

                var messageData = serializer.SerializeBytes(command, commandType);

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

                var buffer = bufferOwner.AsMemory();

                var requireNewConnection = false;
                var responseStarted = false; //once the server responds it has the request, retrying on a new connection could run it twice
            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = TcpCommon.BufferHeader(buffer, data.MessageType, serializer.ContentType);

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, 0, requireNewConnection, cancellationToken);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, Memory<byte>.Empty, requireNewConnection, cancellationToken);
#endif

                    requestBodyStream = new TcpProtocolBodyStream(stream, null, true, true, buffer.Slice(0, requestHeaderLength)); //the header goes out with the body

                    if (encryptor is not null)
                    {
                        requestBodyCryptoStream = encryptor.Encrypt(requestBodyStream, true);
                        await serializer.SerializeAsync(requestBodyCryptoStream, data, cancellationToken);
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
                        await serializer.SerializeAsync(requestBodyStream, data, cancellationToken);
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
                    var requestHeaderEnd = false;
                    while (!requestHeaderEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(TcpCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition, cancellationToken);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition), cancellationToken);
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

                        requestHeaderEnd = TcpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                    }
                    var responseHeader = TcpCommon.ReadHeader(buffer[..headerLength], headerPosition);

                    //Response Body
                    responseBodyStream = new TcpProtocolBodyStream(stream, responseHeader.BodyStartBuffer, false, false);

                    if (encryptor is not null)
                        responseBodyStream = encryptor.Decrypt(responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        var responseException = await ExceptionSerializer.DeserializeAsync(commandType.Name, serializer, responseBodyStream, cancellationToken);
                        isThrowingRemote = true;
                        throw responseException;
                    }

                    if (isStream)
                    {
                        return (TResult)(object)responseBodyStream; //TODO better way to convert type???
                    }
                    else
                    {
                        var model = await serializer.DeserializeAsync<TResult>(responseBodyStream, cancellationToken);
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
                        log?.Error(ex);
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
                                log?.Error(ex);
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
                   
                    if (isThrowingRemote || cancellationToken.IsCancellationRequested)
                        throw;
                    else
                        throw new Exception($"Dispatch failed for {commandType.Name} - {ex.GetBaseException().Message}");
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
            var bufferOwner = ArrayPoolHelper<byte>.Rent(TcpCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                var messageTypeName = eventType.AssemblyQualifiedName ?? throw new ArgumentException("Event Type must have Assembly Qualified Name");

                var messageData = serializer.SerializeBytes(@event, eventType);

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

                var buffer = bufferOwner.AsMemory();

                var requireNewConnection = false;
                var responseStarted = false; //once the server responds it has the request, retrying on a new connection could run it twice
            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = TcpCommon.BufferHeader(buffer, data.MessageType, serializer.ContentType);

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, 0, requireNewConnection, cancellationToken);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, Memory<byte>.Empty, requireNewConnection, cancellationToken);
#endif

                    requestBodyStream = new TcpProtocolBodyStream(stream, null, true, true, buffer.Slice(0, requestHeaderLength)); //the header goes out with the body

                    if (encryptor is not null)
                    {
                        requestBodyCryptoStream = encryptor.Encrypt(requestBodyStream, true);
                        await serializer.SerializeAsync(requestBodyCryptoStream, data, cancellationToken);
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
                        await serializer.SerializeAsync(requestBodyStream, data, cancellationToken);
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
                    var requestHeaderEnd = false;
                    while (!requestHeaderEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(TcpCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition, cancellationToken);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition), cancellationToken);
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

                        requestHeaderEnd = TcpCommon.TryReadToHeaderEnd(bufferOwner.AsSpan(0, headerLength), ref headerPosition);
                    }
                    var responseHeader = TcpCommon.ReadHeader(buffer[..headerLength], headerPosition);

                    //Response Body
                    responseBodyStream = new TcpProtocolBodyStream(stream, responseHeader.BodyStartBuffer, false, false);

                    if (encryptor is not null)
                        responseBodyStream = encryptor.Decrypt(responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        var responseException = await ExceptionSerializer.DeserializeAsync(eventType.Name, serializer, responseBodyStream, cancellationToken);
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
                        log?.Error(ex);
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
                                log?.Error(ex);
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

                    if (isThrowingRemote || cancellationToken.IsCancellationRequested)
                        throw;
                    else
                        throw new Exception($"Dispatch failed for {eventType.Name} - {ex.GetBaseException().Message}");
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