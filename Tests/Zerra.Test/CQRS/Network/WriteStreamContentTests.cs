// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class WriteStreamContentTests
    {
        [Fact]
        public void Constructor_WithAsyncDelegate_Succeeds()
        {
            static async Task asyncDelegate(Stream stream)
            {
                await stream.WriteAsync([1, 2, 3], 0, 3);
            }

            var content = new WriteStreamContent(asyncDelegate);

            Assert.NotNull(content);
            _ = Assert.IsAssignableFrom<HttpContent>(content);
        }

        [Fact]
        public void Constructor_WithSyncDelegate_Succeeds()
        {
            static void syncDelegate(Stream stream)
            {
                stream.Write([1, 2, 3], 0, 3);
            }

            var content = new WriteStreamContent(syncDelegate);

            Assert.NotNull(content);
            _ = Assert.IsAssignableFrom<HttpContent>(content);
        }

        [Fact]
        public void Constructor_WithNullAsyncDelegate_ThrowsArgumentNullException()
        {
            Func<Stream, Task>? asyncDelegate = null;

            var ex = Assert.Throws<ArgumentNullException>(() => new WriteStreamContent(asyncDelegate!));
            Assert.Equal("streamDelegate", ex.ParamName);
        }

        [Fact]
        public void Constructor_WithNullSyncDelegate_ThrowsArgumentNullException()
        {
            Action<Stream>? syncDelegate = null;

            var ex = Assert.Throws<ArgumentNullException>(() => new WriteStreamContent(syncDelegate!));
            Assert.Equal("streamDelegate", ex.ParamName);
        }

        [Fact]
        public async Task WriteStreamContent_WithAsyncDelegate_CanBeUsedInHttpRequestMessage()
        {
            var testData = new byte[] { 1, 2, 3, 4, 5 };
            var delegateCalled = false;

            async Task asyncDelegate(Stream stream)
            {
                delegateCalled = true;
                await stream.WriteAsync(testData, 0, testData.Length);
            }

            var content = new WriteStreamContent(asyncDelegate);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com")
            {
                Content = content
            };

            Assert.NotNull(request.Content);
            Assert.Same(content, request.Content);
        }

        [Fact]
        public async Task WriteStreamContent_WithSyncDelegate_CanBeUsedInHttpRequestMessage()
        {
            var testData = new byte[] { 10, 20, 30 };

            void syncDelegate(Stream stream)
            {
                stream.Write(testData, 0, testData.Length);
            }

            var content = new WriteStreamContent(syncDelegate);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com")
            {
                Content = content
            };

            Assert.NotNull(request.Content);
            Assert.Same(content, request.Content);
        }

        [Fact]
        public void WriteStreamContent_AsyncDelegate_CreatesValidHttpContent()
        {
            static async Task asyncDelegate(Stream stream)
            {
                await Task.CompletedTask;
            }

            var content = new WriteStreamContent(asyncDelegate);

            Assert.NotNull(content);
            _ = Assert.IsType<WriteStreamContent>(content);
        }

        [Fact]
        public void WriteStreamContent_SyncDelegate_CreatesValidHttpContent()
        {
            static void syncDelegate(Stream stream) { }

            var content = new WriteStreamContent(syncDelegate);

            Assert.NotNull(content);
            _ = Assert.IsType<WriteStreamContent>(content);
        }

        [Fact]
        public void WriteStreamContent_IsDisposable()
        {
            var content = new WriteStreamContent(async stream => { });

            // Should not throw
            content.Dispose();
        }

        [Fact]
        public void WriteStreamContent_WithAsyncDelegate_HeadersCanBeSet()
        {
            var content = new WriteStreamContent(async stream => { });

            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            Assert.NotNull(content.Headers.ContentType);
            Assert.Equal("application/json", content.Headers.ContentType.MediaType);
        }

        [Fact]
        public void WriteStreamContent_WithSyncDelegate_HeadersCanBeSet()
        {
            var content = new WriteStreamContent(stream => { });

            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

            Assert.NotNull(content.Headers.ContentType);
            Assert.Equal("text/plain", content.Headers.ContentType.MediaType);
        }

        [Fact]
        public void WriteStreamContent_WithAsyncDelegate_SupportsContentLength()
        {
            var content = new WriteStreamContent(async stream => { });

            content.Headers.ContentLength = 1024;

            Assert.Equal(1024, content.Headers.ContentLength);
        }

        [Fact]
        public void WriteStreamContent_WithSyncDelegate_SupportsContentLength()
        {
            var content = new WriteStreamContent(stream => { });

            content.Headers.ContentLength = 2048;

            Assert.Equal(2048, content.Headers.ContentLength);
        }

        [Fact]
        public void WriteStreamContent_AsyncDelegate_WithComplexDelegate()
        {
            var callCount = 0;

            async Task complexAsyncDelegate(Stream stream)
            {
                callCount++;
                var data = new byte[] { 1, 2, 3 };
                await stream.WriteAsync(data, 0, data.Length);
                await Task.Delay(10);
                var moreData = new byte[] { 4, 5 };
                await stream.WriteAsync(moreData, 0, moreData.Length);
            }

            var content = new WriteStreamContent(complexAsyncDelegate);

            Assert.NotNull(content);
        }

        [Fact]
        public void WriteStreamContent_SyncDelegate_WithComplexDelegate()
        {
            var callCount = 0;

            void complexSyncDelegate(Stream stream)
            {
                callCount++;
                var data = new byte[] { 1, 2, 3 };
                stream.Write(data, 0, data.Length);
                var moreData = new byte[] { 4, 5 };
                stream.Write(moreData, 0, moreData.Length);
            }

            var content = new WriteStreamContent(complexSyncDelegate);

            Assert.NotNull(content);
        }

        [Fact]
        public void WriteStreamContent_AsyncDelegate_CanBeReusedMultipleTimes()
        {
            static async Task asyncDelegate(Stream stream)
            {
                await stream.WriteAsync([1, 2, 3], 0, 3);
            }

            var content1 = new WriteStreamContent(asyncDelegate);
            var content2 = new WriteStreamContent(asyncDelegate);

            Assert.NotNull(content1);
            Assert.NotNull(content2);
            Assert.NotSame(content1, content2);
        }

        [Fact]
        public void WriteStreamContent_SyncDelegate_CanBeReusedMultipleTimes()
        {
            static void syncDelegate(Stream stream)
            {
                stream.Write([1, 2, 3], 0, 3);
            }

            var content1 = new WriteStreamContent(syncDelegate);
            var content2 = new WriteStreamContent(syncDelegate);

            Assert.NotNull(content1);
            Assert.NotNull(content2);
            Assert.NotSame(content1, content2);
        }

        [Fact]
        public void WriteStreamContent_WithAsyncDelegate_CreatedWithLambda()
        {
            var content = new WriteStreamContent(async stream =>
            {
                var data = new byte[] { 1, 2, 3 };
                await stream.WriteAsync(data, 0, data.Length);
            });

            Assert.NotNull(content);
            _ = Assert.IsType<WriteStreamContent>(content);
        }

        [Fact]
        public void WriteStreamContent_WithSyncDelegate_CreatedWithLambda()
        {
            var content = new WriteStreamContent(stream =>
            {
                var data = new byte[] { 1, 2, 3 };
                stream.Write(data, 0, data.Length);
            });

            Assert.NotNull(content);
            _ = Assert.IsType<WriteStreamContent>(content);
        }
    }
}
