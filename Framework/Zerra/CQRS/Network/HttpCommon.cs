using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.IO;

namespace Zerra.CQRS.Network
{
    public static class HttpCommon
    {
        public const int BufferLength = 1024 * 8; //Limits max header size

        private const string postRequest = "POST ";
        private const string requestEnding = " HTTP/1.1";

        private const string okResponse = "HTTP/1.1 200 OK";
        private const string notFoundResponse = "HTTP/1.1 404 Not Found";
        private const string serverErrorResponse = "HTTP/1.1 500 Server Error";
        public const string OptionsHeader = "OPTIONS";

        public const string ContentLengthHeader = "Content-Length";
        public const string ContentTypeHeader = "Content-Type";
        public const string ProviderTypeHeader = "Provider-Type";
        public const string TransferEncodingHeader = "Transfer-Encoding";
        public const string OriginHeader = "Origin";
        public const string HostHeader = "Host";

        public const string RelayServiceRemove = "remove";

        public const string RelayServiceHeader = "Relay-Service";
        public const string RelayKeyHeader = "Relay-Key";
        public const string RelayServiceAdd = "add";
        public const string AccessControlAllowOriginHeader = "Access-Control-Allow-Origin";
        public const string AccessControlAllowMethodsHeader = "Access-Control-Allow-Methods";
        public const string AccessControlAllowHeadersHeader = "Access-Control-Allow-Headers";

        public const string ContentTypeBytes = "application/octet-stream";
        public const string ContentTypeJson = "application/json; charset=utf-8";
        public const string ContentTypeJsonNameless = "application/jsonnameless; charset=utf-8";
        public const string TransferEncodingChunked = "chunked";

        private const string headerSplit = ": ";
        private const string newLine = "\r\n";

        private static readonly Encoding encoding = Encoding.UTF8;

        private static readonly byte[] postRequestBytes = encoding.GetBytes(postRequest);
        private static readonly byte[] requestEndingBytes = encoding.GetBytes(requestEnding);
        private static readonly byte[] okHeaderBytes = encoding.GetBytes(okResponse);
        private static readonly byte[] notFoundHeaderBytes = encoding.GetBytes(notFoundResponse);
        private static readonly byte[] serverErrorHeaderBytes = encoding.GetBytes(serverErrorResponse);
        private static readonly byte[] transferEncodingChunckedBytes = encoding.GetBytes("Transfer-Encoding: chunked");
        private static readonly byte[] providerTypeHeaderBytes = encoding.GetBytes($"{ProviderTypeHeader}{headerSplit}");

        private static readonly byte[] contentTypeBytesHeaderBytes = encoding.GetBytes($"{ContentTypeHeader}{headerSplit}{ContentTypeBytes}");
        private static readonly byte[] contentTypeJsonHeaderBytes = encoding.GetBytes($"{ContentTypeHeader}{headerSplit}{ContentTypeJson}");
        private static readonly byte[] contentTypeJsonNamelessHeaderBytes = encoding.GetBytes($"{ContentTypeHeader}{headerSplit}{ContentTypeJsonNameless}");

        private static readonly byte[] headerSplitBytes = encoding.GetBytes(headerSplit);
        private static readonly byte[] newLineBytes = encoding.GetBytes(newLine);

        private static readonly byte[] corsOriginHeadersBytes = encoding.GetBytes($"{OriginHeader}: ");
        private static readonly byte[] corsAllowOriginHeadersBytes = encoding.GetBytes($"{AccessControlAllowOriginHeader}: ");
        private static readonly byte[] corsAllOriginsHeadersBytes = encoding.GetBytes($"{AccessControlAllowOriginHeader}: *");
        private static readonly byte[] corsAllowHeadersBytes = encoding.GetBytes($"{AccessControlAllowMethodsHeader}: *\r\n{AccessControlAllowHeadersHeader}: *");

        private static readonly byte[] hostHeadersBytes = encoding.GetBytes($"{HostHeader}: ");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe (IList<string>, IDictionary<string, IList<string?>>) ParseHeaders(ReadOnlySpan<char> chars)
        {
            var declarations = new List<string>();
            var headers = new Dictionary<string, IList<string?>>();

            var start = 0;
            var length = 0;

            var firstLineDone = false;
            fixed (char* pChars = chars)
            {
                for (var index = 0; index < chars.Length; index++)
                {
                    var c = pChars[index];
                    switch (c)
                    {
                        case ' ':
                            {
#if NETSTANDARD2_0
                                var value = new string(chars.Slice(start, length).ToArray());
#else
                                var value = chars.Slice(start, length).ToString();
#endif
                                declarations.Add(value);
                                start = index + 1;
                                length = 0;
                                break;
                            }
                        case '\r':
                        case '\n':
                            {
#if NETSTANDARD2_0
                                var value = new string(chars.Slice(start, length).ToArray());
#else
                                var value = chars.Slice(start, length).ToString();
#endif
                                declarations.Add(value);
                                start = index + 1;
                                length = 0;
                                firstLineDone = true;
                                break;
                            }
                        default:
                            length++;
                            break;
                    }
                    if (firstLineDone)
                        break;
                }

                string? key = null;
                var keyPartDone = false;
                for (var index = start; index < chars.Length; index++)
                {
                    var c = pChars[index];
                    switch (c)
                    {
                        case ':':
                            if (!keyPartDone)
                            {
#if NETSTANDARD2_0
                                key = new string(chars.Slice(start, length).ToArray());
#else
                                key = chars.Slice(start, length).ToString();
#endif
                                keyPartDone = true;
                                start = index + 1;
                                length = 0;
                            }
                            else
                            {
                                length++;
                            }
                            break;
                        case ' ':
                            if (keyPartDone && length == 0)
                            {
                                start = index + 1;
                            }
                            else
                            {
                                length++;
                            }
                            break;
                        case '\r':
                        case '\n':
                            if (keyPartDone)
                            {
#if NETSTANDARD2_0
                                var value = new string(chars.Slice(start, length).ToArray());
#else
                                var value = chars.Slice(start, length).ToString();
#endif
                                if (headers.TryGetValue(key!, out var values))
                                {
                                    values.Add(value);
                                }
                                else
                                {
                                    values = new List<string?>();
                                    values.Add(value);
                                    headers.Add(key!, values);
                                }
                                key = null;
                                keyPartDone = false;
                            }
                            start = index + 1;
                            length = 0;
                            break;
                        default:
                            length++;
                            break;
                    }
                }
            }
            return (declarations, headers);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HttpRequestHeader ReadHeader(ReadOnlyMemory<byte> buffer, int position, int length)
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
                (var declarations, var headers) = ParseHeaders(chars.AsSpan().Slice(0, charsLength));

                if (declarations.Count == 0 || headers.Count == 0)
                    throw new Exception("Invalid Header");

                var headerInfo = new HttpRequestHeader()
                {
                    Declarations = declarations,
                    Headers = headers
                };

                headerInfo.IsError = declarations[0].StartsWith(serverErrorResponse);

                if (headers.TryGetValue(ContentTypeHeader, out var contentTypeHeaderValue))
                {
                    if (String.Equals(contentTypeHeaderValue[0], ContentTypeBytes, StringComparison.InvariantCultureIgnoreCase))
                        headerInfo.ContentType = ContentType.Bytes;
                    else if (String.Equals(contentTypeHeaderValue[0], ContentTypeJson, StringComparison.InvariantCultureIgnoreCase))
                        headerInfo.ContentType = ContentType.Json;
                    else if (String.Equals(contentTypeHeaderValue[0], ContentTypeJsonNameless, StringComparison.InvariantCultureIgnoreCase))
                        headerInfo.ContentType = ContentType.JsonNameless;
                    else
                        throw new CqrsNetworkException("Invalid Header");
                }

                if (headers.TryGetValue(ContentLengthHeader, out var contentLengthHeaderValue))
                {
                    if (Int32.TryParse(contentLengthHeaderValue[0], out var contentLengthHeaderValueParsed))
                        headerInfo.ContentLength = contentLengthHeaderValueParsed;
                }

                if (headers.TryGetValue(TransferEncodingHeader, out var transferEncodingHeaderValue))
                {
                    if (transferEncodingHeaderValue[0] == TransferEncodingChunked)
                        headerInfo.Chuncked = true;
                }

                if (headers.TryGetValue(ProviderTypeHeader, out var providerTypeHeaderValue))
                    headerInfo.ProviderType = providerTypeHeaderValue[0];

                if (headers.TryGetValue(OriginHeader, out var originHeaderValue))
                    headerInfo.Origin = originHeaderValue[0];

                if (declarations.Count > 0 && declarations[0] == OptionsHeader)
                    headerInfo.Preflight = true;

                if (headers.TryGetValue(RelayServiceHeader, out var relayServiceHeaderValue))
                {
                    if (relayServiceHeaderValue[0] == RelayServiceAdd)
                        headerInfo.RelayServiceAddRemove = true;
                    else if (relayServiceHeaderValue[0] == RelayServiceRemove)
                        headerInfo.RelayServiceAddRemove = false;
                }

                if (headers.TryGetValue(RelayKeyHeader, out var relayKeyHeaderValue))
                    headerInfo.RelayKey = relayKeyHeaderValue[0];

                headerInfo.BodyStartBuffer = buffer.Slice(position, length - position);

                return headerInfo;
#if !NETSTANDARD2_0
            }
            finally
            {
                BufferArrayPool<char>.Return(chars);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool ReadToHeaderEnd(Memory<byte> buffer, ref int position, int length)
        {
            var headerEndSequence = 0;
            fixed (byte* pHeaderBuffer = buffer.Span)
            {
                while (position < length)
                {
                    var b = pHeaderBuffer[position];
                    if (b == (headerEndSequence % 2 == 0 ? '\r' : '\n'))
                        headerEndSequence++;
                    else
                        headerEndSequence = b == '\r' ? 1 : 0;

                    if (headerEndSequence == 4)
                    {
                        position++;
                        return true;
                    }
                    position++;
                }
            }
            position -= 3; //back off minimum for reattempt
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool ReadToBreak(Memory<byte> buffer, ref int position, int length)
        {
            var headerEndSequence = 0;
            fixed (byte* pHeaderBuffer = buffer.Span)
            {
                while (position < length)
                {
                    var b = pHeaderBuffer[position];
                    if (b == (headerEndSequence % 2 == 0 ? '\r' : '\n'))
                        headerEndSequence++;
                    else
                        headerEndSequence = b == '\r' ? 1 : 0;

                    if (headerEndSequence == 2)
                    {
                        position++;
                        return true;
                    }
                    position++;
                }
            }
            position -= 1; //back off minimum for reattempt
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferPreflightResponse(Memory<byte> buffer, string? origin)
        {
            var headerBuffer = new ByteBuilder(buffer.Span);

            headerBuffer.Write(okHeaderBytes);
            headerBuffer.Write(newLineBytes);

            if (!String.IsNullOrWhiteSpace(origin))
            {
                var allowOriginBytes = encoding.GetBytes(origin);
                headerBuffer.Write(corsAllowOriginHeadersBytes);
                headerBuffer.Write(allowOriginBytes);
                headerBuffer.Write(newLineBytes);
            }
            else
            {
                headerBuffer.Write(corsAllOriginsHeadersBytes);
            }
            headerBuffer.Write(corsAllowHeadersBytes);

            headerBuffer.Write(newLineBytes);

            return headerBuffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferPostRequestHeader(Memory<byte> buffer, Uri serviceUrl, string? origion, string? providerType, ContentType? contentType, IDictionary<string, IList<string?>>? authHeaders)
        {
            var headerBuffer = new ByteBuilder(buffer.Span);

            var destinationUrlBytes = encoding.GetBytes(serviceUrl.ToString());
            headerBuffer.Write(postRequestBytes);
            headerBuffer.Write(destinationUrlBytes);
            headerBuffer.Write(requestEndingBytes);
            headerBuffer.Write(newLineBytes);

            if (!String.IsNullOrWhiteSpace(providerType))
            {
                var providerTypeBytes = encoding.GetBytes(providerType);
                headerBuffer.Write(providerTypeHeaderBytes);
                headerBuffer.Write(providerTypeBytes);
                headerBuffer.Write(newLineBytes);
            }

            if (contentType.HasValue)
            {
                switch (contentType.Value)
                {
                    case ContentType.Bytes:
                        headerBuffer.Write(contentTypeBytesHeaderBytes);
                        break;
                    case ContentType.Json:
                        headerBuffer.Write(contentTypeJsonHeaderBytes);
                        break;
                    case ContentType.JsonNameless:
                        headerBuffer.Write(contentTypeJsonNamelessHeaderBytes);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                headerBuffer.Write(newLineBytes);
            }

            if (authHeaders != null)
            {
                foreach (var authHeader in authHeaders)
                {
                    foreach (var authHeaderValue in authHeader.Value)
                    {
                        if (authHeaderValue == null)
                            continue;
                        headerBuffer.Write(encoding.GetBytes(authHeader.Key));
                        headerBuffer.Write(headerSplitBytes);
                        headerBuffer.Write(encoding.GetBytes(authHeaderValue));
                        headerBuffer.Write(newLineBytes);
                    }
                }
            }

            if (String.IsNullOrWhiteSpace(origion))
            {
                headerBuffer.Write(corsAllOriginsHeadersBytes);
                headerBuffer.Write(newLineBytes);
            }
            else
            {
                var allowOriginBytes = encoding.GetBytes(origion);
                headerBuffer.Write(corsAllowOriginHeadersBytes);
                headerBuffer.Write(allowOriginBytes);
                headerBuffer.Write(newLineBytes);
            }

            headerBuffer.Write(corsAllowHeadersBytes);
            headerBuffer.Write(newLineBytes);

            headerBuffer.Write(transferEncodingChunckedBytes);
            headerBuffer.Write(newLineBytes);

            var hostBytes = encoding.GetBytes(serviceUrl.Authority);
            headerBuffer.Write(hostHeadersBytes);
            headerBuffer.Write(hostBytes);
            headerBuffer.Write(newLineBytes);

            var originBytes = encoding.GetBytes(serviceUrl.Host);
            headerBuffer.Write(corsOriginHeadersBytes);
            headerBuffer.Write(originBytes);
            headerBuffer.Write(newLineBytes);

            headerBuffer.Write(newLineBytes);
            return headerBuffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferErrorResponseHeader(Memory<byte> buffer, string? origin)
        {
            var headerBuffer = new ByteBuilder(buffer.Span);

            headerBuffer.Write(serverErrorHeaderBytes);
            headerBuffer.Write(newLineBytes);

            if (!String.IsNullOrWhiteSpace(origin))
            {
                var allowOriginBytes = encoding.GetBytes(origin);
                headerBuffer.Write(corsAllowOriginHeadersBytes);
                headerBuffer.Write(allowOriginBytes);
                headerBuffer.Write(newLineBytes);
            }
            else
            {
                headerBuffer.Write(corsAllOriginsHeadersBytes);
                headerBuffer.Write(newLineBytes);
            }
            headerBuffer.Write(corsAllowHeadersBytes);
            headerBuffer.Write(newLineBytes);

            headerBuffer.Write(transferEncodingChunckedBytes);
            headerBuffer.Write(newLineBytes);
            headerBuffer.Write(newLineBytes);

            return headerBuffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferOkResponseHeader(Memory<byte> buffer)
        {
            var headerBuffer = new ByteBuilder(buffer.Span);

            headerBuffer.Write(okHeaderBytes);
            headerBuffer.Write(newLineBytes);
            headerBuffer.Write(newLineBytes);

            return headerBuffer.Length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferOkResponseHeader(Memory<byte> buffer, string? origion, string? providerType, ContentType? contentType, IDictionary<string, IList<string>>? authHeaders)
        {
            var headerBuffer = new ByteBuilder(buffer.Span);

            headerBuffer.Write(okHeaderBytes);
            headerBuffer.Write(newLineBytes);

            if (!String.IsNullOrWhiteSpace(providerType))
            {
                var providerTypeBytes = encoding.GetBytes(providerType);
                headerBuffer.Write(providerTypeHeaderBytes);
                headerBuffer.Write(providerTypeBytes);
                headerBuffer.Write(newLineBytes);
            }

            if (contentType.HasValue)
            {
                switch (contentType.Value)
                {
                    case ContentType.Bytes:
                        headerBuffer.Write(contentTypeBytesHeaderBytes);
                        break;
                    case ContentType.Json:
                        headerBuffer.Write(contentTypeJsonHeaderBytes);
                        break;
                    case ContentType.JsonNameless:
                        headerBuffer.Write(contentTypeJsonNamelessHeaderBytes);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                headerBuffer.Write(newLineBytes);
            }

            if (authHeaders != null)
            {
                foreach (var authHeader in authHeaders)
                {
                    foreach (var authHeaderValue in authHeader.Value)
                    {
                        headerBuffer.Write(encoding.GetBytes(authHeader.Key));
                        headerBuffer.Write(headerSplitBytes);
                        headerBuffer.Write(encoding.GetBytes(authHeaderValue));
                        headerBuffer.Write(newLineBytes);
                    }
                }
            }

            if (String.IsNullOrWhiteSpace(origion))
            {
                headerBuffer.Write(corsAllOriginsHeadersBytes);
                headerBuffer.Write(newLineBytes);
            }
            else
            {
                var allowOriginBytes = encoding.GetBytes(origion);
                headerBuffer.Write(corsAllowOriginHeadersBytes);
                headerBuffer.Write(allowOriginBytes);
                headerBuffer.Write(newLineBytes);
            }

            headerBuffer.Write(corsAllowHeadersBytes);
            headerBuffer.Write(newLineBytes);

            headerBuffer.Write(transferEncodingChunckedBytes);
            headerBuffer.Write(newLineBytes);
            headerBuffer.Write(newLineBytes);

            return headerBuffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferNotFoundResponseHeader(Memory<byte> buffer)
        {
            var headerBuffer = new ByteBuilder(buffer.Span);

            headerBuffer.Write(notFoundHeaderBytes);
            headerBuffer.Write(newLineBytes);
            headerBuffer.Write(newLineBytes);

            return headerBuffer.Length;
        }
    }
}
