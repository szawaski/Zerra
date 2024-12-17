// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Buffers;
using System.Text;
using System.Collections.Generic;
using System.Linq;

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
            {3145776u, (char)0}, {3211312u, (char)1}, {3276848u, (char)2}, {3342384u, (char)3}, {3407920u, (char)4}, {3473456u, (char)5}, {3538992u, (char)6}, {3604528u, (char)7}, {3670064u, (char)8}, {3735600u, (char)9}, {6357040u, (char)10},{4259888u, (char)10}, {6422576u, (char)11},{4325424u, (char)11}, {6488112u, (char)12},{4390960u, (char)12}, {6553648u, (char)13},{4456496u, (char)13}, {6619184u, (char)14},{4522032u, (char)14}, {6684720u, (char)15},{4587568u, (char)15},
            {3145777u, (char)16}, {3211313u, (char)17}, {3276849u, (char)18}, {3342385u, (char)19}, {3407921u, (char)20}, {3473457u, (char)21}, {3538993u, (char)22}, {3604529u, (char)23}, {3670065u, (char)24}, {3735601u, (char)25}, {6357041u, (char)26},{4259889u, (char)26}, {6422577u, (char)27},{4325425u, (char)27}, {6488113u, (char)28},{4390961u, (char)28}, {6553649u, (char)29},{4456497u, (char)29}, {6619185u, (char)30},{4522033u, (char)30}, {6684721u, (char)31},{4587569u, (char)31},
            {3145778u, (char)32}, {3211314u, (char)33}, {3276850u, (char)34}, {3342386u, (char)35}, {3407922u, (char)36}, {3473458u, (char)37}, {3538994u, (char)38}, {3604530u, (char)39}, {3670066u, (char)40}, {3735602u, (char)41}, {6357042u, (char)42},{4259890u, (char)42}, {6422578u, (char)43},{4325426u, (char)43}, {6488114u, (char)44},{4390962u, (char)44}, {6553650u, (char)45},{4456498u, (char)45}, {6619186u, (char)46},{4522034u, (char)46}, {6684722u, (char)47},{4587570u, (char)47},
            {3145779u, (char)48}, {3211315u, (char)49}, {3276851u, (char)50}, {3342387u, (char)51}, {3407923u, (char)52}, {3473459u, (char)53}, {3538995u, (char)54}, {3604531u, (char)55}, {3670067u, (char)56}, {3735603u, (char)57}, {6357043u, (char)58},{4259891u, (char)58}, {6422579u, (char)59},{4325427u, (char)59}, {6488115u, (char)60},{4390963u, (char)60}, {6553651u, (char)61},{4456499u, (char)61}, {6619187u, (char)62},{4522035u, (char)62}, {6684723u, (char)63},{4587571u, (char)63},
            {3145780u, (char)64}, {3211316u, (char)65}, {3276852u, (char)66}, {3342388u, (char)67}, {3407924u, (char)68}, {3473460u, (char)69}, {3538996u, (char)70}, {3604532u, (char)71}, {3670068u, (char)72}, {3735604u, (char)73}, {6357044u, (char)74},{4259892u, (char)74}, {6422580u, (char)75},{4325428u, (char)75}, {6488116u, (char)76},{4390964u, (char)76}, {6553652u, (char)77},{4456500u, (char)77}, {6619188u, (char)78},{4522036u, (char)78}, {6684724u, (char)79},{4587572u, (char)79},
            {3145781u, (char)80}, {3211317u, (char)81}, {3276853u, (char)82}, {3342389u, (char)83}, {3407925u, (char)84}, {3473461u, (char)85}, {3538997u, (char)86}, {3604533u, (char)87}, {3670069u, (char)88}, {3735605u, (char)89}, {6357045u, (char)90},{4259893u, (char)90}, {6422581u, (char)91},{4325429u, (char)91}, {6488117u, (char)92},{4390965u, (char)92}, {6553653u, (char)93},{4456501u, (char)93}, {6619189u, (char)94},{4522037u, (char)94}, {6684725u, (char)95},{4587573u, (char)95},
            {3145782u, (char)96}, {3211318u, (char)97}, {3276854u, (char)98}, {3342390u, (char)99}, {3407926u, (char)100}, {3473462u, (char)101}, {3538998u, (char)102}, {3604534u, (char)103}, {3670070u, (char)104}, {3735606u, (char)105}, {6357046u, (char)106},{4259894u, (char)106}, {6422582u, (char)107},{4325430u, (char)107}, {6488118u, (char)108},{4390966u, (char)108}, {6553654u, (char)109},{4456502u, (char)109}, {6619190u, (char)110},{4522038u, (char)110}, {6684726u, (char)111},{4587574u, (char)111},
            {3145783u, (char)112}, {3211319u, (char)113}, {3276855u, (char)114}, {3342391u, (char)115}, {3407927u, (char)116}, {3473463u, (char)117}, {3538999u, (char)118}, {3604535u, (char)119}, {3670071u, (char)120}, {3735607u, (char)121}, {6357047u, (char)122},{4259895u, (char)122}, {6422583u, (char)123},{4325431u, (char)123}, {6488119u, (char)124},{4390967u, (char)124}, {6553655u, (char)125},{4456503u, (char)125}, {6619191u, (char)126},{4522039u, (char)126}, {6684727u, (char)127},{4587575u, (char)127},
            {3145784u, (char)128}, {3211320u, (char)129}, {3276856u, (char)130}, {3342392u, (char)131}, {3407928u, (char)132}, {3473464u, (char)133}, {3539000u, (char)134}, {3604536u, (char)135}, {3670072u, (char)136}, {3735608u, (char)137}, {6357048u, (char)138},{4259896u, (char)138}, {6422584u, (char)139},{4325432u, (char)139}, {6488120u, (char)140},{4390968u, (char)140}, {6553656u, (char)141},{4456504u, (char)141}, {6619192u, (char)142},{4522040u, (char)142}, {6684728u, (char)143},{4587576u, (char)143},
            {3145785u, (char)144}, {3211321u, (char)145}, {3276857u, (char)146}, {3342393u, (char)147}, {3407929u, (char)148}, {3473465u, (char)149}, {3539001u, (char)150}, {3604537u, (char)151}, {3670073u, (char)152}, {3735609u, (char)153}, {6357049u, (char)154},{4259897u, (char)154}, {6422585u, (char)155},{4325433u, (char)155}, {6488121u, (char)156},{4390969u, (char)156}, {6553657u, (char)157},{4456505u, (char)157}, {6619193u, (char)158},{4522041u, (char)158}, {6684729u, (char)159},{4587577u, (char)159},
            {3145825u, (char)160},{3145793u, (char)160}, {3211361u, (char)161},{3211329u, (char)161}, {3276897u, (char)162},{3276865u, (char)162}, {3342433u, (char)163},{3342401u, (char)163}, {3407969u, (char)164},{3407937u, (char)164}, {3473505u, (char)165},{3473473u, (char)165}, {3539041u, (char)166},{3539009u, (char)166}, {3604577u, (char)167},{3604545u, (char)167}, {3670113u, (char)168},{3670081u, (char)168}, {3735649u, (char)169},{3735617u, (char)169}, {6357089u, (char)170},{4259905u, (char)170}, {6422625u, (char)171},{4325441u, (char)171}, {6488161u, (char)172},{4390977u, (char)172}, {6553697u, (char)173},{4456513u, (char)173}, {6619233u, (char)174},{4522049u, (char)174}, {6684769u, (char)175},{4587585u, (char)175},
            {3145826u, (char)176},{3145794u, (char)176}, {3211362u, (char)177},{3211330u, (char)177}, {3276898u, (char)178},{3276866u, (char)178}, {3342434u, (char)179},{3342402u, (char)179}, {3407970u, (char)180},{3407938u, (char)180}, {3473506u, (char)181},{3473474u, (char)181}, {3539042u, (char)182},{3539010u, (char)182}, {3604578u, (char)183},{3604546u, (char)183}, {3670114u, (char)184},{3670082u, (char)184}, {3735650u, (char)185},{3735618u, (char)185}, {6357090u, (char)186},{4259906u, (char)186}, {6422626u, (char)187},{4325442u, (char)187}, {6488162u, (char)188},{4390978u, (char)188}, {6553698u, (char)189},{4456514u, (char)189}, {6619234u, (char)190},{4522050u, (char)190}, {6684770u, (char)191},{4587586u, (char)191},
            {3145827u, (char)192},{3145795u, (char)192}, {3211363u, (char)193},{3211331u, (char)193}, {3276899u, (char)194},{3276867u, (char)194}, {3342435u, (char)195},{3342403u, (char)195}, {3407971u, (char)196},{3407939u, (char)196}, {3473507u, (char)197},{3473475u, (char)197}, {3539043u, (char)198},{3539011u, (char)198}, {3604579u, (char)199},{3604547u, (char)199}, {3670115u, (char)200},{3670083u, (char)200}, {3735651u, (char)201},{3735619u, (char)201}, {6357091u, (char)202},{4259907u, (char)202}, {6422627u, (char)203},{4325443u, (char)203}, {6488163u, (char)204},{4390979u, (char)204}, {6553699u, (char)205},{4456515u, (char)205}, {6619235u, (char)206},{4522051u, (char)206}, {6684771u, (char)207},{4587587u, (char)207},
            {3145828u, (char)208},{3145796u, (char)208}, {3211364u, (char)209},{3211332u, (char)209}, {3276900u, (char)210},{3276868u, (char)210}, {3342436u, (char)211},{3342404u, (char)211}, {3407972u, (char)212},{3407940u, (char)212}, {3473508u, (char)213},{3473476u, (char)213}, {3539044u, (char)214},{3539012u, (char)214}, {3604580u, (char)215},{3604548u, (char)215}, {3670116u, (char)216},{3670084u, (char)216}, {3735652u, (char)217},{3735620u, (char)217}, {6357092u, (char)218},{4259908u, (char)218}, {6422628u, (char)219},{4325444u, (char)219}, {6488164u, (char)220},{4390980u, (char)220}, {6553700u, (char)221},{4456516u, (char)221}, {6619236u, (char)222},{4522052u, (char)222}, {6684772u, (char)223},{4587588u, (char)223},
            {3145829u, (char)224},{3145797u, (char)224}, {3211365u, (char)225},{3211333u, (char)225}, {3276901u, (char)226},{3276869u, (char)226}, {3342437u, (char)227},{3342405u, (char)227}, {3407973u, (char)228},{3407941u, (char)228}, {3473509u, (char)229},{3473477u, (char)229}, {3539045u, (char)230},{3539013u, (char)230}, {3604581u, (char)231},{3604549u, (char)231}, {3670117u, (char)232},{3670085u, (char)232}, {3735653u, (char)233},{3735621u, (char)233}, {6357093u, (char)234},{4259909u, (char)234}, {6422629u, (char)235},{4325445u, (char)235}, {6488165u, (char)236},{4390981u, (char)236}, {6553701u, (char)237},{4456517u, (char)237}, {6619237u, (char)238},{4522053u, (char)238}, {6684773u, (char)239},{4587589u, (char)239},
            {3145830u, (char)240},{3145798u, (char)240}, {3211366u, (char)241},{3211334u, (char)241}, {3276902u, (char)242},{3276870u, (char)242}, {3342438u, (char)243},{3342406u, (char)243}, {3407974u, (char)244},{3407942u, (char)244}, {3473510u, (char)245},{3473478u, (char)245}, {3539046u, (char)246},{3539014u, (char)246}, {3604582u, (char)247},{3604550u, (char)247}, {3670118u, (char)248},{3670086u, (char)248}, {3735654u, (char)249},{3735622u, (char)249}, {6357094u, (char)250},{4259910u, (char)250}, {6422630u, (char)251},{4325446u, (char)251}, {6488166u, (char)252},{4390982u, (char)252}, {6553702u, (char)253},{4456518u, (char)253}, {6619238u, (char)254},{4522054u, (char)254}, {6684774u, (char)255},{4587590u, (char)255},
        };

        //Unsafe.ReadUnaligned<uint>(pBytes)
        internal static readonly Dictionary<uint, char> LowUnicodeByteHexToChar = new()
        {
            {808464432u, (char)0}, {825241648u, (char)1}, {842018864u, (char)2}, {858796080u, (char)3}, {875573296u, (char)4}, {892350512u, (char)5}, {909127728u, (char)6}, {925904944u, (char)7}, {942682160u, (char)8}, {959459376u, (char)9}, {1630548016u, (char)10},{1093677104u, (char)10}, {1647325232u, (char)11},{1110454320u, (char)11}, {1664102448u, (char)12},{1127231536u, (char)12}, {1680879664u, (char)13},{1144008752u, (char)13}, {1697656880u, (char)14},{1160785968u, (char)14}, {1714434096u, (char)15},{1177563184u, (char)15},
            {808529968u, (char)16}, {825307184u, (char)17}, {842084400u, (char)18}, {858861616u, (char)19}, {875638832u, (char)20}, {892416048u, (char)21}, {909193264u, (char)22}, {925970480u, (char)23}, {942747696u, (char)24}, {959524912u, (char)25}, {1630613552u, (char)26},{1093742640u, (char)26}, {1647390768u, (char)27},{1110519856u, (char)27}, {1664167984u, (char)28},{1127297072u, (char)28}, {1680945200u, (char)29},{1144074288u, (char)29}, {1697722416u, (char)30},{1160851504u, (char)30}, {1714499632u, (char)31},{1177628720u, (char)31},
            {808595504u, (char)32}, {825372720u, (char)33}, {842149936u, (char)34}, {858927152u, (char)35}, {875704368u, (char)36}, {892481584u, (char)37}, {909258800u, (char)38}, {926036016u, (char)39}, {942813232u, (char)40}, {959590448u, (char)41}, {1630679088u, (char)42},{1093808176u, (char)42}, {1647456304u, (char)43},{1110585392u, (char)43}, {1664233520u, (char)44},{1127362608u, (char)44}, {1681010736u, (char)45},{1144139824u, (char)45}, {1697787952u, (char)46},{1160917040u, (char)46}, {1714565168u, (char)47},{1177694256u, (char)47},
            {808661040u, (char)48}, {825438256u, (char)49}, {842215472u, (char)50}, {858992688u, (char)51}, {875769904u, (char)52}, {892547120u, (char)53}, {909324336u, (char)54}, {926101552u, (char)55}, {942878768u, (char)56}, {959655984u, (char)57}, {1630744624u, (char)58},{1093873712u, (char)58}, {1647521840u, (char)59},{1110650928u, (char)59}, {1664299056u, (char)60},{1127428144u, (char)60}, {1681076272u, (char)61},{1144205360u, (char)61}, {1697853488u, (char)62},{1160982576u, (char)62}, {1714630704u, (char)63},{1177759792u, (char)63},
            {808726576u, (char)64}, {825503792u, (char)65}, {842281008u, (char)66}, {859058224u, (char)67}, {875835440u, (char)68}, {892612656u, (char)69}, {909389872u, (char)70}, {926167088u, (char)71}, {942944304u, (char)72}, {959721520u, (char)73}, {1630810160u, (char)74},{1093939248u, (char)74}, {1647587376u, (char)75},{1110716464u, (char)75}, {1664364592u, (char)76},{1127493680u, (char)76}, {1681141808u, (char)77},{1144270896u, (char)77}, {1697919024u, (char)78},{1161048112u, (char)78}, {1714696240u, (char)79},{1177825328u, (char)79},
            {808792112u, (char)80}, {825569328u, (char)81}, {842346544u, (char)82}, {859123760u, (char)83}, {875900976u, (char)84}, {892678192u, (char)85}, {909455408u, (char)86}, {926232624u, (char)87}, {943009840u, (char)88}, {959787056u, (char)89}, {1630875696u, (char)90},{1094004784u, (char)90}, {1647652912u, (char)91},{1110782000u, (char)91}, {1664430128u, (char)92},{1127559216u, (char)92}, {1681207344u, (char)93},{1144336432u, (char)93}, {1697984560u, (char)94},{1161113648u, (char)94}, {1714761776u, (char)95},{1177890864u, (char)95},
            {808857648u, (char)96}, {825634864u, (char)97}, {842412080u, (char)98}, {859189296u, (char)99}, {875966512u, (char)100}, {892743728u, (char)101}, {909520944u, (char)102}, {926298160u, (char)103}, {943075376u, (char)104}, {959852592u, (char)105}, {1630941232u, (char)106},{1094070320u, (char)106}, {1647718448u, (char)107},{1110847536u, (char)107}, {1664495664u, (char)108},{1127624752u, (char)108}, {1681272880u, (char)109},{1144401968u, (char)109}, {1698050096u, (char)110},{1161179184u, (char)110}, {1714827312u, (char)111},{1177956400u, (char)111},
            {808923184u, (char)112}, {825700400u, (char)113}, {842477616u, (char)114}, {859254832u, (char)115}, {876032048u, (char)116}, {892809264u, (char)117}, {909586480u, (char)118}, {926363696u, (char)119}, {943140912u, (char)120}, {959918128u, (char)121}, {1631006768u, (char)122},{1094135856u, (char)122}, {1647783984u, (char)123},{1110913072u, (char)123}, {1664561200u, (char)124},{1127690288u, (char)124}, {1681338416u, (char)125},{1144467504u, (char)125}, {1698115632u, (char)126},{1161244720u, (char)126}, {1714892848u, (char)127},{1178021936u, (char)127},
            {808988720u, (char)128}, {825765936u, (char)129}, {842543152u, (char)130}, {859320368u, (char)131}, {876097584u, (char)132}, {892874800u, (char)133}, {909652016u, (char)134}, {926429232u, (char)135}, {943206448u, (char)136}, {959983664u, (char)137}, {1631072304u, (char)138},{1094201392u, (char)138}, {1647849520u, (char)139},{1110978608u, (char)139}, {1664626736u, (char)140},{1127755824u, (char)140}, {1681403952u, (char)141},{1144533040u, (char)141}, {1698181168u, (char)142},{1161310256u, (char)142}, {1714958384u, (char)143},{1178087472u, (char)143},
            {809054256u, (char)144}, {825831472u, (char)145}, {842608688u, (char)146}, {859385904u, (char)147}, {876163120u, (char)148}, {892940336u, (char)149}, {909717552u, (char)150}, {926494768u, (char)151}, {943271984u, (char)152}, {960049200u, (char)153}, {1631137840u, (char)154},{1094266928u, (char)154}, {1647915056u, (char)155},{1111044144u, (char)155}, {1664692272u, (char)156},{1127821360u, (char)156}, {1681469488u, (char)157},{1144598576u, (char)157}, {1698246704u, (char)158},{1161375792u, (char)158}, {1715023920u, (char)159},{1178153008u, (char)159},
            {811675696u, (char)160},{809578544u, (char)160}, {828452912u, (char)161},{826355760u, (char)161}, {845230128u, (char)162},{843132976u, (char)162}, {862007344u, (char)163},{859910192u, (char)163}, {878784560u, (char)164},{876687408u, (char)164}, {895561776u, (char)165},{893464624u, (char)165}, {912338992u, (char)166},{910241840u, (char)166}, {929116208u, (char)167},{927019056u, (char)167}, {945893424u, (char)168},{943796272u, (char)168}, {962670640u, (char)169},{960573488u, (char)169}, {1633759280u, (char)170},{1094791216u, (char)170}, {1650536496u, (char)171},{1111568432u, (char)171}, {1667313712u, (char)172},{1128345648u, (char)172}, {1684090928u, (char)173},{1145122864u, (char)173}, {1700868144u, (char)174},{1161900080u, (char)174}, {1717645360u, (char)175},{1178677296u, (char)175},
            {811741232u, (char)176},{809644080u, (char)176}, {828518448u, (char)177},{826421296u, (char)177}, {845295664u, (char)178},{843198512u, (char)178}, {862072880u, (char)179},{859975728u, (char)179}, {878850096u, (char)180},{876752944u, (char)180}, {895627312u, (char)181},{893530160u, (char)181}, {912404528u, (char)182},{910307376u, (char)182}, {929181744u, (char)183},{927084592u, (char)183}, {945958960u, (char)184},{943861808u, (char)184}, {962736176u, (char)185},{960639024u, (char)185}, {1633824816u, (char)186},{1094856752u, (char)186}, {1650602032u, (char)187},{1111633968u, (char)187}, {1667379248u, (char)188},{1128411184u, (char)188}, {1684156464u, (char)189},{1145188400u, (char)189}, {1700933680u, (char)190},{1161965616u, (char)190}, {1717710896u, (char)191},{1178742832u, (char)191},
            {811806768u, (char)192},{809709616u, (char)192}, {828583984u, (char)193},{826486832u, (char)193}, {845361200u, (char)194},{843264048u, (char)194}, {862138416u, (char)195},{860041264u, (char)195}, {878915632u, (char)196},{876818480u, (char)196}, {895692848u, (char)197},{893595696u, (char)197}, {912470064u, (char)198},{910372912u, (char)198}, {929247280u, (char)199},{927150128u, (char)199}, {946024496u, (char)200},{943927344u, (char)200}, {962801712u, (char)201},{960704560u, (char)201}, {1633890352u, (char)202},{1094922288u, (char)202}, {1650667568u, (char)203},{1111699504u, (char)203}, {1667444784u, (char)204},{1128476720u, (char)204}, {1684222000u, (char)205},{1145253936u, (char)205}, {1700999216u, (char)206},{1162031152u, (char)206}, {1717776432u, (char)207},{1178808368u, (char)207},
            {811872304u, (char)208},{809775152u, (char)208}, {828649520u, (char)209},{826552368u, (char)209}, {845426736u, (char)210},{843329584u, (char)210}, {862203952u, (char)211},{860106800u, (char)211}, {878981168u, (char)212},{876884016u, (char)212}, {895758384u, (char)213},{893661232u, (char)213}, {912535600u, (char)214},{910438448u, (char)214}, {929312816u, (char)215},{927215664u, (char)215}, {946090032u, (char)216},{943992880u, (char)216}, {962867248u, (char)217},{960770096u, (char)217}, {1633955888u, (char)218},{1094987824u, (char)218}, {1650733104u, (char)219},{1111765040u, (char)219}, {1667510320u, (char)220},{1128542256u, (char)220}, {1684287536u, (char)221},{1145319472u, (char)221}, {1701064752u, (char)222},{1162096688u, (char)222}, {1717841968u, (char)223},{1178873904u, (char)223},
            {811937840u, (char)224},{809840688u, (char)224}, {828715056u, (char)225},{826617904u, (char)225}, {845492272u, (char)226},{843395120u, (char)226}, {862269488u, (char)227},{860172336u, (char)227}, {879046704u, (char)228},{876949552u, (char)228}, {895823920u, (char)229},{893726768u, (char)229}, {912601136u, (char)230},{910503984u, (char)230}, {929378352u, (char)231},{927281200u, (char)231}, {946155568u, (char)232},{944058416u, (char)232}, {962932784u, (char)233},{960835632u, (char)233}, {1634021424u, (char)234},{1095053360u, (char)234}, {1650798640u, (char)235},{1111830576u, (char)235}, {1667575856u, (char)236},{1128607792u, (char)236}, {1684353072u, (char)237},{1145385008u, (char)237}, {1701130288u, (char)238},{1162162224u, (char)238}, {1717907504u, (char)239},{1178939440u, (char)239},
            {812003376u, (char)240},{809906224u, (char)240}, {828780592u, (char)241},{826683440u, (char)241}, {845557808u, (char)242},{843460656u, (char)242}, {862335024u, (char)243},{860237872u, (char)243}, {879112240u, (char)244},{877015088u, (char)244}, {895889456u, (char)245},{893792304u, (char)245}, {912666672u, (char)246},{910569520u, (char)246}, {929443888u, (char)247},{927346736u, (char)247}, {946221104u, (char)248},{944123952u, (char)248}, {962998320u, (char)249},{960901168u, (char)249}, {1634086960u, (char)250},{1095118896u, (char)250}, {1650864176u, (char)251},{1111896112u, (char)251}, {1667641392u, (char)252},{1128673328u, (char)252}, {1684418608u, (char)253},{1145450544u, (char)253}, {1701195824u, (char)254},{1162227760u, (char)254}, {1717973040u, (char)255},{1179004976u, (char)255},
        };

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

