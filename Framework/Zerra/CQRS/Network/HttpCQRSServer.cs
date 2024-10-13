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
using Zerra.Encryption;
using System.IO;
using Zerra.Serialization.Json;
using System.Threading.Tasks;
using Zerra.Serialization.Bytes.Converters.CoreTypes.IReadOnlyListTs;
using Zerra.Buffers;

namespace Zerra.CQRS.Network
{
    public sealed class HttpCqrsServer : TcpCqrsServerBase
    {
        private readonly ContentType? contentType;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly string[]? allowOrigins;

        public HttpCqrsServer(ContentType? contentType, string serverUrl, SymmetricConfig? symmetricConfig, ICqrsAuthorizer? authorizer, string[]? allowOrigins)
            : base(serverUrl)
        {
            this.contentType = contentType;
            this.symmetricConfig = symmetricConfig;
            this.authorizer = authorizer;
            if (allowOrigins is not null && !allowOrigins.Contains("*"))
                this.allowOrigins = allowOrigins.Select(x => x.ToLower()).ToArray();
            else
                allowOrigins = null;
        }

        protected override async Task Handle(Socket socket, CancellationToken cancellationToken)
        {
            if (throttle is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
            if (commandCounter is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");

            try
            {
                for (; ; )
                {
                    await throttle.WaitAsync(cancellationToken);

                    HttpRequestHeader? requestHeader = null;
                    var responseStarted = false;

                    var bufferOwner = ArrayPoolHelper<byte>.Rent(HttpCommon.BufferLength);
                    var buffer = bufferOwner.AsMemory();
                    var stream = new NetworkStream(socket, false);

                    Stream? requestBodyStream = null;
                    Stream? responseBodyStream = null;
                    CryptoFlushStream? responseBodyCryptoStream = null;
                    var isCommand = false;

                    var inHandlerContext = false;
                    try
                    {
                        //Read Request Header
                        //------------------------------------------------------------------------------------------------------------
                        var headerPosition = 0;
                        var headerLength = 0;
                        var headerEnd = false;
                        while (!headerEnd)
                        {
                            if (headerLength == buffer.Length)
                                throw new CqrsNetworkException($"{nameof(HttpCqrsServer)} Header Too Long");

#if NETSTANDARD2_0
                            var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, cancellationToken);
#else
                            var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);
#endif

                            if (bytesRead == 0)
                                return; //not an abort if we haven't started receiving, simple socket disconnect
                            headerLength += bytesRead;

                            headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                        }
                        requestHeader = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);

                        if (contentType.HasValue && requestHeader.ContentType.HasValue && requestHeader.ContentType != contentType)
                        {
                            _ = Log.ErrorAsync($"{nameof(HttpCqrsServer)} Received Invalid Content Type {requestHeader.ContentType}");
                            throw new CqrsNetworkException("Invalid Content Type");
                        }

                        if (requestHeader.Preflight)
                        {
                            _ = Log.TraceAsync($"{nameof(HttpCqrsServer)} Received Preflight {socket.RemoteEndPoint}");

                            var preflightLength = HttpCommon.BufferPreflightResponse(buffer, requestHeader.Origin);
#if NETSTANDARD2_0
                            await stream.WriteAsync(bufferOwner, 0, preflightLength, cancellationToken);
#else
                            await stream.WriteAsync(buffer.Slice(0, preflightLength), cancellationToken);
#endif

                            await stream.FlushAsync(cancellationToken);
                            continue;
                        }

                        if (!requestHeader.ContentType.HasValue)
                        {
                            _ = Log.ErrorAsync($"{nameof(HttpCqrsServer)} Received Invalid Content Type {requestHeader.ContentType}");
                            throw new CqrsNetworkException("Invalid Content Type");
                        }

                        if (allowOrigins is not null && allowOrigins.Length > 0)
                        {
                            if (allowOrigins.Contains(requestHeader.Origin))
                            {
                                throw new CqrsNetworkException($"Origin Not Allowed {requestHeader.Origin}");
                            }
                        }

                        //Read Request Body
                        //------------------------------------------------------------------------------------------------------------

                        requestBodyStream = new HttpProtocolBodyStream(requestHeader.ContentLength, stream, requestHeader.BodyStartBuffer, true);

                        if (symmetricConfig is not null)
                            requestBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, requestBodyStream, false);

                        var data = await ContentTypeSerializer.DeserializeAsync<CqrsRequestData>(requestHeader.ContentType.Value, requestBodyStream);
                        if (data is null)
                            throw new CqrsNetworkException("Empty request body");

#if NETSTANDARD2_0
                        requestBodyStream.Dispose();
#else
                        await requestBodyStream.DisposeAsync();
#endif
                        requestBodyStream = null;

                        //Authorize
                        //------------------------------------------------------------------------------------------------------------
                        if (this.authorizer is not null)
                        {
                            if (requestHeader.Headers is not null)
                                this.authorizer.Authorize(requestHeader.Headers);
                        }
                        else
                        {
                            if (data.Claims is not null)
                            {
                                var claimsIdentity = new ClaimsIdentity(data.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                            }
                            else
                            {
                                Thread.CurrentPrincipal = null;
                            }
                        }

                        //Process and Respond
                        //----------------------------------------------------------------------------------------------------
                        if (!String.IsNullOrWhiteSpace(data.ProviderType))
                        {
                            if (providerHandlerAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");

                            if (String.IsNullOrWhiteSpace(data.ProviderMethod)) throw new Exception("Invalid Request");
                            if (data.ProviderArguments is null) throw new Exception("Invalid Request");
                            if (String.IsNullOrWhiteSpace(data.Source)) throw new Exception("Invalid Request");
                            if (requestHeader.ProviderType != data.ProviderType) throw new Exception("Invalid Request");

                            var providerType = Discovery.GetTypeFromName(data.ProviderType);
                            var typeDetail = TypeAnalyzer.GetTypeDetail(providerType);

                            if (!this.interfaceTypes.Contains(providerType))
                                throw new CqrsNetworkException($"Unhandled Provider Type {providerType.FullName}");

                            inHandlerContext = true;
                            var result = await this.providerHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments, socket.AddressFamily.ToString(), false);
                            inHandlerContext = false;

                            responseStarted = true;

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
                            if (result.Stream is not null)
                            {
                                if (symmetricConfig is not null)
                                {
                                    responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

#if NETSTANDARD2_0
                                    while ((bytesRead = await result.Stream.ReadAsync(bufferOwner, 0, bufferOwner.Length, cancellationToken)) > 0)
                                        await responseBodyCryptoStream.WriteAsync(bufferOwner, 0, bytesRead, cancellationToken);
#else
                                    while ((bytesRead = await result.Stream.ReadAsync(buffer, cancellationToken)) > 0)
                                        await responseBodyCryptoStream.WriteAsync(buffer.Slice(0, bytesRead), cancellationToken);
#endif
#if NET5_0_OR_GREATER
                                    await responseBodyCryptoStream.FlushFinalBlockAsync();
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                    continue;
                                }
                                else
                                {
#if NETSTANDARD2_0
                                    while ((bytesRead = await result.Stream.ReadAsync(bufferOwner, 0, bufferOwner.Length, cancellationToken)) > 0)
                                        await responseBodyStream.WriteAsync(bufferOwner, 0, bytesRead, cancellationToken);
#else
                                    while ((bytesRead = await result.Stream.ReadAsync(buffer, cancellationToken)) > 0)
                                        await responseBodyStream.WriteAsync(buffer.Slice(0, bytesRead), cancellationToken);
#endif
                                    await responseBodyStream.FlushAsync(cancellationToken);
                                    continue;
                                }
                            }
                            else
                            {
                                if (symmetricConfig is not null)
                                {
                                    responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

                                    await ContentTypeSerializer.SerializeAsync(requestHeader.ContentType.Value, responseBodyCryptoStream, result.Model);
#if NET5_0_OR_GREATER
                                    await responseBodyCryptoStream.FlushFinalBlockAsync();
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                    continue;
                                }
                                else
                                {
                                    await ContentTypeSerializer.SerializeAsync(requestHeader.ContentType.Value, responseBodyStream, result.Model);
                                    await responseBodyStream.FlushAsync(cancellationToken);
                                    continue;
                                }
                            }
                        }
                        else if (!String.IsNullOrWhiteSpace(data.MessageType))
                        {
                            if (data.MessageData is null) throw new Exception("Invalid Request");
                            if (String.IsNullOrWhiteSpace(data.Source)) throw new Exception("Invalid Request");
                            if (requestHeader.ProviderType != data.MessageType) throw new Exception("Invalid Request");

                            isCommand = true;
                            if (!commandCounter.BeginReceive())
                                throw new CqrsNetworkException("Cannot receive any more commands");

                            var commandType = Discovery.GetTypeFromName(data.MessageType);
                            var typeDetail = TypeAnalyzer.GetTypeDetail(commandType);

                            if (!typeDetail.Interfaces.Contains(typeof(ICommand)))
                                throw new CqrsNetworkException($"Type {data.MessageType} is not a command");

                            if (!this.commandTypes.Contains(commandType))
                                throw new CqrsNetworkException($"Unhandled Command Type {commandType.FullName}");

                            var command = (ICommand?)ContentTypeSerializer.Deserialize(requestHeader.ContentType.Value, commandType, data.MessageData);
                            if (command is null)
                                throw new Exception($"Invalid {nameof(data.MessageData)}");

                            bool hasResult;
                            object? result = null;

                            inHandlerContext = true;
                            if (data.MessageResult == true)
                            {
                                if (handlerWithResultAwaitAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
                                result = await handlerWithResultAwaitAsync(command, data.Source, false);
                                hasResult = true;
                            }
                            if (data.MessageAwait == true)
                            {
                                if (handlerAwaitAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
                                await handlerAwaitAsync(command, data.Source, false);
                                hasResult = false;
                            }
                            else
                            {
                                if (handlerAsync is null) throw new InvalidOperationException($"{nameof(HttpCqrsServer)} is not setup");
                                await handlerAsync(command, data.Source, false);
                                hasResult = false;
                            }
                            inHandlerContext = false;

                            responseStarted = true;

                            //Response Header
                            var responseHeaderLength = HttpCommon.BufferOkResponseHeader(buffer, requestHeader.Origin, requestHeader.ProviderType, contentType, null);
#if NETSTANDARD2_0
                            await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                            await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                            if (hasResult)
                            {
                                //Response Body
                                responseBodyStream = new HttpProtocolBodyStream(null, stream, null, false);
                                if (symmetricConfig is not null)
                                {
                                    responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

                                    await ContentTypeSerializer.SerializeAsync(requestHeader.ContentType.Value, responseBodyCryptoStream, result);
#if NET5_0_OR_GREATER
                                    await responseBodyCryptoStream.FlushFinalBlockAsync();
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                }
                                else
                                {
                                    await ContentTypeSerializer.SerializeAsync(requestHeader.ContentType.Value, responseBodyStream, result);
                                }
                            }
                            else
                            {
                                //Response Body Empty
                                responseBodyStream = new HttpProtocolBodyStream(null, stream, null, false);
                                await responseBodyStream.FlushAsync(cancellationToken);
                            }

                            continue;
                        }

                        throw new CqrsNetworkException("Invalid Request");
                    }
                    catch (Exception ex)
                    {
                        if (!inHandlerContext)
                        {
                            _ = Log.ErrorAsync(ex);
                            return; //aborted or network error
                        }

                        if (socket.Connected && !responseStarted && requestHeader is not null && requestHeader.ContentType.HasValue)
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
                                if (symmetricConfig is not null)
                                {
                                    responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

                                    await ContentTypeSerializer.SerializeExceptionAsync(requestHeader.ContentType.Value, responseBodyCryptoStream, ex);
#if NET5_0_OR_GREATER
                                    await responseBodyCryptoStream.FlushFinalBlockAsync();
#else
                                    responseBodyCryptoStream.FlushFinalBlock();
#endif
                                }
                                else
                                {
                                    await ContentTypeSerializer.SerializeExceptionAsync(requestHeader.ContentType.Value, responseBodyStream, ex);
                                }
                            }
                            catch (Exception ex2)
                            {
                                _ = Log.ErrorAsync($"{nameof(HttpCqrsServer)} Error {socket.RemoteEndPoint}", ex2);
                            }
                        }
                    }
                    finally
                    {
                        if (responseBodyCryptoStream is not null)
                        {
#if NETSTANDARD2_0
                            responseBodyCryptoStream.Dispose();
#else
                            await responseBodyCryptoStream.DisposeAsync();
#endif
                        }
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
                        if (stream is not null)
                        {
#if NETSTANDARD2_0
                            stream.Dispose();
#else
                            await stream.DisposeAsync();
#endif
                        }
                        ArrayPoolHelper<byte>.Return(bufferOwner);
                        if (isCommand)
                            commandCounter.CompleteReceive(throttle);
                        else
                            throttle.Release();
                    }
                }
            }
            finally
            {
                socket.Dispose();
            }
        }

        public static HttpCqrsServer CreateDefault(string serverUrl, SymmetricConfig? symmetricConfig, ICqrsAuthorizer? authorizer, string[]? allowOrigins)
        {
            return new HttpCqrsServer(ContentType.Json, serverUrl, symmetricConfig, authorizer, allowOrigins);
        }
    }
}