// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Zerra.Buffers;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.IO
{
    public ref partial struct JsonReader
    {
        //https://www.rfc-editor.org/rfc/rfc4627

        //last byte 128 to 191
        //1 bytes: 0 to 127
        //2 bytes: 192 to ?
        //3 bytes: 224 to ?
        //4 bytes: 240 to ?

        private const byte openBracketByte = (byte)'[';
        private const byte closeBracketByte = (byte)']';
        private const byte openBraceByte = (byte)'{';
        private const byte closeBraceByte = (byte)'}';
        private const byte commaByte = (byte)',';
        private const byte colonByte = (byte)':';
        private const byte quoteByte = (byte)'"';
        private const byte escapeByte = (byte)'\\';

        private static readonly byte[] ullBytes = [(byte)'u', (byte)'l', (byte)'l'];
        private static readonly byte[] rueBytes = [(byte)'r', (byte)'u', (byte)'e'];
        private static readonly byte[] alseBytes = [(byte)'a', (byte)'l', (byte)'s', (byte)'e'];

        private static readonly char[] ullChars = ['u', 'l', 'l'];
        private static readonly char[] rueChars = ['r', 'u', 'e'];
        private static readonly char[] alseChars = ['a', 'l', 's', 'e'];

        private static readonly SearchValues<byte> quoteEscapeBytes = SearchValues.Create((byte)'"', (byte)'\\');
        private static readonly SearchValues<char> quoteEscapeChars = SearchValues.Create('"', '\\');

        private const byte uByte = (byte)'u';
        private const byte bByte = (byte)'b';
        private const byte tByte = (byte)'t';
        private const byte nByte = (byte)'n';
        private const byte fByte = (byte)'f';
        private const byte rByte = (byte)'r';

        //Numbers
        private static readonly char[] numberChars = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '-', '.', 'e', 'E'];
        private static readonly byte[] numberBytes = [(byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'+', (byte)'-', (byte)'.', (byte)'e', (byte)'E'];
        private const byte eByte = (byte)'e';
        private const byte eUpperByte = (byte)'E';

        private const byte plusByte = (byte)'+'; //43
        private const byte minusByte = (byte)'-'; //45
        private const byte dotByte = (byte)'.'; //46
        private const byte zeroByte = (byte)'0'; //48
        private const byte nineByte = (byte)'9'; //57

        //JSON whitespace
        private const byte spaceByte = (byte)' '; //32
        private const byte tabByte = (byte)'\t'; //9
        private const byte returnByte = (byte)'\r'; //13
        private const byte newlineByte = (byte)'\n'; //10
        private static readonly byte[] whiteSpaceBytes = [(byte)' ', (byte)'\t', (byte)'\r', (byte)'\n'];
        private static readonly char[] whiteSpaceChars = [' ', '\t', '\r', '\n'];

#if DEBUG
        public static bool Testing = false;

        public bool Alternate = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DebugShouldReturn()
        {
            if (!JsonReader.Testing)
                return false;
            if (Alternate)
            {
                Alternate = false;
                return false;
            }
            else
            {
                Alternate = true;
                return true;
            }
        }
#endif

        public JsonToken Token;
        public int StringPositionOfFirstEscape = -1;
        public ReadOnlySpan<byte> StringBytes;
        public ReadOnlySpan<char> StringChars;

        public unsafe bool TryReadToken(out int sizeNeeded)
        {
#if DEBUG
            if (DebugShouldReturn())
            {
                sizeNeeded = 1;
                return false;
            }
#endif
            sizeNeeded = 0;

            if (length - position < 1)
            {
                sizeNeeded = 1;
                return false;
            }


            var originalPosition = position;

            if (useBytes)
            {
                var localBufferBytes = bufferBytes;
                fixed (byte* pBuffer = localBufferBytes)
                {
                    var b = pBuffer[position];

                    if (b <= spaceByte)
                    {
                        if (b == spaceByte || b == tabByte || b == returnByte || b == newlineByte)
                        {
                            var lengthToNonWhiteSpace = localBufferBytes.Slice(position).IndexOfAnyExcept(whiteSpaceBytes);
                            if (lengthToNonWhiteSpace == -1)
                            {
                                sizeNeeded = 1;
                                return false;
                            }
                            position += lengthToNonWhiteSpace;
                            if (length - position < 1)
                            {
                                sizeNeeded = 1;
                                return false;
                            }
                            b = pBuffer[position];
                        }
                    }

                    switch (b)
                    {
                        case openBraceByte:
                            position++;
                            Token = JsonToken.ObjectStart;
                            return true;
                        case closeBraceByte:
                            position++;
                            Token = JsonToken.ObjectEnd;
                            return true;
                        case openBracketByte:
                            position++;
                            Token = JsonToken.ArrayStart;
                            return true;
                        case closeBracketByte:
                            position++;
                            Token = JsonToken.ArrayEnd;
                            return true;
                        case commaByte:
                            position++;
                            Token = JsonToken.NextItem;
                            return true;
                        case colonByte:
                            position++;
                            Token = JsonToken.PropertySeperator;
                            return true;

                        case nByte:
                            {
                                if (length - position < 4)
                                {
                                    sizeNeeded = 4;
                                    return false;
                                }
                                position++;
                                var valid = localBufferBytes.Slice(position, 3).SequenceEqual(ullBytes);
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                                position += 3;
                            }
                            Token = JsonToken.Null;
                            return true;

                        case tByte:
                            {
                                if (length - position < 4)
                                {
                                    sizeNeeded = 4;
                                    return false;
                                }
                                position++;
                                var valid = localBufferBytes.Slice(position, 3).SequenceEqual(rueBytes);
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                                position += 3;
                            }
                            Token = JsonToken.True;
                            return true;

                        case fByte:
                            {
                                if (length - position < 5)
                                {
                                    sizeNeeded = 5;
                                    return false;
                                }
                                position++;
                                var valid = bufferBytes.Slice(position, 4).SequenceEqual(alseBytes);
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                                position += 4;
                            }
                            Token = JsonToken.False;
                            return true;

                        case quoteByte:
                            Token = JsonToken.String;

                            {
                                position++;
                                var startPosition = position;
                                if (length - position < 1)
                                {
                                    position = originalPosition;
                                    sizeNeeded = 1;
                                    return false;
                                }

                                StringPositionOfFirstEscape = -1;

                                var stringLength = localBufferBytes.Slice(position).IndexOfAny(quoteEscapeBytes);
                                if (stringLength == -1)
                                {
                                    position = originalPosition;
                                    sizeNeeded = 1;
                                    return false;
                                }
                                position += stringLength;

                                if (localBufferBytes[position] == quoteByte)
                                {
                                    if (position - startPosition == 0)
                                    {
                                        StringBytes = Span<byte>.Empty;
                                        position++;
                                        return true;
                                    }
                                    StringBytes = localBufferBytes.Slice(startPosition, position - startPosition);
                                    position++;
                                    return true;
                                }
                                else
                                {
                                    if (length - position < 2)
                                    {
                                        position = originalPosition;
                                        sizeNeeded = 1;
                                        return false;
                                    }
                                    StringPositionOfFirstEscape = position - startPosition;
                                    position++;
                                    if (localBufferBytes[position] == uByte)
                                    {
                                        if (length - position < 5)
                                        {
                                            position = originalPosition;
                                            sizeNeeded = 1;
                                            return false;
                                        }
                                        position += 5;
                                    }
                                    else
                                    {
                                        position++;
                                    }
                                }

                                for (; ; )
                                {
                                    stringLength = localBufferBytes.Slice(position).IndexOfAny(quoteEscapeBytes);
                                    if (stringLength == -1)
                                    {
                                        position = originalPosition;
                                        sizeNeeded = 1;
                                        return false;
                                    }
                                    position += stringLength;

                                    if (localBufferBytes[position] == quoteByte)
                                    {
                                        StringBytes = localBufferBytes.Slice(startPosition, position - startPosition);
                                        position++;
                                        return true;
                                    }
                                    else
                                    {
                                        if (length - position < 2)
                                        {
                                            position = originalPosition;
                                            sizeNeeded = 1;
                                            return false;
                                        }
                                        position++;
                                        if (localBufferBytes[position] == uByte)
                                        {
                                            if (length - position < 5)
                                            {
                                                position = originalPosition;
                                                sizeNeeded = 1;
                                                return false;
                                            }
                                            position += 5;
                                        }
                                        else
                                        {
                                            position++;
                                        }
                                    }
                                }
                            }

                        case byte b2 when ((b2 >= zeroByte && b2 <= nineByte) || b2 == minusByte):
                            Token = JsonToken.Number;
                            {
                                var startPosition = position;

                                var numberLength = localBufferBytes.Slice(position).IndexOfAnyExcept(numberBytes);
                                if (numberLength == -1)
                                {
                                    if (!isFinalBlock)
                                    {
                                        position = originalPosition;
                                        sizeNeeded = 1;
                                        return false;
                                    }
                                    position += localBufferBytes.Length - position;
                                }
                                else
                                {
                                    position += numberLength;
                                }

                                StringBytes = localBufferBytes.Slice(startPosition, position - startPosition);
                                return true;
                            }

                        default:
                            throw CreateException("Invalid JSON value");
                    }
                }
            }
            else
            {
                var localBufferChars = bufferChars;
                fixed (char* pBuffer = localBufferChars)
                {
                    var c = pBuffer[position];

                    if (c <= ' ')
                    {
                        if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                        {
                            var lengthToNonWhiteSpace = localBufferChars.Slice(position).IndexOfAnyExcept(whiteSpaceChars);
                            if (lengthToNonWhiteSpace == -1)
                            {
                                sizeNeeded = 1;
                                return false;
                            }
                            position += lengthToNonWhiteSpace;
                            if (length - position < 1)
                            {
                                sizeNeeded = 1;
                                return false;
                            }
                            c = pBuffer[position];
                        }
                    }

                    switch (c)
                    {
                        case '{':
                            position++;
                            Token = JsonToken.ObjectStart;
                            return true;
                        case '}':
                            position++;
                            Token = JsonToken.ObjectEnd;
                            return true;
                        case '[':
                            position++;
                            Token = JsonToken.ArrayStart;
                            return true;
                        case ']':
                            position++;
                            Token = JsonToken.ArrayEnd;
                            return true;
                        case ',':
                            position++;
                            Token = JsonToken.NextItem;
                            return true;
                        case ':':
                            position++;
                            Token = JsonToken.PropertySeperator;
                            return true;

                        case 'n':
                            {
                                if (length - position < 4)
                                {
                                    sizeNeeded = 4;
                                    return false;
                                }
                                position++;
                                var valid = localBufferChars.Slice(position, 3).SequenceEqual(ullChars);
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                                position += 3;
                            }
                            Token = JsonToken.Null;
                            return true;

                        case 't':
                            {
                                if (length - position < 4)
                                {
                                    sizeNeeded = 4;
                                    return false;
                                }
                                position++;
                                var valid = localBufferChars.Slice(position, 3).SequenceEqual(rueChars);
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                                position += 3;
                            }
                            Token = JsonToken.True;
                            return true;

                        case 'f':
                            {
                                if (length - position < 5)
                                {
                                    sizeNeeded = 5;
                                    return false;
                                }
                                position++;
                                var valid = localBufferChars.Slice(position, 4).SequenceEqual(alseChars);
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                                position += 4;
                            }
                            Token = JsonToken.False;
                            return true;

                        case '"':
                            Token = JsonToken.String;

                            {
                                position++;
                                var startPosition = position;
                                if (length - position < 1)
                                {
                                    position = originalPosition;
                                    sizeNeeded = 1;
                                    return false;
                                }

                                StringPositionOfFirstEscape = -1;

                                var stringLength = localBufferChars.Slice(position).IndexOfAny(quoteEscapeChars);
                                if (stringLength == -1)
                                {
                                    position = originalPosition;
                                    sizeNeeded = 1;
                                    return false;
                                }
                                position += stringLength;

                                if (localBufferChars[position] == '"')
                                {
                                    if (position - startPosition == 0)
                                    {
                                        StringChars = Span<char>.Empty;
                                        position++;
                                        return true;
                                    }
                                    StringChars = localBufferChars.Slice(startPosition, position - startPosition);
                                    position++;
                                    return true;
                                }
                                else
                                {
                                    if (length - position < 2)
                                    {
                                        position = originalPosition;
                                        sizeNeeded = 1;
                                        return false;
                                    }
                                    StringPositionOfFirstEscape = position - startPosition;
                                    position++;
                                    if (localBufferChars[position] == 'u')
                                    {
                                        if (length - position < 5)
                                        {
                                            position = originalPosition;
                                            sizeNeeded = 1;
                                            return false;
                                        }
                                        position += 5;
                                    }
                                    else
                                    {
                                        position++;
                                    }
                                }

                                for (; ; )
                                {
                                    stringLength = localBufferChars.Slice(position).IndexOfAny(quoteEscapeChars);
                                    if (stringLength == -1)
                                    {
                                        position = originalPosition;
                                        sizeNeeded = 1;
                                        return false;
                                    }
                                    position += stringLength;

                                    if (localBufferChars[position] == '"')
                                    {
                                        StringChars = localBufferChars.Slice(startPosition, position - startPosition);
                                        position++;
                                        return true;
                                    }
                                    else
                                    {
                                        if (length - position < 2)
                                        {
                                            position = originalPosition;
                                            sizeNeeded = 1;
                                            return false;
                                        }
                                        position++;
                                        if (localBufferChars[position] == 'u')
                                        {
                                            if (length - position < 5)
                                            {
                                                position = originalPosition;
                                                sizeNeeded = 1;
                                                return false;
                                            }
                                            position += 5;
                                        }
                                        else
                                        {
                                            position++;
                                        }
                                    }
                                }
                            }

                        case char c2 when ((c2 >= '0' && c2 <= '9') || c2 == '-'):
                            Token = JsonToken.Number;
                            {
                                var startPosition = position;

                                var numberLength = localBufferChars.Slice(position).IndexOfAnyExcept(numberChars);
                                if (numberLength == -1)
                                {
                                    if (!isFinalBlock)
                                    {
                                        position = originalPosition;
                                        sizeNeeded = 1;
                                        return false;
                                    }
                                    position += localBufferChars.Length - position;
                                }
                                else
                                {
                                    position += numberLength;
                                }

                                StringChars = localBufferChars.Slice(startPosition, position - startPosition);
                                return true;
                            }

                        default:
                            throw CreateException("Invalid JSON value");
                    }
                }
            }
        }

        public unsafe string UnescapeStringBytes()
        {
            if (StringBytes.Length == 0)
                return String.Empty;

            fixed (byte* pBuffer = StringBytes)
            {
                if (StringPositionOfFirstEscape == -1)
                    return encoding.GetString(pBuffer, StringBytes.Length);

                var localStringBytes = StringBytes;
                var length = localStringBytes.Length;

                var position = StringPositionOfFirstEscape;
                var escapeStart = position;

                var maxSize = encoding.GetMaxCharCount(length);
                char[]? escapeBufferOwner;
                scoped Span<char> escapeBuffer;
                if (maxSize > 128)
                {
                    escapeBufferOwner = ArrayPoolHelper<char>.Rent(maxSize);
                    escapeBuffer = escapeBufferOwner;
                }
                else
                {
                    escapeBufferOwner = null;
                    escapeBuffer = stackalloc char[maxSize];
                }

                var bufferIndex = 0;
                fixed (char* pEscapeBuffer = escapeBuffer)
                {
                    var start = 0;

                unescape:

                    if (position > start)
                    {
                        bufferIndex += encoding.GetChars(&pBuffer[start], position - start, &pEscapeBuffer[bufferIndex], maxSize - bufferIndex);
                    }

                    var b = pBuffer[++position];

                    switch (b)
                    {
                        case bByte:
                            pEscapeBuffer[bufferIndex++] = '\b';
                            start = position++ + 1;
                            break;
                        case tByte:
                            pEscapeBuffer[bufferIndex++] = '\t';
                            start = position++ + 1;
                            break;
                        case nByte:
                            pEscapeBuffer[bufferIndex++] = '\n';
                            start = position++ + 1;
                            break;
                        case fByte:
                            pEscapeBuffer[bufferIndex++] = '\f';
                            start = position++ + 1;
                            break;
                        case rByte:
                            pEscapeBuffer[bufferIndex++] = '\r';
                            start = position++ + 1;
                            break;
                        case uByte:
                            var key = Unsafe.ReadUnaligned<uint>(&pBuffer[++position]);
                            if (!StringHelper.LowUnicodeByteHexToChar.TryGetValue(key, out var unicodeChar))
                                throw CreateException("Invalid escape sequence");
                            position += 3;
                            pEscapeBuffer[bufferIndex++] = unicodeChar;
                            start = position + 1;
                            break;
                        default:
                            if (b > 128)
                                throw CreateException("Invalid escape sequence");
                            pEscapeBuffer[bufferIndex++] = (char)b;
                            start = position++ + 1;
                            break;
                    }

                    var next = localStringBytes.Slice(position).IndexOf(escapeByte);
                    if (next != -1)
                    {
                        position += next;
                        goto unescape;
                    }

                    if (start < length)
                    {
                        bufferIndex += encoding.GetChars(&pBuffer[start], length - start, &pEscapeBuffer[bufferIndex], maxSize - bufferIndex);
                    }
                }

                var result = escapeBuffer.Slice(0, bufferIndex).ToString();

                if (escapeBufferOwner is not null)
                    ArrayPoolHelper<char>.Return(escapeBufferOwner);

                return result;
            }
        }

        public unsafe string UnescapeStringChars()
        {
            if (StringChars.Length == 0)
                return String.Empty;
            if (StringPositionOfFirstEscape == -1)
                return StringChars.ToString();

            var localStringChars = StringChars;
            var length = localStringChars.Length;

            fixed (char* pBuffer = localStringChars)
            {
                var position = StringPositionOfFirstEscape;
                var escapeStart = position;

                var maxSize = localStringChars.Length;
                char[]? escapeBufferOwner;
                scoped Span<char> escapeBuffer;
                if (maxSize > 128)
                {
                    escapeBufferOwner = ArrayPoolHelper<char>.Rent(maxSize);
                    escapeBuffer = escapeBufferOwner;
                }
                else
                {
                    escapeBufferOwner = null;
                    escapeBuffer = stackalloc char[maxSize];
                }

                var bufferIndex = 0;
                fixed (char* pEscapeBuffer = escapeBuffer)
                {
                    var start = 0;

                unescape:

                    if (position > start)
                    {
                        Buffer.MemoryCopy(&pBuffer[start], &pEscapeBuffer[bufferIndex], (maxSize - bufferIndex) * 2, (position - start) * 2);
                        bufferIndex += position - start;
                    }

                    var c = pBuffer[++position];

                    switch (c)
                    {
                        case 'b':
                            pEscapeBuffer[bufferIndex++] = '\b';
                            start = position++ + 1;
                            break;
                        case 't':
                            pEscapeBuffer[bufferIndex++] = '\t';
                            start = position++ + 1;
                            break;
                        case 'n':
                            pEscapeBuffer[bufferIndex++] = '\n';
                            start = position++ + 1;
                            break;
                        case 'f':
                            pEscapeBuffer[bufferIndex++] = '\f';
                            start = position++ + 1;
                            break;
                        case 'r':
                            pEscapeBuffer[bufferIndex++] = '\r';
                            start = position++ + 1;
                            break;
                        case 'u':
                            var key = Unsafe.ReadUnaligned<ulong>(&pBuffer[++position]);
                            if (!StringHelper.LowUnicodeCharHexToChar.TryGetValue(key, out var unicodeChar))
                                throw CreateException("Invalid escape sequence");
                            position += 3;
                            pEscapeBuffer[bufferIndex++] = unicodeChar;
                            start = position + 1;

                            if (start < length)
                            {
                                Buffer.MemoryCopy(&pBuffer[start], &pEscapeBuffer[bufferIndex], (maxSize - bufferIndex) * 2, (length - start) * 2);
                                bufferIndex += length - start;
                            }

                            break;
                        default:
                            if (c > 128)
                                throw CreateException("Invalid escape sequence");
                            pEscapeBuffer[bufferIndex++] = c;
                            start = position++ + 1;
                            break;
                    }

                    var next = localStringChars.Slice(position).IndexOf('\\');
                    if (next != -1)
                    {
                        position += next;
                        goto unescape;
                    }

                    if (start < length)
                    {
                        Buffer.MemoryCopy(&pBuffer[start], &pEscapeBuffer[bufferIndex], (maxSize - bufferIndex) * 2, (length - start) * 2);
                        bufferIndex += length - start;
                    }
                }

                var result = escapeBuffer.Slice(0, bufferIndex).ToString();

                if (escapeBufferOwner is not null)
                    ArrayPoolHelper<char>.Return(escapeBufferOwner);

                return result;
            }
        }

        public unsafe bool TryPeakArrayLength(out int length)
        {
            var openBrackets = 1;
            var openBraces = 0;
            var quoted = false;
            var escaping = false;
            length = 0;

            if (Token == JsonToken.ObjectStart)
                openBraces++;
            else if (Token == JsonToken.ArrayStart)
                openBrackets++;

            if (useBytes)
            {
                fixed (byte* ptr = bufferBytes.Slice(position))
                {
                    byte* ptr2 = ptr;
                    for (var i = position; i < bufferBytes.Length; i++)
                    {
                        var b = *ptr2++;
                        switch (b)
                        {
                            case commaByte:
                                if (!quoted && openBrackets == 1 && openBraces == 0)
                                    length++;
                                if (escaping)
                                    escaping = false;
                                continue;
                            case openBracketByte:
                                if (!quoted)
                                    openBrackets++;
                                else if (escaping)
                                    escaping = false;
                                continue;
                            case closeBracketByte:
                                if (!quoted)
                                {
                                    if (--openBrackets == 0)
                                    {
                                        if (i != position)
                                            length++;
                                        return true;
                                    }
                                }
                                else if (escaping)
                                {
                                    escaping = false;
                                }
                                continue;
                            case openBraceByte:
                                if (!quoted)
                                    openBraces++;
                                else if (escaping)
                                    escaping = false;
                                continue;
                            case closeBraceByte:
                                if (!quoted)
                                    openBraces--;
                                else if (escaping)
                                    escaping = false;
                                continue;
                            case quoteByte:
                                if (!quoted)
                                    quoted = true;
                                else if (escaping)
                                    escaping = false;
                                else
                                    quoted = false;
                                continue;
                            case escapeByte:
                                if (quoted)
                                {
                                    if (escaping)
                                        escaping = false;
                                    else
                                        escaping = true;
                                }
                                continue;
                            default:
                                if (escaping)
                                    escaping = false;
                                continue;
                        }
                    }
                }
                return false;
            }
            else
            {
                fixed (char* ptr = bufferChars.Slice(position))
                {
                    char* ptr2 = ptr;
                    for (var i = position; i < bufferChars.Length; i++)
                    {
                        var c = *ptr2++;
                        switch (c)
                        {
                            case ',':
                                if (!quoted && openBrackets == 1 && openBraces == 0)
                                    length++;
                                if (escaping)
                                    escaping = false;
                                continue;
                            case '[':
                                if (!quoted)
                                    openBrackets++;
                                else if (escaping)
                                    escaping = false;
                                continue;
                            case ']':
                                if (!quoted)
                                {
                                    if (--openBrackets == 0)
                                    {
                                        if (i != position)
                                            length++;
                                        return true;
                                    }
                                }
                                else if (escaping)
                                {
                                    escaping = false;
                                }
                                continue;
                            case '{':
                                if (!quoted)
                                    openBraces++;
                                else if (escaping)
                                    escaping = false;
                                continue;
                            case '}':
                                if (!quoted)
                                    openBraces--;
                                else if (escaping)
                                    escaping = false;
                                continue;
                            case '"':
                                if (!quoted)
                                    quoted = true;
                                else if (escaping)
                                    escaping = false;
                                else
                                    quoted = false;
                                continue;
                            case '\\':
                                if (quoted)
                                {
                                    if (escaping)
                                        escaping = false;
                                    else
                                        escaping = true;
                                }
                                continue;
                            default:
                                if (escaping)
                                    escaping = false;
                                continue;

                        }
                    }
                }
                return false;
            }
        }
    }
}