using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Buffers;

namespace Zerra.CQRS.Network
{
    internal static class HttpCommon
    {
        public const int BufferLength = 1024 * 16; //Limits max header size

        private const string postRequest = "POST ";
        private const string httpVersion = "HTTP/";
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

        private const string mediaTypeJson = "application/json";
        private const string mediaTypeJsonNameless = "application/jsonnameless";

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
        private static readonly byte[] contentLengthZeroBytes = encoding.GetBytes($"{ContentLengthHeader}{headerSplit}0");

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

        //a response line is "HTTP/1.1 <status> <reason>", any 4xx or 5xx is an error
        //the status alone is checked because the reason differs by server, Kestrel sends "Internal Server Error" where this sends "Server Error", and a proxy sends its own
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsErrorStatus(ReadOnlySpan<char> declarations, out ReadOnlySpan<char> status)
        {
            status = default;
            if (!declarations.StartsWith(httpVersion.AsSpan()))
                return false;
            var statusStart = declarations.IndexOf(' ') + 1;
            if (statusStart < 1 || statusStart == declarations.Length)
                return false;
            status = declarations.Slice(statusStart);
            return status[0] == '4' || status[0] == '5';
        }

        //known headers are read from the chars directly, the declarations string and headers dictionary are only built when asked for
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool ParseHeaders(ReadOnlySpan<char> chars, HttpRequestHeader headerInfo, bool parseAllHeaders)
        {
            var hasDeclarations = false;
            var headerCount = 0;
            var headers = parseAllHeaders ? new Dictionary<string, List<string?>>(StringComparer.OrdinalIgnoreCase) : null; //header names are case-insensitive

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
//                        case ' ':
//                            {
//#if NETSTANDARD2_0
//                                var value = new string(chars.Slice(start, length).ToArray());
//#else
//                                var value = chars.Slice(start, length).ToString();
//#endif
//                                declarations.Add(value);
//                                start = index + 1;
//                                length = 0;
//                                break;
//                            }
                        case '\r':
                        case '\n':
                            {
                                var declarations = chars.Slice(start, length);
                                headerInfo.IsError = IsErrorStatus(declarations, out var errorStatus);
                                if (headerInfo.IsError)
                                    headerInfo.ErrorStatus = errorStatus.ToString();
                                headerInfo.Preflight = declarations.StartsWith(OptionsHeader.AsSpan());
                                if (parseAllHeaders)
                                    headerInfo.Declarations = declarations.ToString();
                                hasDeclarations = true;
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

                var keyStart = 0;
                var keyLength = 0;
                var keyPartDone = false;
                for (var index = start; index < chars.Length; index++)
                {
                    var c = pChars[index];
                    switch (c)
                    {
                        case ':':
                            if (!keyPartDone)
                            {
                                keyStart = start;
                                keyLength = length;
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
                                var key = chars.Slice(keyStart, keyLength);
                                var value = chars.Slice(start, length);
                                //when all headers are kept the value string is shared with the known header
                                var valueString = headers is not null ? value.ToString() : null;
                                headerCount++;

                                //the first value of a known header is used
                                if (key.Equals(ContentTypeHeader.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!headerInfo.ContentType.HasValue)
                                    {
                                        //match the media type, parameters such as charset are optional
                                        var contentTypeParametersIndex = value.IndexOf(';');
                                        var mediaType = (contentTypeParametersIndex >= 0 ? value.Slice(0, contentTypeParametersIndex) : value).Trim();
                                        if (mediaType.Equals(ContentTypeBytes.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                            headerInfo.ContentType = ContentType.Bytes;
                                        else if (mediaType.Equals(mediaTypeJson.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                            headerInfo.ContentType = ContentType.Json;
                                        else if (mediaType.Equals(mediaTypeJsonNameless.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                            headerInfo.ContentType = ContentType.JsonNameless;
                                        else
                                            throw new CqrsNetworkException("Invalid Header");
                                    }
                                }
                                else if (key.Equals(ContentLengthHeader.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!headerInfo.ContentLength.HasValue && Int32.TryParse(value, out var contentLength))
                                        headerInfo.ContentLength = contentLength;
                                }
                                else if (key.Equals(TransferEncodingHeader.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                {
                                    //a list of codings that may span header lines, only chunked is read so any other coding would leave the body unreadable and the connection out of step
                                    var codings = value;
                                    while (codings.Length > 0)
                                    {
                                        var codingEnd = codings.IndexOf(',');
                                        var coding = (codingEnd >= 0 ? codings.Slice(0, codingEnd) : codings).Trim();
                                        codings = codingEnd >= 0 ? codings.Slice(codingEnd + 1) : default;
                                        if (coding.Length == 0)
                                            continue; //empty list elements are allowed
                                        if (headerInfo.Chuncked || !coding.Equals(TransferEncodingChunked.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                            throw new CqrsNetworkException($"Unsupported {TransferEncodingHeader}"); //chunked can only be applied once
                                        headerInfo.Chuncked = true;
                                    }
                                    if (!headerInfo.Chuncked)
                                        throw new CqrsNetworkException($"Unsupported {TransferEncodingHeader}");
                                }
                                else if (key.Equals(ProviderTypeHeader.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                {
                                    headerInfo.ProviderType ??= valueString ?? value.ToString();
                                }
                                else if (key.Equals(OriginHeader.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                {
                                    headerInfo.Origin ??= valueString ?? value.ToString();
                                }
                                else if (key.Equals(RelayServiceHeader.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!headerInfo.RelayServiceAddRemove.HasValue)
                                    {
                                        if (value.SequenceEqual(RelayServiceAdd.AsSpan()))
                                            headerInfo.RelayServiceAddRemove = true;
                                        else if (value.SequenceEqual(RelayServiceRemove.AsSpan()))
                                            headerInfo.RelayServiceAddRemove = false;
                                    }
                                }
                                else if (key.Equals(RelayKeyHeader.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                {
                                    headerInfo.RelayKey ??= valueString ?? value.ToString();
                                }

                                if (headers is not null)
                                {
                                    var keyString = key.ToString();
                                    if (headers.TryGetValue(keyString, out var values))
                                    {
                                        values.Add(valueString);
                                    }
                                    else
                                    {
                                        values = new List<string?>();
                                        values.Add(valueString);
                                        headers.Add(keyString, values);
                                    }
                                }
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
            headerInfo.Headers = headers;
            return hasDeclarations && headerCount > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HttpRequestHeader ReadHeader(ReadOnlyMemory<byte> buffer, int position, bool parseAllHeaders = false)
        {
#if NETSTANDARD2_0
            var chars = encoding.GetChars(buffer.Span.Slice(0, position).ToArray());
            var charsLength = chars.Length;
#else
            var chars = ArrayPoolHelper<char>.Rent(encoding.GetMaxCharCount(position));
            try
            {
                var charsLength = encoding.GetChars(buffer.Span[..position], chars.AsSpan());
#endif
                var headerInfo = new HttpRequestHeader();
                if (!ParseHeaders(chars.AsSpan()[..charsLength], headerInfo, parseAllHeaders))
                    throw new Exception("Invalid Header");

                headerInfo.BodyStartBuffer = buffer[position..];

                return headerInfo;
#if !NETSTANDARD2_0
            }
            finally
            {
                ArrayPoolHelper<char>.Return(chars);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryReadToHeaderEnd(ReadOnlySpan<byte> buffer, ref int position)
        {
            var headerEndSequence = 0;
            fixed (byte* pHeaderBuffer = buffer)
            {
                while (position < buffer.Length)
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
            position = Math.Max(0, position - 3); //back off minimum for reattempt
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool ReadToBreak(ReadOnlySpan<byte> buffer, ref int position)
        {
            var headerEndSequence = 0;
            fixed (byte* pHeaderBuffer = buffer)
            {
                while (position < buffer.Length)
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
            var headerBuffer = new SpanWriter<byte>(buffer.Span);

            headerBuffer.Write(okHeaderBytes);
            headerBuffer.Write(newLineBytes);

            if (!String.IsNullOrWhiteSpace(origin))
            {
                headerBuffer.Write(corsAllowOriginHeadersBytes);
                headerBuffer.Advance(encoding.GetBytes(origin, headerBuffer.Remaining));
                headerBuffer.Write(newLineBytes);
            }
            else
            {
                headerBuffer.Write(corsAllOriginsHeadersBytes);
                headerBuffer.Write(newLineBytes);
            }
            headerBuffer.Write(corsAllowHeadersBytes);
            headerBuffer.Write(newLineBytes);

            headerBuffer.Write(contentLengthZeroBytes);
            headerBuffer.Write(newLineBytes);

            headerBuffer.Write(newLineBytes);

            return headerBuffer.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferPostRequestHeader(Memory<byte> buffer, Uri serviceUrl, string? providerType, ContentType? contentType, Dictionary<string, List<string?>>? authHeaders)
        {
            var headerBuffer = new SpanWriter<byte>(buffer.Span);

            headerBuffer.Write(postRequestBytes);
            headerBuffer.Advance(encoding.GetBytes(serviceUrl.PathAndQuery, headerBuffer.Remaining)); //the escaped path, the host goes in the Host header
            headerBuffer.Write(requestEndingBytes);
            headerBuffer.Write(newLineBytes);

            if (!String.IsNullOrWhiteSpace(providerType))
            {
                headerBuffer.Write(providerTypeHeaderBytes);
                headerBuffer.Advance(encoding.GetBytes(providerType, headerBuffer.Remaining));
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

            if (authHeaders is not null)
            {
                foreach (var authHeader in authHeaders)
                {
                    foreach (var authHeaderValue in authHeader.Value)
                    {
                        if (authHeaderValue is null)
                            continue;
                        headerBuffer.Advance(encoding.GetBytes(authHeader.Key, headerBuffer.Remaining));
                        headerBuffer.Write(headerSplitBytes);
                        headerBuffer.Advance(encoding.GetBytes(authHeaderValue, headerBuffer.Remaining));
                        headerBuffer.Write(newLineBytes);
                    }
                }
            }

            //Access-Control-Allow headers are only for responses so a request doesn't send them

            headerBuffer.Write(transferEncodingChunckedBytes);
            headerBuffer.Write(newLineBytes);

            headerBuffer.Write(hostHeadersBytes);
            headerBuffer.Advance(encoding.GetBytes(serviceUrl.Authority, headerBuffer.Remaining));
            headerBuffer.Write(newLineBytes);

            headerBuffer.Write(corsOriginHeadersBytes);
            headerBuffer.Advance(encoding.GetBytes(serviceUrl.Host, headerBuffer.Remaining));
            headerBuffer.Write(newLineBytes);

            headerBuffer.Write(newLineBytes);
            return headerBuffer.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferErrorResponseHeader(Memory<byte> buffer, string? origin)
        {
            var headerBuffer = new SpanWriter<byte>(buffer.Span);

            headerBuffer.Write(serverErrorHeaderBytes);
            headerBuffer.Write(newLineBytes);

            if (!String.IsNullOrWhiteSpace(origin))
            {
                headerBuffer.Write(corsAllowOriginHeadersBytes);
                headerBuffer.Advance(encoding.GetBytes(origin, headerBuffer.Remaining));
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

            return headerBuffer.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferOkResponseHeader(Memory<byte> buffer)
        {
            var headerBuffer = new SpanWriter<byte>(buffer.Span);

            headerBuffer.Write(okHeaderBytes);
            headerBuffer.Write(newLineBytes);
            headerBuffer.Write(newLineBytes);

            return headerBuffer.Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferOkResponseHeader(Memory<byte> buffer, string? origion, string? providerType, ContentType? contentType, Dictionary<string, List<string?>>? authHeaders, bool hasBody = true)
        {
            var headerBuffer = new SpanWriter<byte>(buffer.Span);

            headerBuffer.Write(okHeaderBytes);
            headerBuffer.Write(newLineBytes);

            if (!String.IsNullOrWhiteSpace(providerType))
            {
                headerBuffer.Write(providerTypeHeaderBytes);
                headerBuffer.Advance(encoding.GetBytes(providerType, headerBuffer.Remaining));
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

            if (authHeaders is not null)
            {
                foreach (var authHeader in authHeaders)
                {
                    foreach (var authHeaderValue in authHeader.Value)
                    {
                        if (authHeaderValue is null)
                            continue;
                        headerBuffer.Advance(encoding.GetBytes(authHeader.Key, headerBuffer.Remaining));
                        headerBuffer.Write(headerSplitBytes);
                        headerBuffer.Advance(encoding.GetBytes(authHeaderValue, headerBuffer.Remaining));
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
                headerBuffer.Write(corsAllowOriginHeadersBytes);
                headerBuffer.Advance(encoding.GetBytes(origion, headerBuffer.Remaining));
                headerBuffer.Write(newLineBytes);
            }

            headerBuffer.Write(corsAllowHeadersBytes);
            headerBuffer.Write(newLineBytes);

            //a response without a body must not claim a chunked body
            headerBuffer.Write(hasBody ? transferEncodingChunckedBytes : contentLengthZeroBytes);
            headerBuffer.Write(newLineBytes);
            headerBuffer.Write(newLineBytes);

            return headerBuffer.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferNotFoundResponseHeader(Memory<byte> buffer)
        {
            var headerBuffer = new SpanWriter<byte>(buffer.Span);

            headerBuffer.Write(notFoundHeaderBytes);
            headerBuffer.Write(newLineBytes);
            headerBuffer.Write(newLineBytes);

            return headerBuffer.Position;
        }
    }
}
