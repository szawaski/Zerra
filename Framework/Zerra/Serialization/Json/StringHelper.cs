// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Buffers;
using System.Text;

namespace Zerra.Serialization.Json
{
    internal static class StringHelper
    {
        private static readonly Encoding encoding = Encoding.UTF8;
        private const int maxEscapeCharacterSize = 6;

        private const byte quoteByte = (byte)'"';
        private const byte colonByte = (byte)':';
        private const byte escapeByte = (byte)'\\';

        private const byte bByte = (byte)'b';
        private const byte fByte = (byte)'f';
        private const byte nByte = (byte)'n';
        private const byte rByte = (byte)'r';
        private const byte tByte = (byte)'t';

        //invalid UTF8 surrogate characters
        private const char lowerSurrogate = (char)55296; //D800
        private const char upperSurrogate = (char)57343; //DFFF

        public unsafe static byte[]? EscapeAndEncodeString(string? value, bool quoteAndColon)
        {
            if (value is null)
                return null;
            if (value.Length == 0)
                return Array.Empty<byte>();

            byte[] bytes;
            fixed (char* pValue = value)
            {
                bool needsEscaped = false;
                var i = 0;
                for (; i < value.Length; i++)
                {
                    var c = pValue[i];
                    if (c < ' ' || c == '"' || c == '\\' || (c >= lowerSurrogate && c <= upperSurrogate))
                    {
                        needsEscaped = true;
                        break;
                    }
                }

                if (!needsEscaped)
                {
                    bytes = new byte[encoding.GetByteCount(pValue, value.Length) + (quoteAndColon ? 3 : 0)];
                    fixed (byte* pBytes = bytes)
                    {
                        if (quoteAndColon)
                        {
                            pBytes[0] = quoteByte;
                            _ = encoding.GetBytes(pValue, value.Length, &pBytes[1], bytes.Length - 1);
                            pBytes[bytes.Length - 2] = quoteByte;
                            pBytes[bytes.Length - 1] = colonByte;
                        }
                        else
                        {
                            _ = encoding.GetBytes(pValue, value.Length, pBytes, bytes.Length);
                        }
                    }
                    return bytes;
                }

                var maxSize = encoding.GetMaxByteCount((i + ((value.Length - i) * maxEscapeCharacterSize)) + 2);

                byte[]? escapeBufferOwner;
                scoped Span<byte> escapeBuffer;
                if (maxSize > 256)
                {
                    escapeBufferOwner = ArrayPoolHelper<byte>.Rent(maxSize);
                    escapeBuffer = escapeBufferOwner;
                }
                else
                {
                    escapeBufferOwner = null;
                    escapeBuffer = stackalloc byte[maxSize];
                }

                fixed (byte* pEscapeBuffer = escapeBuffer)
                {
                    var start = 0;
                    var bufferIndex = 0;

                    for (; i < value.Length; i++)
                    {
                        var c = pValue[i];
                        byte escapedByte;
                        switch (c)
                        {
                            case '"':
                                escapedByte = quoteByte;
                                break;
                            case '\\':
                                escapedByte = escapeByte;
                                break;
                            case >= ' ':
                                if (c >= lowerSurrogate && c <= upperSurrogate)
                                {
                                    bufferIndex += encoding.GetBytes(&pValue[start], i - start, pEscapeBuffer, bufferIndex - escapeBuffer.Length);

                                    var surrogateCode = SurrogateIntToEncodedHexBytes[c];
                                    fixed (byte* pCode = surrogateCode)
                                    {
                                        Buffer.MemoryCopy(pCode, &pEscapeBuffer[bufferIndex], bufferIndex - escapeBuffer.Length, surrogateCode.Length);
                                        bufferIndex += surrogateCode.Length;
                                    }

                                    start = i + 1;
                                    continue;
                                }
                                continue;
                            case '\b':
                                escapedByte = bByte;
                                break;
                            case '\f':
                                escapedByte = fByte;
                                break;
                            case '\n':
                                escapedByte = nByte;
                                break;
                            case '\r':
                                escapedByte = rByte;
                                break;
                            case '\t':
                                escapedByte = tByte;
                                break;
                            default:

                                bufferIndex += encoding.GetBytes(&pValue[start], i - start, pEscapeBuffer, bufferIndex - escapeBuffer.Length);

                                var code = LowUnicodeIntToEncodedHexBytes[c];
                                fixed (byte* pCode = code)
                                {
                                    Buffer.MemoryCopy(pCode, &pEscapeBuffer[bufferIndex], bufferIndex - escapeBuffer.Length, code.Length);
                                    bufferIndex += code.Length;
                                }

                                start = i + 1;
                                continue;
                        }

                        bufferIndex += encoding.GetBytes(&pValue[start], i - start, pEscapeBuffer, bufferIndex - escapeBuffer.Length);

                        pEscapeBuffer[bufferIndex++] = escapeByte;
                        pEscapeBuffer[bufferIndex++] = escapedByte;
                        start = i + 1;
                    }

                    if (value.Length > start)
                    {
                        bufferIndex += encoding.GetBytes(&pValue[start], value.Length - start, pEscapeBuffer, bufferIndex - escapeBuffer.Length);
                    }

                    bytes = new byte[bufferIndex + (quoteAndColon ? 3 : 0)];
                    fixed (byte* pBytes = bytes)
                    {
                        if (quoteAndColon)
                        {
                            pBytes[0] = quoteByte;
                            Buffer.MemoryCopy(pEscapeBuffer, &pBytes[1], bytes.Length - 1, bufferIndex);
                            pBytes[bytes.Length - 2] = quoteByte;
                            pBytes[bytes.Length - 1] = colonByte;
                        }
                        else
                        {
                            Buffer.MemoryCopy(pEscapeBuffer, pBytes, bytes.Length - 1, bufferIndex);
                        }
                    }
                }

                if (escapeBufferOwner is not null)
                    ArrayPoolHelper<byte>.Return(escapeBufferOwner);
            }

            return bytes;
        }

        public unsafe static char[]? EscapeString(string? value, bool quoteAndColon)
        {
            if (value is null)
                return null;
            if (value.Length == 0)
                return Array.Empty<char>();

            char[] chars;
            fixed (char* pValue = value)
            {
                bool needsEscaped = false;
                var i = 0;
                for (; i < value.Length; i++)
                {
                    var c = pValue[i];
                    if (c < ' ' || c == '"' || c == '\\' || (c >= lowerSurrogate && c <= upperSurrogate))
                    {
                        needsEscaped = true;
                        break;
                    }
                }

                if (!needsEscaped)
                {
                    if (!quoteAndColon)
                        return value.ToCharArray();

                    chars = new char[value.Length + 3];
                    fixed (char* pChar = chars)
                    {
                        pChar[0] = '"';
                        Buffer.MemoryCopy(pValue, &pChar[1], (chars.Length - 1) * 2, value.Length * 2);
                        pChar[chars.Length - 2] = '"';
                        pChar[chars.Length - 1] = ':';
                    }

                    return chars;
                }

                var maxSize = i + ((value.Length - i) * maxEscapeCharacterSize);
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

                fixed (char* pEscapeBuffer = escapeBuffer)
                {
                    var start = 0;
                    var bufferIndex = 0;

                    for (; i < value.Length; i++)
                    {
                        var c = pValue[i];
                        char escapedChar;
                        switch (c)
                        {
                            case '"':
                                escapedChar = '"';
                                break;
                            case '\\':
                                escapedChar = '\\';
                                break;
                            case >= ' ':
                                if (c >= lowerSurrogate && c <= upperSurrogate)
                                {
                                    Buffer.MemoryCopy(&pValue[start], &pEscapeBuffer[bufferIndex], (escapeBuffer.Length - bufferIndex) * 2, (i - start) * 2);
                                    bufferIndex += i - start;

                                    var surrogateCode = SurrogateIntToEncodedHexChars[c];
                                    fixed (char* pCode = surrogateCode)
                                    {
                                        Buffer.MemoryCopy(pCode, &pEscapeBuffer[bufferIndex], (escapeBuffer.Length - bufferIndex) * 2, surrogateCode.Length * 2);
                                    }
                                    bufferIndex += surrogateCode.Length;

                                    start = i + 1;
                                    continue;
                                }
                                continue;
                            case '\b':
                                escapedChar = 'b';
                                break;
                            case '\f':
                                escapedChar = 'f';
                                break;
                            case '\n':
                                escapedChar = 'n';
                                break;
                            case '\r':
                                escapedChar = 'r';
                                break;
                            case '\t':
                                escapedChar = 't';
                                break;
                            default:

                                Buffer.MemoryCopy(&pValue[start], &pEscapeBuffer[bufferIndex], (escapeBuffer.Length - bufferIndex) * 2, (i - start) * 2);
                                bufferIndex += i - start;

                                var code = LowUnicodeIntToEncodedHexChars[c];
                                fixed (char* pCode = code)
                                {
                                    Buffer.MemoryCopy(pCode, &pEscapeBuffer[bufferIndex], (escapeBuffer.Length - bufferIndex) * 2, code.Length * 2);
                                }
                                bufferIndex += code.Length;

                                start = i + 1;
                                continue;
                        }

                        Buffer.MemoryCopy(&pValue[start], &pEscapeBuffer[bufferIndex], (escapeBuffer.Length - bufferIndex) * 2, (i - start) * 2);
                        bufferIndex += i - start;

                        pValue[bufferIndex++] = '\\';
                        pValue[bufferIndex++] = escapedChar;
                        start = i + 1;
                    }

                    if (value.Length > start)
                    {
                        Buffer.MemoryCopy(&pValue[start], &pEscapeBuffer[bufferIndex], (escapeBuffer.Length - bufferIndex) * 2, (value.Length - start) * 2);
                        bufferIndex += value.Length - start;
                    }

                    chars = new char[encoding.GetByteCount(pEscapeBuffer, bufferIndex) + (quoteAndColon ? 3 : 0)];
                    fixed (char* pChar = chars)
                    {
                        if (quoteAndColon)
                        {
                            pChar[0] = '"';
                        }
                        Buffer.MemoryCopy(pValue, &pChar[1], (chars.Length - 1) * 2, value.Length * 2);
                        if (quoteAndColon)
                        {
                            pChar[chars.Length - 2] = '"'; ;
                            pChar[chars.Length - 1] = ':'; ;
                        }
                    }
                }

                if (escapeBufferOwner is not null)
                    ArrayPoolHelper<char>.Return(escapeBufferOwner);

                return chars;
            }
        }

        internal static readonly string[] LowUnicodeIntToEncodedHexChars =
        [
            "\\u0000","\\u0001","\\u0002","\\u0003","\\u0004","\\u0005","\\u0006","\\u0007","\\u0008","\\u0009","\\u000A","\\u000B","\\u000C","\\u000D","\\u000E","\\u000F",
            "\\u0010","\\u0011","\\u0012","\\u0013","\\u0014","\\u0015","\\u0016","\\u0017","\\u0018","\\u0019","\\u001A","\\u001B","\\u001C","\\u001D","\\u001E","\\u001F",

            //we are using non-unicode special cases u0022 as \" and 005C as \\
            ////no escapes over 92 (escape slash), we really only need 0022 and 005C here
        ];
        internal static readonly byte[][] LowUnicodeIntToEncodedHexBytes = LowUnicodeIntToEncodedHexChars.Select(x => encoding.GetBytes(x)).ToArray();

        internal static readonly Dictionary<ulong, char> LowUnicodeCharHexToChar;
        internal static readonly Dictionary<uint, char> LowUnicodeByteHexToChar;

        internal static readonly Dictionary<int, string> SurrogateIntToEncodedHexChars;
        internal static readonly Dictionary<int, byte[]> SurrogateIntToEncodedHexBytes;

        static unsafe StringHelper()
        {
            LowUnicodeCharHexToChar = new();
            LowUnicodeByteHexToChar = new();
            SurrogateIntToEncodedHexChars = new();
            SurrogateIntToEncodedHexBytes = new();

            for (ushort i = 0; i < ushort.MaxValue; i++)
            {
                var c = (char)i;
                var chars = i.ToString("x4");
                var bytes = Encoding.UTF8.GetBytes(chars);

                if (c >= lowerSurrogate && c <= upperSurrogate)
                {
                    var unicode = $"\\u{chars}";
                    SurrogateIntToEncodedHexChars.Add(i, unicode);
                    SurrogateIntToEncodedHexBytes.Add(i, Encoding.UTF8.GetBytes(unicode));
                }

                fixed (char* pChars = chars)
                {
                    var charIndex = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<ulong>(pChars);
                    LowUnicodeCharHexToChar.Add(charIndex, c);
                }

                fixed (byte* pBytes = bytes)
                {
                    var byteIndex = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(pBytes);
                    LowUnicodeByteHexToChar.Add(byteIndex, c);
                }

                var chars2 = i.ToString("X4");
                if (chars == chars2)
                    continue;
                var bytes2 = Encoding.UTF8.GetBytes(chars2);

                fixed (char* pChars = chars2)
                {
                    var charIndex = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<ulong>(pChars);
                    LowUnicodeCharHexToChar.Add(charIndex, c);
                }

                fixed (byte* pBytes = bytes2)
                {
                    var byteIndex = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(pBytes);
                    LowUnicodeByteHexToChar.Add(byteIndex, c);
                }
            }
        }

        //public unsafe static void GenerateLowUnicodeIndexes()
        //{
        //    var sbChars = new StringBuilder();
        //    var sbBytes = new StringBuilder();
        //    for (var i = 0; i < 256; i++)
        //    {
        //        var chars = i.ToString("x4");
        //        var bytes = Encoding.UTF8.GetBytes(chars);

        //        uint charIndex;
        //        fixed (char* pChars = chars)
        //        {
        //            charIndex = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(&pChars[2]);
        //        }

        //        uint byteIndex;
        //        fixed (byte* pBytes = bytes)
        //        {
        //            byteIndex = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(pBytes);
        //        }

        //        sbChars.Append($"{{{charIndex}u, (char){i}}},");
        //        sbBytes.Append($"{{{byteIndex}u, (char){i}}},");

        //        var charsUpper = i.ToString("X4");

        //        if (charsUpper != chars)
        //        {
        //            var bytesUpper = Encoding.UTF8.GetBytes(charsUpper);
        //            fixed (char* pChars = charsUpper)
        //            {
        //                charIndex = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(&pChars[2]);
        //            }

        //            fixed (byte* pBytes = bytesUpper)
        //            {
        //                byteIndex = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(pBytes);
        //            }

        //            sbChars.Append($"{{{charIndex}u, (char){i}}},");
        //            sbBytes.Append($"{{{byteIndex}u, (char){i}}},");
        //        }

        //        if (i < 255)
        //        {
        //            if ((i + 1) % 16 == 0)
        //            {
        //                sbChars.Append("\r\n");
        //                sbBytes.Append("\r\n");
        //            }
        //            else
        //            {
        //                sbChars.Append(" ");
        //                sbBytes.Append(" ");
        //            }
        //        }
        //    }

        //    var strChars = sbChars.ToString();
        //    var strBytes = sbBytes.ToString();
        //}
    }
}

