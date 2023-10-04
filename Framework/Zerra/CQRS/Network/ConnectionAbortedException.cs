// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    public sealed class ConnectionAbortedException : Exception
    {
        public ConnectionAbortedException() : base("Connection aborted while processing request") { }
        public ConnectionAbortedException(string message) : base(message) { }
    }
}
