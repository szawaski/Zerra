// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET5_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Text;

namespace Zerra.CQRS.Relay
{
    internal static class TcpCommon
    {
        private static readonly string[] tcpRawHeaderPrefixes = ["RAW", "ERR"];
        private static readonly string[] httpHeaderPrefixes = ["HTTP/1.0", "HTTP/1.1", "HTTP/2.0", "GET", "POST", "PUT", "DELETE", "OPTIONS", "HEAD", "TRACE", "PATCH"];
        private const char protocolHeaderPrefixEnd = ' ';
        public const int MaxProtocolHeaderPrefixLength = 10;

        private static readonly Encoding encoding = Encoding.UTF8;

        private static readonly Dictionary<string, CQRSProtocolType> protocolLookup;
        static TcpCommon()
        {
            protocolLookup = new Dictionary<string, CQRSProtocolType>();

            foreach (var key in tcpRawHeaderPrefixes)
                protocolLookup.Add(key, CQRSProtocolType.TcpRaw);

            foreach (var key in httpHeaderPrefixes)
                protocolLookup.Add(key, CQRSProtocolType.Http);
        }

        public static unsafe CQRSProtocolType ReadProtocol(Span<byte> buffer)
        {
            var position = 0;
            var length = Math.Min(buffer.Length, MaxProtocolHeaderPrefixLength);
            fixed (byte* pBuffer = buffer)
            {
                for (; position < length; position++)
                {
                    var b = buffer[position];
                    if (b == protocolHeaderPrefixEnd || position > MaxProtocolHeaderPrefixLength)
                        break;
                }
            }
            var prefix = encoding.GetString(buffer.Slice(0, position));

            if (!protocolLookup.TryGetValue(prefix, out var protocol))
                throw new NotSupportedException("Unknown Protocol");

            return protocol;
        }
    }
}

#endif