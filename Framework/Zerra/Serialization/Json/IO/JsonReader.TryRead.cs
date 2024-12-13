// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
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
        private const byte quoteByte = (byte)'"';
        private const byte escapeByte = (byte)'\\';
        private const byte nByte = (byte)'n';
        private const byte uByte = (byte)'u';
        private const byte lByte = (byte)'l';
        private const byte tByte = (byte)'t';
        private const byte rByte = (byte)'r';
        private const byte eByte = (byte)'e';
        private const byte fByte = (byte)'f';
        private const byte aByte = (byte)'a';
        private const byte sByte = (byte)'s';
        private const byte bByte = (byte)'b';

        private const byte eUpperByte = (byte)'E';

        private const byte plusByte = (byte)'+'; //43
        private const byte minusByte = (byte)'-'; //45
        private const byte dotByte = (byte)'.'; //46
        private const byte fowardSlashByte = (byte)'/'; //47
        private const byte zeroByte = (byte)'0'; //48
        private const byte oneByte = (byte)'1';
        private const byte twoByte = (byte)'2';
        private const byte threeByte = (byte)'3';
        private const byte fourByte = (byte)'4';
        private const byte fiveByte = (byte)'5';
        private const byte sixByte = (byte)'6';
        private const byte sevenByte = (byte)'7';
        private const byte eightByte = (byte)'8';
        private const byte nineByte = (byte)'9'; //57

        //JSON whitespace
        private const byte spaceByte = (byte)' ';
        private const byte tabByte = (byte)'\t';
        private const byte returnByte = (byte)'\r';
        private const byte newlineByte = (byte)'\n';

#if DEBUG
        public static bool Testing = false;

        private static bool Alternate = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Skip()
        {
            if (!JsonReader.Testing)
                return false;
            if (JsonReader.Alternate)
            {
                JsonReader.Alternate = false;
                return false;
            }
            else
            {
                JsonReader.Alternate = true;
                return true;
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadNextSkipWhiteSpace(out char c)
        {
#if DEBUG
            if (Skip())
            {
                c = default;
                return false;
            }
#endif

            if (useBytes)
            {
                fixed (byte* pBuffer = bufferBytes)
                {
                bytesAgain:
                    if (position >= length)
                    {
                        c = default;
                        return false;
                    }

                    var b = pBuffer[position++];
                    if (b == spaceByte || b == tabByte || b == returnByte || b == newlineByte)
                        goto bytesAgain;

                    //JSON has no tokens over the 1-byte uft8 range
                    if (b > 192)
                        throw CreateException("Unexpected Character");

                    c = (char)b;
                    return true;
                }
            }
            else
            {
                fixed (char* pBuffer = bufferChars)
                {
                charsAgain:
                    if (position >= length)
                    {
                        c = default;
                        return false;
                    }

                    c = pBuffer[position++];
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                        goto charsAgain;
                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadValueType(out JsonValueType valueType, out int sizeNeeded)
        {
#if DEBUG
            if (Skip())
            {
                sizeNeeded = 1;
                valueType = default;
                return false;
            }
#endif

            sizeNeeded = 1;
            if (length - position < sizeNeeded)
            {
                valueType = default;
                return false;
            }

            if (useBytes)
            {
                fixed (byte* pBuffer = bufferBytes)
                {
                whiteSpaceBytesGoAgain:
                    var b = pBuffer[position];
                    switch (b)
                    {
                        case spaceByte:
                        case returnByte:
                        case newlineByte:
                        case tabByte:
                            position++;
                            if (length - position < sizeNeeded)
                            {
                                valueType = default;
                                return false;
                            }
                            goto whiteSpaceBytesGoAgain;

                        case quoteByte:
                            position++;
                            valueType = JsonValueType.String;
                            return true;

                        case openBraceByte:
                            position++;
                            valueType = JsonValueType.Object;
                            return true;

                        case openBracketByte:
                            position++;
                            valueType = JsonValueType.Array;
                            return true;

                        case nByte:
                            {
                                sizeNeeded = 4;
                                if (length - position < sizeNeeded)
                                {
                                    valueType = default;
                                    return false;
                                }
                                var valid = true;
                                position++;
                                valid &= pBuffer[position++] == 'u';
                                valid &= pBuffer[position++] == 'l';
                                valid &= pBuffer[position++] == 'l';
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                            }
                            valueType = JsonValueType.Null_Completed;
                            return true;

                        case tByte:
                            {
                                sizeNeeded = 4;
                                if (length - position < sizeNeeded)
                                {
                                    valueType = default;
                                    return false;
                                }
                                var valid = true;
                                position++;
                                valid &= pBuffer[position++] == 'r';
                                valid &= pBuffer[position++] == 'u';
                                valid &= pBuffer[position++] == 'e';
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                            }
                            valueType = JsonValueType.True_Completed;
                            return true;

                        case fByte:
                            {
                                sizeNeeded = 5;
                                if (length - position < sizeNeeded)
                                {
                                    valueType = default;
                                    return false;
                                }
                                var valid = true;
                                position++;
                                valid &= pBuffer[position++] == 'a';
                                valid &= pBuffer[position++] == 'l';
                                valid &= pBuffer[position++] == 's';
                                valid &= pBuffer[position++] == 'e';
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                            }
                            valueType = JsonValueType.False_Completed;
                            return true;

                        case byte b2 when ((b2 >= zeroByte && b2 <= nineByte) || b2 == minusByte):
                            valueType = JsonValueType.Number;
                            return true;

                        default:
                            throw CreateException("Invalid JSON value");
                    }
                }
            }
            else
            {
                fixed (char* pBuffer = bufferChars)
                {
                whiteSpaceCharsGoAgain:
                    var c = pBuffer[position];
                    switch (c)
                    {
                        case ' ':
                        case '\r':
                        case '\n':
                        case '\t':
                            position++;
                            if (length - position < sizeNeeded)
                            {
                                valueType = default;
                                return false;
                            }
                            goto whiteSpaceCharsGoAgain;

                        case '"':
                            position++;
                            valueType = JsonValueType.String;
                            return true;

                        case '{':
                            position++;
                            valueType = JsonValueType.Object;
                            return true;

                        case '[':
                            position++;
                            valueType = JsonValueType.Array;
                            return true;

                        case 'n':
                            {
                                sizeNeeded = 4;
                                if (length - position < sizeNeeded)
                                {
                                    valueType = default;
                                    return false;
                                }
                                var valid = true;
                                position++;
                                valid &= pBuffer[position++] == 'u';
                                valid &= pBuffer[position++] == 'l';
                                valid &= pBuffer[position++] == 'l';
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                            }
                            valueType = JsonValueType.Null_Completed;
                            return true;

                        case 't':
                            {
                                sizeNeeded = 4;
                                if (length - position < sizeNeeded)
                                {
                                    valueType = default;
                                    return false;
                                }
                                var valid = true;
                                position++;
                                valid &= pBuffer[position++] == 'r';
                                valid &= pBuffer[position++] == 'u';
                                valid &= pBuffer[position++] == 'e';
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                            }
                            valueType = JsonValueType.True_Completed;
                            return true;

                        case 'f':
                            {
                                sizeNeeded = 5;
                                if (length - position < sizeNeeded)
                                {
                                    valueType = default;
                                    return false;
                                }
                                var valid = true;
                                position++;
                                valid &= pBuffer[position++] == 'a';
                                valid &= pBuffer[position++] == 'l';
                                valid &= pBuffer[position++] == 's';
                                valid &= pBuffer[position++] == 'e';
                                if (!valid)
                                    throw CreateException("Invalid number/true/false/null");
                            }
                            valueType = JsonValueType.False_Completed;
                            return true;

                        case char c2 when ((c2 >= '0' && c2 <= '9') || c2 == '-'):
                            valueType = JsonValueType.Number;
                            return true;

                        default:
                            throw CreateException("Invalid JSON value");
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryPeakArrayLength(out int length)
        {
            var openBrackets = 1;
            var openBraces = 0;
            var quoted = false;
            var escaping = false;
            length = 0;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadStringEscapedQuoted(bool hasStarted,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out string s, out int sizeNeeded)
        {
            if (position >= length
#if DEBUG
            || Skip()
#endif
            )
            {
                sizeNeeded = 1;
                s = null;
                return false;
            }

            var originalPosition = position;
            if (useBytes)
            {
                fixed (byte* pBuffer = bufferBytes)
                {
                    if (!hasStarted)
                    {
                        var b = pBuffer[position++];
                        if (b != quoteByte)
                        {
                            if (position == length)
                            {
                                s = null;
                                position = originalPosition;
                                sizeNeeded = length - position + 1;
                                return false;
                            }
                            b = pBuffer[position++];

                            while (b == spaceByte || b == tabByte || b == returnByte || b == newlineByte)
                            {
                                if (position == length)
                                {
                                    s = null;
                                    position = originalPosition;
                                    sizeNeeded = length - position + 1;
                                    return false;
                                }
                                b = pBuffer[position++];
                            }

                            if (b != quoteByte)
                                throw CreateException("Unexpected character");
                        }
                    }

                    var startPosition = position;

                    var escapeStart = -1;
                    for (; position < length; position++)
                    {
                        var b = pBuffer[position];
                        if (b == quoteByte)
                            break;

                        if (b == escapeByte)
                        {
                            if (escapeStart == -1)
                                escapeStart = position;

                            position++;
                            if (position == length)
                                break;

                            b = pBuffer[position];
                            if (b == uByte)
                            {
                                position += 3; //4 with end++
                                continue;
                            }
                        }
                    }
                    if (position >= length)
                    {
                        s = null;
                        position = originalPosition;
                        sizeNeeded = length - position + 1;
                        return false;
                    }

                    var endPosition = position;
                    position++;

                    if (escapeStart == -1)
                    {
                        sizeNeeded = 0;
                        s = encoding.GetString(&pBuffer[startPosition], endPosition - startPosition);
                        return true;
                    }

                    var maxSize = encoding.GetMaxCharCount(endPosition - startPosition);
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

                    var uChars = stackalloc char[4];

                    fixed (char* pEscapeBuffer = escapeBuffer)
                    {
                        var bufferIndex = 0;
                        var start = startPosition;

                        for (var i = startPosition; i < endPosition; i++)
                        {
                            var b = pBuffer[i];

                            if (b != escapeByte)
                                continue;

                            if (i > start)
                            {
                                bufferIndex += encoding.GetChars(&pBuffer[start], i - start, &pEscapeBuffer[bufferIndex], maxSize - bufferIndex);
                            }

                            b = pBuffer[++i];

                            switch (b)
                            {
                                case bByte:
                                    pEscapeBuffer[bufferIndex++] = '\b';
                                    start = i + 1;
                                    continue;
                                case tByte:
                                    pEscapeBuffer[bufferIndex++] = '\t';
                                    start = i + 1;
                                    continue;
                                case nByte:
                                    pEscapeBuffer[bufferIndex++] = '\n';
                                    start = i + 1;
                                    continue;
                                case fByte:
                                    pEscapeBuffer[bufferIndex++] = '\f';
                                    start = i + 1;
                                    continue;
                                case rByte:
                                    pEscapeBuffer[bufferIndex++] = '\r';
                                    start = i + 1;
                                    continue;
                                case uByte:
                                    var key = Unsafe.ReadUnaligned<uint>(&pBuffer[++i]);
                                    i += 3;
                                    if (!StringHelper.LowUnicodeByteHexToChar.TryGetValue(key, out var unicodeChar))
                                        throw CreateException("Invalid escape sequence");
                                    pEscapeBuffer[bufferIndex++] = unicodeChar;
                                    start = i + 5;
                                    break;
                                default:
                                    if (b > 128)
                                        throw CreateException("Invalid escape sequence");
                                    pEscapeBuffer[bufferIndex++] = (char)b;
                                    start = i + 1;
                                    continue;
                            }
                        }

                        if (start < endPosition)
                        {
                            bufferIndex += encoding.GetChars(&pBuffer[start], endPosition - start, &pEscapeBuffer[bufferIndex], maxSize - bufferIndex);
                        }

                        s = escapeBuffer.Slice(0, bufferIndex).ToString();
                    }

                    if (escapeBufferOwner is not null)
                        ArrayPoolHelper<char>.Return(escapeBufferOwner);

                    sizeNeeded = 0;
                    return true;
                }
            }
            else
            {
                fixed (char* pBuffer = bufferChars)
                {
                    if (!hasStarted)
                    {
                        var c = pBuffer[position++];
                        if (c != '\"')
                        {
                            if (position == length)
                            {
                                s = null;
                                position = originalPosition;
                                sizeNeeded = length - position + 1;
                                return false;
                            }
                            c = pBuffer[position++];

                            while (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                            {
                                if (position == length)
                                {
                                    s = null;
                                    position = originalPosition;
                                    sizeNeeded = length - position + 1;
                                    return false;
                                }
                                c = pBuffer[position++];
                            }

                            if (c != '\"')
                                throw CreateException("Unexpected character");
                        }
                    }

                    var startPosition = position;

                    var escapeStart = -1;
                    for (; position < length; position++)
                    {
                        var c = pBuffer[position];
                        if (c == '"')
                            break;

                        if (c == '\\')
                        {
                            if (escapeStart == -1)
                                escapeStart = position;

                            position++;
                            if (position == length)
                                break;

                            c = pBuffer[position];
                            if (c == 'u')
                            {
                                position += 3; //4 with end++
                                continue;
                            }
                        }
                    }
                    if (position >= length)
                    {
                        s = null;
                        position = originalPosition;
                        sizeNeeded = length - position + 1;
                        return false;
                    }

                    var endPosition = position;
                    position++;

                    if (escapeStart == -1)
                    {
                        sizeNeeded = 0;
                        s = new string(pBuffer, startPosition, endPosition - startPosition);
                        return true;
                    }

                    var maxSize = endPosition - startPosition;
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

                    var uChars = stackalloc char[4];

                    fixed (char* pEscapeBuffer = escapeBuffer)
                    {
                        var bufferIndex = 0;
                        var start = startPosition;

                        for (var i = startPosition; i < endPosition; i++)
                        {
                            var c = pBuffer[i];

                            if (c != '\\')
                                continue;

                            if (i > start)
                            {
                                Buffer.MemoryCopy(&pBuffer[start], &pEscapeBuffer[bufferIndex], (maxSize - bufferIndex) * 2, (i - start) * 2);
                                bufferIndex += i - start;
                            }

                            c = pBuffer[++i];

                            switch (c)
                            {
                                case 'b':
                                    pEscapeBuffer[bufferIndex++] = '\b';
                                    start = i + 1;
                                    continue;
                                case 't':
                                    pEscapeBuffer[bufferIndex++] = '\t';
                                    start = i + 1;
                                    continue;
                                case 'n':
                                    pEscapeBuffer[bufferIndex++] = '\n';
                                    start = i + 1;
                                    continue;
                                case 'f':
                                    pEscapeBuffer[bufferIndex++] = '\f';
                                    start = i + 1;
                                    continue;
                                case 'r':
                                    pEscapeBuffer[bufferIndex++] = '\r';
                                    start = i + 1;
                                    continue;
                                case 'u':
                                    var key = Unsafe.ReadUnaligned<uint>(&pBuffer[++i + 2]);
                                    i += 3;
                                    if (!StringHelper.LowUnicodeCharHexToChar.TryGetValue(key, out var unicodeChar))
                                        throw CreateException("Invalid escape sequence");
                                    pEscapeBuffer[bufferIndex++] = unicodeChar;
                                    start = i + 5;
                                    break;
                                default:
                                    if (c > 128)
                                        throw CreateException("Invalid escape sequence");
                                    pEscapeBuffer[bufferIndex++] = c;
                                    start = i + 1;
                                    continue;
                            }
                        }

                        if (start < endPosition)
                        {
                            Buffer.MemoryCopy(&pBuffer[start], &pEscapeBuffer[bufferIndex], (maxSize - bufferIndex) * 2, (endPosition - start) * 2);
                            bufferIndex += endPosition - start;
                        }

                        s = escapeBuffer.Slice(0, bufferIndex).ToString();
                    }

                    if (escapeBufferOwner is not null)
                        ArrayPoolHelper<char>.Return(escapeBufferOwner);

                    sizeNeeded = 0;
                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadStringQuotedBytes(bool hasStarted,
#if !NETSTANDARD2_0
    [MaybeNullWhen(false)]
#endif
        out ReadOnlySpan<byte> bytes, out int sizeNeeded)
        {
            if (!useBytes)
                throw new InvalidOperationException($"{nameof(TryReadStringQuotedBytes)} {nameof(useBytes)} is the wrong setting for this call");

            if (position >= length
#if DEBUG
            || Skip()
#endif
            )
            {
                sizeNeeded = 1;
                bytes = null;
                return false;
            }

            var originalPosition = position;

            fixed (byte* pBuffer = bufferBytes)
            {
                if (!hasStarted)
                {
                    var b = pBuffer[position++];
                    while (b == spaceByte || b == tabByte || b == returnByte || b == newlineByte)
                    {
                        if (position == length)
                        {
                            bytes = null;
                            position = originalPosition;
                            sizeNeeded = length - position + 1;
                            return false;
                        }
                        b = pBuffer[position++];
                    }

                    if (b != quoteByte)
                        throw CreateException("Unexpected character");
                }

                var startPosition = position;

                for (; position < length; position++)
                {
                    var b = pBuffer[position];
                    if (b == quoteByte)
                        break;

                    if (b == escapeByte)
                    {
                        position++;
                        if (position == length)
                            break;

                        b = pBuffer[position];
                        if (b == uByte)
                            position += 3; //4 with position++
                        continue;
                    }
                }
                if (position >= length)
                {
                    bytes = null;
                    position = originalPosition;
                    sizeNeeded = length - position + 1;
                    return false;
                }

                sizeNeeded = 0;
                bytes = bufferBytes.Slice(startPosition, position - startPosition);
                position++;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadStringQuotedChars(bool hasStarted,
#if !NETSTANDARD2_0
[MaybeNullWhen(false)]
#endif
        out ReadOnlySpan<char> chars, out int sizeNeeded)
        {
            if (useBytes)
                throw new InvalidOperationException($"{nameof(TryReadStringQuotedChars)} {nameof(useBytes)} is the wrong setting for this call");

            if (position >= length
#if DEBUG
            || Skip()
#endif
            )
            {
                sizeNeeded = 1;
                chars = null;
                return false;
            }

            var originalPosition = position;

            fixed (char* pBuffer = bufferChars)
            {
                if (!hasStarted)
                {
                    var c = pBuffer[position++];
                    while (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    {
                        if (position == length)
                        {
                            chars = null;
                            position = originalPosition;
                            sizeNeeded = length - position + 1;
                            return false;
                        }
                        c = pBuffer[position++];
                    }

                    if (c != '\"')
                        throw CreateException("Unexpected character");
                }

                var startPosition = position;

                for (; position < length; position++)
                {
                    var c = pBuffer[position];
                    if (c == '\"')
                        break;

                    if (c == escapeByte)
                    {
                        position++;
                        if (position == length)
                            break;

                        c = pBuffer[position];
                        if (c == 'u')
                            position += 3; //4 with position++
                        continue;
                    }
                }
                if (position >= length)
                {
                    chars = null;
                    position = originalPosition;
                    sizeNeeded = length - position + 1;
                    return false;
                }

                sizeNeeded = 0;
                chars = bufferChars.Slice(startPosition, position - startPosition);
                position++;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadNumberBytes(
#if !NETSTANDARD2_0
[MaybeNullWhen(false)]
#endif
        out ReadOnlySpan<byte> bytes, out int sizeNeeded)
        {
            if (!useBytes)
                throw new InvalidOperationException($"{nameof(TryReadStringQuotedBytes)} {nameof(useBytes)} is the wrong setting for this call");

            if (position >= length
#if DEBUG
            || Skip()
#endif
            )
            {
                sizeNeeded = 1;
                bytes = null;
                return false;
            }

            var originalPosition = position;

            fixed (byte* pBuffer = bufferBytes)
            {
                var startPosition = position;

                for (; position < length; position++)
                {
                    var b = pBuffer[position];
                    if ((b < zeroByte && b != minusByte && b != dotByte && b != plusByte) || (b > nineByte && b != eByte && b != eUpperByte))
                        break;
                }

                if (position == length && !isFinalBlock)
                {
                    bytes = null;
                    position = originalPosition;
                    sizeNeeded = length - position + 1;
                    return false;
                }

                sizeNeeded = 0;
                bytes = bufferBytes.Slice(startPosition, position - startPosition);
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadNumberChars(
#if !NETSTANDARD2_0
[MaybeNullWhen(false)]
#endif
        out ReadOnlySpan<char> chars, out int sizeNeeded)
        {
            if (useBytes)
                throw new InvalidOperationException($"{nameof(TryReadStringQuotedBytes)} {nameof(useBytes)} is the wrong setting for this call");

            if (position >= length
#if DEBUG
            || Skip()
#endif
            )
            {
                sizeNeeded = 1;
                chars = null;
                return false;
            }

            var originalPosition = position;

            fixed (char* pBuffer = bufferChars)
            {
                var startPosition = position;

                for (; position < length; position++)
                {
                    var c = pBuffer[position];
                    if ((c < '0' && c != '-' && c != '.' && c != '+') || (c > '9' && c != 'e' && c != 'E'))
                        break;
                }

                if (position == length && !isFinalBlock)
                {
                    chars = null;
                    position = originalPosition;
                    sizeNeeded = length - position + 1;
                    return false;
                }

                sizeNeeded = 0;
                chars = bufferChars.Slice(startPosition, position - startPosition);
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadNumberString(
#if !NETSTANDARD2_0
[MaybeNullWhen(false)]
#endif
        out string str, out int sizeNeeded)
        {
            if (position >= length
#if DEBUG
            || Skip()
#endif
            )
            {
                sizeNeeded = 1;
                str = null;
                return false;
            }

            var originalPosition = position;

            if (useBytes)
            {
                fixed (byte* pBuffer = bufferBytes)
                {
                    var startPosition = position;

                    for (; position < length; position++)
                    {
                        var b = pBuffer[position];
                        if ((b < zeroByte && b != minusByte && b != dotByte && b != plusByte) || (b > nineByte && b != eByte && b != eUpperByte))
                            break;
                    }

                    if (position == length && !isFinalBlock)
                    {
                        str = null;
                        position = originalPosition;
                        sizeNeeded = length - position + 1;
                        return false;
                    }

                    sizeNeeded = 0;

                    //all in 1-byte UTF8 range so we don't need to encode

                    var charsLength = position - startPosition;

                    if (charsLength < 128)
                    {
                        var pChars = stackalloc char[charsLength];
                        for (var i = 0; i < charsLength; i++)
                            pChars[i] = (char)pBuffer[startPosition + i];
                        str = new string(pChars, 0, charsLength);
                    }
                    else
                    {
                        var chars = ArrayPoolHelper<char>.Rent(charsLength);
                        fixed (char* pChars = chars)
                        {
                            for (var i = 0; i < charsLength; i++)
                                pChars[i] = (char)pBuffer[startPosition + i];
                            str = new string(pChars, 0, charsLength);
                        }
                        ArrayPoolHelper<char>.Return(chars);
                    }

                    return true;
                }
            }
            else
            {
                fixed (char* pBuffer = bufferChars)
                {
                    var startPosition = position;

                    for (; position < length; position++)
                    {
                        var c = pBuffer[position];
                        if ((c < '0' && c != '-' && c != '.' && c != '+') || (c > '9' && c != 'e' && c != 'E'))
                            break;
                    }

                    if (position == length && !isFinalBlock)
                    {
                        str = null;
                        position = originalPosition;
                        sizeNeeded = length - position + 1;
                        return false;
                    }

                    sizeNeeded = 0;
                    str = new string(pBuffer, startPosition, position - startPosition);
                    return true;
                }
            }
        }
    }
}