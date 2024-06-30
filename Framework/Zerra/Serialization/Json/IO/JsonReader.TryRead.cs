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

        private static readonly byte quoteByte = (byte)'\"';
        private static readonly byte escapeByte = (byte)'\\';
        private static readonly byte uByte = (byte)'u';
        private static readonly byte lByte = (byte)'l';
        private static readonly byte rByte = (byte)'r';
        private static readonly byte eByte = (byte)'e';
        private static readonly byte aByte = (byte)'a';
        private static readonly byte sByte = (byte)'s';

        //JSON whitespace
        private static readonly byte spaceByte = (byte)' ';
        private static readonly byte tabByte = (byte)'\t';
        private static readonly byte returnByte = (byte)'\r';
        private static readonly byte newlineByte = (byte)'\n';

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
                    fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bufferBytes.Slice(position)))
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
                    fixed (byte* bytesPtr = &MemoryMarshal.GetReference(bufferBytes.Slice(position)))
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
        public void BackOne()
        {
            if (position == 0)
                throw new InvalidOperationException($"Cannot {nameof(BackOne)} before position of zero.");
            position--;
        }
    }
}