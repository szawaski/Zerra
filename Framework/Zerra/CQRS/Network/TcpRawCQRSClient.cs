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
    public sealed class TcpRawCQRSClient : TcpCQRSClientBase
    {
        private readonly ContentType contentType;
        private readonly SymmetricConfig symmetricConfig;
        private readonly SocketPool socketPool;

        public TcpRawCQRSClient(ContentType contentType, string serviceUrl, SymmetricConfig symmetricConfig)
            : base(serviceUrl)
        {
            this.contentType = contentType;
            this.symmetricConfig = symmetricConfig;
            this.socketPool = SocketPool.Default;

            _ = Log.InfoAsync($"{nameof(TcpRawCQRSClient)} started for {this.contentType} at {serviceUrl} as {this.ipEndpoint}");
        }

        protected override TReturn CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source)
        {
            throttle.Wait();

            //Socket socket = null;
            Stream stream = null;
            Stream requestBodyStream = null;
            FinalBlockStream requestBodyCryptoStream = null;
            Stream responseBodyStream = null;
            var bufferOwner = BufferArrayPool<byte>.Rent(TcpRawCommon.BufferLength);
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

                stream = SocketPool.Default.GetStream(ipEndpoint);

                //socket = new Socket(ipEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //socket.NoDelay = true;
                //socket.Connect(ipEndpoint.Address, ipEndpoint.Port);
                //stream = new NetworkStream(socket, true);

                var buffer = bufferOwner.AsMemory();

                //Request Header
                var requestHeaderLength = TcpRawCommon.BufferHeader(buffer, data.ProviderType, contentType);
#if NETSTANDARD2_0
                stream.Write(bufferOwner, 0, requestHeaderLength);
#else
                stream.Write(buffer.Span.Slice(0, requestHeaderLength));
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
                        throw new Exception($"{nameof(TcpRawCQRSClient)} Header Too Long");

#if NETSTANDARD2_0
                    var bytesRead = stream.Read(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                    var bytesRead = stream.Read(buffer.Span.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                    if (bytesRead == 0)
                        throw new CQRSRequestAbortedException();
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
                    //socket?.Dispose();
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
                if (stream != null)
                    stream.Dispose();
                //socket?.Dispose();
                throw;
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

            //Socket socket = null;
            Stream stream = null;
            Stream requestBodyStream = null;
            FinalBlockStream requestBodyCryptoStream = null;
            Stream responseBodyStream = null;
            var bufferOwner = BufferArrayPool<byte>.Rent(TcpRawCommon.BufferLength);
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

                stream = await socketPool.GetStreamAsync(ipEndpoint);

                //socket = new Socket(ipEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //socket.NoDelay = true;
                //socket.Connect(ipEndpoint.Address, ipEndpoint.Port);
                //stream = new NetworkStream(socket, false);

                var buffer = bufferOwner.AsMemory();

                //Request Header
                var requestHeaderLength = TcpRawCommon.BufferHeader(buffer, data.ProviderType, contentType);
#if NETSTANDARD2_0
                await stream.WriteAsync(bufferOwner, 0, requestHeaderLength);
#else
                await stream.WriteAsync(buffer.Slice(0, requestHeaderLength));
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
                        throw new Exception($"{nameof(TcpRawCQRSClient)} Header Too Long");

#if NETSTANDARD2_0
                    var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                    var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                    if (bytesRead == 0)
                        throw new CQRSRequestAbortedException();
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
                    //socket.Dispose();
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
                if (stream != null)
                {
#if NETSTANDARD2_0
                    stream.Dispose();
#else
                    await stream.DisposeAsync();
#endif
                }
                //socket?.Dispose();
                throw;
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

            Socket socket = null;
            Stream stream = null;
            Stream requestBodyStream = null;
            FinalBlockStream requestBodyCryptoStream = null;
            Stream responseBodyStream = null;
            var bufferOwner = BufferArrayPool<byte>.Rent(TcpRawCommon.BufferLength);
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

                stream = await socketPool.GetStreamAsync(ipEndpoint);

                //socket = new Socket(ipEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //socket.NoDelay = true;
                //socket.Connect(ipEndpoint.Address, ipEndpoint.Port);
                //stream = new NetworkStream(socket, true);

                var buffer = bufferOwner.AsMemory();

                //Request Header
                var requestHeaderLength = TcpRawCommon.BufferHeader(buffer, data.MessageType, contentType);
#if NETSTANDARD2_0
                await stream.WriteAsync(bufferOwner, 0, requestHeaderLength);
#else
                await stream.WriteAsync(buffer.Slice(0, requestHeaderLength));
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
                        throw new Exception($"{nameof(TcpRawCQRSClient)} Header Too Long");

#if NETSTANDARD2_0
                    var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                    var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                    if (bytesRead == 0)
                        throw new CQRSRequestAbortedException();
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
                socket.Dispose();
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
                if (stream != null)
                {
#if NETSTANDARD2_0
                    stream.Dispose();
#else
                    await stream.DisposeAsync();
#endif
                }
                socket?.Dispose();
                throw;
            }
            finally
            {
                BufferArrayPool<byte>.Return(bufferOwner);
                throttle.Release();
            }
        }

        public static TcpRawCQRSClient CreateDefault(string endpoint, SymmetricConfig symmetricConfig)
        {
            return new TcpRawCQRSClient(ContentType.Bytes, endpoint, symmetricConfig);
        }
    }
}