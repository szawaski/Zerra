// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.IO;
using Zerra.Serialization;
using System.Security.Claims;
using System.Threading;
using System.Linq;

namespace Zerra.CQRS.Network
{
    public sealed class TcpRawCqrsClient : TcpCqrsClientBase
    {
        private readonly ContentType contentType;
        private readonly SymmetricConfig symmetricConfig;
        private readonly SocketClientPool socketPool;

        public TcpRawCqrsClient(ContentType contentType, string serviceUrl, SymmetricConfig symmetricConfig)
            : base(serviceUrl)
        {
            this.contentType = contentType;
            this.symmetricConfig = symmetricConfig;
            this.socketPool = SocketClientPool.Shared;

            _ = Log.InfoAsync($"{nameof(TcpRawCqrsClient)} started for {this.contentType} at {serviceUrl}");
        }

        protected override TReturn CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source)
        {
            throttle.Wait();

            //Socket socket = null;
            SocketPoolStream stream = null;
            Stream requestBodyStream = null;
            CryptoFlushStream requestBodyCryptoStream = null;
            Stream responseBodyStream = null;
            var bufferOwner = BufferArrayPool<byte>.Rent(TcpRawCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                string[][] claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CQRSRequestData()
                {
                    ProviderType = interfaceType.Name,
                    ProviderMethod = methodName,

                    Claims = claims,
                    Source = source
                };
                data.AddProviderArguments(arguments);

                var buffer = bufferOwner.AsMemory();

            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = TcpRawCommon.BufferHeader(buffer, data.ProviderType, contentType);

#if NETSTANDARD2_0
                    stream = socketPool.BeginStream(host, port, ProtocolType.Tcp, bufferOwner, 0, requestHeaderLength);
#else
                    stream = socketPool.BeginStream(host, port, ProtocolType.Tcp, buffer.Span.Slice(0, requestHeaderLength));
#endif

                    requestBodyStream = new TcpRawProtocolBodyStream(stream, null, true);

                    if (symmetricConfig != null)
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
                    var requestHeaderEnd = false;
                    while (!requestHeaderEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(TcpRawCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                        var bytesRead = stream.Read(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                        var bytesRead = stream.Read(buffer.Span.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.NewConnection)
                                throw new ConnectionAbortedException();
                            stream = null;
                            goto newconnection;
                        }
                        headerLength += bytesRead;

                        requestHeaderEnd = TcpRawCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                    }
                    var responseHeader = TcpRawCommon.ReadHeader(buffer, headerPosition, headerLength);

                    responseBodyStream = new TcpRawProtocolBodyStream(stream, responseHeader.BodyStartBuffer, false);

                    if (symmetricConfig != null)
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
                catch
                {
                    try
                    {
                        //crypto stream can error, we want to throw the actual error
                        responseBodyStream?.Dispose();
                    }
                    catch { }
                    if (requestBodyStream != null)
                        requestBodyStream.Dispose();
                    if (requestBodyCryptoStream != null)
                        requestBodyCryptoStream.Dispose();
                    if (isThrowingRemote)
                    {
                        if (stream != null)
                            stream.Dispose();
                    }
                    else
                    {
                        if (stream != null)
                        {
                            stream.DisposeSocket();
                            if (!stream.NewConnection)
                            {
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
                BufferArrayPool<byte>.Return(bufferOwner);
                _ = throttle.Release();
            }
        }

        protected override async Task<TReturn> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source)
        {
            await throttle.WaitAsync();

            SocketPoolStream stream = null;
            Stream requestBodyStream = null;
            CryptoFlushStream requestBodyCryptoStream = null;
            Stream responseBodyStream = null;
            var bufferOwner = BufferArrayPool<byte>.Rent(TcpRawCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                string[][] claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CQRSRequestData()
                {
                    ProviderType = interfaceType.Name,
                    ProviderMethod = methodName,

                    Claims = claims,
                    Source = source
                };
                data.AddProviderArguments(arguments);

                var buffer = bufferOwner.AsMemory();

            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = TcpRawCommon.BufferHeader(buffer, data.ProviderType, contentType);

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, requestHeaderLength);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, buffer.Slice(0, requestHeaderLength));
#endif

                    requestBodyStream = new TcpRawProtocolBodyStream(stream, null, true);

                    if (symmetricConfig != null)
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
                    var requestHeaderEnd = false;
                    while (!requestHeaderEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(TcpRawCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.NewConnection)
                                throw new ConnectionAbortedException();
                            stream = null;
                            goto newconnection;
                        }
                        headerLength += bytesRead;

                        requestHeaderEnd = TcpRawCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                    }
                    var responseHeader = TcpRawCommon.ReadHeader(buffer, headerPosition, headerLength);

                    //Response Body
                    responseBodyStream = new TcpRawProtocolBodyStream(stream, responseHeader.BodyStartBuffer, false);

                    if (symmetricConfig != null)
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
                        if (responseBodyStream != null)
                        {
#if NETSTANDARD2_0
                            responseBodyStream.Dispose();
#else
                            await responseBodyStream.DisposeAsync();
#endif
                        }
                        return model;
                    }
                }
                catch
                {
                    if (responseBodyStream != null)
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
                    if (requestBodyStream != null)
                    {
#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                    }
                    if (requestBodyCryptoStream != null)
                    {
#if NETSTANDARD2_0
                        requestBodyCryptoStream.Dispose();
#else
                        await requestBodyCryptoStream.DisposeAsync();
#endif
                    }
                    if (isThrowingRemote)
                    {
                        if (stream != null)
                            stream.Dispose();
                    }
                    else
                    {
                        if (stream != null)
                        {
                            stream.DisposeSocket();
                            if (!stream.NewConnection)
                            {
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
                BufferArrayPool<byte>.Return(bufferOwner);
                throttle.Release();
            }
        }

        protected override async Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source)
        {
            await throttle.WaitAsync();

            SocketPoolStream stream = null;
            Stream requestBodyStream = null;
            CryptoFlushStream requestBodyCryptoStream = null;
            Stream responseBodyStream = null;
            var bufferOwner = BufferArrayPool<byte>.Rent(TcpRawCommon.BufferLength);
            var isThrowingRemote = false;
            try
            {
                var messageTypeName = commandType.GetNiceName();

                var messageData = JsonSerializer.Serialize(command, commandType);

                string[][] claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var data = new CQRSRequestData()
                {
                    MessageType = messageTypeName,
                    MessageData = messageData,
                    MessageAwait = messageAwait,

                    Claims = claims,
                    Source = source
                };

                var buffer = bufferOwner.AsMemory();

            newconnection:
                try
                {
                    //Request Header
                    var requestHeaderLength = TcpRawCommon.BufferHeader(buffer, data.MessageType, contentType);

#if NETSTANDARD2_0
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, bufferOwner, 0, requestHeaderLength);
#else
                    stream = await socketPool.BeginStreamAsync(host, port, ProtocolType.Tcp, buffer.Slice(0, requestHeaderLength));
#endif

                    requestBodyStream = new TcpRawProtocolBodyStream(stream, null, true);

                    if (symmetricConfig != null)
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
                    var requestHeaderEnd = false;
                    while (!requestHeaderEnd)
                    {
                        if (headerLength == buffer.Length)
                            throw new CqrsNetworkException($"{nameof(TcpRawCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                        var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                        var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                        if (bytesRead == 0)
                        {
                            stream.DisposeSocket();
                            if (stream.NewConnection)
                                throw new ConnectionAbortedException();
                            stream = null;
                            goto newconnection;
                        }
                        headerLength += bytesRead;

                        requestHeaderEnd = TcpRawCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                    }
                    var responseHeader = TcpRawCommon.ReadHeader(buffer, headerPosition, headerLength);

                    //Response Body
                    responseBodyStream = new TcpRawProtocolBodyStream(stream, responseHeader.BodyStartBuffer, false);

                    if (symmetricConfig != null)
                        responseBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, responseBodyStream, false);

                    if (responseHeader.IsError)
                    {
                        var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream);
                        isThrowingRemote = true;
                        throw responseException;
                    }

                    if (responseBodyStream != null)
                    {
#if NETSTANDARD2_0
                        responseBodyStream.Dispose();
#else
                        await responseBodyStream.DisposeAsync();
#endif
                    }
                }
                catch
                {
                    if (responseBodyStream != null)
                    {
#if NETSTANDARD2_0
                        responseBodyStream.Dispose();
#else
                        await responseBodyStream.DisposeAsync();
#endif
                    }
                    if (requestBodyStream != null)
                    {
#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                    }
                    if (requestBodyCryptoStream != null)
                    {
#if NETSTANDARD2_0
                        requestBodyCryptoStream.Dispose();
#else
                        await requestBodyCryptoStream.DisposeAsync();
#endif
                    }
                    if (isThrowingRemote)
                    {
                        if (stream != null)
                            stream.Dispose();
                    }
                    else
                    {
                        if (stream != null)
                        {
                            stream.DisposeSocket();
                            if (!stream.NewConnection)
                            {
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
                BufferArrayPool<byte>.Return(bufferOwner);
                throttle.Release();
            }
        }

        public static TcpRawCqrsClient CreateDefault(string endpoint, SymmetricConfig symmetricConfig)
        {
            return new TcpRawCqrsClient(ContentType.Bytes, endpoint, symmetricConfig);
        }
    }
}