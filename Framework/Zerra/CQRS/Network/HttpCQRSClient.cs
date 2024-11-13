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
using Zerra.Serialization.Json;
using System.Security.Claims;
using System.Threading;
using System.Linq;
using Zerra.Buffers;

namespace Zerra.CQRS.Network
{
    public sealed class HttpCqrsClient : TcpCqrsClientBase
    {
        private readonly ContentType contentType;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly SocketClientPool socketPool;

        public HttpCqrsClient(ContentType contentType, string serviceUrl, SymmetricConfig? symmetricConfig, ICqrsAuthorizer? authorizer)
            : base(serviceUrl)
        {
            this.contentType = contentType;
            this.symmetricConfig = symmetricConfig;
            this.authorizer = authorizer;
            this.socketPool = SocketClientPool.Shared;

            _ = Log.InfoAsync($"{nameof(HttpCqrsClient)} started for {this.contentType} at {serviceUrl}");
        }

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

                IDictionary<string, IList<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = authorizer.BuildAuthHeaders();

                var buffer = bufferOwner.AsMemory();

            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUri, null, data.ProviderType, contentType, authHeaders);

#if NETSTANDARD2_0
                    stream = socketPool.BeginStream(host, port, ProtocolType.Tcp, bufferOwner, 0, requestHeaderLength);
#else
                    stream = socketPool.BeginStream(host, port, ProtocolType.Tcp, buffer.Span.Slice(0, requestHeaderLength));
#endif

                    requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true);

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

#if NETSTANDARD2_0
                        var bytesRead = stream.Read(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                        var bytesRead = stream.Read(buffer.Span.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.IsNewConnection)
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

                        headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                    }
                    var responseHeader = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);

                    //Response Body
                    responseBodyStream = new HttpProtocolBodyStream(null, stream, responseHeader.BodyStartBuffer, false);

                    if (symmetricConfig is not null)
                        responseBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        var responseException = ContentTypeSerializer.DeserializeException(contentType, responseBodyStream);
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
                    if (responseBodyStream is not null)
                        responseBodyStream.Dispose();
                    if (requestBodyCryptoStream is not null)
                        requestBodyCryptoStream.Dispose();
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
                            if (!stream.IsNewConnection)
                            {
                                _ = Log.ErrorAsync(ex);
                                stream = null;
                                goto newconnection;
                            }
                        }
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

        protected override async Task<TReturn?> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source) where TReturn : default
        {
            await throttle.WaitAsync();

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

                IDictionary<string, IList<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = authorizer.BuildAuthHeaders();

                var buffer = bufferOwner.AsMemory();

            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUri, null, data.ProviderType, contentType, authHeaders);

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, requestHeaderLength);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, buffer.Slice(0, requestHeaderLength));
#endif

                    requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true);

                    if (symmetricConfig is not null)
                    {
                        requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true);
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data);
#if NET5_0_OR_GREATER
                        await requestBodyCryptoStream.FlushFinalBlockAsync();
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
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyStream, data);
                        await requestBodyStream.FlushAsync();
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
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.IsNewConnection)
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

                        headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                    }
                    var responseHeader = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);

                    //Response Body
                    responseBodyStream = new HttpProtocolBodyStream(null, stream, responseHeader.BodyStartBuffer, false);

                    if (symmetricConfig is not null)
                        responseBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream);
                        isThrowingRemote = true;
                        throw responseException;
                    }

                    if (isStream)
                    {
                        return (TReturn)(object)responseBodyStream; //TODO better way to convert type???
                    }
                    else
                    {
                        var model = await ContentTypeSerializer.DeserializeAsync<TReturn>(contentType, responseBodyStream);
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
                    if (requestBodyCryptoStream is not null)
                    {
#if NETSTANDARD2_0
                        requestBodyCryptoStream.Dispose();
#else
                        await requestBodyCryptoStream.DisposeAsync();
#endif
                    }
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
                            if (!stream.IsNewConnection)
                            {
                                _ = Log.ErrorAsync(ex);
                                stream = null;
                                goto newconnection;
                            }
                        }
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

        protected override async Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source)
        {
            await throttle.WaitAsync();

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

                IDictionary<string, IList<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = authorizer.BuildAuthHeaders();

                var buffer = bufferOwner.AsMemory();

            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUri, null, data.ProviderType, contentType, authHeaders);

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, requestHeaderLength);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, buffer.Slice(0, requestHeaderLength));
#endif

                    requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true);

                    if (symmetricConfig is not null)
                    {
                        requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true);
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data);
#if NET5_0_OR_GREATER
                        await requestBodyCryptoStream.FlushFinalBlockAsync();
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
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyStream, data);
                        await requestBodyStream.FlushAsync();

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
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.IsNewConnection)
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

                        headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                    }
                    var responseHeader = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);

                    //Response Body
                    responseBodyStream = new HttpProtocolBodyStream(null, stream, responseHeader.BodyStartBuffer, false);

                    if (symmetricConfig is not null)
                        responseBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream);
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
                    if (requestBodyCryptoStream is not null)
                    {
#if NETSTANDARD2_0
                        requestBodyCryptoStream.Dispose();
#else
                        await requestBodyCryptoStream.DisposeAsync();
#endif
                    }
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
                            if (!stream.IsNewConnection)
                            {
                                _ = Log.ErrorAsync(ex);
                                stream = null;
                                goto newconnection;
                            }
                        }
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

        protected override async Task<TResult?> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source) where TResult : default
        {
            await throttle.WaitAsync();

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

                IDictionary<string, IList<string?>>? authHeaders = null;
                if (authorizer is not null)
                    authHeaders = authorizer.BuildAuthHeaders();

                var buffer = bufferOwner.AsMemory();

            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUri, null, data.ProviderType, contentType, authHeaders);

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, requestHeaderLength);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, buffer.Slice(0, requestHeaderLength));
#endif

                    requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true);

                    if (symmetricConfig is not null)
                    {
                        requestBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, requestBodyStream, true);
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyCryptoStream, data);
#if NET5_0_OR_GREATER
                        await requestBodyCryptoStream.FlushFinalBlockAsync();
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
                        await ContentTypeSerializer.SerializeAsync(contentType, requestBodyStream, data);
                        await requestBodyStream.FlushAsync();

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
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.IsNewConnection)
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

                        headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                    }
                    var responseHeader = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);

                    //Response Body
                    responseBodyStream = new HttpProtocolBodyStream(null, stream, responseHeader.BodyStartBuffer, false);

                    if (symmetricConfig is not null)
                        responseBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream);
                        isThrowingRemote = true;
                        throw responseException;
                    }

                    if (isStream)
                    {
                        return (TResult)(object)responseBodyStream; //TODO better way to convert type???
                    }
                    else
                    {
                        var model = await ContentTypeSerializer.DeserializeAsync<TResult>(contentType, responseBodyStream);
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
                    if (requestBodyCryptoStream is not null)
                    {
#if NETSTANDARD2_0
                        requestBodyCryptoStream.Dispose();
#else
                        await requestBodyCryptoStream.DisposeAsync();
#endif
                    }
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
                            if (!stream.IsNewConnection)
                            {
                                _ = Log.ErrorAsync(ex);
                                stream = null;
                                goto newconnection;
                            }
                        }
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