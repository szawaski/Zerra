﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Buffers;
using System.Text;
using System.Collections.Generic;

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
                        }
                        _ = encoding.GetBytes(pValue, value.Length, &pBytes[1], bytes.Length - 1);
                        if (quoteAndColon)
                        {
                            pBytes[bytes.Length - 2] = quoteByte;
                            pBytes[bytes.Length - 1] = colonByte;
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

                                var code = LowUnicodeIntToEncodedHex[c];
                                fixed (char* pCode = code)
                                {
                                    bufferIndex += encoding.GetBytes(pCode, code.Length, pEscapeBuffer, bufferIndex - escapeBuffer.Length);
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
                        }
                        Buffer.MemoryCopy(pEscapeBuffer, pBytes, bytes.Length - 1, bufferIndex);
                        if (quoteAndColon)
                        {
                            pBytes[bytes.Length - 2] = quoteByte;
                            pBytes[bytes.Length - 1] = colonByte;
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

                                var code = LowUnicodeIntToEncodedHex[c];
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

        internal static readonly string[] LowUnicodeIntToEncodedHex =
        [
            "\\u0000","\\u0001","\\u0002","\\u0003","\\u0004","\\u0005","\\u0006","\\u0007","\\u0008","\\u0009","\\u000A","\\u000B","\\u000C","\\u000D","\\u000E","\\u000F",
            "\\u0010","\\u0011","\\u0012","\\u0013","\\u0014","\\u0015","\\u0016","\\u0017","\\u0018","\\u0019","\\u001A","\\u001B","\\u001C","\\u001D","\\u001E","\\u001F",
            "\\u0020","\\u0021","\\u0022","\\u0023","\\u0024","\\u0025","\\u0026","\\u0027","\\u0028","\\u0029","\\u002A","\\u002B","\\u002C","\\u002D","\\u002E","\\u002F",
            "\\u0030","\\u0031","\\u0032","\\u0033","\\u0034","\\u0035","\\u0036","\\u0037","\\u0038","\\u0039","\\u003A","\\u003B","\\u003C","\\u003D","\\u003E","\\u003F",
            "\\u0040","\\u0041","\\u0042","\\u0043","\\u0044","\\u0045","\\u0046","\\u0047","\\u0048","\\u0049","\\u004A","\\u004B","\\u004C","\\u004D","\\u004E","\\u004F",
            "\\u0050","\\u0051","\\u0052","\\u0053","\\u0054","\\u0055","\\u0056","\\u0057","\\u0058","\\u0059","\\u005A","\\u005B","\\u005C","\\u005D","\\u005E","\\u005F",
            "\\u0060","\\u0061","\\u0062","\\u0063","\\u0064","\\u0065","\\u0066","\\u0067","\\u0068","\\u0069","\\u006A","\\u006B","\\u006C","\\u006D","\\u006E","\\u006F",
            "\\u0070","\\u0071","\\u0072","\\u0073","\\u0074","\\u0075","\\u0076","\\u0077","\\u0078","\\u0079","\\u007A","\\u007B","\\u007C","\\u007D","\\u007E","\\u007F",
            "\\u0080","\\u0081","\\u0082","\\u0083","\\u0084","\\u0085","\\u0086","\\u0087","\\u0088","\\u0089","\\u008A","\\u008B","\\u008C","\\u008D","\\u008E","\\u008F",
            "\\u0090","\\u0091","\\u0092","\\u0093","\\u0094","\\u0095","\\u0096","\\u0097","\\u0098","\\u0099","\\u009A","\\u009B","\\u009C","\\u009D","\\u009E","\\u009F",
            "\\u00A0","\\u00A1","\\u00A2","\\u00A3","\\u00A4","\\u00A5","\\u00A6","\\u00A7","\\u00A8","\\u00A9","\\u00AA","\\u00AB","\\u00AC","\\u00AD","\\u00AE","\\u00AF",
            "\\u00B0","\\u00B1","\\u00B2","\\u00B3","\\u00B4","\\u00B5","\\u00B6","\\u00B7","\\u00B8","\\u00B9","\\u00BA","\\u00BB","\\u00BC","\\u00BD","\\u00BE","\\u00BF",
            "\\u00C0","\\u00C1","\\u00C2","\\u00C3","\\u00C4","\\u00C5","\\u00C6","\\u00C7","\\u00C8","\\u00C9","\\u00CA","\\u00CB","\\u00CC","\\u00CD","\\u00CE","\\u00CF",
            "\\u00D0","\\u00D1","\\u00D2","\\u00D3","\\u00D4","\\u00D5","\\u00D6","\\u00D7","\\u00D8","\\u00D9","\\u00DA","\\u00DB","\\u00DC","\\u00DD","\\u00DE","\\u00DF",
            "\\u00E0","\\u00E1","\\u00E2","\\u00E3","\\u00E4","\\u00E5","\\u00E6","\\u00E7","\\u00E8","\\u00E9","\\u00EA","\\u00EB","\\u00EC","\\u00ED","\\u00EE","\\u00EF",
            "\\u00F0","\\u00F1","\\u00F2","\\u00F3","\\u00F4","\\u00F5","\\u00F6","\\u00F7","\\u00F8","\\u00F9","\\u00FA","\\u00FB","\\u00FC","\\u00FD","\\u00FE","\\u00FF"
        ];
        internal static readonly Dictionary<string, char> LowUnicodeHexToChar = new()
        {
            //upper case hex
            {"0000",(char)0},{"0001",(char)1},{"0002",(char)2},{"0003",(char)3},{"0004",(char)4},{"0005",(char)5},{"0006",(char)6},{"0007",(char)7},{"0008",(char)8},{"0009",(char)9},{"000A",(char)10},{"000B",(char)11},{"000C",(char)12},{"000D",(char)13},{"000E",(char)14},{"000F",(char)15},
            {"0010",(char)16},{"0011",(char)17},{"0012",(char)18},{"0013",(char)19},{"0014",(char)20},{"0015",(char)21},{"0016",(char)22},{"0017",(char)23},{"0018",(char)24},{"0019",(char)25},{"001A",(char)26},{"001B",(char)27},{"001C",(char)28},{"001D",(char)29},{"001E",(char)30},{"001F",(char)31},
            {"0020",(char)32},{"0021",(char)33},{"0022",(char)34},{"0023",(char)35},{"0024",(char)36},{"0025",(char)37},{"0026",(char)38},{"0027",(char)39},{"0028",(char)40},{"0029",(char)41},{"002A",(char)42},{"002B",(char)43},{"002C",(char)44},{"002D",(char)45},{"002E",(char)46},{"002F",(char)47},
            {"0030",(char)48},{"0031",(char)49},{"0032",(char)50},{"0033",(char)51},{"0034",(char)52},{"0035",(char)53},{"0036",(char)54},{"0037",(char)55},{"0038",(char)56},{"0039",(char)57},{"003A",(char)58},{"003B",(char)59},{"003C",(char)60},{"003D",(char)61},{"003E",(char)62},{"003F",(char)63},
            {"0040",(char)64},{"0041",(char)65},{"0042",(char)66},{"0043",(char)67},{"0044",(char)68},{"0045",(char)69},{"0046",(char)70},{"0047",(char)71},{"0048",(char)72},{"0049",(char)73},{"004A",(char)74},{"004B",(char)75},{"004C",(char)76},{"004D",(char)77},{"004E",(char)78},{"004F",(char)79},
            {"0050",(char)80},{"0051",(char)81},{"0052",(char)82},{"0053",(char)83},{"0054",(char)84},{"0055",(char)85},{"0056",(char)86},{"0057",(char)87},{"0058",(char)88},{"0059",(char)89},{"005A",(char)90},{"005B",(char)91},{"005C",(char)92},{"005D",(char)93},{"005E",(char)94},{"005F",(char)95},
            {"0060",(char)96},{"0061",(char)97},{"0062",(char)98},{"0063",(char)99},{"0064",(char)100},{"0065",(char)101},{"0066",(char)102},{"0067",(char)103},{"0068",(char)104},{"0069",(char)105},{"006A",(char)106},{"006B",(char)107},{"006C",(char)108},{"006D",(char)109},{"006E",(char)110},{"006F",(char)111},
            {"0070",(char)112},{"0071",(char)113},{"0072",(char)114},{"0073",(char)115},{"0074",(char)116},{"0075",(char)117},{"0076",(char)118},{"0077",(char)119},{"0078",(char)120},{"0079",(char)121},{"007A",(char)122},{"007B",(char)123},{"007C",(char)124},{"007D",(char)125},{"007E",(char)126},{"007F",(char)127},
            {"0080",(char)128},{"0081",(char)129},{"0082",(char)130},{"0083",(char)131},{"0084",(char)132},{"0085",(char)133},{"0086",(char)134},{"0087",(char)135},{"0088",(char)136},{"0089",(char)137},{"008A",(char)138},{"008B",(char)139},{"008C",(char)140},{"008D",(char)141},{"008E",(char)142},{"008F",(char)142},
            {"0090",(char)144},{"0091",(char)145},{"0092",(char)146},{"0093",(char)147},{"0094",(char)148},{"0095",(char)149},{"0096",(char)150},{"0097",(char)151},{"0098",(char)152},{"0099",(char)153},{"009A",(char)154},{"009B",(char)155},{"009C",(char)156},{"009D",(char)157},{"009E",(char)158},{"009F",(char)159},
            {"00A0",(char)160},{"00A1",(char)161},{"00A2",(char)162},{"00A3",(char)163},{"00A4",(char)164},{"00A5",(char)165},{"00A6",(char)166},{"00A7",(char)167},{"00A8",(char)168},{"00A9",(char)169},{"00AA",(char)170},{"00AB",(char)171},{"00AC",(char)172},{"00AD",(char)173},{"00AE",(char)174},{"00AF",(char)175},
            {"00B0",(char)176},{"00B1",(char)177},{"00B2",(char)178},{"00B3",(char)179},{"00B4",(char)180},{"00B5",(char)181},{"00B6",(char)182},{"00B7",(char)183},{"00B8",(char)184},{"00B9",(char)185},{"00BA",(char)186},{"00BB",(char)187},{"00BC",(char)188},{"00BD",(char)189},{"00BE",(char)190},{"00BF",(char)191},
            {"00C0",(char)192},{"00C1",(char)193},{"00C2",(char)194},{"00C3",(char)195},{"00C4",(char)196},{"00C5",(char)197},{"00C6",(char)198},{"00C7",(char)199},{"00C8",(char)200},{"00C9",(char)201},{"00CA",(char)202},{"00CB",(char)203},{"00CC",(char)204},{"00CD",(char)205},{"00CE",(char)206},{"00CF",(char)207},
            {"00D0",(char)208},{"00D1",(char)209},{"00D2",(char)210},{"00D3",(char)211},{"00D4",(char)212},{"00D5",(char)213},{"00D6",(char)214},{"00D7",(char)215},{"00D8",(char)216},{"00D9",(char)217},{"00DA",(char)218},{"00DB",(char)219},{"00DC",(char)220},{"00DD",(char)221},{"00DE",(char)222},{"00DF",(char)223},
            {"00E0",(char)224},{"00E1",(char)225},{"00E2",(char)226},{"00E3",(char)227},{"00E4",(char)228},{"00E5",(char)229},{"00E6",(char)230},{"00E7",(char)231},{"00E8",(char)232},{"00E9",(char)233},{"00EA",(char)234},{"00EB",(char)235},{"00EC",(char)236},{"00ED",(char)237},{"00EE",(char)238},{"00EF",(char)239},
            {"00F0",(char)240},{"00F1",(char)241},{"00F2",(char)242},{"00F3",(char)243},{"00F4",(char)244},{"00F5",(char)245},{"00F6",(char)246},{"00F7",(char)247},{"00F8",(char)248},{"00F9",(char)249},{"00FA",(char)250},{"00FB",(char)251},{"00FC",(char)252},{"00FD",(char)253},{"00FE",(char)254},{"00FF",(char)255},

            //lower case hex
            {"000a",(char)10},{"000b",(char)11},{"000c",(char)12},{"000d",(char)13},{"000e",(char)14},{"000f",(char)15},
            {"001a",(char)26},{"001b",(char)27},{"001c",(char)28},{"001d",(char)29},{"001e",(char)30},{"001f",(char)31},
            {"002a",(char)42},{"002b",(char)43},{"002c",(char)44},{"002d",(char)45},{"002e",(char)46},{"002f",(char)47},
            {"003a",(char)58},{"003b",(char)59},{"003c",(char)60},{"003d",(char)61},{"003e",(char)62},{"003f",(char)63},
            {"004a",(char)74},{"004b",(char)75},{"004c",(char)76},{"004d",(char)77},{"004e",(char)78},{"004f",(char)79},
            {"005a",(char)90},{"005b",(char)91},{"005c",(char)92},{"005d",(char)93},{"005e",(char)94},{"005f",(char)95},
            {"006a",(char)106},{"006b",(char)107},{"006c",(char)108},{"006d",(char)109},{"006e",(char)110},{"006f",(char)111},
            {"007a",(char)122},{"007b",(char)123},{"007c",(char)124},{"007d",(char)125},{"007e",(char)126},{"007f",(char)127},
            {"008a",(char)138},{"008b",(char)139},{"008c",(char)140},{"008d",(char)141},{"008e",(char)142},{"008f",(char)142},
            {"009a",(char)154},{"009b",(char)155},{"009c",(char)156},{"009d",(char)157},{"009e",(char)158},{"009f",(char)159},
            {"00a0",(char)160},{"00a1",(char)161},{"00a2",(char)162},{"00a3",(char)163},{"00a4",(char)164},{"00a5",(char)165},{"00a6",(char)166},{"00a7",(char)167},{"00a8",(char)168},{"00a9",(char)169},{"00aa",(char)170},{"00ab",(char)171},{"00ac",(char)172},{"00ad",(char)173},{"00ae",(char)174},{"00af",(char)175},
            {"00b0",(char)176},{"00b1",(char)177},{"00b2",(char)178},{"00b3",(char)179},{"00b4",(char)180},{"00b5",(char)181},{"00b6",(char)182},{"00b7",(char)183},{"00b8",(char)184},{"00b9",(char)185},{"00ba",(char)186},{"00bb",(char)187},{"00bc",(char)188},{"00bd",(char)189},{"00be",(char)190},{"00bf",(char)191},
            {"00c0",(char)192},{"00c1",(char)193},{"00c2",(char)194},{"00c3",(char)195},{"00c4",(char)196},{"00c5",(char)197},{"00c6",(char)198},{"00c7",(char)199},{"00c8",(char)200},{"00c9",(char)201},{"00ca",(char)202},{"00cb",(char)203},{"00cc",(char)204},{"00cd",(char)205},{"00ce",(char)206},{"00cf",(char)207},
            {"00d0",(char)208},{"00d1",(char)209},{"00d2",(char)210},{"00d3",(char)211},{"00d4",(char)212},{"00d5",(char)213},{"00d6",(char)214},{"00d7",(char)215},{"00d8",(char)216},{"00d9",(char)217},{"00da",(char)218},{"00db",(char)219},{"00dc",(char)220},{"00dd",(char)221},{"00de",(char)222},{"00df",(char)223},
            {"00e0",(char)224},{"00e1",(char)225},{"00e2",(char)226},{"00e3",(char)227},{"00e4",(char)228},{"00e5",(char)229},{"00e6",(char)230},{"00e7",(char)231},{"00e8",(char)232},{"00e9",(char)233},{"00ea",(char)234},{"00eb",(char)235},{"00ec",(char)236},{"00ed",(char)237},{"00ee",(char)238},{"00ef",(char)239},
            {"00f0",(char)240},{"00f1",(char)241},{"00f2",(char)242},{"00f3",(char)243},{"00f4",(char)244},{"00f5",(char)245},{"00f6",(char)246},{"00f7",(char)247},{"00f8",(char)248},{"00f9",(char)249},{"00fa",(char)250},{"00fb",(char)251},{"00fc",(char)252},{"00fd",(char)253},{"00fe",(char)254},{"00ff",(char)255},
        };
    }
}
