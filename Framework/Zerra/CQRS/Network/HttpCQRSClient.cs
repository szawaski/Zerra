﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Zerra.Logging;
using Zerra.IO;
using System.Collections.Generic;
using System.Text;
using Zerra.Serialization;
using System.Security.Claims;
using System.Threading;
using System.Linq;

namespace Zerra.CQRS.Network
{
    public sealed class HttpCqrsClient : TcpCqrsClientBase
    {
        private readonly ContentType contentType;
        private readonly ICqrsAuthorizer authorizer;
        private readonly SocketClientPool socketPool;

        public HttpCqrsClient(ContentType contentType, string serviceUrl, ICqrsAuthorizer authorizer)
            : base(serviceUrl)
        {
            this.contentType = contentType;
            this.authorizer = authorizer;
            this.socketPool = SocketClientPool.Default;

            _ = Log.InfoAsync($"{nameof(HttpCqrsClient)} started for {this.contentType} at {serviceUrl} as {this.ipEndpoint}");
        }

        protected override TReturn CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source)
        {
            throttle.Wait();

            //Socket socket = null;
            Stream stream = null;
            Stream requestBodyStream = null;
            Stream responseBodyStream = null;
            var bufferOwner = BufferArrayPool<byte>.Rent(HttpCommon.BufferLength);
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

                IDictionary<string, IList<string>> authHeaders = null;
                if (authorizer != null)
                    authHeaders = authorizer.BuildAuthHeaders();

                var buffer = bufferOwner.AsMemory();

                //Request Header

                var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUrl, null, data.ProviderType, contentType, authHeaders);

#if NETSTANDARD2_0
                stream = SocketClientPool.Default.BeginStream(ipEndpoint, ProtocolType.Tcp, bufferOwner, 0, requestHeaderLength);
#else
                stream = SocketClientPool.Default.BeginStream(ipEndpoint, ProtocolType.Tcp, buffer.Span.Slice(0, requestHeaderLength));
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
                        throw new Exception($"{nameof(HttpCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                    var bytesRead = stream.Read(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                    var bytesRead = stream.Read(buffer.Span.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                    if (bytesRead == 0)
                        throw new ConnectionAbortedException();
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
                if (responseBodyStream != null)
                    responseBodyStream.Dispose();
                if (requestBodyStream != null)
                    requestBodyStream.Dispose();
                if (stream != null)
                    stream.Dispose();
                throw;
            }
            finally
            {
                BufferArrayPool<byte>.Return(bufferOwner);
                throttle.Release();
            }
        }

        protected override async Task<TReturn> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source)
        {
            await throttle.WaitAsync();

            //Socket socket = null;
            Stream stream = null;
            Stream requestBodyStream = null;
            Stream responseBodyStream = null;
            var bufferOwner = BufferArrayPool<byte>.Rent(HttpCommon.BufferLength);
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

                IDictionary<string, IList<string>> authHeaders = null;
                if (authorizer != null)
                    authHeaders = authorizer.BuildAuthHeaders();

                var buffer = bufferOwner.AsMemory();

                //Request Header
                var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUrl, null, data.ProviderType, contentType, authHeaders);


#if NETSTANDARD2_0
                stream = await socketPool.BeginStreamAsync(ipEndpoint, ProtocolType.Tcp,bufferOwner, 0, requestHeaderLength); 
#else
                stream = await socketPool.BeginStreamAsync(ipEndpoint, ProtocolType.Tcp, buffer.Slice(0, requestHeaderLength));
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
                        throw new Exception($"{nameof(HttpCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                    var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                    var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                    if (bytesRead == 0)
                        throw new ConnectionAbortedException();
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

            //Socket socket = null;
            Stream stream = null;
            Stream requestBodyStream = null;
            Stream responseBodyStream = null;
            var bufferOwner = BufferArrayPool<byte>.Rent(HttpCommon.BufferLength);
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

                IDictionary<string, IList<string>> authHeaders = null;
                if (authorizer != null)
                    authHeaders = authorizer.BuildAuthHeaders();

                var buffer = bufferOwner.AsMemory();

                //Request Header
                var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUrl, null, data.ProviderType, contentType, authHeaders);

#if NETSTANDARD2_0
                stream = await socketPool.BeginStreamAsync(ipEndpoint, ProtocolType.Tcp,bufferOwner, 0, requestHeaderLength); 
#else
                stream = await socketPool.BeginStreamAsync(ipEndpoint, ProtocolType.Tcp, buffer.Slice(0, requestHeaderLength));
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
                        throw new Exception($"{nameof(HttpCqrsClient)} Header Too Long");

#if NETSTANDARD2_0
                    var bytesRead = await stream.ReadAsync(bufferOwner, headerPosition, buffer.Length - headerPosition);
#else
                    var bytesRead = await stream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition));
#endif

                    if (bytesRead == 0)
                        throw new ConnectionAbortedException();
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
                throw;
            }
            finally
            {
                BufferArrayPool<byte>.Return(bufferOwner);
                throttle.Release();
            }
        }
    }
}