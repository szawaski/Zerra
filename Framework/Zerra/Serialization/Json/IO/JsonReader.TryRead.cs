// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        private const byte commaByte = (byte)',';
        private const byte quoteByte = (byte)'"';
        private const byte escapeByte = (byte)'\\';
        private const byte uByte = (byte)'u';
        private const byte lByte = (byte)'l';
        private const byte rByte = (byte)'r';
        private const byte eByte = (byte)'e';
        private const byte aByte = (byte)'a';
        private const byte sByte = (byte)'s';

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
        public unsafe bool TryValidateULL(out bool valid)
        {
            if (useBytes)
            {
                if (position + 3 > length)
                {
                    valid = default;
                    return false;
                }
                valid = true;
                valid &= bufferBytes[position++] == uByte;
                valid &= bufferBytes[position++] == lByte;
                valid &= bufferBytes[position++] == lByte;
                return true;
            }
            else
            {
                if (position + 3 > length)
                {
                    valid = default;
                    return false;
                }
                valid = true;
                valid &= bufferChars[position++] == 'u';
                valid &= bufferBytes[position++] == 'l';
                valid &= bufferBytes[position++] == 'l';
                return true;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryValidateRUE(out bool valid)
        {
            if (useBytes)
            {
                if (position + 3 > length)
                {
                    valid = default;
                    return false;
                }
                valid = true;
                valid &= bufferBytes[position++] == rByte;
                valid &= bufferBytes[position++] == uByte;
                valid &= bufferBytes[position++] == eByte;
                return true;
            }
            else
            {
                if (position + 3 > length)
                {
                    valid = default;
                    return false;
                }
                valid = true;
                valid &= bufferChars[position++] == 'r';
                valid &= bufferBytes[position++] == 'u';
                valid &= bufferBytes[position++] == 'e';
                return true;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryValidateALSE(out bool valid)
        {
            if (useBytes)
            {
                if (position + 4 > length)
                {
                    valid = default;
                    return false;
                }
                valid = true;
                valid &= bufferBytes[position++] == aByte;
                valid &= bufferBytes[position++] == lByte;
                valid &= bufferBytes[position++] == sByte;
                valid &= bufferBytes[position++] == eByte;
                return true;
            }
            else
            {
                if (position + 4 > length)
                {
                    valid = default;
                    return false;
                }
                valid = true;
                valid &= bufferChars[position++] == 'a';
                valid &= bufferBytes[position++] == 'l';
                valid &= bufferBytes[position++] == 's';
                valid &= bufferBytes[position++] == 'e';
                return true;
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
        public unsafe bool TryPeakArrayLength(char firstChar, out int length)
        {
            if (useBytes)
            {
                fixed (byte* ptr = bufferBytes.Slice(position))
                {
                    var openBrackets = 0;
                    var quoted = false;
                    length = 0;

                    switch (firstChar)
                    {
                        case ',':
                            throw CreateException("Unexpected Character");
                        case '[':
                            openBrackets++;
                            break;
                        case ']':
                            return true;
                        case '"':
                            quoted = !quoted;
                            break;
                    }

                    length++;

                    byte* ptr2 = ptr;
                    for (var i = position; i < bufferBytes.Length; i++)
                    {
                        var b = *ptr2++;
                        switch (b)
                        {
                            case commaByte:
                                if (!quoted && openBrackets == 0)
                                    length++;
                                continue;
                            case openBracketByte:
                                if (!quoted)
                                    openBrackets++;
                                continue;
                            case closeBracketByte:
                                if (!quoted)
                                {
                                    if (openBrackets == 0)
                                        return true;
                                    openBrackets--;
                                }
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
                    var openBrackets = 0;
                    var quoted = false;
                    length = 0;

                    switch (firstChar)
                    {
                        case ',':
                            throw CreateException("Unexpected Character");
                        case '[':
                            openBrackets++;
                            break;
                        case ']':
                            return true;
                        case '"':
                            quoted = !quoted;
                            break;
                    }

                    length++;

                    char* ptr2 = ptr;
                    for (var i = position; i < bufferChars.Length; i++)
                    {
                        var c = *ptr2++;
                        switch (c)
                        {
                            case ',':
                                if (!quoted && openBrackets == 0)
                                    length++;
                                continue;
                            case '[':
                                if (!quoted)
                                    openBrackets++;
                                continue;
                            case ']':
                                if (!quoted)
                                {
                                    if (openBrackets == 0)
                                        return true;
                                    openBrackets--;
                                }
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