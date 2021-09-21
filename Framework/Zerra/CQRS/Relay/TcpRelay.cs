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

namespace Zerra.CQRS.Relay
{
    public sealed class TcpRelay : TcpCQRSServerBase, IDisposable
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
            Log.InfoAsync($"{nameof(TcpRelay)} Starting");
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            RelayConnectedServicesManager.LoadState().GetAwaiter().GetResult();
            RelayConnectedServicesManager.StartExpireStatistics(canceller.Token);

            Log.InfoAsync($"{nameof(TcpRelay)} Opened On {ConnectionString}");

            Open();

            Log.InfoAsync($"{nameof(TcpRelay)} Closed On {ConnectionString}");
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {

            canceller.Cancel();
        }

        protected override async void Handle(TcpClient client, CancellationToken cancellationToken)
        {
            TcpClient outgoingClient = null;
            CQRSProtocolType? protocolType = null;
            RelayConnectedService service = null;
            Stopwatch stopwatch = null;
            bool responseStarted = false;

            var bufferOwner = BufferArrayPool<byte>.Rent(bufferLength);
            var buffer = bufferOwner.AsMemory();

            Stream incommingStream = null;
            Stream incommingBodyStream = null;
            Stream outgoingWritingBodyStream = null;

            Stream outgoingStream = null;
            Stream outgoingBodyStream = null;
            Stream incommingWritingBodyStream = null;

            try
            {
                incommingStream = client.GetStream();

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

                string providerType;

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

                                var serviceInfo = await System.Text.Json.JsonSerializer.DeserializeAsync<ServiceInfo>(incommingBodyStream);
                                await incommingBodyStream.DisposeAsync();
                                incommingBodyStream = null;

                                if (header.RelayServiceAddRemove == true)
                                    RelayConnectedServicesManager.AddOrUpdate(serviceInfo);
                                else
                                    RelayConnectedServicesManager.Remove(serviceInfo.Url);

                                var requestHeaderLength = HttpCommon.BufferOkHeader(buffer);
                                await incommingStream.WriteAsync(buffer.Slice(0, requestHeaderLength), cancellationToken);
                                await incommingStream.FlushAsync();
                                await incommingStream.DisposeAsync();
                                incommingStream = null;
                                client.Dispose();
                                return;
                            }
                            break;
                        }
                    default: throw new NotImplementedException();
                }

                while (outgoingClient == null)
                {
                    service = RelayConnectedServicesManager.GetBestService(providerType);
                    if (service == null)
                        break;

                    try
                    {
                        _ = Log.TraceAsync($"Relaying {providerType} to {service.Url}");

                        var outgoingEndpoint = IPResolver.GetIPEndPoints(service.Url, 80).First();
                        if (outgoingEndpoint != null)
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

                if (outgoingClient == null)
                {
                    _ = Log.TraceAsync($"Destination not found {providerType}");
                    switch (protocolType.Value)
                    {
                        case CQRSProtocolType.TcpRaw:
                            {
                                var requestHeaderLength = HttpCommon.BufferNotFoundHeader(buffer);
                                await incommingStream.WriteAsync(buffer.Slice(0, requestHeaderLength));
                                return;
                            }
                        case CQRSProtocolType.Http:
                            {
                                var requestHeaderLength = TcpRawCommon.BufferErrorHeader(buffer, null, default);
                                await incommingStream.WriteAsync(buffer.Slice(0, requestHeaderLength));
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
                            await outgoingWritingBodyStream.FlushAsync();
                            break;
                        }
                    case CQRSProtocolType.Http:
                        {
                            outgoingWritingBodyStream = new HttpProtocolBodyStream(null, outgoingStream, null, true);
                            await incommingBodyStream.CopyToAsync(outgoingWritingBodyStream, cancellationToken);
                            await outgoingWritingBodyStream.FlushAsync();
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

                                headerLength += await outgoingStream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength));

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

                                headerLength += await outgoingStream.ReadAsync(buffer.Slice(headerLength, buffer.Length - headerLength));

                                headerEnd = HttpCommon.ReadToHeaderEnd(buffer, ref headerPosition, headerLength);
                            }
                            var header = HttpCommon.ReadHeader(buffer, headerPosition, headerLength);
                            outgoingBodyStream = new HttpProtocolBodyStream(header.ContentLength, outgoingStream, header.BodyStartBuffer, false);
                            break;
                        }
                }

                await incommingStream.WriteAsync(buffer.Slice(0, headerPosition), cancellationToken);
                switch (protocolType.Value)
                {
                    case CQRSProtocolType.TcpRaw:
                        {
                            incommingWritingBodyStream = new TcpRawProtocolBodyStream(incommingStream, null, false);
                            await outgoingBodyStream.CopyToAsync(incommingWritingBodyStream, cancellationToken);
                            await incommingWritingBodyStream.FlushAsync();
                            break;
                        }
                    case CQRSProtocolType.Http:
                        {
                            incommingWritingBodyStream = new HttpProtocolBodyStream(null, incommingStream, null, false);
                            await outgoingBodyStream.CopyToAsync(incommingWritingBodyStream, cancellationToken);
                            await incommingWritingBodyStream.FlushAsync();
                            break;
                        }
                    default: throw new NotImplementedException();
                }

                await outgoingBodyStream.DisposeAsync();
                outgoingBodyStream = null;
                await incommingWritingBodyStream.DisposeAsync();
                incommingWritingBodyStream = null;
                outgoingClient.Dispose();
                client.Dispose();
            }
            catch (Exception ex)
            {
                if (ex is IOException ioException)
                {
                    if (ioException.InnerException != null && ioException.InnerException is SocketException socketException)
                        if (socketException.SocketErrorCode == SocketError.ConnectionAborted)
                            return;
                }

                _ = Log.ErrorAsync(null, ex);

                if (!responseStarted && incommingStream != null && protocolType.HasValue)
                {
                    try
                    {
                        switch (protocolType.Value)
                        {
                            case CQRSProtocolType.TcpRaw:
                                {
                                    var requestHeaderLength = HttpCommon.BufferErrorHeader(buffer, null);
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
                        _ = Log.ErrorAsync(null, ex2);
                    }
                }

                if (outgoingWritingBodyStream != null)
                    await outgoingWritingBodyStream.DisposeAsync();
                if (outgoingBodyStream != null)
                    await outgoingBodyStream.DisposeAsync();
                if (outgoingStream != null)
                    await outgoingStream.DisposeAsync();
                if (incommingWritingBodyStream != null)
                    await incommingWritingBodyStream.DisposeAsync();
                if (incommingBodyStream != null)
                    await incommingBodyStream.DisposeAsync();
                if (incommingStream != null)
                    await incommingStream.DisposeAsync();

                if (outgoingClient != null)
                    outgoingClient.Dispose();

                client.Dispose();
            }
            finally
            {
                BufferArrayPool<byte>.Return(bufferOwner);

                if (service != null && stopwatch != null)
                {
                    service.EndRequestRunning(stopwatch);
                }
            }
        }
    }
}

#endif