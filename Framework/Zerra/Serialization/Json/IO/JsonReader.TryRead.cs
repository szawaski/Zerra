// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        private const byte zeroByte = (byte)'0';
        private const byte oneByte = (byte)'1';
        private const byte twoByte = (byte)'2';
        private const byte threeByte = (byte)'3';
        private const byte fourByte = (byte)'4';
        private const byte fiveByte = (byte)'5';
        private const byte sixByte = (byte)'6';
        private const byte sevenByte = (byte)'7';
        private const byte eightByte = (byte)'8';
        private const byte nineByte = (byte)'9';
        private const byte minusByte = (byte)'-';

        //JSON whitespace
        private const byte spaceByte = (byte)' ';
        private const byte tabByte = (byte)'\t';
        private const byte returnByte = (byte)'\r';
        private const byte newlineByte = (byte)'\n';


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadNext(out char c)
        {
            if (useBytes)
            {
                if (position >= length)
                {
                    c = default;
                    return false;
                }
                var b = bufferBytes[position];
                if (b < 192)
                {
                    c = (char)b;
                    position++;
                    return true;
                }
                else if (b < 224)
                {
                    if (position + 1 >= length)
                    {
                        c = default;
                        return false;
                    }
                    var chars = stackalloc char[1];
                    fixed (byte* bytesPtr = bufferBytes.Slice(position))
                    {
                        encoding.GetChars(bytesPtr, 2, chars, 0);
                    }
                    c = chars[0];
                    position += 2;
                    return true;
                }
                else if (b < 240)
                {
                    if (position + 2 >= length)
                    {
                        c = default;
                        return false;
                    }
                    var chars = stackalloc char[1];
                    fixed (byte* bytesPtr = bufferBytes.Slice(position))
                    {
                        encoding.GetChars(bytesPtr, 3, chars, 0);
                        c = chars[0];
                        position += 3;
                        return true;
                    }
                }
                else
                {
                    throw CreateException("Unsupported UTF8 byte sequence");
                }
            }
            else
            {
                if (position >= length)
                {
                    c = default;
                    return false;
                }
                c = bufferChars[position++];
                return true;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadNextSkipWhiteSpace(out char c)
        {
            if (useBytes)
            {
            bytesAgain:
                if (position >= length)
                {
                    c = default;
                    return false;
                }
                var b = bufferBytes[position];
                if (b == spaceByte || b == tabByte || b == returnByte || b == newlineByte)
                {
                    position++;
                    goto bytesAgain;
                }
                if (b < 192)
                {
                    c = (char)b;
                    position++;
                    return true;
                }
                else if (b < 224)
                {
                    if (position + 1 >= length)
                    {
                        c = default;
                        return false;
                    }
                    var chars = stackalloc char[1];
                    fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bufferBytes.Slice(position)))
                    {
                        encoding.GetChars(bytesPtr, 2, chars, 0);
                    }
                    c = chars[0];
                    position += 2;
                    return true;
                }
                else if (b < 240)
                {
                    if (position + 2 >= length)
                    {
                        c = default;
                        return false;
                    }
                    var chars = stackalloc char[1];
                    fixed (byte* bytesPtr = bufferBytes.Slice(position))
                    {
                        encoding.GetChars(bytesPtr, 3, chars, 0);
                        c = chars[0];
                        position += 3;
                        return true;
                    }
                }
                else
                {
                    throw CreateException("Unsupported UTF8 byte sequence");
                }
            }
            else
            {
            charsAgain:
                if (position >= length)
                {
                    c = default;
                    return false;
                }
                c = bufferChars[position++];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    goto charsAgain;
                return true;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadNextQuote()
        {
            if (useBytes)
            {
            bytesAgain:
                if (position >= length)
                    return false;
                var b = bufferBytes[position++];
                if (b == spaceByte || b == tabByte || b == returnByte || b == newlineByte)
                    goto bytesAgain;
                if (b != quoteByte)
                    throw CreateException("Unexpected character");
                return true;
            }
            else
            {
            charsAgain:
                if (position >= length)
                    return false;
                var c = bufferChars[position++];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    goto charsAgain;
                if (c != '"')
                    throw CreateException("Unexpected character");
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadValueType(out JsonValueType valueType)
        {
        whiteSpaceGoAgain:
            if (position + 1 > length)
            {
                valueType = default;
                return false;
            }

            if (useBytes)
            {
                var b = bufferBytes[position];
                switch (b)
                {
                    case spaceByte:
                    case returnByte:
                    case newlineByte:
                    case tabByte:
                        position++;
                        goto whiteSpaceGoAgain;

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
                            if (position + 4 > length)
                            {
                                valueType = default;
                                return false;
                            }
                            var valid = true;
                            position++;
                            valid &= bufferBytes[position++] == 'u';
                            valid &= bufferBytes[position++] == 'l';
                            valid &= bufferBytes[position++] == 'l';
                            if (!valid)
                                throw CreateException("Invalid number/true/false/null");
                        }
                        valueType = JsonValueType.Null_Completed;
                        return true;

                    case tByte:
                        {
                            if (position + 4 > length)
                            {
                                valueType = default;
                                return false;
                            }
                            var valid = true;
                            position++;
                            valid &= bufferBytes[position++] == 'r';
                            valid &= bufferBytes[position++] == 'u';
                            valid &= bufferBytes[position++] == 'e';
                            if (!valid)
                                throw CreateException("Invalid number/true/false/null");
                        }
                        valueType = JsonValueType.True_Completed;
                        return true;

                    case fByte:
                        {
                            if (position + 5 > length)
                            {
                                valueType = default;
                                return false;
                            }
                            var valid = true;
                            position++;
                            valid &= bufferBytes[position++] == 'a';
                            valid &= bufferBytes[position++] == 'l';
                            valid &= bufferBytes[position++] == 's';
                            valid &= bufferBytes[position++] == 'e';
                            if (!valid)
                                throw CreateException("Invalid number/true/false/null");
                        }
                        valueType = JsonValueType.False_Completed;
                        return true;

                    case zeroByte:
                    case oneByte:
                    case twoByte:
                    case threeByte:
                    case fourByte:
                    case fiveByte:
                    case sixByte:
                    case sevenByte:
                    case eightByte:
                    case nineByte:
                    case minusByte:
                        valueType = JsonValueType.Number;
                        return true;

                    default:
                        throw CreateException("Invalid JSON value");
                }
            }
            else
            {
                var c = bufferChars[position];
                switch (c)
                {
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        position++;
                        goto whiteSpaceGoAgain;

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
                            if (position + 4 > length)
                            {
                                valueType = default;
                                return false;
                            }
                            var valid = true;
                            position++;
                            valid &= bufferChars[position++] == 'u';
                            valid &= bufferChars[position++] == 'l';
                            valid &= bufferChars[position++] == 'l';
                            if (!valid)
                                throw CreateException("Invalid number/true/false/null");
                        }
                        valueType = JsonValueType.Null_Completed;
                        return true;

                    case 't':
                        {
                            if (position + 4 > length)
                            {
                                valueType = default;
                                return false;
                            }
                            var valid = true;
                            position++;
                            valid &= bufferChars[position++] == 'r';
                            valid &= bufferChars[position++] == 'u';
                            valid &= bufferChars[position++] == 'e';
                            if (!valid)
                                throw CreateException("Invalid number/true/false/null");
                        }
                        valueType = JsonValueType.True_Completed;
                        return true;

                    case 'f':
                        {
                            if (position + 5 > length)
                            {
                                valueType = default;
                                return false;
                            }
                            var valid = true;
                            position++;
                            valid &= bufferChars[position++] == 'a';
                            valid &= bufferChars[position++] == 'l';
                            valid &= bufferChars[position++] == 's';
                            valid &= bufferChars[position++] == 'e';
                            if (!valid)
                                throw CreateException("Invalid number/true/false/null");
                        }
                        valueType = JsonValueType.False_Completed;
                        return true;

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                        valueType = JsonValueType.Number;
                        return true;

                    default:
                        throw CreateException("Invalid JSON value");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadEscapeHex(out ReadOnlySpan<char> s)
        {
            if (useBytes)
            {
                if (position + 4 > length)
                {
                    s = default;
                    return false;
                }
                var chars = new char[4];
                chars[0] = (char)bufferBytes[position++];
                chars[1] = (char)bufferBytes[position++];
                chars[2] = (char)bufferBytes[position++];
                chars[3] = (char)bufferBytes[position++];
                s = chars;
                return true;
            }
            else
            {
                if (position + 4 > length)
                {
                    s = default;
                    return false;
                }
                s = bufferChars.Slice(position, 4);
                position += 4;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSpanUntilQuoteOrEscape(out ReadOnlySpan<char> s)
        {
            if (useBytes)
            {
                if (position >= length)
                {
                    s = default;
                    return false;
                }
                var start = position;
                var b = bufferBytes[position++];
                while (b != quoteByte && b != escapeByte)
                {
                    if (position >= length)
                    {
                        position = start;
                        s = default;
                        return false;
                    }
                    b = bufferBytes[position++];
                }
#if NETSTANDARD2_0
                s = encoding.GetString(bufferBytes.Slice(start, position - start).ToArray()).AsSpan();
#else
                s = encoding.GetString(bufferBytes.Slice(start, position - start));
#endif
                return true;
            }
            else
            {
                if (position >= length)
                {
                    s = default;
                    return false;
                }
                var start = position;
                var c = bufferChars[position++];
                while (c != '\"' && c != '\\')
                {
                    if (position >= length)
                    {
                        position = start;
                        s = default;
                        return false;
                    }
                    c = bufferChars[position++];
                }
                s = bufferChars.Slice(start, position - start);
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryPeakArrayLength(out int length)
        {
            if (useBytes)
            {
                fixed (byte* ptr = bufferBytes.Slice(position))
                {
                    var openBrackets = 1;
                    var openBraces = 0;
                    var quoted = false;
                    length = 0;

                    byte* ptr2 = ptr;
                    for (var i = position; i < bufferBytes.Length; i++)
                    {
                        var b = *ptr2++;
                        switch (b)
                        {
                            case commaByte:
                                if (!quoted && openBrackets == 1 && openBraces == 0)
                                    length++;
                                continue;
                            case openBracketByte:
                                if (!quoted)
                                    openBrackets++;
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
                                continue;
                            case openBraceByte:
                                if (!quoted)
                                    openBraces++;
                                continue;
                            case closeBraceByte:
                                if (!quoted)
                                    openBraces--;
                                continue;
                            case quoteByte:
                                quoted = !quoted;
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
                    var openBrackets = 1;
                    var openBraces = 0;
                    var quoted = false;
                    length = 0;

                    char* ptr2 = ptr;
                    for (var i = position; i < bufferChars.Length; i++)
                    {
                        var c = *ptr2++;
                        switch (c)
                        {
                            case ',':
                                if (!quoted && openBrackets == 1 && openBraces == 0)
                                    length++;
                                continue;
                            case '[':
                                if (!quoted)
                                    openBrackets++;
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
                                continue;
                            case '{':
                                if (!quoted)
                                    openBraces++;
                                continue;
                            case '}':
                                if (!quoted)
                                    openBraces--;
                                continue;
                            case '"':
                                quoted = !quoted;
                                continue;
                        }
                    }
                }
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BackOne()
        {
            if (position == 0)
                throw new InvalidOperationException($"Cannot {nameof(BackOne)} before position of zero.");
            position--;
        }
    }
}