// Copyright © KaKush LLC
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
    public sealed class HttpCQRSClient : TcpCQRSClientBase
    {
        private readonly ContentType contentType;
        private readonly ICQRSAuthorizer authorizer;

        public HttpCQRSClient(ContentType contentType, string serviceUrl, ICQRSAuthorizer authorizer)
            : base(serviceUrl)
        {
            this.contentType = contentType;
            this.authorizer = authorizer;

            _ = Log.InfoAsync($"{nameof(HttpCQRSClient)} started for {this.contentType} at {serviceUrl} as {this.ipEndpoint}");
        }

        protected override TReturn CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source)
        {
            throttle.Wait();

            TcpClient client = null;
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

                client = new TcpClient(ipEndpoint.AddressFamily);
                client.NoDelay = true;

                var buffer = bufferOwner.AsMemory();

                client.Connect(ipEndpoint.Address, ipEndpoint.Port);

                stream = client.GetStream();

                //Request Header

                var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUrl, null, data.ProviderType, contentType, authHeaders);
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
                        throw new CQRSRequestAbortedException();
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
                throttle.Release();
            }
        }

        protected override async Task<TReturn> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, object[] arguments, string source)
        {
            await throttle.WaitAsync();

            TcpClient client = null;
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

                client = new TcpClient(ipEndpoint.AddressFamily);
                client.NoDelay = true;

                var buffer = bufferOwner.AsMemory();

                await client.ConnectAsync(ipEndpoint.Address, ipEndpoint.Port);

                stream = client.GetStream();

                //Request Header
                var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUrl, null, data.ProviderType, contentType, authHeaders);

                var test = Encoding.UTF8.GetString(buffer.Span.Slice(0, requestHeaderLength).ToArray());


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
                        throw new CQRSRequestAbortedException();
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
                client?.Close();
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

            TcpClient client = null;
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

                client = new TcpClient(ipEndpoint.AddressFamily);
                client.NoDelay = true;

                var buffer = bufferOwner.AsMemory();

                await client.ConnectAsync(ipEndpoint.Address, ipEndpoint.Port);

                stream = client.GetStream();

                //Request Header
                var requestHeaderLength = HttpCommon.BufferPostRequestHeader(buffer, serviceUrl, null, data.ProviderType, contentType, authHeaders);
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
                        throw new CQRSRequestAbortedException();
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
                throttle.Release();
            }
        }
    }
}