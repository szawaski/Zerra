// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Net;
using System.Net.Sockets;
using Xunit;
using Zerra.CQRS.Network;
using Zerra.Serialization;

namespace Zerra.Test.CQRS.Network
{
    public class ApiCqrsCookieAuthorizerTests
    {
        private const int timeout = 10000;
        private const string loginBody = "{\"user\":\"tester\",\"password\":\"secret\"}";

        [Fact]
        public void Authorize_PassesRequestCookies()
        {
            var authorizer = new TestCookieAuthorizer("http://localhost:9999/login");

            authorizer.Authorize(new Dictionary<string, List<string?>>() { ["Cookie"] = ["session=abc; theme=dark"] });

            Assert.NotNull(authorizer.ReceivedCookies);
            Assert.Equal("abc", authorizer.ReceivedCookies["session"]);
            Assert.Equal("dark", authorizer.ReceivedCookies["theme"]);
        }

        [Fact(Timeout = timeout)]
        public async Task Login_SendsBodyAndStoresCookies()
        {
            using var server = new FakeLoginServer();
            var authorizer = new TestCookieAuthorizer(server.Url);

            await authorizer.Login(TestContext.Current.CancellationToken);

            Assert.Equal(loginBody, Assert.Single(server.ReceivedBodies));
            Assert.NotNull(authorizer.Cookies);
            Assert.Equal("abc", authorizer.Cookies["session"]?.Value);

            //uses the cookies from the login instead of logging in again
            var headers = await authorizer.GetAuthorizationHeadersAsync(TestContext.Current.CancellationToken);
            Assert.Equal(["session=abc"], headers["Cookie"]);
            _ = Assert.Single(server.ReceivedBodies);
        }

        [Fact]
        public void Constructor_Validates()
        {
            _ = Assert.Throws<ArgumentException>(() => new ApiCqrsCookieAuthorizer(new ZerraByteSerializer(), "http://localhost/login", loginBody, "application/json"));

            //without a scheme the endpoint is http
            var authorizer = new ApiCqrsCookieAuthorizer(new ZerraJsonSerializer(), "localhost:9999/login", loginBody, "application/json");
            _ = Assert.Throws<NotImplementedException>(() => authorizer.Authorize(new Dictionary<string, List<string?>>()));
        }

        [Fact]
        public void CookiesFromString_KeepsEqualsAndSpacesInValues()
        {
            var cookies = ApiCqrsCookieAuthorizer.CookiesFromString("token=a=b; name=two words;  last=x");
            Assert.Equal("a=b", cookies["token"]);
            Assert.Equal("two words", cookies["name"]);
            Assert.Equal("x", cookies["last"]);
        }

        [Fact(Timeout = timeout)]
        public async Task GetAuthorizationHeaders_Sync_LogsInAndJoinsCookies()
        {
            using var server = new FakeLoginServer(200, "session=abc; Path=/", "theme=dark; Path=/");
            var authorizer = new TestCookieAuthorizer(server.Url);

            var headers = await Task.Run(() => authorizer.GetAuthorizationHeaders(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
            var cookie = Assert.Single(headers["Cookie"]);
            Assert.Contains("session=abc", cookie);
            Assert.Contains("; ", cookie);
            Assert.Contains("theme=dark", cookie);

            var asyncHeaders = await authorizer.GetAuthorizationHeadersAsync(TestContext.Current.CancellationToken);
            Assert.Equal(headers["Cookie"], asyncHeaders["Cookie"]);
            _ = Assert.Single(server.ReceivedBodies);
            Assert.Equal(loginBody, server.ReceivedBodies[0]);
        }

        [Fact(Timeout = timeout)]
        public async Task Login_Failure_Throws()
        {
            using var server = new FakeLoginServer(401);
            var authorizer = new TestCookieAuthorizer(server.Url);

            _ = await Assert.ThrowsAnyAsync<Exception>(() => authorizer.Login(TestContext.Current.CancellationToken));
            _ = await Assert.ThrowsAnyAsync<Exception>(() => Task.Run(() => authorizer.GetAuthorizationHeaders(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));
            Assert.Null(authorizer.Cookies);
        }

        private sealed class TestCookieAuthorizer : ApiCqrsCookieAuthorizer
        {
            public Dictionary<string, string>? ReceivedCookies { get; private set; }

            public TestCookieAuthorizer(string loginEndpoint)
                : base(new ZerraJsonSerializer(), loginEndpoint, loginBody, "application/json") { }

            public override void AuthorizeCookies(Dictionary<string, string>? cookies) => ReceivedCookies = cookies;
        }

        private sealed class FakeLoginServer : IDisposable
        {
            private readonly HttpListener listener;

            public List<string> ReceivedBodies { get; } = new();
            public string Url { get; }

            private readonly int statusCode;
            private readonly string[] setCookies;

            public FakeLoginServer(int statusCode = 200, params string[] setCookies)
            {
                this.statusCode = statusCode;
                this.setCookies = setCookies.Length > 0 ? setCookies : ["session=abc; Path=/"];
                int port;
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    port = ((IPEndPoint)socket.LocalEndPoint!).Port;
                }
                Url = $"http://localhost:{port}/login/";

                listener = new HttpListener();
                listener.Prefixes.Add(Url);
                listener.Start();
                _ = HandleRequests();
            }

            private async Task HandleRequests()
            {
                try
                {
                    for (; ; )
                    {
                        var context = await listener.GetContextAsync();
                        using (var reader = new StreamReader(context.Request.InputStream))
                            ReceivedBodies.Add(await reader.ReadToEndAsync());

                        context.Response.StatusCode = statusCode;
                        foreach (var setCookie in setCookies)
                            context.Response.AppendHeader("Set-Cookie", setCookie);
                        context.Response.ContentLength64 = 0;
                        context.Response.Close();
                    }
                }
                catch { } //stopped
            }

            public void Dispose() => listener.Close();
        }
    }
}
