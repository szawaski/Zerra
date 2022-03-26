// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Zerra.Logging;
using Zerra.IO;
using System.Collections.Generic;

namespace Zerra.CQRS.Network
{
    public class HttpCQRSClient : TcpCQRSClientBase
    {
        private readonly NetworkType networkType;
        private readonly ContentType contentType;
        private readonly IHttpApiAuthorizer apiAuthorizer;

        public HttpCQRSClient(NetworkType networkType, ContentType contentType, string serviceUrl, IHttpApiAuthorizer apiAuthorizer)
            : base(serviceUrl)
        {
            this.networkType = networkType;
            this.contentType = contentType;
            this.apiAuthorizer = apiAuthorizer;

            _ = Log.TraceAsync($"{nameof(HttpCQRSClient)} Started For {this.networkType} {this.contentType} {this.endpoint}");
        }

        protected override TReturn CallInternal<TReturn>(bool isStream, Type interfaceType, string methodName, object[] arguments)
        {
            var data = new CQRSRequestData()
            {
                ProviderType = interfaceType.Name,
                ProviderMethod = methodName
            };
            data.AddProviderArguments(arguments);

            IDictionary<string, IList<string>> authHeaders = null;
            switch (networkType)
            {
                case NetworkType.Internal:
                    data.AddClaims();
                    break;
                case NetworkType.Api:
                    if (apiAuthorizer != null)
                        authHeaders = apiAuthorizer.BuildAuthHeaders();
                    break;
                default:
                    throw new NotImplementedException();
            }

            var stopwatch = Stopwatch.StartNew();

            var client = new TcpClient(endpoint.AddressFamily);
            client.NoDelay = true;

            var bufferOwner = BufferArrayPool<byte>.Rent(HttpCommon.BufferLength);
            var buffer = bufferOwner.AsMemory();
            Stream stream = null;
            Stream requestBodyStream = null;
            Stream responseBodyStream = null;
            try
            {
                client.Connect(endpoint.Address, endpoint.Port);

                stream = client.GetStream();

                //Request Header
                var requestHeaderLength = HttpCommon.BufferHeader(buffer, data.ProviderType, contentType, serviceUrl, authHeaders);
#if NETSTANDARD2_0
                stream.Write(bufferOwner, 0, requestHeaderLength);
#else
                stream.Write(buffer.Span.Slice(0, requestHeaderLength));
#endif

                requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true);

                ContentTypeSerializer.Serialize(contentType, requestBodyStream, data);
                requestBodyStream.Flush();

                requestBodyStream.Dispose();
                requestBodyStream = null;

                //Response Header
                var headerPosition = 0;
                var headerLength = 0;
                var headerEnd = false;
                while (!headerEnd)
                {
                    if (headerLength == buffer.Length)
                        throw new Exception($"{nameof(HttpCQRSClient)} Header Too Long");

#if NETSTANDARD2_0
                    var bytesRead = stream.Read(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                    var bytesRead = stream.Read(buffer.Span.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                    if (bytesRead == 0)
                        throw new EndOfStreamException();
                    headerLength += bytesRead;

                    headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                }
                var responseHeader = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);

                //Response Body
                responseBodyStream = new HttpProtocolBodyStream(null, stream, responseHeader.BodyStartBuffer, false);

                if (responseHeader.IsError)
                {
                    var responseException = ContentTypeSerializer.DeserializeException(contentType, responseBodyStream);
                    throw responseException;
                }

                stopwatch.Stop();
                _ = Log.TraceAsync($"{nameof(HttpCQRSClient)} Query: {interfaceType.GetNiceName()}.{data.ProviderMethod} {stopwatch.ElapsedMilliseconds}");

                if (isStream)
                {
                    return (TReturn)(object)responseBodyStream;
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
                if (responseBodyStream != null)
                    responseBodyStream.Dispose();
                if (requestBodyStream != null)
                    requestBodyStream.Dispose();
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

            IDictionary<string, IList<string>> authHeaders = null;
            switch (networkType)
            {
                case NetworkType.Internal:
                    data.AddClaims();
                    break;
                case NetworkType.Api:
                    if (apiAuthorizer != null)
                        authHeaders = apiAuthorizer.BuildAuthHeaders();
                    break;
                default:
                    throw new NotImplementedException();
            }

            var stopwatch = Stopwatch.StartNew();

            var client = new TcpClient(endpoint.AddressFamily);
            client.NoDelay = true;

            var bufferOwner = BufferArrayPool<byte>.Rent(HttpCommon.BufferLength);
            var buffer = bufferOwner.AsMemory();
            Stream stream = null;
            Stream requestBodyStream = null;
            Stream responseBodyStream = null;
            try
            {
                await client.ConnectAsync(endpoint.Address, endpoint.Port);

                stream = client.GetStream();

                //Request Header
                var requestHeaderLength = HttpCommon.BufferHeader(buffer, data.ProviderType, contentType, serviceUrl, authHeaders);
#if NETSTANDARD2_0
                await stream.WriteAsync(bufferOwner, 0, requestHeaderLength);
#else
                await stream.WriteAsync(buffer.Slice(0, requestHeaderLength));
#endif

                requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true);

                await ContentTypeSerializer.SerializeAsync(contentType, requestBodyStream, data);
                await requestBodyStream.FlushAsync();

#if NETSTANDARD2_0
                requestBodyStream.Dispose();
#else
                await requestBodyStream.DisposeAsync();
#endif
                requestBodyStream = null;

                //Response Header
                var headerPosition = 0;
                var headerLength = 0;
                var headerEnd = false;
                while (!headerEnd)
                {
                    if (headerLength == buffer.Length)
                        throw new Exception($"{nameof(HttpCQRSClient)} Header Too Long");

#if NETSTANDARD2_0
                    var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                    var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                    if (bytesRead == 0)
                        throw new EndOfStreamException();
                    headerLength += bytesRead;

                    headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                }
                var responseHeader = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);

                //Response Body
                responseBodyStream = new HttpProtocolBodyStream(null, stream, responseHeader.BodyStartBuffer, false);

                if (responseHeader.IsError)
                {
                    var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream);
                    throw responseException;
                }

                stopwatch.Stop();
                _ = Log.TraceAsync($"{nameof(HttpCQRSClient)} Query: {interfaceType.GetNiceName()}.{data.ProviderMethod} {stopwatch.ElapsedMilliseconds}");

                if (isStream)
                {
                    return (TReturn)(object)responseBodyStream;
                }
                else
                {
                    var model = await ContentTypeSerializer.DeserializeAsync<TReturn>(contentType, responseBodyStream);
#if NETSTANDARD2_0
                    responseBodyStream.Dispose();
#else
                    await responseBodyStream.DisposeAsync();
#endif
                    client.Dispose();
                    return model;
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
                if (stream != null)
                {
#if NETSTANDARD2_0
                    stream.Dispose();
#else
                    await stream.DisposeAsync();
#endif
                }
                client.Close();
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

            IDictionary<string, IList<string>> authHeaders = null;
            switch (networkType)
            {
                case NetworkType.Internal:
                    data.AddClaims();
                    break;
                case NetworkType.Api:
                    if (apiAuthorizer != null)
                        authHeaders = apiAuthorizer.BuildAuthHeaders();
                    break;
                default:
                    throw new NotImplementedException();
            }

            var stopwatch = Stopwatch.StartNew();

            var client = new TcpClient(endpoint.AddressFamily);
            client.NoDelay = true;

            var bufferOwner = BufferArrayPool<byte>.Rent(HttpCommon.BufferLength);
            var buffer = bufferOwner.AsMemory();
            Stream stream = null;
            Stream requestBodyStream = null;
            Stream responseBodyStream = null;
            try
            {
                await client.ConnectAsync(endpoint.Address, endpoint.Port);

                stream = client.GetStream();

                //Request Header
                var requestHeaderLength = HttpCommon.BufferHeader(buffer, data.ProviderType, contentType, serviceUrl, authHeaders);
#if NETSTANDARD2_0
                await stream.WriteAsync(bufferOwner, 0, requestHeaderLength);
#else
                await stream.WriteAsync(buffer.Slice(0, requestHeaderLength));
#endif

                requestBodyStream = new HttpProtocolBodyStream(null, stream, null, true);

                await ContentTypeSerializer.SerializeAsync(contentType, requestBodyStream, data);
                await requestBodyStream.FlushAsync();

#if NETSTANDARD2_0
                requestBodyStream.Dispose();
#else
                await requestBodyStream.DisposeAsync();
#endif
                requestBodyStream = null;

                //Response Header
                var headerPosition = 0;
                var headerLength = 0;
                var headerEnd = false;
                while (!headerEnd)
                {
                    if (headerLength == buffer.Length)
                        throw new Exception($"{nameof(HttpCQRSClient)} Header Too Long");

#if NETSTANDARD2_0
                    var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                    var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                    if (bytesRead == 0)
                        throw new EndOfStreamException();
                    headerLength += bytesRead;

                    headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                }
                var responseHeader = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);

                //Response Body
                responseBodyStream = new HttpProtocolBodyStream(null, stream, responseHeader.BodyStartBuffer, false);

                if (responseHeader.IsError)
                {
                    var responseException = await ContentTypeSerializer.DeserializeExceptionAsync(contentType, responseBodyStream);
                    throw responseException;
                }

#if NETSTANDARD2_0
                responseBodyStream.Dispose();
#else
                await responseBodyStream.DisposeAsync();
#endif
                client.Dispose();

                stopwatch.Stop();
                _ = Log.TraceAsync($"{nameof(HttpCQRSClient)}Sent: {messageTypeName} {stopwatch.ElapsedMilliseconds}");
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

        public static HttpCQRSClient CreateDefault(string endpoint, IHttpApiAuthorizer apiAuthorizer)
        {
            return new HttpCQRSClient(NetworkType.Api, ContentType.Json, endpoint, apiAuthorizer);
        }
    }
}