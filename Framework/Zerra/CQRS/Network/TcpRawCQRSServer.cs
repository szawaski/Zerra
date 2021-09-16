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

namespace Zerra.CQRS.Network
{
    public sealed class TcpRawCQRSServer : TcpCQRSServerBase
    {
        private readonly NetworkType networkType;
        private readonly ContentType? contentType;
        private readonly SymmetricKey encryptionKey;
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.RijndaelManaged;

        public TcpRawCQRSServer(NetworkType networkType, ContentType? contentType, string serverUrl, SymmetricKey encryptionKey)
            : base(serverUrl)
        {
            this.networkType = networkType;
            this.contentType = contentType;
            this.encryptionKey = encryptionKey;
        }

        protected override async void Handle(TcpClient client, CancellationToken cancellationToken)
        {
            TcpRequestHeader requestHeader = null;
            bool responseStarted = false;

            var bufferOwner = BufferArrayPool<byte>.Rent(TcpRawCommon.BufferLength);
            var buffer = bufferOwner.AsMemory();
            Stream stream = null;
            Stream requestBodyStream = null;
            Stream responseBodyStream = null;
            FinalBlockStream responseBodyCryptoStream = null;
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
                        throw new EndOfStreamException();
                    headerLength += bytesRead;

                    requestHeaderEnd = TcpRawCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                }
                requestHeader = TcpRawCommon.ReadHeader(buffer, headerPosition, headerLength);

                if (!requestHeader.ContentType.HasValue || (contentType.HasValue && requestHeader.ContentType.HasValue && requestHeader.ContentType != contentType))
                {
                    _ = Log.ErrorAsync($"{nameof(TcpRawCQRSServer)} Received Invalid Content Type {requestHeader.ContentType}");
                    throw new Exception("Invalid Content Type");
                }

                _ = Log.TraceAsync($"{nameof(TcpRawCQRSServer)} Received On {client.Client.LocalEndPoint} From {client.Client.RemoteEndPoint} {requestHeader.ProviderType}");

                //Read Request Body
                //------------------------------------------------------------------------------------------------------------

                requestBodyStream = new TcpRawProtocolBodyStream(stream, requestHeader.BodyStartBuffer, true);

                if (encryptionKey != null)
                {
                    requestBodyStream = networkType switch
                    {
                        NetworkType.Internal => SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, requestBodyStream, false, true, false),
                        NetworkType.Api => throw new NotSupportedException($"Encryption Not Supported for Network Type {EnumName.GetEnumName(networkType)}"),
                        _ => throw new NotImplementedException(),
                    };
                }

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
                switch (networkType)
                {
                    case NetworkType.Internal:
                        if (data.Claims != null)
                        {
                            var claimsIdentity = new ClaimsIdentity(data.Claims.Select(x => new Claim(x.Type, x.Value)), "CQRS");
                            Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                        }
                        else
                        {
                            Thread.CurrentPrincipal = null;
                        }
                        break;
                    case NetworkType.Api:
                        break;
                    default:
                        throw new NotImplementedException();
                }

                //Process and Respond
                //----------------------------------------------------------------------------------------------------
                if (!String.IsNullOrWhiteSpace(data.ProviderType))
                {
                    var providerType = Discovery.GetTypeFromName(data.ProviderType);
                    var typeDetail = TypeAnalyzer.GetType(providerType);

                    if (!this.interfaceTypes.Contains(providerType))
                        throw new Exception($"Unhandled Provider Type {providerType.FullName}");

                    bool exposed = typeDetail.Attributes.Any(x => x is ServiceExposedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == networkType))
                        && !typeDetail.Attributes.Any(x => x is ServiceBlockedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == networkType));
                    if (!exposed)
                        throw new Exception($"Provider {data.MessageType} is not exposed to {networkType}");

                    _ = Log.TraceAsync($"Received Call: {providerType.GetNiceName()}.{data.ProviderMethod}");

                    var result = await this.providerHandlerAsync.Invoke(providerType, data.ProviderMethod, data.ProviderArguments);

                    //Response Header
                    var responseHeaderLength = TcpRawCommon.BufferHeader(buffer, requestHeader.ProviderType, requestHeader.ContentType.Value);
#if NETSTANDARD2_0
                    await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                    await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                    //Response Body
                    responseBodyStream = new TcpRawProtocolBodyStream(stream, null, false);

                    int bytesRead;
                    if (result.Stream != null)
                    {
                        if (encryptionKey != null)
                        {
                            responseBodyCryptoStream = networkType switch
                            {
                                NetworkType.Internal => SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, responseBodyStream, true, true, false),
                                NetworkType.Api => throw new NotSupportedException($"Encryption Not Supported for Network Type {EnumName.GetEnumName(networkType)}"),
                                _ => throw new NotImplementedException(),
                            };

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
                        if (encryptionKey != null)
                        {
                            responseBodyCryptoStream = networkType switch
                            {
                                NetworkType.Internal => SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, responseBodyStream, true, true, false),
                                NetworkType.Api => throw new NotSupportedException($"Encryption Not Supported for Network Type {EnumName.GetEnumName(networkType)}"),
                                _ => throw new NotImplementedException(),
                            };

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
                    var typeDetail = TypeAnalyzer.GetType(commandType);

                    if (!typeDetail.Interfaces.Contains(typeof(ICommand)))
                        throw new Exception($"Type {data.MessageType} is not a command");

                    bool exposed = typeDetail.Attributes.Any(x => x is ServiceExposedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == networkType))
                        && !typeDetail.Attributes.Any(x => x is ServiceBlockedAttribute attribute && (!attribute.NetworkType.HasValue || attribute.NetworkType == networkType));
                    if (!exposed)
                        throw new Exception($"Command {data.MessageType} is not exposed to {networkType}");

                    var command = (ICommand)System.Text.Json.JsonSerializer.Deserialize(data.MessageData, commandType);

                    if (data.MessageAwait)
                        await handlerAwaitAsync(command);
                    else
                        await handlerAsync(command);

                    //Response Header
                    var responseHeaderLength = TcpRawCommon.BufferHeader(buffer, requestHeader.ProviderType, requestHeader.ContentType.Value);
#if NETSTANDARD2_0
                    await stream.WriteAsync(bufferOwner, 0, responseHeaderLength, cancellationToken);
#else
                    await stream.WriteAsync(buffer.Slice(0, responseHeaderLength), cancellationToken);
#endif

                    //Response Body Empty
                    responseBodyStream = new TcpRawProtocolBodyStream(stream, null, false);
                    await responseBodyStream.FlushAsync();
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
                        responseBodyStream = new TcpRawProtocolBodyStream(stream, null, false);
                        if (encryptionKey != null)
                        {
                            responseBodyCryptoStream = networkType switch
                            {
                                NetworkType.Internal => SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, responseBodyStream, true, true, false),
                                NetworkType.Api => throw new NotSupportedException($"Encryption Not Supported for Network Type {EnumName.GetEnumName(networkType)}"),
                                _ => throw new NotImplementedException(),
                            };
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

        public static TcpRawCQRSServer CreateDefault(string serverUrl, SymmetricKey encryptionKey)
        {
            return new TcpRawCQRSServer(NetworkType.Internal, ContentType.Bytes, serverUrl, encryptionKey);
        }
    }
}