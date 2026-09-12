// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class CqrsClientBaseTests
    {
        //a url without a scheme is read as http, the port comes from the url or the scheme's default
        [Theory]
        [InlineData("127.0.0.1:9001", "127.0.0.1", 9001)]
        [InlineData("localhost", "localhost", 80)]
        [InlineData("example.com:8080", "example.com", 8080)]
        [InlineData("http://example.com", "example.com", 80)]
        [InlineData("https://example.com", "example.com", 443)]
        [InlineData("https://example.com:8443/path", "example.com", 8443)]
        [InlineData("tcp://example.com:9001", "example.com", 9001)]
        [InlineData("tcp://example.com", "example.com", 80)]
        public void Constructor_ParsesHostAndPort(string url, string host, int port)
        {
            using var client = new TestClient(url);

            Assert.Equal(host, client.Host);
            Assert.Equal(port, client.Port);
            Assert.Equal(url, ((IQueryClient)client).ServiceUrl);
        }

        [Fact]
        public void Constructor_NoScheme_UsesHttp()
        {
            using var client = new TestClient("example.com:9001");

            Assert.Equal(Uri.UriSchemeHttp, client.ServiceUri.Scheme);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_MissingUrl_Throws(string? url)
        {
            _ = Assert.Throws<ArgumentNullException>(() => new TestClient(url!));
        }

        //exposes what the base parsed, the transport members are never called
        private sealed class TestClient : CqrsClientBase
        {
            public TestClient(string serviceUrl) : base(serviceUrl, null) { }

            public string Host => host;
            public int Port => port;
            public Uri ServiceUri => serviceUri;

            protected override TReturn CallInternal<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source) => throw new NotSupportedException();
            protected override Task<TReturn> CallInternalAsync<TReturn>(SemaphoreSlim throttle, bool isStream, Type interfaceType, string methodName, IReadOnlyList<Type> argumentTypes, object[] arguments, string source, CancellationToken cancellationToken) => throw new NotSupportedException();
            protected override Task DispatchInternal(SemaphoreSlim throttle, Type commandType, ICommand command, bool messageAwait, string source, CancellationToken cancellationToken) => throw new NotSupportedException();
            protected override Task<TResult> DispatchInternal<TResult>(SemaphoreSlim throttle, bool isStream, Type commandType, ICommand<TResult> command, string source, CancellationToken cancellationToken) => throw new NotSupportedException();
            protected override Task DispatchInternal(SemaphoreSlim throttle, Type eventType, IEvent @event, string source, CancellationToken cancellationToken) => throw new NotSupportedException();
        }
    }
}
