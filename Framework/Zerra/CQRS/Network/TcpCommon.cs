using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Buffers;

namespace Zerra.CQRS.Network
{
    internal static class TcpCommon
    {
        public const int BufferLength = 1024 * 8; //Limits max header size

        //{prefix}|{contentType}~{int-size:body}{int-size:body}{int-size:body}...{0:null}
        //an upload body is the data in the same segments ended by {0}, then the stream bytes in segments to the body's {0}
        private const string protocolRawPrefix = "RAW";
        private const string protocolUploadPrefix = "UPL";
        private const string protocolErrorPrefix = "ERR";
        private const char headerSeperator = '|';
        private const char headerEnder = '~';

        private static readonly Encoding encoding = Encoding.UTF8;

        private static readonly byte[] protocolRawPrefixBytes = encoding.GetBytes($"{protocolRawPrefix}{headerSeperator}");
        private static readonly byte[] protocolUploadPrefixBytes = encoding.GetBytes($"{protocolUploadPrefix}{headerSeperator}");
        private static readonly byte[] protocolErrorPrefixBytes = encoding.GetBytes($"{protocolErrorPrefix}{headerSeperator}");
        private static readonly byte[] headerEnderBytes = encoding.GetBytes($"{headerEnder}");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryReadToHeaderEnd(ReadOnlySpan<byte> buffer, ref int position)
        {
            fixed (byte* pHeaderBuffer = buffer)
            {
                while (position < buffer.Length)
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
        public static unsafe TcpRequestHeader ReadHeader(ReadOnlyMemory<byte> buffer, int headerLength)
        {
#if NETSTANDARD2_0
            var chars = encoding.GetChars(buffer.Span.Slice(0, headerLength).ToArray());
            var charsLength = chars.Length;
#else
            var chars = ArrayPoolHelper<char>.Rent(encoding.GetMaxCharCount(headerLength));
            try
            {
                var charsLength = encoding.GetChars(buffer.Span.Slice(0, headerLength), chars.AsSpan());
#endif
                string? prefix = null;
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

                if (propertyIndex != 2 || !contentType.HasValue)
                    throw new CqrsNetworkException("Invalid Header");

                var isError = prefix == protocolErrorPrefix;
                var isUpload = prefix == protocolUploadPrefix;
                if (!isError && !isUpload && prefix != protocolRawPrefix)
                    throw new CqrsNetworkException("Invalid Header");

                return new TcpRequestHeader(buffer.Slice(headerLength), isError, isUpload, contentType.Value);
#if !NETSTANDARD2_0
            }
            finally
            {
                ArrayPoolHelper<char>.Return(chars);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferHeader(Memory<byte> buffer, ContentType contentType, bool isUpload = false)
        {
            var headerBuffer = new SpanWriter<byte>(buffer.Span);
            headerBuffer.Write(isUpload ? protocolUploadPrefixBytes : protocolRawPrefixBytes);
            _ = Utf8Formatter.TryFormat((int)contentType, headerBuffer.Remaining, out var contentTypeLength);
            headerBuffer.Advance(contentTypeLength);
            headerBuffer.Write(headerEnderBytes);
            return headerBuffer.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferErrorHeader(Memory<byte> buffer, ContentType contentType)
        {
            var headerBuffer = new SpanWriter<byte>(buffer.Span);

            headerBuffer.Write(protocolErrorPrefixBytes);
            _ = Utf8Formatter.TryFormat((int)contentType, headerBuffer.Remaining, out var contentTypeLength);
            headerBuffer.Advance(contentTypeLength);
            headerBuffer.Write(headerEnderBytes);

            return headerBuffer.Position;
        }
    }
}
