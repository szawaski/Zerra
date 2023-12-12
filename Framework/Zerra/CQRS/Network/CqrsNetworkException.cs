// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    public class CqrsNetworkException : Exception
    {
        public CqrsNetworkException() : base("A network error occured") { }
        public CqrsNetworkException(string message) : base(message) { }
        public CqrsNetworkException(Exception innerException) : base("A network error occured") { }
        public CqrsNetworkException(string message, Exception innerException) : base("A network error occured", innerException) { }
    }
}
