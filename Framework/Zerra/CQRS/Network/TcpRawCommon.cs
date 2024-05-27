using System;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    internal static class TcpRawCommon
    {
        public const int BufferLength = 1024 * 8; //Limits max header size

        //{prefix} {providerType} {contentType}~{int-size:body}{int-size:body}{int-size:body}...{0:null}
        private const string protocolRawPrefix = "RAW";
        private const string protocolErrorPrefix = "ERR";
        private const string nullProviderType = "*";
        private const char headerSeperator = ' ';
        private const char headerEnder = '~';

        private static readonly Encoding encoding = Encoding.UTF8;

        private static readonly byte[] protocolRawPrefixBytes = encoding.GetBytes($"{protocolRawPrefix}{headerSeperator}");
        private static readonly byte[] protocolErrorPrefixBytes = encoding.GetBytes($"{protocolErrorPrefix}{headerSeperator}");
        private static readonly byte[] nullProviderBytes = encoding.GetBytes($"{nullProviderType}{headerSeperator}");
        private static readonly byte[] headerSeperatorBytes = encoding.GetBytes($"{headerSeperator}");
        private static readonly byte[] headerEnderBytes = encoding.GetBytes($"{headerEnder}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool ReadToHeaderEnd(Memory<byte> buffer, ref int position, int length)
        {
            fixed (byte* pHeaderBuffer = buffer.Span)
            {
                while (position < length)
                {
                    if (pHeaderBuffer[position] == headerEnder)
                    {
                        position++;
                        return true;
                    }
                    position++;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TcpRequestHeader ReadHeader(ReadOnlyMemory<byte> buffer, int position, int length)
        {
#if NETSTANDARD2_0
            var chars = encoding.GetChars(buffer.Span.Slice(0, position).ToArray());
            var charsLength = chars.Length;
#else
            var chars = BufferArrayPool<char>.Rent(encoding.GetMaxCharCount(position));
            try
            {
                var charsLength = encoding.GetChars(buffer.Span.Slice(0, position), chars.AsSpan());
#endif
                string? prefix = null;
                string? providerType = null;
                ContentType? contentType = null;

                var propertyIndex = 0;
                var startIndex = 0;
                var indexLength = 0;
                fixed (char* pChars = chars)
                {
                    for (var index = 0; index < charsLength; index++)
                    {
                        var c = pChars[index];
                        switch (c)
                        {
                            case headerSeperator:
                            case headerEnder:
                                switch (propertyIndex)
                                {
                                    case 0:
                                        {
                                            prefix = new string(pChars, startIndex, indexLength);
                                            startIndex = index + 1;
                                            indexLength = 0;
                                        }
                                        break;
                                    case 1:
                                        {
                                            providerType = new string(pChars, startIndex, indexLength);
                                            startIndex = index + 1;
                                            indexLength = 0;
                                        }
                                        break;
                                    case 2:
                                        {
                                            var contentTypeString = new string(pChars, startIndex, indexLength);
                                            if (Int32.TryParse(contentTypeString, out var contentTypeValue))
                                                contentType = (ContentType)contentTypeValue;
                                            else
                                                throw new CqrsNetworkException("Invalid Header");

                                            startIndex = index + 1;
                                            indexLength = 0;
                                        }
                                        break;
                                }
                                propertyIndex++;
                                break;
                            default:
                                indexLength++;
                                break;
                        }
                    }
                }

                if (propertyIndex != 3 || !contentType.HasValue)
                    throw new CqrsNetworkException("Invalid Header");

                var isError = prefix == protocolErrorPrefix;

                if (providerType == nullProviderType)
                    providerType = null;

                return new TcpRequestHeader(buffer.Slice(position, length - position), isError, contentType.Value, providerType);
#if !NETSTANDARD2_0
            }
            finally
            {
                BufferArrayPool<char>.Return(chars);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferHeader(Memory<byte> buffer, string providerType, ContentType contentType)
        {
            var headerBuffer = new ByteBuilder(buffer.Span);
            headerBuffer.Write(protocolRawPrefixBytes);
            if (!String.IsNullOrWhiteSpace(providerType))
            {
                var providerTypeBytes = encoding.GetBytes(providerType);
                headerBuffer.Write(providerTypeBytes);
                headerBuffer.Write(headerSeperatorBytes);
            }
            else
            {
                headerBuffer.Write(nullProviderBytes);
            }

            var contentTypeBytes = encoding.GetBytes(((int)contentType).ToString());
            headerBuffer.Write(contentTypeBytes);
            headerBuffer.Write(headerEnderBytes);
            return headerBuffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferErrorHeader(Memory<byte> buffer, string? providerType, ContentType contentType)
        {
            var headerBuffer = new ByteBuilder(buffer.Span);

            headerBuffer.Write(protocolErrorPrefixBytes);
            if (!String.IsNullOrWhiteSpace(providerType))
            {
                var providerTypeBytes = encoding.GetBytes(providerType);
                headerBuffer.Write(providerTypeBytes);
                headerBuffer.Write(headerSeperatorBytes);
            }
            else
            {
                headerBuffer.Write(nullProviderBytes);
            }

            var contentTypeBytes = encoding.GetBytes(((int)contentType).ToString());
            headerBuffer.Write(contentTypeBytes);
            headerBuffer.Write(headerEnderBytes);

            return headerBuffer.Length;
        }
    }
}
