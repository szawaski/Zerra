// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    public class TcpRawCQRSClient : TcpCQRSClientBase
    {
        private readonly NetworkType networkType;
        private readonly ContentType contentType;
        private readonly SymmetricKey encryptionKey;
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AES;

        public TcpRawCQRSClient(NetworkType networkType, ContentType contentType, string serviceUrl, SymmetricKey encryptionKey)
            : base(serviceUrl)
        {
            this.networkType = networkType;
            this.contentType = contentType;
            this.encryptionKey = encryptionKey;

            _ = Log.TraceAsync($"{nameof(TcpRawCQRSClient)} Started For {this.networkType} {this.contentType} {this.endpoint}");
        }

        protected override TReturn CallInternal<TReturn>(bool isStream, Type interfaceType, string methodName, object[] arguments)
        {
            var data = new CQRSRequestData()
            {
                ProviderType = interfaceType.Name,
                ProviderMethod = methodName
            };
            data.AddProviderArguments(arguments);

            switch (networkType)
            {
                case NetworkType.Internal:
                    data.AddClaims();
                    break;
                case NetworkType.Api:
                    break;
                default: throw new NotImplementedException();
            }

            var stopwatch = Stopwatch.StartNew();

            var client = new TcpClient(endpoint.AddressFamily);

            var bufferOwner = BufferArrayPool<byte>.Rent(TcpRawCommon.BufferLength);
            var buffer = bufferOwner.AsMemory();
            Stream stream = null;
            Stream requestBodyStream = null;
            FinalBlockStream requestBodyCryptoStream = null;
            Stream responseBodyStream = null;
            try
            {
                client.Connect(endpoint.Address, endpoint.Port);

                stream = client.GetStream();

                //Request Header
                var requestHeaderLength = TcpRawCommon.BufferHeader(buffer, data.ProviderType, contentType);
#if NETSTANDARD2_0
                stream.Write(bufferOwner, 0, requestHeaderLength);
#else
                stream.Write(buffer.Span.Slice(0, requestHeaderLength));
#endif

                requestBodyStream = new TcpRawProtocolBodyStream(stream, null, true);

                if (encryptionKey != null)
                {
                    requestBodyCryptoStream = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, requestBodyStream, true, true, true);
                    ContentTypeSerializer.Serialize(contentType, requestBodyCryptoStream, data);
                    requestBodyCryptoStream.FlushFinalBlock();
                    requestBodyCryptoStream.Dispose();
                    requestBodyCryptoStream = null;
                }
                else
                {
                    ContentTypeSerializer.Serialize(contentType, requestBodyStream, data);
                    requestBodyStream.Flush();
                }

                requestBodyStream.Dispose();
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
                        throw new EndOfStreamException();
                    headerLength += bytesRead;

                    requestHeaderEnd = TcpRawCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                }
                var responseHeader = TcpRawCommon.ReadHeader(buffer, headerPosition, headerLength);

                responseBodyStream = new TcpRawProtocolBodyStream(stream, responseHeader.BodyStartBuffer, false);

                if (encryptionKey != null)
                    responseBodyStream = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, responseBodyStream, false, true, false);

                if (responseHeader.IsError)
                {
                    var responseException = ContentTypeSerializer.DeserializeException(contentType, responseBodyStream);
                    throw responseException;
                }

                stopwatch.Stop();
                _ = Log.TraceAsync($"{nameof(TcpRawCQRSClient)} Query: {interfaceType.GetNiceName()}.{data.ProviderMethod} {stopwatch.ElapsedMilliseconds}");

                if (isStream)
                {
                    return (TReturn)(object)responseBodyStream; //TODO better way to convert type???
                }
                else
                {
                    var model = ContentTypeSerializer.Deserialize<TReturn>(contentType, responseBodyStream);
                    responseBodyStream.Dispose();
                    client.Dispose();
                    return model;
                }
            }
            catch
            {
                try
                {
                    //crypto stream can error, we want to throw the actual error
                    if (responseBodyStream != null)
                        responseBodyStream.Dispose();
                }
                catch { }
                if (requestBodyStream != null)
                    requestBodyStream.Dispose();
                if (requestBodyCryptoStream != null)
                    requestBodyCryptoStream.Dispose();
                if (stream != null)
                    stream.Dispose();
                client.Dispose();
                throw;
            }
            finally
            {
                BufferArrayPool<byte>.Return(bufferOwner);
            }
        }

        protected override async Task<TReturn> CallInternalAsync<TReturn>(bool isStream, Type interfaceType, string methodName, object[] arguments)
        {
            var data = new CQRSRequestData()
            {
                ProviderType = interfaceType.Name,
                ProviderMethod = methodName
            };
            data.AddProviderArguments(arguments);

            switch (networkType)
            {
                case NetworkType.Internal:
                    data.AddClaims();
                    break;
                case NetworkType.Api:
                    break;
                default: throw new NotImplementedException();
            }

            var stopwatch = Stopwatch.StartNew();

            var client = new TcpClient(endpoint.AddressFamily);
            client.NoDelay = true;

            var bufferOwner = BufferArrayPool<byte>.Rent(TcpRawCommon.BufferLength);
            var buffer = bufferOwner.AsMemory();
            Stream stream = null;
            Stream requestBodyStream = null;
            FinalBlockStream requestBodyCryptoStream = null;
            Stream responseBodyStream = null;
            try
            {
                await client.ConnectAsync(endpoint.Address, endpoint.Port);

                stream = client.GetStream();

                //Request Header
                var requestHeaderLength = TcpRawCommon.BufferHeader(buffer, data.ProviderType, contentType);
#if NETSTANDARD2_0
                await stream.WriteAsync(bufferOwner, 0, requestHeaderLength);
#else
                await stream.WriteAsync(buffer.Slice(0, requestHeaderLength));
#endif

                requestBodyStream = new TcpRawProtocolBodyStream(stream, null, true);

                if (encryptionKey != null)
                {
                    requestBodyCryptoStream = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, requestBodyStream, true, true, true);
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
                }

#if NETSTANDARD2_0
                requestBodyStream.Dispose();
#else
                await requestBodyStream.DisposeAsync();
#endif
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
                        throw new EndOfStreamException();
                    headerLength += bytesRead;

                    requestHeaderEnd = TcpRawCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                }
                var responseHeader = TcpRawCommon.ReadHeader(buffer, headerPosition, headerLength);

                //Response Body
                responseBodyStream = new TcpRawProtocolBodyStream(stream, responseHeader.BodyStartBuffer, false);

                if (encryptionKey != null)
                    responseBodyStream = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, responseBodyStream, false, true, false);

                if (responseHeader.IsError)
                {
                    var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream);
                    throw responseException;
                }

                stopwatch.Stop();
                _ = Log.TraceAsync($"{nameof(TcpRawCQRSClient)} Query: {interfaceType.GetNiceName()}.{data.ProviderMethod} {stopwatch.ElapsedMilliseconds}");

                if (isStream)
                {
                    //TODO better way to convert type???
                    return (TReturn)(object)responseBodyStream;
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
                    client.Dispose();
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
                client.Dispose();
                throw;
            }
            finally
            {
                BufferArrayPool<byte>.Return(bufferOwner);
            }
        }

        protected override async Task DispatchInternal(ICommand command, bool messageAwait)
        {
            var messageType = command.GetType();
            var messageTypeName = messageType.GetNiceName();

            var messageData = System.Text.Json.JsonSerializer.Serialize(command, messageType);

            var data = new CQRSRequestData()
            {
                MessageType = messageTypeName,
                MessageData = messageData,
                MessageAwait = messageAwait
            };

            switch (networkType)
            {
                case NetworkType.Internal:
                    data.AddClaims();
                    break;
                case NetworkType.Api:
                    break;
                default:
                    throw new NotImplementedException();
            }

            var stopwatch = Stopwatch.StartNew();

            var client = new TcpClient(endpoint.AddressFamily);
            client.NoDelay = true;

            var bufferOwner = BufferArrayPool<byte>.Rent(TcpRawCommon.BufferLength);
            var buffer = bufferOwner.AsMemory();
            Stream stream = null;
            Stream requestBodyStream = null;
            FinalBlockStream requestBodyCryptoStream = null;
            Stream responseBodyStream = null;
            try
            {
                await client.ConnectAsync(endpoint.Address, endpoint.Port);

                stream = client.GetStream();

                //Request Header
                var requestHeaderLength = TcpRawCommon.BufferHeader(buffer, data.MessageType, contentType);
#if NETSTANDARD2_0
                await stream.WriteAsync(bufferOwner, 0, requestHeaderLength);
#else
                await stream.WriteAsync(buffer.Slice(0, requestHeaderLength));
#endif

                requestBodyStream = new TcpRawProtocolBodyStream(stream, null, true);

                if (encryptionKey != null)
                {
                    requestBodyCryptoStream = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, requestBodyStream, true, true, true);
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
                }

#if NETSTANDARD2_0
                requestBodyStream.Dispose();
#else
                await requestBodyStream.DisposeAsync();
#endif
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
                        throw new EndOfStreamException();
                    headerLength += bytesRead;

                    requestHeaderEnd = TcpRawCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                }
                var responseHeader = TcpRawCommon.ReadHeader(buffer, headerPosition, headerLength);

                //Response Body
                responseBodyStream = new TcpRawProtocolBodyStream(stream, responseHeader.BodyStartBuffer, false);

                if (responseHeader.IsError)
                {
                    if (encryptionKey != null)
                        responseBodyStream = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, responseBodyStream, false, true, false);

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
                client.Dispose();

                stopwatch.Stop();
                _ = Log.TraceAsync($"{nameof(TcpRawCQRSClient)}Sent: {messageTypeName} {stopwatch.ElapsedMilliseconds}");
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
                client.Dispose();
                throw;
            }
            finally
            {
                BufferArrayPool<byte>.Return(bufferOwner);
            }
        }

        public static TcpRawCQRSClient CreateDefault(string endpoint, SymmetricKey encryptionKey)
        {
            return new TcpRawCQRSClient(NetworkType.Internal, ContentType.Bytes, endpoint, encryptionKey);
        }
    }
}