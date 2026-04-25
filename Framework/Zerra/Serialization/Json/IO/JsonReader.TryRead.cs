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
        /// <summary>
        /// Attempts to read the next JSON token from the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if a token was successfully read; <c>false</c> if more bytes are needed.</returns>
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

                                PositionOfFirstEscape = -1;

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
                                        ValueBytes = Span<byte>.Empty;
                                        position++;
                                        return true;
                                    }
                                    ValueBytes = localBufferBytes.Slice(startPosition, position - startPosition);
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
                                    PositionOfFirstEscape = position - startPosition;
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
                                        ValueBytes = localBufferBytes.Slice(startPosition, position - startPosition);
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

                                ValueBytes = localBufferBytes.Slice(startPosition, position - startPosition);
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

                                PositionOfFirstEscape = -1;

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
                                        ValueChars = Span<char>.Empty;
                                        position++;
                                        return true;
                                    }
                                    ValueChars = localBufferChars.Slice(startPosition, position - startPosition);
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
                                    PositionOfFirstEscape = position - startPosition;
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
                                        ValueChars = localBufferChars.Slice(startPosition, position - startPosition);
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

                                ValueChars = localBufferChars.Slice(startPosition, position - startPosition);
                                return true;
                            }

                        default:
                            throw CreateException("Invalid JSON value");
                    }
                }
            }
        }

        /// <summary>
        /// Unescapes the current string token bytes.
        /// </summary>
        /// <returns>The unescaped string decoded from <see cref="ValueBytes"/>.</returns>
        public unsafe string UnescapeStringBytes()
        {
            if (ValueBytes.Length == 0)
                return String.Empty;

            fixed (byte* pBuffer = ValueBytes)
            {
                if (PositionOfFirstEscape == -1)
                    return encoding.GetString(pBuffer, ValueBytes.Length);

                var localStringBytes = ValueBytes;
                var length = localStringBytes.Length;

                var position = PositionOfFirstEscape;
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

        /// <summary>
        /// Unescapes the current string token characters.
        /// </summary>
        /// <returns>The unescaped string decoded from <see cref="ValueChars"/>.</returns>
        public unsafe string UnescapeStringChars()
        {
            if (ValueChars.Length == 0)
                return String.Empty;
            if (PositionOfFirstEscape == -1)
                return ValueChars.ToString();

            var localStringChars = ValueChars;
            var length = localStringChars.Length;

            fixed (char* pBuffer = localStringChars)
            {
                var position = PositionOfFirstEscape;
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

        /// <summary>
        /// Peeks ahead to determine the number of elements in the current JSON array without advancing the reader position.
        /// </summary>
        /// <param name="length">When this method returns <c>true</c>, contains the number of elements in the array; otherwise, zero.</param>
        /// <returns><c>true</c> if the array length was successfully determined; <c>false</c> if the buffer contains incomplete data.</returns>
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