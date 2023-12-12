// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    public sealed class ConnectionFailedException : CqrsNetworkException
    {
        public ConnectionFailedException() : base("Failed to establish a connection") { }
        public ConnectionFailedException(Exception innerException) : base("Failed to establish a connection", innerException) { }
        public ConnectionFailedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
