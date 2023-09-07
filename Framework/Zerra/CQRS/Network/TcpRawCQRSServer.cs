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
using Zerra.IO;
using System.IO;
using Zerra.Serialization;

namespace Zerra.CQRS.Network
{
    public sealed class TcpRawCQRSServer : TcpCQRSServerBase
    {
        private readonly ContentType? contentType;
        private readonly SymmetricConfig symmetricConfig;

        public TcpRawCQRSServer(ContentType? contentType, string serverUrl, SymmetricConfig symmetricConfig)
            : base(serverUrl)
        {
            this.contentType = contentType;
            this.symmetricConfig = symmetricConfig;
        }

        protected override async void Handle(TcpClient client, CancellationToken cancellationToken)
        {
            TcpRequestHeader requestHeader = null;
            var responseStarted = false;

            var bufferOwner = BufferArrayPool<byte>.Rent(TcpRawCommon.BufferLength);
            var buffer = bufferOwner.AsMemory();
            Stream stream = null;
            Stream requestBodyStream = null;
            Stream responseBodyStream = null;
            FinalBlockStream responseBodyCryptoStream = null;

            var inHandlerContext = false;
            try
            {
                stream = client.GetStream();

                //Read Request Header
                //------------------------------------------------------------------------------------------------------------
                var headerPosition = 0;
                var headerLength = 0;
                var requestHeaderEnd = false;
                while (!requestHeaderEnd)
                {
                    if (headerLength == buffer.Length)
                        throw new Exception($"{nameof(TcpRawCQRSServer)} Header Too Long");

#if NETSTANDARD2_0
                    var bytesRead = await stream.ReadAsync(bufferOwner, headerLength, buffer.Length - headerLength, cancellationToken);
#else
                    var bytesRead = await stream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);
#endif

                    if (bytesRead == 0)
                        throw new CQRSRequestAbortedException();
                    headerLength += bytesRead;

                    requestHeaderEnd = TcpRawCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                }
                requestHeader = TcpRawCommon.ReadHeader(buffer, headerPosition, headerLength);

                if (!requestHeader.ContentType.HasValue || (contentType.HasValue && requestHeader.ContentType.HasValue && requestHeader.ContentType != contentType))
                {
                    _ = Log.ErrorAsync($"{nameof(TcpRawCQRSServer)} Received Invalid Content Type {requestHeader.ContentType}");
                    throw new Exception("Invalid Content Type");
                }

                //Read Request Body
                //------------------------------------------------------------------------------------------------------------

                requestBodyStream = new TcpRawProtocolBodyStream(stream, requestHeader.BodyStartBuffer, true);

                if (symmetricConfig != null)
                    requestBodyStream = SymmetricEncryptor.Decrypt(symmetricConfig, requestBodyStream, false);

                var data = await ContentTypeSerializer.DeserializeAsync<CQRSRequestData>(requestHeader.ContentType.Value, requestBodyStream);
                if (data == null)
                    throw new Exception("Empty request body");

#if NETSTANDARD2_0
                requestBodyStream.Dispose();
#else
                await requestBodyStream.DisposeAsync();
#endif
                requestBodyStream = null;

                //Authroize
                //------------------------------------------------------------------------------------------------------------
                if (data.Claims != null)
                {
                    var claimsIdentity = new ClaimsIdentity(data.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                    Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                }
                else
                {
                    Thread.CurrentPrincipal = null;
                }

                //Process and Respond
                //----------------------------------------------------------------------------------------------------
                if (!String.IsNullOrWhiteSpace(data.ProviderType))
                {
                    var providerType = Discovery.GetTypeFromName(data.ProviderType);
                    var typeDetail = TypeAnalyzer.GetTypeDetail(providerType);

                    if (!this.interfaceTypes.Contains(providerType))
                        throw new Exception($"Unhandled Provider Type {providerType.FullName}");

                    inHandlerContext = true;
                    var result = await this.providerHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments, data.Source, false);
                    inHandlerContext = false;

                    //Response Header
                    var responseHeaderLength = TcpRawCommon.BufferHeader(buffer, requestHeader.ProviderType, requestHeader.ContentType.Value);
#if NETSTANDARD2_0
                    await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                    await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                    //Response Body
                    responseBodyStream = new TcpRawProtocolBodyStream(stream, null, true);

                    int bytesRead;
                    if (result.Stream != null)
                    {
                        if (symmetricConfig != null)
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
#if NETSTANDARD2_0
                            responseBodyCryptoStream.Dispose();
#else
                            await responseBodyCryptoStream.DisposeAsync();
#endif
                            responseBodyCryptoStream = null;
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
#if NETSTANDARD2_0
                            responseBodyStream.Dispose();
#else
                            await responseBodyStream.DisposeAsync();
#endif
                        }

                        client.Dispose();
                        return;
                    }
                    else
                    {
                        if (symmetricConfig != null)
                        {
                            responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

                            await ContentTypeSerializer.SerializeAsync(requestHeader.ContentType.Value, responseBodyCryptoStream, result.Model);
#if NET5_0_OR_GREATER
                            await responseBodyCryptoStream.FlushFinalBlockAsync();
#else
                            responseBodyCryptoStream.FlushFinalBlock();
#endif
#if NETSTANDARD2_0
                            responseBodyCryptoStream.Dispose();
#else
                            await responseBodyCryptoStream.DisposeAsync();
#endif
                            responseBodyCryptoStream = null;
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
                            return;
                        }
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

                    var command = (ICommand)JsonSerializer.Deserialize(commandType, data.MessageData);

                    inHandlerContext = true;
                    if (data.MessageAwait)
                        await handlerAwaitAsync(command, data.Source, false);
                    else
                        await handlerAsync(command, data.Source, false);
                    inHandlerContext = false;

                    //Response Header
                    var responseHeaderLength = TcpRawCommon.BufferHeader(buffer, requestHeader.ProviderType, requestHeader.ContentType.Value);
#if NETSTANDARD2_0
                    await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                    await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                    //Response Body Empty
                    responseBodyStream = new TcpRawProtocolBodyStream(stream, null, true);
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
                            return;
                        }
                    }
                }

                if (!inHandlerContext)
                    _ = Log.ErrorAsync(null, ex);

                if (client.Connected && !responseStarted && requestHeader != null && requestHeader.ContentType.HasValue)
                {
                    try
                    {
                        //Response Header for Error
                        var responseHeaderLength = TcpRawCommon.BufferErrorHeader(buffer, requestHeader.ProviderType, requestHeader.ContentType.Value);
#if NETSTANDARD2_0
                        await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                        await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                        //Response Body
                        responseBodyStream = new TcpRawProtocolBodyStream(stream, null, true);
                        if (symmetricConfig != null)
                        {
                            responseBodyCryptoStream = SymmetricEncryptor.Encrypt(symmetricConfig, responseBodyStream, true);

                            await ContentTypeSerializer.SerializeExceptionAsync(requestHeader.ContentType.Value, responseBodyCryptoStream, ex);
#if NET5_0_OR_GREATER
                            await responseBodyCryptoStream.FlushFinalBlockAsync();
#else
                            responseBodyCryptoStream.FlushFinalBlock();
#endif
#if NETSTANDARD2_0
                            responseBodyCryptoStream.Dispose();
#else
                            await responseBodyCryptoStream.DisposeAsync();
#endif
                        }
                        else
                        {
                            await ContentTypeSerializer.SerializeExceptionAsync(requestHeader.ContentType.Value, responseBodyStream, ex);
#if NETSTANDARD2_0
                            responseBodyStream.Dispose();
#else
                            await responseBodyStream.DisposeAsync();
#endif
                        }
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
                        _ = Log.ErrorAsync($"{nameof(TcpRawCQRSServer)} Error {client.Client.RemoteEndPoint}", ex2);
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

        public static TcpRawCQRSServer CreateDefault(string serverUrl, SymmetricConfig symmetricConfig)
        {
            return new TcpRawCQRSServer(ContentType.Bytes, serverUrl, symmetricConfig);
        }
    }
}