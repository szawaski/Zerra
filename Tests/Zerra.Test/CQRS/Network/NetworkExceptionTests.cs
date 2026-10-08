// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class NetworkExceptionTests
    {
        [Fact]
        public void RemoteServiceException_CarriesTheRemoteDetails()
        {
            var remote = new RemoteServiceException("InvalidOperationException", "Failed", "server", "at Remote()");
            Assert.Equal("InvalidOperationException", remote.ErrorType);
            Assert.Equal("Failed", remote.Message);
            Assert.Equal("server", remote.Source);
            Assert.Equal("at Remote()", remote.StackTrace);
            Assert.StartsWith("server - InvalidOperationException - ", remote.ToString());

            var plain = new RemoteServiceException("server", "Failed");
            Assert.Null(plain.ErrorType);
            Assert.Equal("server", plain.Source);
            Assert.Null(plain.StackTrace);
            Assert.DoesNotContain(" - ", plain.ToString());
        }

        [Fact]
        public void NetworkExceptions_Messages()
        {
            var inner = new IOException("io");

            Assert.Equal("A network error occured", new CqrsNetworkException().Message);
            Assert.Equal("message", new CqrsNetworkException("message").Message);
            Assert.Same(inner, new CqrsNetworkException(inner).InnerException);
            Assert.Same(inner, new CqrsNetworkException("message", inner).InnerException);

            Assert.Equal("Failed to establish a connection", new ConnectionFailedException().Message);
            Assert.Same(inner, new ConnectionFailedException(inner).InnerException);
            Assert.Equal("message", new ConnectionFailedException("message", inner).Message);

            Assert.Equal("Connection aborted while processing request", new ConnectionAbortedException().Message);
            Assert.Equal("message", new ConnectionAbortedException("message").Message);
        }
    }
}
