// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Network;
using Zerra.Serialization;

namespace Zerra.Test.CQRS.Network
{
    public class ExceptionSerializerTests
    {
        private static ISerializer CreateTestSerializer()
        {
            return new ZerraByteSerializer();
        }

        [Fact]
        public void Serialize_WithInvalidOperationException()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var exception = new InvalidOperationException("Test error message");

            ExceptionSerializer.Serialize(serializer, stream, exception);

            Assert.True(stream.Length > 0);
        }

        [Fact]
        public void Serialize_WithArgumentException()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var exception = new ArgumentException("Argument is invalid", "paramName");

            ExceptionSerializer.Serialize(serializer, stream, exception);

            Assert.True(stream.Length > 0);
        }

        [Fact]
        public void Serialize_WithNestedInnerException()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var innerException = new InvalidOperationException("Inner error");
            var outerException = new Exception("Outer error", innerException);

            ExceptionSerializer.Serialize(serializer, stream, outerException);

            Assert.True(stream.Length > 0);
        }

        [Fact]
        public void Deserialize_WithValidExceptionContent()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var originalException = new InvalidOperationException("Test error message");

            ExceptionSerializer.Serialize(serializer, stream, originalException);
            stream.Position = 0;

            var deserializedException = ExceptionSerializer.Deserialize("Test Source", serializer, stream);

            Assert.NotNull(deserializedException);
            _ = Assert.IsType<RemoteServiceException>(deserializedException);
            Assert.Contains("Test error message", deserializedException.Message);
        }

        [Fact]
        public void Deserialize_WithEmptyStream_ThrowsException()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();

            var ex = Assert.ThrowsAny<Exception>(() => ExceptionSerializer.Deserialize("Test Source", serializer, stream));
            Assert.Contains("Test Source", ex.Message);
        }

        [Fact]
        public void Serialize_PreservesExceptionMessage()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var expectedMessage = "This is the exception message";
            var exception = new InvalidOperationException(expectedMessage);

            ExceptionSerializer.Serialize(serializer, stream, exception);
            stream.Position = 0;

            var deserializedException = ExceptionSerializer.Deserialize("Test Source", serializer, stream);

            Assert.Contains(expectedMessage, deserializedException.Message);
        }

        [Fact]
        public void Serialize_WithCustomException()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var exception = new NotImplementedException("Feature not implemented");

            ExceptionSerializer.Serialize(serializer, stream, exception);

            Assert.True(stream.Length > 0);
        }

        [Fact]
        public async Task SerializeAsync_WithInvalidOperationException()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var exception = new InvalidOperationException("Async test error");
            var cts = new CancellationTokenSource();

            var task = ExceptionSerializer.SerializeAsync(serializer, stream, exception, cts.Token);

            Assert.NotNull(task);
            await task;
            Assert.True(stream.Length > 0);
        }

        [Fact]
        public async Task SerializeAsync_WithArgumentException()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var exception = new ArgumentException("Async argument error", "paramName");

            await ExceptionSerializer.SerializeAsync(serializer, stream, exception, CancellationToken.None);

            Assert.True(stream.Length > 0);
        }

        [Fact]
        public async Task DeserializeAsync_WithValidExceptionContent()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var originalException = new InvalidOperationException("Async test error message");

            await ExceptionSerializer.SerializeAsync(serializer, stream, originalException, CancellationToken.None);
            stream.Position = 0;

            var deserializedException = await ExceptionSerializer.DeserializeAsync("Test Source", serializer, stream, CancellationToken.None);

            Assert.NotNull(deserializedException);
            _ = Assert.IsType<RemoteServiceException>(deserializedException);
            Assert.Contains("Async test error message", deserializedException.Message);
        }

        [Fact]
        public async Task DeserializeAsync_WithEmptyStream_ThrowsException()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();

            var ex = await Assert.ThrowsAnyAsync<Exception>(() => ExceptionSerializer.DeserializeAsync("Test Source", serializer, stream, CancellationToken.None));
            Assert.Contains("Test Source", ex.Message);
        }

        [Fact]
        public async Task SerializeAsync_PreservesExceptionMessage()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var expectedMessage = "Async exception message preservation";
            var exception = new InvalidOperationException(expectedMessage);

            await ExceptionSerializer.SerializeAsync(serializer, stream, exception, CancellationToken.None);
            stream.Position = 0;

            var deserializedException = await ExceptionSerializer.DeserializeAsync("Test Source", serializer, stream, CancellationToken.None);

            Assert.Contains(expectedMessage, deserializedException.Message);
        }

        [Fact]
        public async Task SerializeAsync_WithCustomException()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var exception = new NotImplementedException("Async feature not implemented");

            await ExceptionSerializer.SerializeAsync(serializer, stream, exception, CancellationToken.None);

            Assert.True(stream.Length > 0);
        }

        [Fact]
        public void SerializeDeserialize_RoundTrip_InvalidOperationException()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var originalException = new InvalidOperationException("Round trip test message");

            ExceptionSerializer.Serialize(serializer, stream, originalException);
            stream.Position = 0;

            var deserializedException = ExceptionSerializer.Deserialize("Test Source", serializer, stream);

            Assert.NotNull(deserializedException);
            Assert.Contains("Round trip test message", deserializedException.Message);
        }

        [Fact]
        public async Task SerializeDeserializeAsync_RoundTrip_ArgumentException()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var originalException = new ArgumentException("Async round trip test", "testParam");

            await ExceptionSerializer.SerializeAsync(serializer, stream, originalException, CancellationToken.None);
            stream.Position = 0;

            var deserializedException = await ExceptionSerializer.DeserializeAsync("Test Source", serializer, stream, CancellationToken.None);

            Assert.NotNull(deserializedException);
            Assert.Contains("Async round trip test", deserializedException.Message);
        }

        [Fact]
        public async Task Serialize_WithCancellationToken()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var exception = new OperationCanceledException("Operation was cancelled");
            var cts = new CancellationTokenSource();

            var task = ExceptionSerializer.SerializeAsync(serializer, stream, exception, cts.Token);
            await task;

            Assert.True(stream.Length > 0);
        }

        [Fact]
        public void Deserialize_ReturnsRemoteServiceException_WhenExceptionCannotBeDeserialized()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var exception = new InvalidOperationException("Test");

            ExceptionSerializer.Serialize(serializer, stream, exception);
            stream.Position = 0;

            var result = ExceptionSerializer.Deserialize("Test Source", serializer, stream);

            _ = Assert.IsType<RemoteServiceException>(result);
        }

        [Fact]
        public async Task DeserializeAsync_ReturnsRemoteServiceException_WhenExceptionCannotBeDeserialized()
        {
            var serializer = CreateTestSerializer();
            var stream = new MemoryStream();
            var exception = new InvalidOperationException("Async test");

            await ExceptionSerializer.SerializeAsync(serializer, stream, exception, CancellationToken.None);
            stream.Position = 0;

            var result = await ExceptionSerializer.DeserializeAsync("Test Source", serializer, stream, CancellationToken.None);

            _ = Assert.IsType<RemoteServiceException>(result);
        }

        [Fact]
        public void SerializeBytes_WithException_ReturnsByteArray()
        {
            var serializer = CreateTestSerializer();
            var exception = new InvalidOperationException("Byte array test error");

            var bytes = ExceptionSerializer.Serialize(serializer, exception);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void SerializeBytes_WithArgumentException_ReturnsByteArray()
        {
            var serializer = CreateTestSerializer();
            var exception = new ArgumentException("Byte array argument error", "paramName");

            var bytes = ExceptionSerializer.Serialize(serializer, exception);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void SerializeBytes_WithErrorMessage_ReturnsByteArray()
        {
            var serializer = CreateTestSerializer();
            var errorMessage = "Error message string serialization";

            var bytes = ExceptionSerializer.Serialize(serializer, errorMessage);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public void DeserializeBytes_WithValidBytes_ReturnsRemoteServiceException()
        {
            var serializer = CreateTestSerializer();
            var exception = new InvalidOperationException("Byte deserialization test");

            var bytes = ExceptionSerializer.Serialize(serializer, exception);
            var result = ExceptionSerializer.Deserialize("Test Source", serializer, bytes);

            Assert.NotNull(result);
            _ = Assert.IsType<RemoteServiceException>(result);
            Assert.Contains("Byte deserialization test", result.Message);
        }

        [Fact]
        public void DeserializeBytes_PreservesExceptionMessage()
        {
            var serializer = CreateTestSerializer();
            var expectedMessage = "Preserved byte array message";
            var exception = new InvalidOperationException(expectedMessage);

            var bytes = ExceptionSerializer.Serialize(serializer, exception);
            var result = ExceptionSerializer.Deserialize("Test Source", serializer, bytes);

            Assert.Contains(expectedMessage, result.Message);
        }

        [Fact]
        public void SerializeDeserializeBytes_RoundTrip_ErrorMessage()
        {
            var serializer = CreateTestSerializer();
            var errorMessage = "Round trip error message string";

            var bytes = ExceptionSerializer.Serialize(serializer, errorMessage);
            var result = ExceptionSerializer.Deserialize("Test Source", serializer, bytes);

            Assert.NotNull(result);
            _ = Assert.IsType<RemoteServiceException>(result);
            Assert.Contains(errorMessage, result.Message);
        }
    }
}
