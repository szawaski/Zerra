// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;
using Zerra.Repository.KurrentDB;

namespace Zerra.Repository.Test.KurrentDb
{
    public class KurrentDbEngineTests
    {
        [Fact]
        public async Task ValidateDataSource_HealthCheckAnswers_IsValid()
        {
            //stands in for the server's HTTP health endpoint, the client may open its own connections too so every connection is accepted
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var healthChecks = 0;
            using var stop = new CancellationTokenSource();
            var serverTask = Task.Run(async () =>
            {
                while (!stop.IsCancellationRequested)
                {
                    TcpClient connection;
                    try { connection = await listener.AcceptTcpClientAsync(stop.Token); }
                    catch (OperationCanceledException) { return; }

                    _ = Task.Run(async () =>
                    {
                        using (connection)
                        {
                            using var stream = connection.GetStream();
                            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                            var requestLine = await reader.ReadLineAsync();
                            if (requestLine != "GET /health/live HTTP/1.1")
                                return;
                            while (!String.IsNullOrEmpty(await reader.ReadLineAsync())) { }
                            _ = Interlocked.Increment(ref healthChecks);
                            await stream.WriteAsync("HTTP/1.1 204 No Content\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray());
                        }
                    });
                }
            });

            using var engine = new KurrentDbEngine($"http://127.0.0.1:{port}", true);
            var isValid = await Task.Run(engine.ValidateDataSource);
            stop.Cancel();
            await serverTask;

            Assert.True(isValid);
            Assert.Equal(1, healthChecks);
        }

        [Fact]
        public void ValidateDataSource_NothingListening_IsNotValid()
        {
            //a port that was free a moment ago, nothing accepts on it
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            using var engine = new KurrentDbEngine($"http://127.0.0.1:{port}", true);
            Assert.False(engine.ValidateDataSource());
        }
    }
}
