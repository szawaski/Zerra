﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;
using Zerra.Buffers;

namespace Zerra.Serialization.Json.IO
{
    public ref partial struct JsonWriter
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
        private const byte quoteByte = (byte)'"';
        private const byte colonByte = (byte)':';
        private const byte commaByte = (byte)',';
        private const byte escapeByte = (byte)'\\';

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
        private const byte dotByte = (byte)'.';
        private const byte minusByte = (byte)'-';
        private const byte plusByte = (byte)'+';
        private const byte zUpperByte = (byte)'Z';
        private const byte tUpperByte = (byte)'T';

        private const byte nByte = (byte)'n';
        private const byte uByte = (byte)'u';
        private const byte lByte = (byte)'l';
        private const byte tByte = (byte)'t';
        private const byte rByte = (byte)'r';
        private const byte eByte = (byte)'e';
        private const byte fByte = (byte)'f';
        private const byte aByte = (byte)'a';
        private const byte sByte = (byte)'s';

#if DEBUG
        public static bool Testing = false;

        private static bool Alternate = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Skip()
        {
            if (!JsonWriter.Testing)
                return false;
            if (JsonWriter.Alternate)
            {
                JsonWriter.Alternate = false;
                return false;
            }
            else
            {
                JsonWriter.Alternate = true;
                return true;
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(byte value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteUInt64Chars(value);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(sbyte value, out int sizeNeeded)
        {
            sizeNeeded = 3;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteInt64Chars(value);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(short value, out int sizeNeeded)
        {
            sizeNeeded = 6;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteInt64Chars(value);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(ushort value, out int sizeNeeded)
        {
            sizeNeeded = 5;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteUInt64Chars(value);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(int value, out int sizeNeeded)
        {
            sizeNeeded = 11;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteInt64Chars(value);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(uint value, out int sizeNeeded)
        {
            sizeNeeded = 10;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteUInt64Chars(value);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(long value, out int sizeNeeded)
        {
            sizeNeeded = 20;
            if (length - position < sizeNeeded && !Grow(sizeNeeded))
                return false;

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteInt64Chars(value);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(ulong value, out int sizeNeeded)
        {
            sizeNeeded = 20;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteUInt64Chars(value);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(float value, out int sizeNeeded)
        {
            sizeNeeded = 16; //min
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteLowerChars(value.ToString());
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(double value, out int sizeNeeded)
        {
            sizeNeeded = 32; //min
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteLowerChars(value.ToString());
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(decimal value, out int sizeNeeded)
        {
            sizeNeeded = 31;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteLowerChars(value.ToString());
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteQuoted(string? value, out int sizeNeeded)
        {
            if (value is null)
            {
                sizeNeeded = 0;
                return true;
            }
            if (value.Length == 0)
            {
                sizeNeeded = 2;
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (Skip())
                    return false;
#endif

                if (useBytes)
                {
                    bufferBytes[position++] = quoteByte;
                    bufferBytes[position++] = quoteByte;
                }
                else
                {
                    bufferChars[position++] = '"';
                    bufferChars[position++] = '"';
                }
                return true;
            }

            if (useBytes)
            {
                sizeNeeded = encoding.GetMaxByteCount(value.Length) + 2;
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (Skip())
                    return false;
#endif

                bufferBytes[position++] = quoteByte;
                fixed (char* pSource = value)
                fixed (byte* pBuffer = &bufferBytes[position])
                {
                    position += encoding.GetBytes(pSource, value.Length, pBuffer, bufferBytes.Length - position);
                }
                bufferBytes[position++] = quoteByte;

                return true;
            }
            else
            {
                sizeNeeded = value.Length + 2;
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (Skip())
                    return false;
#endif

                bufferChars[position++] = '"';
                fixed (char* pSource = value, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, value.Length * 2);
                }
                position += value.Length;
                bufferChars[position++] = '"';

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteLowerChars(string value)
        {
            //This can only be used for chars < 128
            if (useBytes)
            {
                fixed (char* pSource = value)
                fixed (byte* pBuffer = &bufferBytes[position])
                {
                    position += encoding.GetBytes(pSource, value.Length, pBuffer, bufferBytes.Length - position);
                }
            }
            else
            {
                fixed (char* pSource = value, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, value.Length * 2);
                }
                position += value.Length;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateTime value, out int sizeNeeded)
        {
            //ISO8601
            //"yyyy-MM-ddTHH:mm:ss.fffffff+00:00"
            sizeNeeded = 35;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;

                if (value.Year < 10)
                    bufferBytes[position++] = zeroByte;
                if (value.Year < 100)
                    bufferBytes[position++] = zeroByte;
                if (value.Year < 1000)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Year, bufferBytes.Slice(position), out var written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Month < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Month, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Day < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Day, bufferBytes.Slice(position), out written);
                position += written;

                bufferBytes[position++] = tUpperByte;

                if (value.Hour < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Hour, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Minute < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Minute, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Second < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Second, bufferBytes.Slice(position), out written);
                position += written;

                var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000;
                if (fraction > 0)
                {
                    bufferBytes[position++] = dotByte;
                    if (fraction < 10)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 100)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 1000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 10000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 100000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 1000000)
                        bufferBytes[position++] = zeroByte;
                    while (fraction % 10 == 0)
                        fraction /= 10;
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(fraction, bufferBytes.Slice(position), out written);
                    position += written;
                }

                switch (value.Kind)
                {
                    case DateTimeKind.Utc:
                        {
                            bufferBytes[position++] = zUpperByte;
                            break;
                        }
                    case DateTimeKind.Local:
                        {
                            var offset = (DateTimeOffset)value;
                            if (offset.Offset.Hours < 0)
                                bufferBytes[position++] = minusByte;
                            else
                                bufferBytes[position++] = plusByte;
                            if (offset.Offset.Hours < 10)
                                bufferBytes[position++] = zeroByte;
                            _ = System.Buffers.Text.Utf8Formatter.TryFormat(offset.Offset.Hours < 0 ? -offset.Offset.Hours : offset.Offset.Hours, bufferBytes.Slice(position), out written);
                            position += written;
                            bufferBytes[position++] = colonByte;

                            if (offset.Offset.Minutes < 10)
                                bufferBytes[position++] = zeroByte;
                            _ = System.Buffers.Text.Utf8Formatter.TryFormat(offset.Offset.Minutes, bufferBytes.Slice(position), out written);
                            position += written;
                            break;
                        }
                    case DateTimeKind.Unspecified:
                        {
                            //nothing
                            break;
                        }
                    default: throw new NotImplementedException();
                }

                bufferBytes[position++] = quoteByte;

                return true;
            }
            else
            {
                bufferChars[position++] = '"';

                if (value.Year < 10)
                    bufferChars[position++] = '0';
                if (value.Year < 100)
                    bufferChars[position++] = '0';
                if (value.Year < 1000)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Year);
                bufferChars[position++] = '-';

                if (value.Month < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Month);
                bufferChars[position++] = '-';

                if (value.Day < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Day);

                bufferChars[position++] = 'T';

                if (value.Hour < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Hour);
                bufferChars[position++] = ':';

                if (value.Minute < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Minute);
                bufferChars[position++] = ':';

                if (value.Second < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Second);

                var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000;
                if (fraction > 0)
                {
                    bufferChars[position++] = '.';
                    if (fraction < 10)
                        bufferChars[position++] = '0';
                    if (fraction < 100)
                        bufferChars[position++] = '0';
                    if (fraction < 1000)
                        bufferChars[position++] = '0';
                    if (fraction < 10000)
                        bufferChars[position++] = '0';
                    if (fraction < 100000)
                        bufferChars[position++] = '0';
                    if (fraction < 1000000)
                        bufferChars[position++] = '0';
                    while (fraction % 10 == 0)
                        fraction /= 10;
                    WriteInt64Chars(fraction);
                }

                switch (value.Kind)
                {
                    case DateTimeKind.Utc:
                        {
                            bufferChars[position++] = 'Z';
                            break;
                        }
                    case DateTimeKind.Local:
                        {
                            var offset = (DateTimeOffset)value;
                            if (offset.Offset.Hours < 0)
                                bufferChars[position++] = '-';
                            else
                                bufferChars[position++] = '+';
                            if (offset.Offset.Hours < 10)
                                bufferChars[position++] = '0';
                            WriteInt64Chars(offset.Offset.Hours < 0 ? -offset.Offset.Hours : offset.Offset.Hours);
                            bufferChars[position++] = ':';

                            if (offset.Offset.Minutes < 10)
                                bufferChars[position++] = '0';
                            WriteInt64Chars(offset.Offset.Minutes);
                            break;
                        }
                    case DateTimeKind.Unspecified:
                        {
                            //nothing
                            break;
                        }
                    default: throw new NotImplementedException();
                }

                bufferChars[position++] = '"';
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateTimeOffset value, out int sizeNeeded)
        {
            //ISO8601
            //"yyyy-MM-ddTHH:mm:ss.fffffff+00:00"
            sizeNeeded = 35;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;

                if (value.Year < 10)
                    bufferBytes[position++] = zeroByte;
                if (value.Year < 100)
                    bufferBytes[position++] = zeroByte;
                if (value.Year < 1000)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Year, bufferBytes.Slice(position), out var written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Month < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Month, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Day < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Day, bufferBytes.Slice(position), out written);
                position += written;

                bufferBytes[position++] = tUpperByte;

                if (value.Hour < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Hour, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Minute < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Minute, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Second < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Second, bufferBytes.Slice(position), out written);
                position += written;

                var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000;
                if (fraction > 0)
                {
                    bufferBytes[position++] = dotByte;
                    if (fraction < 10)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 100)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 1000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 10000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 100000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 1000000)
                        bufferBytes[position++] = zeroByte;
                    while (fraction % 10 == 0)
                        fraction /= 10;
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(fraction, bufferBytes.Slice(position), out written);
                    position += written;
                }

                if (value.Offset.Hours < 0)
                    bufferBytes[position++] = minusByte;
                else
                    bufferBytes[position++] = plusByte;
                if (value.Offset.Hours < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Offset.Hours < 0 ? -value.Offset.Hours : value.Offset.Hours, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Offset.Minutes < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Offset.Minutes, bufferBytes.Slice(position), out written);
                position += written;

                bufferBytes[position++] = quoteByte;

                return true;
            }
            else
            {
                bufferChars[position++] = '"';

                if (value.Year < 10)
                    bufferChars[position++] = '0';
                if (value.Year < 100)
                    bufferChars[position++] = '0';
                if (value.Year < 1000)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Year);
                bufferChars[position++] = '-';

                if (value.Month < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Month);
                bufferChars[position++] = '-';

                if (value.Day < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Day);

                bufferChars[position++] = 'T';

                if (value.Hour < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Hour);
                bufferChars[position++] = ':';

                if (value.Minute < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Minute);
                bufferChars[position++] = ':';

                if (value.Second < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Second);

                var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000;
                if (fraction > 0)
                {
                    bufferChars[position++] = '.';
                    if (fraction < 10)
                        bufferChars[position++] = '0';
                    if (fraction < 100)
                        bufferChars[position++] = '0';
                    if (fraction < 1000)
                        bufferChars[position++] = '0';
                    if (fraction < 10000)
                        bufferChars[position++] = '0';
                    if (fraction < 100000)
                        bufferChars[position++] = '0';
                    if (fraction < 1000000)
                        bufferChars[position++] = '0';
                    while (fraction % 10 == 0)
                        fraction /= 10;
                    WriteInt64Chars(fraction);
                }

                if (value.Offset.Hours < 0)
                    bufferChars[position++] = '-';
                else
                    bufferChars[position++] = '+';
                if (value.Offset.Hours < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Offset.Hours < 0 ? -value.Offset.Hours : value.Offset.Hours);
                bufferChars[position++] = ':';

                if (value.Offset.Minutes < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Offset.Minutes);

                bufferChars[position++] = '"';

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(TimeSpan value, out int sizeNeeded)
        {
            //ISO8601
            //(-)dddddddd.HH:mm:ss.fffffff
            sizeNeeded = 28;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;

                if (value.Ticks < 0)
                    bufferBytes[position++] = minusByte;

                if (value.Days > 0)
                {
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Days, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = dotByte;
                }
                else if (value.Days < 0)
                {
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(-value.Days, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = dotByte;
                }

                if (value.Hours > -1)
                {
                    if (value.Hours < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Hours, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = colonByte;
                }
                else
                {
                    if (-value.Hours < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(-value.Hours, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = colonByte;
                }

                if (value.Minutes > -1)
                {
                    if (value.Minutes < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Minutes, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = colonByte;
                }
                else
                {
                    if (-value.Minutes < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(-value.Minutes, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = colonByte;
                }

                if (value.Seconds > -1)
                {
                    if (value.Seconds < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Seconds, bufferBytes.Slice(position), out var written);
                    position += written;
                }
                else
                {
                    if (-value.Seconds < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(-value.Seconds, bufferBytes.Slice(position), out var written);
                    position += written;
                }

                long fraction;
                if (value.Ticks > -1)
                    fraction = value.Ticks - (value.Ticks / 10000000) * 10000000;
                else
                    fraction = -value.Ticks - (-value.Ticks / 10000000) * 10000000;
                if (fraction > 0)
                {
                    bufferBytes[position++] = dotByte;
                    if (fraction < 10)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 100)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 1000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 10000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 100000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 1000000)
                        bufferBytes[position++] = zeroByte;
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(fraction, bufferBytes.Slice(position), out var written);
                    position += written;
                }

                bufferBytes[position++] = quoteByte;

                return true;
            }
            else
            {
                bufferChars[position++] = '"';

                if (value.Ticks < 0)
                    bufferChars[position++] = '-';

                if (value.Days > 0)
                {
                    WriteInt64Chars(value.Days);
                    bufferChars[position++] = '.';
                }
                else if (value.Days < 0)
                {
                    WriteInt64Chars(-value.Days);
                    bufferChars[position++] = '.';
                }

                if (value.Hours > -1)
                {
                    if (value.Hours < 10)
                        bufferChars[position++] = '0';
                    WriteInt64Chars(value.Hours);
                    bufferChars[position++] = ':';
                }
                else
                {
                    if (-value.Hours < 10)
                        bufferChars[position++] = '0';
                    WriteInt64Chars(-value.Hours);
                    bufferChars[position++] = ':';
                }

                if (value.Minutes > -1)
                {
                    if (value.Minutes < 10)
                        bufferChars[position++] = '0';
                    WriteInt64Chars(value.Minutes);
                    bufferChars[position++] = ':';
                }
                else
                {
                    if (-value.Minutes < 10)
                        bufferChars[position++] = '0';
                    WriteInt64Chars(-value.Minutes);
                    bufferChars[position++] = ':';
                }

                if (value.Seconds > -1)
                {
                    if (value.Seconds < 10)
                        bufferChars[position++] = '0';
                    WriteInt64Chars(value.Seconds);
                }
                else
                {
                    if (-value.Seconds < 10)
                        bufferChars[position++] = '0';
                    WriteInt64Chars(-value.Seconds);
                }

                long fraction;
                if (value.Ticks > -1)
                    fraction = value.Ticks - (value.Ticks / 10000000) * 10000000;
                else
                    fraction = -value.Ticks - (-value.Ticks / 10000000) * 10000000;
                if (fraction > 0)
                {
                    bufferChars[position++] = '.';
                    if (fraction < 10)
                        bufferChars[position++] = '0';
                    if (fraction < 100)
                        bufferChars[position++] = '0';
                    if (fraction < 1000)
                        bufferChars[position++] = '0';
                    if (fraction < 10000)
                        bufferChars[position++] = '0';
                    if (fraction < 100000)
                        bufferChars[position++] = '0';
                    if (fraction < 1000000)
                        bufferChars[position++] = '0';
                    WriteInt64Chars(fraction);
                }

                bufferChars[position++] = '"';

                return true;
            }
        }

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateOnly value, out int sizeNeeded)
        {
            //ISO8601
            //"yyyy-MM-dd"
            sizeNeeded = 12;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif


            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;

                if (value.Year < 10)
                    bufferBytes[position++] = zeroByte;
                if (value.Year < 100)
                    bufferBytes[position++] = zeroByte;
                if (value.Year < 1000)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Year, bufferBytes.Slice(position), out var written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Month < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Month, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Day < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Day, bufferBytes.Slice(position), out written);
                position += written;

                bufferBytes[position++] = quoteByte;

                return true;
            }
            else
            {
                bufferChars[position++] = '"';

                if (value.Year < 10)
                    bufferChars[position++] = '0';
                if (value.Year < 100)
                    bufferChars[position++] = '0';
                if (value.Year < 1000)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Year);
                bufferChars[position++] = '-';

                if (value.Month < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Month);
                bufferChars[position++] = '-';

                if (value.Day < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Day);

                bufferChars[position++] = '"';

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(TimeOnly value, out int sizeNeeded)
        {
            //ISO8601
            //"HH:mm:ss.fffffff"
            sizeNeeded = 18;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif


            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;

                if (value.Hour < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Hour, bufferBytes.Slice(position), out var written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Minute < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Minute, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Second < 10)
                    bufferBytes[position++] = zeroByte;
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value.Second, bufferBytes.Slice(position), out written);
                position += written;

                var fraction = value.Ticks - (value.Ticks / 10000000) * 10000000;
                if (fraction > 0)
                {
                    bufferBytes[position++] = dotByte;
                    if (fraction < 10)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 100)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 1000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 10000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 100000)
                        bufferBytes[position++] = zeroByte;
                    if (fraction < 1000000)
                        bufferBytes[position++] = zeroByte;
                    //while (fraction % 10 == 0)  System.Text.Json does all figures
                    //    fraction /= 10;
                    _ = System.Buffers.Text.Utf8Formatter.TryFormat(fraction, bufferBytes.Slice(position), out written);
                    position += written;
                }

                bufferBytes[position++] = quoteByte;

                return true;
            }
            else
            {
                bufferChars[position++] = '"';

                if (value.Hour < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Hour);
                bufferChars[position++] = ':';

                if (value.Minute < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Minute);
                bufferChars[position++] = ':';

                if (value.Second < 10)
                    bufferChars[position++] = '0';
                WriteInt64Chars(value.Second);

                var fraction = value.Ticks - (value.Ticks / 10000000) * 10000000;
                if (fraction > 0)
                {
                    bufferChars[position++] = '.';
                    if (fraction < 10)
                        bufferChars[position++] = '0';
                    if (fraction < 100)
                        bufferChars[position++] = '0';
                    if (fraction < 1000)
                        bufferChars[position++] = '0';
                    if (fraction < 10000)
                        bufferChars[position++] = '0';
                    if (fraction < 100000)
                        bufferChars[position++] = '0';
                    if (fraction < 1000000)
                        bufferChars[position++] = '0';
                    //while (fraction % 10 == 0)  System.Text.Json does all figures
                    //    fraction /= 10;
                    WriteInt64Chars(fraction);
                }

                bufferChars[position++] = '"';

                return true;
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(Guid value, out int sizeNeeded)
        {
            sizeNeeded = 38;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
                bufferBytes[position++] = quoteByte;
            else
                bufferChars[position++] = '"';

            if (useBytes)
            {
                _ = System.Buffers.Text.Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
                position += written;
            }
            else
            {
                WriteLowerChars(value.ToString());
            }

            if (useBytes)
                bufferBytes[position++] = quoteByte;
            else
                bufferChars[position++] = '"';

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(ReadOnlySpan<char> value, out int sizeNeeded)
        {
            if (value.Length == 0)
            {
                sizeNeeded = 0;
                return true;
            }

            if (useBytes)
            {
                sizeNeeded = encoding.GetMaxByteCount(value.Length);
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (Skip())
                    return false;
#endif

                fixed (char* pSource = value)
                fixed (byte* pBuffer = &bufferBytes[position])
                {
                    position += encoding.GetBytes(pSource, value.Length, pBuffer, bufferBytes.Length - position);
                }
                return true;
            }
            else
            {
                sizeNeeded = value.Length;
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (Skip())
                    return false;
#endif

                fixed (char* pSource = value, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, value.Length * 2);
                }
                position += value.Length;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteInt64Chars(long value)
        {
            long num1 = value, num2, num3, num4, num5, div;

            if (value < 0)
            {
                if (value == Int64.MinValue)
                {
                    //Min value is one less than max, can't invert signs
                    bufferChars[position++] = '-';
                    bufferChars[position++] = '9';
                    bufferChars[position++] = '2';
                    bufferChars[position++] = '2';
                    bufferChars[position++] = '3';
                    bufferChars[position++] = '3';
                    bufferChars[position++] = '7';
                    bufferChars[position++] = '2';
                    bufferChars[position++] = '0';
                    bufferChars[position++] = '3';
                    bufferChars[position++] = '6';
                    bufferChars[position++] = '8';
                    bufferChars[position++] = '5';
                    bufferChars[position++] = '4';
                    bufferChars[position++] = '7';
                    bufferChars[position++] = '7';
                    bufferChars[position++] = '5';
                    bufferChars[position++] = '8';
                    bufferChars[position++] = '0';
                    bufferChars[position++] = '8';
                    return;
                }
                bufferChars[position++] = '-';
                num1 = unchecked(-value);
            }

            if (num1 < 10000)
            {
                if (num1 < 10)
                {
                    goto L1;
                }
                if (num1 < 100)
                {
                    goto L2;
                }
                if (num1 < 1000)
                {
                    goto L3;
                }
                goto L4;
            }
            else
            {
                num2 = num1 / 10000;
                num1 -= num2 * 10000;
                if (num2 < 10000)
                {
                    if (num2 < 10)
                    {
                        goto L5;
                    }
                    if (num2 < 100)
                    {
                        goto L6;
                    }
                    if (num2 < 1000)
                    {
                        goto L7;
                    }
                    goto L8;
                }
                else
                {
                    num3 = num2 / 10000;
                    num2 -= num3 * 10000;
                    if (num3 < 10000)
                    {
                        if (num3 < 10)
                        {
                            goto L9;
                        }
                        if (num3 < 100)
                        {
                            goto L10;
                        }
                        if (num3 < 1000)
                        {
                            goto L11;
                        }
                        goto L12;
                    }
                    else
                    {
                        num4 = num3 / 10000;
                        num3 -= num4 * 10000;
                        if (num4 < 10000)
                        {
                            if (num4 < 10)
                            {
                                goto L13;
                            }
                            if (num4 < 100)
                            {
                                goto L14;
                            }
                            if (num4 < 1000)
                            {
                                goto L15;
                            }
                            goto L16;
                        }
                        else
                        {
                            num5 = num4 / 10000;
                            num4 -= num5 * 10000;
                            if (num5 < 10000)
                            {
                                if (num5 < 10)
                                {
                                    goto L17;
                                }
                                if (num5 < 100)
                                {
                                    goto L18;
                                }
                                if (num5 < 1000)
                                {
                                    goto L19;
                                }
                                goto L20;
                            }
                        L20:
                            bufferChars[position++] = (char)('0' + (div = (num5 * 8389L) >> 23));
                            num5 -= div * 1000;
                        L19:
                            bufferChars[position++] = (char)('0' + (div = (num5 * 5243L) >> 19));
                            num5 -= div * 100;
                        L18:
                            bufferChars[position++] = (char)('0' + (div = (num5 * 6554L) >> 16));
                            num5 -= div * 10;
                        L17:
                            bufferChars[position++] = (char)('0' + (num5));
                        }
                    L16:
                        bufferChars[position++] = (char)('0' + (div = (num4 * 8389L) >> 23));
                        num4 -= div * 1000;
                    L15:
                        bufferChars[position++] = (char)('0' + (div = (num4 * 5243L) >> 19));
                        num4 -= div * 100;
                    L14:
                        bufferChars[position++] = (char)('0' + (div = (num4 * 6554L) >> 16));
                        num4 -= div * 10;
                    L13:
                        bufferChars[position++] = (char)('0' + (num4));
                    }
                L12:
                    bufferChars[position++] = (char)('0' + (div = (num3 * 8389L) >> 23));
                    num3 -= div * 1000;
                L11:
                    bufferChars[position++] = (char)('0' + (div = (num3 * 5243L) >> 19));
                    num3 -= div * 100;
                L10:
                    bufferChars[position++] = (char)('0' + (div = (num3 * 6554L) >> 16));
                    num3 -= div * 10;
                L9:
                    bufferChars[position++] = (char)('0' + (num3));
                }
            L8:
                bufferChars[position++] = (char)('0' + (div = (num2 * 8389L) >> 23));
                num2 -= div * 1000;
            L7:
                bufferChars[position++] = (char)('0' + (div = (num2 * 5243L) >> 19));
                num2 -= div * 100;
            L6:
                bufferChars[position++] = (char)('0' + (div = (num2 * 6554L) >> 16));
                num2 -= div * 10;
            L5:
                bufferChars[position++] = (char)('0' + (num2));
            }
        L4:
            bufferChars[position++] = (char)('0' + (div = (num1 * 8389L) >> 23));
            num1 -= div * 1000;
        L3:
            bufferChars[position++] = (char)('0' + (div = (num1 * 5243L) >> 19));
            num1 -= div * 100;
        L2:
            bufferChars[position++] = (char)('0' + (div = (num1 * 6554L) >> 16));
            num1 -= div * 10;
        L1:
            bufferChars[position++] = (char)('0' + (num1));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteUInt64Chars(ulong value)
        {
            ulong num1 = value, num2, num3, num4, num5, div;

            if (num1 < 10000)
            {
                if (num1 < 10)
                {
                    goto L1;
                }
                if (num1 < 100)
                {
                    goto L2;
                }
                if (num1 < 1000)
                {
                    goto L3;
                }
                goto L4;
            }
            else
            {
                num2 = num1 / 10000;
                num1 -= num2 * 10000;
                if (num2 < 10000)
                {
                    if (num2 < 10)
                    {
                        goto L5;
                    }
                    if (num2 < 100)
                    {
                        goto L6;
                    }
                    if (num2 < 1000)
                    {
                        goto L7;
                    }
                    goto L8;
                }
                else
                {
                    num3 = num2 / 10000;
                    num2 -= num3 * 10000;
                    if (num3 < 10000)
                    {
                        if (num3 < 10)
                        {
                            goto L9;
                        }
                        if (num3 < 100)
                        {
                            goto L10;
                        }
                        if (num3 < 1000)
                        {
                            goto L11;
                        }
                        goto L12;
                    }
                    else
                    {
                        num4 = num3 / 10000;
                        num3 -= num4 * 10000;
                        if (num4 < 10000)
                        {
                            if (num4 < 10)
                            {
                                goto L13;
                            }
                            if (num4 < 100)
                            {
                                goto L14;
                            }
                            if (num4 < 1000)
                            {
                                goto L15;
                            }
                            goto L16;
                        }
                        else
                        {
                            num5 = num4 / 10000;
                            num4 -= num5 * 10000;
                            if (num5 < 10000)
                            {
                                if (num5 < 10)
                                {
                                    goto L17;
                                }
                                if (num5 < 100)
                                {
                                    goto L18;
                                }
                                if (num5 < 1000)
                                {
                                    goto L19;
                                }
                                goto L20;
                            }
                        L20:
                            bufferChars[position++] = (char)('0' + (div = (num5 * 8389UL) >> 23));
                            num5 -= div * 1000;
                        L19:
                            bufferChars[position++] = (char)('0' + (div = (num5 * 5243UL) >> 19));
                            num5 -= div * 100;
                        L18:
                            bufferChars[position++] = (char)('0' + (div = (num5 * 6554UL) >> 16));
                            num5 -= div * 10;
                        L17:
                            bufferChars[position++] = (char)('0' + (num5));
                        }
                    L16:
                        bufferChars[position++] = (char)('0' + (div = (num4 * 8389UL) >> 23));
                        num4 -= div * 1000;
                    L15:
                        bufferChars[position++] = (char)('0' + (div = (num4 * 5243UL) >> 19));
                        num4 -= div * 100;
                    L14:
                        bufferChars[position++] = (char)('0' + (div = (num4 * 6554UL) >> 16));
                        num4 -= div * 10;
                    L13:
                        bufferChars[position++] = (char)('0' + (num4));
                    }
                L12:
                    bufferChars[position++] = (char)('0' + (div = (num3 * 8389UL) >> 23));
                    num3 -= div * 1000;
                L11:
                    bufferChars[position++] = (char)('0' + (div = (num3 * 5243UL) >> 19));
                    num3 -= div * 100;
                L10:
                    bufferChars[position++] = (char)('0' + (div = (num3 * 6554UL) >> 16));
                    num3 -= div * 10;
                L9:
                    bufferChars[position++] = (char)('0' + (num3));
                }
            L8:
                bufferChars[position++] = (char)('0' + (div = (num2 * 8389UL) >> 23));
                num2 -= div * 1000;
            L7:
                bufferChars[position++] = (char)('0' + (div = (num2 * 5243UL) >> 19));
                num2 -= div * 100;
            L6:
                bufferChars[position++] = (char)('0' + (div = (num2 * 6554UL) >> 16));
                num2 -= div * 10;
            L5:
                bufferChars[position++] = (char)('0' + (num2));
            }
        L4:
            bufferChars[position++] = (char)('0' + (div = (num1 * 8389UL) >> 23));
            num1 -= div * 1000;
        L3:
            bufferChars[position++] = (char)('0' + (div = (num1 * 5243UL) >> 19));
            num1 -= div * 100;
        L2:
            bufferChars[position++] = (char)('0' + (div = (num1 * 6554UL) >> 16));
            num1 -= div * 10;
        L1:
            bufferChars[position++] = (char)('0' + (num1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteNull(out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = nByte;
                bufferBytes[position++] = uByte;
                bufferBytes[position++] = lByte;
                bufferBytes[position++] = lByte;
                return true;
            }
            else
            {
                bufferChars[position++] = 'n';
                bufferChars[position++] = 'u';
                bufferChars[position++] = 'l';
                bufferChars[position++] = 'l';
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteTrue(out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = tByte;
                bufferBytes[position++] = rByte;
                bufferBytes[position++] = uByte;
                bufferBytes[position++] = eByte;
                return true;
            }
            else
            {
                bufferChars[position++] = 't';
                bufferChars[position++] = 'r';
                bufferChars[position++] = 'u';
                bufferChars[position++] = 'e';
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteFalse(out int sizeNeeded)
        {
            sizeNeeded = 5;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = fByte;
                bufferBytes[position++] = aByte;
                bufferBytes[position++] = lByte;
                bufferBytes[position++] = sByte;
                bufferBytes[position++] = eByte;
                return true;
            }
            else
            {
                bufferChars[position++] = 'f';
                bufferChars[position++] = 'a';
                bufferChars[position++] = 'l';
                bufferChars[position++] = 's';
                bufferChars[position++] = 'e';
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWritePropertyName(string? value, bool startWithComma, out int sizeNeeded)
        {
            sizeNeeded = startWithComma ? 4 : 3;
            if (value is null || value.Length == 0)
                return true;

            if (useBytes)
                throw new InvalidOperationException($"{nameof(TryWritePropertyName)} {nameof(useBytes)} is the wrong setting for this call");

            sizeNeeded = value.Length + (startWithComma ? 4 : 3);
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            fixed (char* pSource = value, pBuffer = bufferChars)
            {
                if (startWithComma)
                    pBuffer[position++] = ',';

                pBuffer[position++] = '"';

                Buffer.MemoryCopy(pSource, &pBuffer[position], (bufferChars.Length - position) * 2, value.Length * 2);
                position += value.Length;

                pBuffer[position++] = '"';
                pBuffer[position++] = ':';
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWritePropertyName(ReadOnlySpan<byte> value, bool startWithComma, out int sizeNeeded)
        {
            sizeNeeded = startWithComma ? 4 : 3;
            if (value.Length == 0)
                return true;

            if (!useBytes)
                throw new InvalidOperationException($"{nameof(TryWritePropertyName)} {nameof(useBytes)} is the wrong setting for this call");

            sizeNeeded = encoding.GetMaxByteCount(value.Length) + (startWithComma ? 4 : 3);
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            fixed (byte* pSource = value)
            fixed (byte* pBuffer = bufferBytes)
            {
                if (startWithComma)
                    pBuffer[position++] = commaByte;

                pBuffer[position++] = quoteByte;

                Buffer.MemoryCopy(pSource, &pBuffer[position], bufferBytes.Length - position, value.Length);
                position += value.Length;

                pBuffer[position++] = quoteByte;
                pBuffer[position++] = colonByte;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteComma(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = commaByte;
                return true;
            }
            else
            {
                bufferChars[position++] = ',';
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteOpenBracket(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = openBracketByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '[';
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteCloseBracket(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = closeBracketByte;
                return true;
            }
            else
            {
                bufferChars[position++] = ']';
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteEmptyBracket(out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = openBracketByte;
                bufferBytes[position++] = closeBracketByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '[';
                bufferChars[position++] = ']';
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteOpenBrace(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = openBraceByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '{';
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteCloseBrace(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = closeBraceByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '}';
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteEmptyBrace(out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = openBraceByte;
                bufferBytes[position++] = closeBraceByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '{';
                bufferChars[position++] = '}';
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteEmptyString(out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;
                bufferBytes[position++] = quoteByte;
            }
            else
            {
                bufferChars[position++] = '"';
                bufferChars[position++] = '"';
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteEscapedQuoted(string? value, out int sizeNeeded)
        {
            const int maxEscapeCharacterSize = 6;

            if (value is null)
            {
                sizeNeeded = 0;
                return true;
            }
            if (value.Length == 0)
            {
                sizeNeeded = 2;
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (Skip())
                    return false;
#endif
                if (useBytes)
                {
                    bufferBytes[position++] = quoteByte;
                    bufferBytes[position++] = quoteByte;
                }
                else
                {
                    bufferChars[position++] = '"';
                    bufferChars[position++] = '"';
                }
                return true;
            }

            if (useBytes)
            {
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

                    if (needsEscaped)
                    {
                        sizeNeeded = encoding.GetMaxByteCount((i + ((value.Length - i) * maxEscapeCharacterSize)) + 2);
                        if (length - position < sizeNeeded)
                        {
                            if (!Grow(sizeNeeded))
                                return false;
                        }
#if DEBUG
                        if (Skip())
                            return false;
#endif

                        var escapeBufferOwner = ArrayPoolHelper<char>.Rent(sizeNeeded);

                        fixed (char* pEscapeBuffer = escapeBufferOwner)
                        {
                            var start = 0;
                            var escapeBufferIndex = 0;

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

                                        Buffer.MemoryCopy(&pValue[start], &pEscapeBuffer[escapeBufferIndex], (escapeBufferOwner.Length - escapeBufferIndex) * 2, (i - start) * 2);
                                        escapeBufferIndex += i - start;

                                        var code = StringHelper.LowUnicodeIntToEncodedHex[c];
                                        fixed (char* pCode = code)
                                        {
                                            Buffer.MemoryCopy(pCode, &pEscapeBuffer[escapeBufferIndex], (escapeBufferOwner.Length - escapeBufferIndex) * 2, code.Length * 2);
                                        }
                                        escapeBufferIndex += code.Length;

                                        start = i + 1;
                                        continue;
                                }

                                Buffer.MemoryCopy(&pValue[start], &pEscapeBuffer[escapeBufferIndex], (escapeBufferOwner.Length - escapeBufferIndex) * 2, (i - start) * 2);
                                escapeBufferIndex += i - start;

                                pEscapeBuffer[escapeBufferIndex++] = '\\';
                                pEscapeBuffer[escapeBufferIndex++] = escapedChar;
                                start = i + 1;
                            }

                            if (value.Length > start)
                            {
                                Buffer.MemoryCopy(&pValue[start], &pEscapeBuffer[escapeBufferIndex], (escapeBufferOwner.Length - escapeBufferIndex) * 2, (value.Length - start) * 2);
                                escapeBufferIndex += value.Length - start;
                            }

                            fixed (byte* pBuffer = bufferBytes)
                            {
                                pBuffer[position++] = quoteByte;
                                position += encoding.GetBytes(pEscapeBuffer, escapeBufferIndex, &pBuffer[position], length - position);
                                pBuffer[position++] = quoteByte;
                            }
                        }
                        ArrayPoolHelper<char>.Return(escapeBufferOwner);
                    }
                    else
                    {
                        sizeNeeded = encoding.GetMaxByteCount(value.Length + 2);
                        if (length - position < sizeNeeded)
                        {
                            if (!Grow(sizeNeeded))
                                return false;
                        }
#if DEBUG
                        if (Skip())
                            return false;
#endif

                        fixed (byte* pBuffer = bufferBytes)
                        {
                            pBuffer[position++] = quoteByte;
                            position += encoding.GetBytes(pValue, value.Length, &pBuffer[position], length - position);
                            pBuffer[position++] = quoteByte;
                        }
                    }
                }
            }
            else
            {
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

                    if (needsEscaped)
                    {
                        sizeNeeded = i + ((value.Length - i) * maxEscapeCharacterSize) + 2;
                        if (length - position < sizeNeeded)
                        {
                            if (!Grow(sizeNeeded))
                                return false;
                        }
#if DEBUG
                        if (Skip())
                            return false;
#endif

                        fixed (char* pBuffer = bufferChars)
                        {
                            pBuffer[position++] = '\"';

                            var start = 0;

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

                                        Buffer.MemoryCopy(&pValue[start], &pBuffer[position], (length - position) * 2, (i - start) * 2);
                                        position += i - start;

                                        var code = StringHelper.LowUnicodeIntToEncodedHex[c];
                                        fixed (char* pCode = code)
                                        {
                                            Buffer.MemoryCopy(pCode, &pBuffer[position], (length - position) * 2, code.Length * 2);
                                        }
                                        position += code.Length;

                                        start = i + 1;
                                        continue;
                                }

                                Buffer.MemoryCopy(&pValue[start], &pBuffer[position], (length - position) * 2, (i - start) * 2);
                                position += i - start;

                                pBuffer[position++] = '\\';
                                pBuffer[position++] = escapedChar;
                                start = i + 1;
                            }

                            if (value.Length > start)
                            {
                                Buffer.MemoryCopy(&pValue[start], &pBuffer[position], (length - position) * 2, (value.Length - start) * 2);
                                position += value.Length - start;
                            }

                            pBuffer[position++] = '\"';
                        }
                    }
                    else
                    {
                        sizeNeeded = value.Length + 2;
                        if (length - position < sizeNeeded)
                        {
                            if (!Grow(sizeNeeded))
                                return false;
                        }
#if DEBUG
                        if (Skip())
                            return false;
#endif

                        fixed (char* pBuffer = bufferChars)
                        {
                            pBuffer[position++] = '\"';
                            Buffer.MemoryCopy(pValue, &pBuffer[position], (length - position) * 2, value.Length * 2);
                            position += value.Length;
                            pBuffer[position++] = '\"';
                        }
                    }
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteEscapedQuoted(char value, out int sizeNeeded)
        {
            if (useBytes)
            {
                if (value < 128)
                {
                    char escapedChar;
                    switch (value)
                    {
                        case '"':
                            escapedChar = '"';
                            break;
                        case '\\':
                            escapedChar = '\\';
                            break;
                        case >= ' ': //32

                            sizeNeeded = 3;
                            if (length - position < sizeNeeded)
                            {
                                if (!Grow(sizeNeeded))
                                    return false;
                            }
#if DEBUG
                            if (Skip())
                                return false;
#endif

                            bufferBytes[position++] = quoteByte;
                            bufferBytes[position++] = (byte)value;
                            bufferBytes[position++] = quoteByte;
                            return true;

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
                            var code = StringHelper.LowUnicodeIntToEncodedHex[value];

                            sizeNeeded = code.Length + 2;
                            if (length - position < sizeNeeded)
                            {
                                if (!Grow(sizeNeeded))
                                    return false;
                            }
#if DEBUG
                            if (Skip())
                                return false;
#endif
                            fixed (char* pCode = code)
                            fixed (byte* pBuffer = bufferBytes)
                            {
                                pBuffer[position++] = quoteByte;

                                //chars are 2 bytes but we want the 1 byte encoding
                                for (var i = 0; i < code.Length; i++)
                                    pBuffer[position++] = (byte)pCode[i];

                                pBuffer[position++] = quoteByte;
                            }
                            return true;
                    }

                    sizeNeeded = 4;
                    if (length - position < sizeNeeded)
                    {
                        if (!Grow(sizeNeeded))
                            return false;
                    }
#if DEBUG
                    if (Skip())
                        return false;
#endif

                    bufferBytes[position++] = quoteByte;
                    bufferBytes[position++] = escapeByte;
                    bufferBytes[position++] = (byte)escapedChar;
                    bufferBytes[position++] = quoteByte;
                    return true;
                }
                else
                {
                    sizeNeeded = 6;
                    if (length - position < sizeNeeded)
                    {
                        if (!Grow(sizeNeeded))
                            return false;
                    }
#if DEBUG
                    if (Skip())
                        return false;
#endif

                    bufferBytes[position++] = quoteByte;
                    var valueArray = stackalloc char[] { value };
                    fixed (byte* pBuffer = &bufferBytes[position])
                    {
                        position += encoding.GetBytes(valueArray, 1, pBuffer, length - position);
                    }
                    bufferBytes[position++] = quoteByte;

                    return true;
                }
            }
            else
            {
                char escapedChar;
                switch (value)
                {
                    case '"':
                        escapedChar = '"';
                        break;
                    case '\\':
                        escapedChar = '\\';
                        break;
                    case >= ' ': //32

                        sizeNeeded = 3;
                        if (length - position < sizeNeeded)
                        {
                            if (!Grow(sizeNeeded))
                                return false;
                        }
#if DEBUG
                        if (Skip())
                            return false;
#endif

                        bufferChars[position++] = '"';
                        bufferChars[position++] = value;
                        bufferChars[position++] = '"';

                        return true;

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
                        var code = StringHelper.LowUnicodeIntToEncodedHex[value];

                        sizeNeeded = code.Length + 2;
                        if (length - position < sizeNeeded)
                        {
                            if (!Grow(sizeNeeded))
                                return false;
                        }
#if DEBUG
                        if (Skip())
                            return false;
#endif

                        fixed (char* pCode = code, pBuffer = bufferChars)
                        {
                            pBuffer[position++] = '"';
                            Buffer.MemoryCopy(pCode, &pBuffer[position], (length - position) * 2, code.Length * 2);
                            position += code.Length;
                            pBuffer[position++] = '"';
                        }
                        return true;
                }

                sizeNeeded = 4;
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (Skip())
                    return false;
#endif

                bufferChars[position++] = '"';
                bufferChars[position++] = '\\';
                bufferChars[position++] = escapedChar;
                bufferChars[position++] = '"';

                return true;
            }
        }
    }
}