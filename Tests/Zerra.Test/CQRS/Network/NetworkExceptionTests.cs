// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.Network;

namespace Zerra.Test.CQRS.Network
{
    public class NetworkExceptionTests
    {
        [Fact]
        public void CqrsServiceException_CarriesTheRemoteDetails()
        {
            var ex = new CqrsServiceException("InvalidOperationException", "Failed", "server", "at Remote()");
            Assert.Equal("InvalidOperationException", ex.ErrorType);
            Assert.Equal("Failed", ex.Message);
            Assert.Equal("server", ex.Source);
            Assert.Equal("at Remote()", ex.StackTrace);
            Assert.StartsWith("server - InvalidOperationException - ", ex.ToString());
        }

        [Fact]
        public void CqrsServiceException_FromException_UsesTheBaseException()
        {
            InvalidOperationException thrown;
            try
            {
                throw new InvalidOperationException("Failed");
            }
            catch (InvalidOperationException caught)
            {
                thrown = caught;
            }

            var ex = new CqrsServiceException(new AggregateException(thrown), "IQuery.Get");
            Assert.Equal(nameof(InvalidOperationException), ex.ErrorType);
            Assert.Equal("Failed", ex.Message);
            Assert.Equal("IQuery.Get", ex.Source);
            Assert.Equal(thrown.StackTrace, ex.StackTrace);
        }

        [Fact]
        public void CqrsServiceException_FromRelayed_KeepsErrorTypeAndJoinsSource()
        {
            var relayed = new CqrsServiceException("KeyNotFoundException", "Missing", "IOther.Get", "at Remote()");

            var ex = new CqrsServiceException(relayed, "IQuery.Get");
            Assert.Equal("KeyNotFoundException", ex.ErrorType);
            Assert.Equal("Missing", ex.Message);
            Assert.Equal("IOther.Get - IQuery.Get", ex.Source);
            Assert.Equal("at Remote()", ex.StackTrace);
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
