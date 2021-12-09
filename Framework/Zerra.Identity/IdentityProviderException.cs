// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Identity
{
    public class IdentityProviderException : Exception
    {
        public string DebugMessage { get; private set; }
        public IdentityProviderException(string message, string debugMessage = null) : base(message)
        {
            this.DebugMessage = debugMessage;
        }
    }
}
