// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET5_0_OR_GREATER

namespace Zerra.CQRS.Relay
{
    internal enum CQRSProtocolType : byte
    {
        TcpRaw = 0,
        Http = 1
    }
}

#endif