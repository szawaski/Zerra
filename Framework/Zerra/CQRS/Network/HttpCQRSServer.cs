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
using Zerra.IO;
using System.IO;

namespace Zerra.CQRS.Network
{
    public sealed class HttpCQRSServer : TcpCQRSServerBase
    {
        private readonly ContentType? contentType;
        private readonly ICQRSAuthorizer authorizer;
        private readonly string[] allowOrigins;

        public HttpCQRSServer(ContentType? contentType, string serverUrl, ICQRSAuthorizer authorizer, string[] allowOrigins)
            : base(serverUrl)
        {
            this.contentType = contentType;
            this.authorizer = authorizer;
            if (allowOrigins != null && !allowOrigins.Contains("*"))
                this.allowOrigins = allowOrigins.Select(x => x.ToLower()).ToArray();
            else
                allowOrigins = null;
        }

        protected override async void Handle(TcpClient client, CancellationToken cancellationToken)
        {
            HttpRequestHeader requestHeader = null;
            var responseStarted = false;

            var bufferOwner = BufferArrayPool<byte>.Rent(HttpCommon.BufferLength);
            var buffer = bufferOwner.AsMemory();
            Stream stream = null;
            Stream requestBodyStream = null;
            Stream responseBodyStream = null;

            try
            {
                stream = client.GetStream();

                //Read Request Header
                //------------------------------------------------------------------------------------------------------------
                var headerPosition = 0;
                var headerLength = 0;
                var headerEnd = false;
                while (!headerEnd)
                {
                    if (headerLength == buffer.Length)
                        throw new Exception($"{nameof(HttpCQRSServer)} Header Too Long");

#if NETSTANDARD2_0
                    var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, cancellationToken);
#else
                    var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);
#endif

                    if (bytesRead == 0)
                        throw new CQRSRequestAbortedException();
                    headerLength += bytesRead;

                    headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                }
                requestHeader = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);

                if (contentType.HasValue && requestHeader.ContentType.HasValue && requestHeader.ContentType != contentType)
                {
                    _ = Log.ErrorAsync($"{nameof(HttpCQRSServer)} Received Invalid Content Type {requestHeader.ContentType}");
                    throw new Exception("Invalid Content Type");
                }

                if (requestHeader.Preflight)
                {
                    _ = Log.TraceAsync($"{nameof(HttpCQRSServer)} Received Preflight {client.Client.RemoteEndPoint}");

                    var preflightLength = HttpCommon.BufferPreflightResponse(buffer, requestHeader.Origin);
#if NETSTANDARD2_0
                    await stream.WriteAsync(bufferOwner, 0, preflightLength, cancellationToken);
#else
                    await stream.WriteAsync(buffer.Slice(0, preflightLength), cancellationToken);
#endif

                    await stream.FlushAsync(cancellationToken);
                    return;
                }

                if (!requestHeader.ContentType.HasValue)
                {
                    _ = Log.ErrorAsync($"{nameof(HttpCQRSServer)} Received Invalid Content Type {requestHeader.ContentType}");
                    throw new Exception("Invalid Content Type");
                }

                _ = Log.TraceAsync($"{nameof(HttpCQRSServer)} Received On {client.Client.LocalEndPoint} From {client.Client.RemoteEndPoint} {requestHeader.ProviderType}");
                if (allowOrigins != null && allowOrigins.Length > 0)
                {
                    if (allowOrigins.Contains(requestHeader.Origin))
                    {
                        throw new Exception($"Origin Not Allowed {requestHeader.Origin}");
                    }
                }

                //Read Request Body
                //------------------------------------------------------------------------------------------------------------

                requestBodyStream = new HttpProtocolBodyStream(requestHeader.ContentLength, stream, requestHeader.BodyStartBuffer, true);

                var data = await ContentTypeSerializer.DeserializeAsync<CQRSRequestData>(requestHeader.ContentType.Value, requestBodyStream);
                if (data == null)
                    throw new Exception("Empty request body");

#if NETSTANDARD2_0
                requestBodyStream.Dispose();
#else
                await requestBodyStream.DisposeAsync();
#endif
                requestBodyStream = null;

                //Authorize
                //------------------------------------------------------------------------------------------------------------
                if (this.authorizer != null)
                {
                    if (data.Claims != null)
                    {
                        var claimsIdentity = new ClaimsIdentity(data.Claims.Select(x => new Claim(x.Type, x.Value)), "CQRS");
                        Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                    }
                    else
                    {
                        Thread.CurrentPrincipal = null;
                    }
                }
                else
                {
                    this.authorizer.Authorize(requestHeader.Headers);
                }

                //Process and Respond
                //----------------------------------------------------------------------------------------------------
                if (!String.IsNullOrWhiteSpace(data.ProviderType))
                {
                    var providerType = Discovery.GetTypeFromName(data.ProviderType);
                    var typeDetail = TypeAnalyzer.GetTypeDetail(providerType);

                    if (!this.interfaceTypes.Contains(providerType))
                        throw new Exception($"Unhandled Provider Type {providerType.FullName}");

                    var exposed = typeDetail.Attributes.Any(x => x is ServiceExposedAttribute);
                    if (!exposed)
                        throw new Exception($"Provider {data.MessageType} is not exposed");

                    _ = Log.TraceAsync($"Received Call: {providerType.GetNiceName()}.{data.ProviderMethod}");

                    var result = await this.providerHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments);


                    //Response Header
                    var responseHeaderLength = HttpCommon.BufferOkResponseHeader(buffer, requestHeader.Origin, requestHeader.ProviderType, requestHeader.ContentType.Value, null);
#if NETSTANDARD2_0
                    await stream.WriteAsync(bufferOwner, 0, responseHeaderLength);
#else
                    await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                    //Response Body
                    responseBodyStream = new HttpProtocolBodyStream(null, stream, null, false);

                    int bytesRead;
                    if (result.Stream != null)
                    {
#if NETSTANDARD2_0
                        while ((bytesRead = await result.Stream.ReadAsync(bufferOwner, 0, bufferOwner.Length, cancellationToken)) > 0)
                            await responseBodyStream.WriteAsync(bufferOwner, 0, bytesRead, cancellationToken);
#else
                        while ((bytesRead = await result.Stream.ReadAsync(buffer, cancellationToken)) > 0)
                            await responseBodyStream.WriteAsync(buffer.Slice(0, bytesRead), cancellationToken);
#endif
                        await responseBodyStream.FlushAsync(cancellationToken);
#if NETSTANDARD2_0
                        responseBodyStream.Dispose();
#else
                        await responseBodyStream.DisposeAsync();
#endif
                        client.Dispose();
                        return;
                    }
                    else
                    {
                        await ContentTypeSerializer.SerializeAsync(requestHeader.ContentType.Value, responseBodyStream, result.Model);
                        await responseBodyStream.FlushAsync(cancellationToken);
#if NETSTANDARD2_0
                        responseBodyStream.Dispose();
#else
                        await responseBodyStream.DisposeAsync();
#endif
                        client.Dispose();
                        return;
                    }
                }
                else if (!String.IsNullOrWhiteSpace(data.MessageType))
                {
                    var commandType = Discovery.GetTypeFromName(data.MessageType);
                    var typeDetail = TypeAnalyzer.GetTypeDetail(commandType);

                    if (!typeDetail.Interfaces.Contains(typeof(ICommand)))
                        throw new Exception($"Type {data.MessageType} is not a command");

                    if (!this.commandTypes.Contains(commandType))
                        throw new Exception($"Unhandled Command Type {commandType.FullName}");

                    var exposed = typeDetail.Attributes.Any(x => x is ServiceExposedAttribute);
                    if (!exposed)
                        throw new Exception($"Command {data.MessageType} is not exposed");

                    var command = (ICommand)System.Text.Json.JsonSerializer.Deserialize(data.MessageData, commandType);

                    if (data.MessageAwait)
                        await handlerAwaitAsync(command);
                    else
                        await handlerAsync(command);

                    //Response Header
                    var responseHeaderLength = HttpCommon.BufferOkResponseHeader(buffer, requestHeader.Origin, requestHeader.ProviderType, contentType, null);
#if NETSTANDARD2_0
                    await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                    await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                    //Response Body Empty
                    responseBodyStream = new HttpProtocolBodyStream(null, stream, null, false);
                    await responseBodyStream.FlushAsync(cancellationToken);
#if NETSTANDARD2_0
                    responseBodyStream.Dispose();
#else
                    await responseBodyStream.DisposeAsync();
#endif
                    client.Dispose();
                    return;
                }

                throw new Exception("Invalid Request");
            }
            catch (Exception ex)
            {
                if (ex is IOException ioException)
                {
                    if (ioException.InnerException != null && ioException.InnerException is SocketException socketException)
                    {
                        if (socketException.SocketErrorCode == SocketError.ConnectionAborted)
                            return;
                    }
                }

                _ = Log.ErrorAsync(null, ex);

                if (client.Connected && !responseStarted && requestHeader != null && requestHeader.ContentType.HasValue)
                {
                    try
                    {

                        //Response Header
                        var responseHeaderLength = HttpCommon.BufferErrorResponseHeader(buffer, requestHeader.Origin);
#if NETSTANDARD2_0
                        await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                        await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                        //Response Body
                        responseBodyStream = new HttpProtocolBodyStream(null, stream, null, false);
                        await ContentTypeSerializer.SerializeExceptionAsync(requestHeader.ContentType.Value, responseBodyStream, ex);
#if NETSTANDARD2_0
                        responseBodyStream.Dispose();
#else
                        await responseBodyStream.DisposeAsync();
#endif
                        client.Dispose();
                    }
                    catch (Exception ex2)
                    {
                        if (responseBodyStream != null)
                        {
#if NETSTANDARD2_0
                            responseBodyStream.Dispose();
#else
                            await responseBodyStream.DisposeAsync();
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
                        _ = Log.ErrorAsync($"{nameof(HttpCQRSServer)} Error {client.Client.RemoteEndPoint}", ex2);
                    }
                    return;
                }

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
            }
            finally
            {
                BufferArrayPool<byte>.Return(bufferOwner);
            }
        }

        public static HttpCQRSServer CreateDefault(string serverUrl, ICQRSAuthorizer authorizer, string[] allowOrigins)
        {
            return new HttpCQRSServer(ContentType.Json, serverUrl, authorizer, allowOrigins);
        }
    }
}