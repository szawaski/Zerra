// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Network;
using Zerra.Serialization;

namespace Zerra.Test.CQRS.Network
{
    public class AcknowledgementTests
    {
        private static ISerializer CreateTestSerializer()
        {
            return new ZerraByteSerializer();
        }

        [Fact]
        public void Constructor_WithErrorMessage_SetsException()
        {
            var serializer = CreateTestSerializer();
            var ack = new Acknowledgement(serializer, "Something went wrong");

            Assert.NotNull(ack.Exception);
            Assert.True(ack.Exception.Length > 0);
            Assert.Null(ack.Data);
            Assert.Null(ack.DataType);
        }

        [Fact]
        public void Constructor_WithException_SetsException()
        {
            var serializer = CreateTestSerializer();
            var ex = new InvalidOperationException("Operation failed");
            var ack = new Acknowledgement(serializer, null, ex);

            Assert.NotNull(ack.Exception);
            Assert.True(ack.Exception.Length > 0);
            Assert.Null(ack.Data);
            Assert.Null(ack.DataType);
        }

        [Fact]
        public void Constructor_WithResult_SetsDataAndDataType()
        {
            var serializer = CreateTestSerializer();
            var result = "Hello, World!";
            var ack = new Acknowledgement(serializer, result, null);

            Assert.Null(ack.Exception);
            Assert.NotNull(ack.Data);
            Assert.NotNull(ack.DataType);
            Assert.True(ack.Data.Length > 0);
        }

        [Fact]
        public void Constructor_WithNullResultAndNoException_SetsNothing()
        {
            var serializer = CreateTestSerializer();
            var ack = new Acknowledgement(serializer, null, null);

            Assert.Null(ack.Exception);
            Assert.Null(ack.Data);
            Assert.Null(ack.DataType);
        }

        [Fact]
        public void ThrowIfFailed_WithNullAcknowledgement_ThrowsRemoteServiceException()
        {
            var serializer = CreateTestSerializer();

            _ = Assert.Throws<RemoteServiceException>(() => Acknowledgement.ThrowIfFailed(serializer, null));
        }

        [Fact]
        public void ThrowIfFailed_WithSuccessfulAcknowledgement_DoesNotThrow()
        {
            var serializer = CreateTestSerializer();
            var ack = new Acknowledgement(serializer, "result", null);

            Acknowledgement.ThrowIfFailed(serializer, ack);
        }

        [Fact]
        public void ThrowIfFailed_WithFailedAcknowledgement_ThrowsRemoteServiceException()
        {
            var serializer = CreateTestSerializer();
            var ack = new Acknowledgement(serializer, null, new InvalidOperationException("Remote failure"));

            var ex = Assert.Throws<RemoteServiceException>(() => Acknowledgement.ThrowIfFailed(serializer, ack));
            Assert.Contains("Remote failure", ex.Message);
        }

        [Fact]
        public void ThrowIfFailed_WithErrorMessageAcknowledgement_ThrowsRemoteServiceException()
        {
            var serializer = CreateTestSerializer();
            var ack = new Acknowledgement(serializer, "Error from service");

            var ex = Assert.Throws<RemoteServiceException>(() => Acknowledgement.ThrowIfFailed(serializer, ack));
            Assert.Contains("Error from service", ex.Message);
        }

        [Fact]
        public void GetResultOrThrowIfFailed_WithNullAcknowledgement_ThrowsRemoteServiceException()
        {
            var serializer = CreateTestSerializer();

            _ = Assert.Throws<RemoteServiceException>(() => Acknowledgement.GetResultOrThrowIfFailed(serializer, null));
        }

        [Fact]
        public void GetResultOrThrowIfFailed_WithFailedAcknowledgement_ThrowsRemoteServiceException()
        {
            var serializer = CreateTestSerializer();
            var ack = new Acknowledgement(serializer, null, new ArgumentException("Bad argument"));

            var ex = Assert.Throws<RemoteServiceException>(() => Acknowledgement.GetResultOrThrowIfFailed(serializer, ack));
            Assert.Contains("Bad argument", ex.Message);
        }

        [Fact]
        public void GetResultOrThrowIfFailed_WithErrorMessageAcknowledgement_ThrowsRemoteServiceException()
        {
            var serializer = CreateTestSerializer();
            var ack = new Acknowledgement(serializer, "Service error message");

            var ex = Assert.Throws<RemoteServiceException>(() => Acknowledgement.GetResultOrThrowIfFailed(serializer, ack));
            Assert.Contains("Service error message", ex.Message);
        }

        [Fact]
        public void GetResultOrThrowIfFailed_WithNullResult_ReturnsNull()
        {
            var serializer = CreateTestSerializer();
            var ack = new Acknowledgement(serializer, null, null);

            var result = Acknowledgement.GetResultOrThrowIfFailed(serializer, ack);

            Assert.Null(result);
        }

        [Fact]
        public void GetResultOrThrowIfFailed_WithStringResult_ReturnsResult()
        {
            var serializer = CreateTestSerializer();
            var expected = "Hello, World!";
            var ack = new Acknowledgement(serializer, expected, null);

            var result = Acknowledgement.GetResultOrThrowIfFailed(serializer, ack);

            Assert.NotNull(result);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetResultOrThrowIfFailed_WithIntResult_ReturnsResult()
        {
            var serializer = CreateTestSerializer();
            var expected = 42;
            var ack = new Acknowledgement(serializer, expected, null);

            var result = Acknowledgement.GetResultOrThrowIfFailed(serializer, ack);

            Assert.NotNull(result);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Constructor_WithExceptionTakesPriorityOverResult()
        {
            var serializer = CreateTestSerializer();
            var ex = new InvalidOperationException("Exception wins");
            var ack = new Acknowledgement(serializer, "some result", ex);

            Assert.NotNull(ack.Exception);
            Assert.Null(ack.Data);
            Assert.Null(ack.DataType);
        }
    }
}
