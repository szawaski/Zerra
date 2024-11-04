// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Buffers;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
                    if (c < ' ' || c == '"' || c == '\\')
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
                            case >= ' ': //32
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
                    if (c < ' ' || c == '"' || c == '\\')
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
                            case >= ' ': //32
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
            //"\\u0020","\\u0021","\\u0022","\\u0023","\\u0024","\\u0025","\\u0026","\\u0027","\\u0028","\\u0029","\\u002A","\\u002B","\\u002C","\\u002D","\\u002E","\\u002F",
            //"\\u0030","\\u0031","\\u0032","\\u0033","\\u0034","\\u0035","\\u0036","\\u0037","\\u0038","\\u0039","\\u003A","\\u003B","\\u003C","\\u003D","\\u003E","\\u003F",
            //"\\u0040","\\u0041","\\u0042","\\u0043","\\u0044","\\u0045","\\u0046","\\u0047","\\u0048","\\u0049","\\u004A","\\u004B","\\u004C","\\u004D","\\u004E","\\u004F",
            //"\\u0050","\\u0051","\\u0052","\\u0053","\\u0054","\\u0055","\\u0056","\\u0057","\\u0058","\\u0059","\\u005A","\\u005B","\\u005C",
        ];
        internal static readonly byte[][] LowUnicodeIntToEncodedHexBytes = LowUnicodeIntToEncodedHexChars.Select(x => encoding.GetBytes(x)).ToArray();

        //Unsafe.ReadUnaligned<uint>(&pChars[2])
        internal static readonly Dictionary<uint, char> LowUnicodeCharHexToChar = new()
        {
            //upper case hex
            {3145776u, (char)0}, {3211312u, (char)1}, {3276848u, (char)2}, {3342384u, (char)3}, {3407920u, (char)4}, {3473456u, (char)5}, {3538992u, (char)6}, {3604528u, (char)7}, {3670064u, (char)8}, {3735600u, (char)9}, {4259888u, (char)10}, {4325424u, (char)11}, {4390960u, (char)12}, {4456496u, (char)13}, {4522032u, (char)14}, {4587568u, (char)15},
            {3145777u, (char)16}, {3211313u, (char)17}, {3276849u, (char)18}, {3342385u, (char)19}, {3407921u, (char)20}, {3473457u, (char)21}, {3538993u, (char)22}, {3604529u, (char)23}, {3670065u, (char)24}, {3735601u, (char)25}, {4259889u, (char)26}, {4325425u, (char)27}, {4390961u, (char)28}, {4456497u, (char)29}, {4522033u, (char)30}, {4587569u, (char)31},

            {3276850u, (char)34}, //quote
            {4390965u, (char)92}, //escape slash
            
            //lower case hex
            {6357040u, (char)10}, {6422576u, (char)11}, {6488112u, (char)12}, {6553648u, (char)13}, {6619184u, (char)14}, {6684720u, (char)15},
            {6357041u, (char)26}, {6422577u, (char)27}, {6488113u, (char)28}, {6553649u, (char)29}, {6619185u, (char)30}, {6684721u, (char)31},

            {6488117u, (char)92} //upper case escape slash
        };

        //Unsafe.ReadUnaligned<uint>(pBytes)
        internal static readonly Dictionary<uint, char> LowUnicodeByteHexToChar = new()
        {        
            //upper case hex
            {808464432u, (char)0}, {825241648u, (char)1}, {842018864u, (char)2}, {858796080u, (char)3}, {875573296u, (char)4}, {892350512u, (char)5}, {909127728u, (char)6}, {925904944u, (char)7}, {942682160u, (char)8}, {959459376u, (char)9}, {1093677104u, (char)10}, {1110454320u, (char)11}, {1127231536u, (char)12}, {1144008752u, (char)13}, {1160785968u, (char)14}, {1177563184u, (char)15},
            {808529968u, (char)16}, {825307184u, (char)17}, {842084400u, (char)18}, {858861616u, (char)19}, {875638832u, (char)20}, {892416048u, (char)21}, {909193264u, (char)22}, {925970480u, (char)23}, {942747696u, (char)24}, {959524912u, (char)25}, {1093742640u, (char)26}, {1110519856u, (char)27}, {1127297072u, (char)28}, {1144074288u, (char)29}, {1160851504u, (char)30}, {1177628720u, (char)31},
            
            {842149936u, (char)34}, //quote
            {1127559216u, (char)92}, //escape slash
            
            //lower case hex
            {1630548016u, (char)10}, {1647325232u, (char)11}, {1664102448u, (char)12}, {1680879664u, (char)13}, {1697656880u, (char)14}, {1714434096u, (char)15},
            {1630613552u, (char)26}, {1647390768u, (char)27}, {1664167984u, (char)28}, {1680945200u, (char)29}, {1697722416u, (char)30}, {1714499632u, (char)31},
            
            {1664430128u, (char)92} //upper case escape slash
        };

        //internal unsafe static readonly string LowUnicodeCharHexToCharTemp = "{" + String.Join("}, {", (new Dictionary<string, char>()
        //{
        //    //upper case hex
        //    {"0000",(char)0},{"0001",(char)1},{"0002",(char)2},{"0003",(char)3},{"0004",(char)4},{"0005",(char)5},{"0006",(char)6},{"0007",(char)7},{"0008",(char)8},{"0009",(char)9},{"000A",(char)10},{"000B",(char)11},{"000C",(char)12},{"000D",(char)13},{"000E",(char)14},{"000F",(char)15},
        //    {"0010",(char)16},{"0011",(char)17},{"0012",(char)18},{"0013",(char)19},{"0014",(char)20},{"0015",(char)21},{"0016",(char)22},{"0017",(char)23},{"0018",(char)24},{"0019",(char)25},{"001A",(char)26},{"001B",(char)27},{"001C",(char)28},{"001D",(char)29},{"001E",(char)30},{"001F",(char)31},

        //    {"0022",(char)34}, //quote,
        //    {"005C",(char)92}, //escape slash,

        //    //lower case hex
        //    {"000a",(char)10},{"000b",(char)11},{"000c",(char)12},{"000d",(char)13},{"000e",(char)14},{"000f",(char)15},
        //    {"001a",(char)26},{"001b",(char)27},{"001c",(char)28},{"001d",(char)29},{"001e",(char)30},{"001f",(char)31},

        //    {"005c",(char)92}, //escape slash,
        //}).ToDictionary(x => {
        //    fixed(char* pChars = x.Key.ToCharArray())
        //    {
        //        return Unsafe.ReadUnaligned<uint>(&pChars[2]);
        //    }
        //}, x => x.Value).Select(x => $"{x.Key}u, (char){(int)x.Value}")) + "}";

        //internal unsafe static readonly string LowUnicodeByteHexToCharTemp = "{" + String.Join("}, {", (new Dictionary<string, char>()
        //{
        //    //upper case hex
        //    {"0000",(char)0},{"0001",(char)1},{"0002",(char)2},{"0003",(char)3},{"0004",(char)4},{"0005",(char)5},{"0006",(char)6},{"0007",(char)7},{"0008",(char)8},{"0009",(char)9},{"000A",(char)10},{"000B",(char)11},{"000C",(char)12},{"000D",(char)13},{"000E",(char)14},{"000F",(char)15},
        //    {"0010",(char)16},{"0011",(char)17},{"0012",(char)18},{"0013",(char)19},{"0014",(char)20},{"0015",(char)21},{"0016",(char)22},{"0017",(char)23},{"0018",(char)24},{"0019",(char)25},{"001A",(char)26},{"001B",(char)27},{"001C",(char)28},{"001D",(char)29},{"001E",(char)30},{"001F",(char)31},

        //    {"0022",(char)34}, //quote,
        //    {"005C",(char)92}, //escape slash,

        //    //lower case hex
        //    {"000a",(char)10},{"000b",(char)11},{"000c",(char)12},{"000d",(char)13},{"000e",(char)14},{"000f",(char)15},
        //    {"001a",(char)26},{"001b",(char)27},{"001c",(char)28},{"001d",(char)29},{"001e",(char)30},{"001f",(char)31},

        //    {"005c",(char)92}, //escape slash,
        //}).ToDictionary(x =>
        //{
        //    fixed (byte* pBytes = encoding.GetBytes(x.Key))
        //    {
        //        return Unsafe.ReadUnaligned<uint>(pBytes);
        //    }
        //}, x => x.Value).Select(x => $"{x.Key}u, (char){(int)x.Value}")) + "}";
    }
}
