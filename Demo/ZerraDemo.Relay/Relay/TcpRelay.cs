// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET5_0_OR_GREATER

using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security;
using System.Threading;
using Zerra.Logging;
using Zerra.IO;
using Zerra.CQRS.Network;
using System.IO;
using System.Linq;
using Zerra.Serialization.Json;
using System.Threading.Tasks;
using Zerra.Buffers;

namespace Zerra.CQRS.Relay
{
    public sealed class TcpRelay : TcpCqrsServerBase, IDisposable
    {
        private const int bufferLength = 65536;
        private readonly string relayKey;
        private readonly CancellationTokenSource canceller;

        public TcpRelay(string serverUrl, string relayKey) 
            : base(serverUrl)
        {
            this.relayKey = relayKey;
            this.canceller = new CancellationTokenSource();
        }

        public void Run()
        {
            _ = Log.InfoAsync($"{nameof(TcpRelay)} Starting");
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            RelayConnectedServicesManager.LoadState().GetAwaiter().GetResult();
            _ = RelayConnectedServicesManager.StartExpireStatistics(canceller.Token);

            _ = Log.InfoAsync($"{nameof(TcpRelay)} Opened On {ServiceUrl}");

            Open();

            _ = Log.InfoAsync($"{nameof(TcpRelay)} Closed On {ServiceUrl}");
        }

        private void CurrentDomain_ProcessExit(object? sender, EventArgs? e)
        {
            canceller.Cancel();
        }

        protected override async Task Handle(Socket socket, CancellationToken cancellationToken)
        {
            TcpClient? outgoingClient = null;
            CQRSProtocolType? protocolType = null;
            RelayConnectedService? service = null;
            Stopwatch? stopwatch = null;
            var responseStarted = false;

            var bufferOwner = ArrayPoolHelper<byte>.Rent(bufferLength);
            var buffer = bufferOwner.AsMemory();

            Stream? incommingStream = null;
            Stream? incommingBodyStream = null;
            Stream? outgoingWritingBodyStream = null;

            Stream? outgoingStream = null;
            Stream? outgoingBodyStream = null;
            Stream? incommingWritingBodyStream = null;

            try
            {
                incommingStream = new NetworkStream(socket, true);

                var headerPosition = 0;
                var headerLength = 0;
                var headerEnd = false;
                while (headerLength < TcpCommon.MaxProtocolHeaderPrefixLength)
                {
                    if (headerLength == buffer.Length)
                        throw new Exception($"{nameof(TcpRelay)} Header Too Long");

                    headerLength += await incommingStream.ReadAsync(buffer.Slice(headerPosition, buffer.Length - headerPosition), cancellationToken);
                }
                protocolType = TcpCommon.ReadProtocol(buffer.Span);

                string? providerType;

                switch (protocolType.Value)
                {
                    case CQRSProtocolType.TcpRaw:
                        {
                            headerEnd = TcpRawCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                            while (!headerEnd)
                            {
                                if (headerLength == buffer.Length)
                                    throw new Exception($"{nameof(TcpRelay)} Header Too Long");

                                headerLength += await incommingStream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);

                                headerEnd = TcpRawCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                            }
                            var header = TcpRawCommon.ReadHeader(buffer, headerPosition, headerLength);
                            providerType = header.ProviderType;
                            incommingBodyStream = new TcpRawProtocolBodyStream(incommingStream, header.BodyStartBuffer, true);
                            break;
                        }
                    case CQRSProtocolType.Http:
                        {
                            headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                            while (!headerEnd)
                            {
                                if (headerLength == buffer.Length)
                                    throw new Exception($"{nameof(TcpRelay)} Header Too Long");

                                headerLength += await incommingStream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);

                                headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                            }
                            var header = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);
                            providerType = header.ProviderType;
                            incommingBodyStream = new HttpProtocolBodyStream(header.ContentLength, incommingStream, header.BodyStartBuffer, true);

                            if (header.RelayServiceAddRemove.HasValue)
                            {
                                if (header.RelayKey != relayKey)
                                    throw new SecurityException("Invalid Relay Key");

                                var serviceInfo = await JsonSerializer.DeserializeAsync<ServiceInfo>(incommingBodyStream);
                                await incommingBodyStream.DisposeAsync();
                                incommingBodyStream = null;

                                if (serviceInfo is not null)
                                {
                                    if (header.RelayServiceAddRemove == true)
                                        RelayConnectedServicesManager.AddOrUpdate(serviceInfo);
                                    else
                                        RelayConnectedServicesManager.Remove(serviceInfo.Url);
                                }

                                var requestHeaderLength = HttpCommon.BufferOkResponseHeader(buffer);
                                await incommingStream.WriteAsync(buffer.Slice(0, requestHeaderLength), cancellationToken);
                                await incommingStream.FlushAsync(cancellationToken);
                                await incommingStream.DisposeAsync();
                                incommingStream = null;
                                socket.Dispose();
                                return;
                            }
                            break;
                        }
                    default: throw new NotImplementedException();
                }

                if (providerType is null)
                    throw new Exception("ProviderType not supplied");

                while (outgoingClient is null)
                {
                    service = RelayConnectedServicesManager.GetBestService(providerType);
                    if (service is null || String.IsNullOrWhiteSpace(service.Url))
                        break;

                    try
                    {
                        _ = Log.TraceAsync($"Relaying {providerType} to {service.Url}");

                        var outgoingEndpoint = IPResolver.GetIPEndPoints(service.Url).First();
                        if (outgoingEndpoint is not null)
                        {
                            outgoingClient = new TcpClient(outgoingEndpoint.AddressFamily);
                            outgoingClient.NoDelay = true;
                            await outgoingClient.ConnectAsync(outgoingEndpoint.Address, outgoingEndpoint.Port);
                            service.FlagConnectionSuccess();
                        }
                        else
                        {
                            service.FlagConnectionFailed();
                        }
                    }
                    catch
                    {
                        service.FlagConnectionFailed();
                    }
                }

                if (outgoingClient is null || service is null)
                {
                    _ = Log.WarnAsync($"Destination not found {providerType}");
                    switch (protocolType.Value)
                    {
                        case CQRSProtocolType.TcpRaw:
                            {
                                var requestHeaderLength = HttpCommon.BufferNotFoundResponseHeader(buffer);
                                await incommingStream.WriteAsync(buffer.Slice(0, requestHeaderLength), cancellationToken);
                                return;
                            }
                        case CQRSProtocolType.Http:
                            {
                                var requestHeaderLength = TcpRawCommon.BufferErrorHeader(buffer, null, default);
                                await incommingStream.WriteAsync(buffer.Slice(0, requestHeaderLength), cancellationToken);
                                return;
                            }
                        default: throw new NotImplementedException();
                    }
                }

                outgoingStream = outgoingClient.GetStream();

                stopwatch = service.StartRequestRunning();

                responseStarted = true;
                await outgoingStream.WriteAsync(buffer.Slice(0, headerPosition), cancellationToken);
                switch (protocolType.Value)
                {
                    case CQRSProtocolType.TcpRaw:
                        {
                            outgoingWritingBodyStream = new TcpRawProtocolBodyStream(outgoingStream, null, true);
                            await incommingBodyStream.CopyToAsync(outgoingWritingBodyStream, cancellationToken);
                            await outgoingWritingBodyStream.FlushAsync(cancellationToken);
                            break;
                        }
                    case CQRSProtocolType.Http:
                        {
                            outgoingWritingBodyStream = new HttpProtocolBodyStream(null, outgoingStream, null, true);
                            await incommingBodyStream.CopyToAsync(outgoingWritingBodyStream, cancellationToken);
                            await outgoingWritingBodyStream.FlushAsync(cancellationToken);
                            break;
                        }
                    default: throw new NotImplementedException();
                }

                await incommingBodyStream.DisposeAsync();
                incommingBodyStream = null;
                await outgoingWritingBodyStream.DisposeAsync();
                outgoingWritingBodyStream = null;

                //Response
                //-----------------------------------------------------------------------------------------------------------------------

                headerPosition = 0;
                headerLength = 0;
                headerEnd = false;
                switch (protocolType.Value)
                {
                    case CQRSProtocolType.TcpRaw:
                        {
                            while (!headerEnd)
                            {
                                if (headerLength == buffer.Length)
                                    throw new Exception($"{nameof(TcpRelay)} Header Too Long");

                                headerLength += await outgoingStream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);

                                headerEnd = TcpRawCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                            }
                            outgoingBodyStream = new TcpRawProtocolBodyStream(outgoingStream, buffer.Slice(headerPosition, headerLength - headerPosition), false);
                            break;
                        }
                    case CQRSProtocolType.Http:
                        {
                            while (!headerEnd)
                            {
                                if (headerLength == buffer.Length)
                                    throw new Exception($"{nameof(TcpRelay)} Header Too Long");

                                headerLength += await outgoingStream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength), cancellationToken);

                                headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                            }
                            var header = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);
                            outgoingBodyStream = new HttpProtocolBodyStream(header.ContentLength, outgoingStream, header.BodyStartBuffer, false);
                            break;
                        }
                    default:
                        throw new NotImplementedException();
                }

                await incommingStream.WriteAsync(buffer.Slice(0, headerPosition), cancellationToken);
                switch (protocolType.Value)
                {
                    case CQRSProtocolType.TcpRaw:
                        {
                            incommingWritingBodyStream = new TcpRawProtocolBodyStream(incommingStream, null, false);
                            await outgoingBodyStream.CopyToAsync(incommingWritingBodyStream, cancellationToken);
                            await incommingWritingBodyStream.FlushAsync(cancellationToken);
                            break;
                        }
                    case CQRSProtocolType.Http:
                        {
                            incommingWritingBodyStream = new HttpProtocolBodyStream(null, incommingStream, null, false);
                            await outgoingBodyStream.CopyToAsync(incommingWritingBodyStream, cancellationToken);
                            await incommingWritingBodyStream.FlushAsync(cancellationToken);
                            break;
                        }
                    default: throw new NotImplementedException();
                }

                await outgoingBodyStream.DisposeAsync();
                outgoingBodyStream = null;
                await incommingWritingBodyStream.DisposeAsync();
                incommingWritingBodyStream = null;
                outgoingClient.Dispose();
                socket.Dispose();
            }
            catch (Exception ex)
            {
                if (ex is IOException ioException)
                {
                    if (ioException.InnerException is not null && ioException.InnerException is SocketException socketException)
                    {
                        if (socketException.SocketErrorCode == SocketError.ConnectionAborted)
                            return;
                    }
                }

                _ = Log.ErrorAsync(ex);

                if (!responseStarted && incommingStream is not null && protocolType.HasValue)
                {
                    try
                    {
                        switch (protocolType.Value)
                        {
                            case CQRSProtocolType.TcpRaw:
                                {
                                    var requestHeaderLength = HttpCommon.BufferErrorResponseHeader(buffer, null);
                                    await incommingStream.WriteAsync(buffer.Slice(0, requestHeaderLength), cancellationToken);
                                    break;
                                }
                            case CQRSProtocolType.Http:
                                {
                                    var requestHeaderLength = TcpRawCommon.BufferErrorHeader(buffer, null, default);
                                    await incommingStream.WriteAsync(buffer.Slice(0, requestHeaderLength), cancellationToken);
                                    break;
                                }
                            default: throw new NotImplementedException();
                        }
                    }
                    catch (Exception ex2)
                    {
                        _ = Log.ErrorAsync(ex2);
                    }
                }

                if (outgoingWritingBodyStream is not null)
                    await outgoingWritingBodyStream.DisposeAsync();
                if (outgoingBodyStream is not null)
                    await outgoingBodyStream.DisposeAsync();
                if (outgoingStream is not null)
                    await outgoingStream.DisposeAsync();
                if (incommingWritingBodyStream is not null)
                    await incommingWritingBodyStream.DisposeAsync();
                if (incommingBodyStream is not null)
                    await incommingBodyStream.DisposeAsync();
                if (incommingStream is not null)
                    await incommingStream.DisposeAsync();

                if (outgoingClient is not null)
                    outgoingClient.Dispose();

                socket.Dispose();
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(bufferOwner);

                if (service is not null && stopwatch is not null)
                {
                    service.EndRequestRunning(stopwatch);
                }
            }
        }
    }
}

#endif