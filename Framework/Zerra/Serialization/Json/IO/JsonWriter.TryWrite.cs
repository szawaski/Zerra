// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers.Text;
using System.Runtime.CompilerServices;

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
        private const byte bByte = (byte)'b';

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
        public unsafe bool TryWrite(byte value, out int sizeNeeded)
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
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(sbyte value, out int sizeNeeded)
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
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(short value, out int sizeNeeded)
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
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(ushort value, out int sizeNeeded)
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
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(int value, out int sizeNeeded)
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
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(uint value, out int sizeNeeded)
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
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(long value, out int sizeNeeded)
        {
            sizeNeeded = 20;
            if (length - position < sizeNeeded && !Grow(sizeNeeded))
                return false;

            if (useBytes)
            {
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(ulong value, out int sizeNeeded)
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
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(float value, out int sizeNeeded)
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
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(double value, out int sizeNeeded)
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
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(decimal value, out int sizeNeeded)
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
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
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
                _ = Utf8Formatter.TryFormat(value.Year, bufferBytes.Slice(position), out var written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Month < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Month, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Day < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Day, bufferBytes.Slice(position), out written);
                position += written;

                bufferBytes[position++] = tUpperByte;

                if (value.Hour < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Hour, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Minute < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Minute, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Second < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Second, bufferBytes.Slice(position), out written);
                position += written;

                var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
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
                    _ = Utf8Formatter.TryFormat(fraction, bufferBytes.Slice(position), out written);
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
                            _ = Utf8Formatter.TryFormat(offset.Offset.Hours < 0 ? -offset.Offset.Hours : offset.Offset.Hours, bufferBytes.Slice(position), out written);
                            position += written;
                            bufferBytes[position++] = colonByte;

                            if (offset.Offset.Minutes < 10)
                                bufferBytes[position++] = zeroByte;
                            _ = Utf8Formatter.TryFormat(offset.Offset.Minutes, bufferBytes.Slice(position), out written);
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
                WriteInt32Chars(value.Year);
                bufferChars[position++] = '-';

                if (value.Month < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Month);
                bufferChars[position++] = '-';

                if (value.Day < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Day);

                bufferChars[position++] = 'T';

                if (value.Hour < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Hour);
                bufferChars[position++] = ':';

                if (value.Minute < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Minute);
                bufferChars[position++] = ':';

                if (value.Second < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Second);

                var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
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
                            WriteInt32Chars(offset.Offset.Hours < 0 ? -offset.Offset.Hours : offset.Offset.Hours);
                            bufferChars[position++] = ':';

                            if (offset.Offset.Minutes < 10)
                                bufferChars[position++] = '0';
                            WriteInt32Chars(offset.Offset.Minutes);
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
                _ = Utf8Formatter.TryFormat(value.Year, bufferBytes.Slice(position), out var written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Month < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Month, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Day < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Day, bufferBytes.Slice(position), out written);
                position += written;

                bufferBytes[position++] = tUpperByte;

                if (value.Hour < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Hour, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Minute < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Minute, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Second < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Second, bufferBytes.Slice(position), out written);
                position += written;

                var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
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
                    _ = Utf8Formatter.TryFormat(fraction, bufferBytes.Slice(position), out written);
                    position += written;
                }

                if (value.Offset.Hours < 0)
                    bufferBytes[position++] = minusByte;
                else
                    bufferBytes[position++] = plusByte;
                if (value.Offset.Hours < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Offset.Hours < 0 ? -value.Offset.Hours : value.Offset.Hours, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Offset.Minutes < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Offset.Minutes, bufferBytes.Slice(position), out written);
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
                WriteInt32Chars(value.Year);
                bufferChars[position++] = '-';

                if (value.Month < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Month);
                bufferChars[position++] = '-';

                if (value.Day < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Day);

                bufferChars[position++] = 'T';

                if (value.Hour < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Hour);
                bufferChars[position++] = ':';

                if (value.Minute < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Minute);
                bufferChars[position++] = ':';

                if (value.Second < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Second);

                var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
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
                WriteInt32Chars(value.Offset.Hours < 0 ? -value.Offset.Hours : value.Offset.Hours);
                bufferChars[position++] = ':';

                if (value.Offset.Minutes < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Offset.Minutes);

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
                    _ = Utf8Formatter.TryFormat(value.Days, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = dotByte;
                }
                else if (value.Days < 0)
                {
                    _ = Utf8Formatter.TryFormat(-value.Days, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = dotByte;
                }

                if (value.Hours > -1)
                {
                    if (value.Hours < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = Utf8Formatter.TryFormat(value.Hours, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = colonByte;
                }
                else
                {
                    if (-value.Hours < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = Utf8Formatter.TryFormat(-value.Hours, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = colonByte;
                }

                if (value.Minutes > -1)
                {
                    if (value.Minutes < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = Utf8Formatter.TryFormat(value.Minutes, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = colonByte;
                }
                else
                {
                    if (-value.Minutes < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = Utf8Formatter.TryFormat(-value.Minutes, bufferBytes.Slice(position), out var written);
                    position += written;
                    bufferBytes[position++] = colonByte;
                }

                if (value.Seconds > -1)
                {
                    if (value.Seconds < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = Utf8Formatter.TryFormat(value.Seconds, bufferBytes.Slice(position), out var written);
                    position += written;
                }
                else
                {
                    if (-value.Seconds < 10)
                        bufferBytes[position++] = zeroByte;
                    _ = Utf8Formatter.TryFormat(-value.Seconds, bufferBytes.Slice(position), out var written);
                    position += written;
                }

                long fraction;
                if (value.Ticks > -1)
                    fraction = value.Ticks - (value.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
                else
                    fraction = -value.Ticks - (-value.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
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
                    _ = Utf8Formatter.TryFormat(fraction, bufferBytes.Slice(position), out var written);
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
                    WriteInt32Chars(value.Days);
                    bufferChars[position++] = '.';
                }
                else if (value.Days < 0)
                {
                    WriteInt32Chars(-value.Days);
                    bufferChars[position++] = '.';
                }

                if (value.Hours > -1)
                {
                    if (value.Hours < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(value.Hours);
                    bufferChars[position++] = ':';
                }
                else
                {
                    if (-value.Hours < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(-value.Hours);
                    bufferChars[position++] = ':';
                }

                if (value.Minutes > -1)
                {
                    if (value.Minutes < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(value.Minutes);
                    bufferChars[position++] = ':';
                }
                else
                {
                    if (-value.Minutes < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(-value.Minutes);
                    bufferChars[position++] = ':';
                }

                if (value.Seconds > -1)
                {
                    if (value.Seconds < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(value.Seconds);
                }
                else
                {
                    if (-value.Seconds < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(-value.Seconds);
                }

                long fraction;
                if (value.Ticks > -1)
                    fraction = value.Ticks - (value.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
                else
                    fraction = -value.Ticks - (-value.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
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
                _ = Utf8Formatter.TryFormat(value.Year, bufferBytes.Slice(position), out var written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Month < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Month, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = minusByte;

                if (value.Day < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Day, bufferBytes.Slice(position), out written);
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
                WriteInt32Chars(value.Year);
                bufferChars[position++] = '-';

                if (value.Month < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Month);
                bufferChars[position++] = '-';

                if (value.Day < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Day);

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
                _ = Utf8Formatter.TryFormat(value.Hour, bufferBytes.Slice(position), out var written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Minute < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Minute, bufferBytes.Slice(position), out written);
                position += written;
                bufferBytes[position++] = colonByte;

                if (value.Second < 10)
                    bufferBytes[position++] = zeroByte;
                _ = Utf8Formatter.TryFormat(value.Second, bufferBytes.Slice(position), out written);
                position += written;

                var fraction = value.Ticks - (value.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
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
                    _ = Utf8Formatter.TryFormat(fraction, bufferBytes.Slice(position), out written);
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
                WriteInt32Chars(value.Hour);
                bufferChars[position++] = ':';

                if (value.Minute < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Minute);
                bufferChars[position++] = ':';

                if (value.Second < 10)
                    bufferChars[position++] = '0';
                WriteInt32Chars(value.Second);

                var fraction = value.Ticks - (value.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
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
        public unsafe bool TryWrite(Guid value, out int sizeNeeded)
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
#if NET8_0_OR_GREATER
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
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
        private unsafe void WriteInt32Chars(int value)
        {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
            _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
            position += consumed;
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteInt64Chars(long value)
        {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
            _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
            position += consumed;
#endif
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
        public unsafe bool TryWriteNameSegment(ReadOnlySpan<char> value, bool startWithComma, out int sizeNeeded)
        {
            if (useBytes)
                throw new InvalidOperationException($"{nameof(TryWriteNameSegment)} {nameof(useBytes)} is the wrong setting for this call");

            sizeNeeded = value.Length + (startWithComma ? 1 : 0);
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

                Buffer.MemoryCopy(pSource, &pBuffer[position], (bufferChars.Length - position) * 2, value.Length * 2);
                position += value.Length;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteNameSegment(ReadOnlySpan<byte> value, bool startWithComma, out int sizeNeeded)
        {
            if (!useBytes)
                throw new InvalidOperationException($"{nameof(TryWriteNameSegment)} {nameof(useBytes)} is the wrong setting for this call");

            sizeNeeded = value.Length + (startWithComma ? 1 : 0);
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

                Buffer.MemoryCopy(pSource, &pBuffer[position], bufferBytes.Length - position, value.Length);
                position += value.Length;
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

                    if (!needsEscaped)
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
                        return true;
                    }

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

                    fixed (byte* pBuffer = bufferBytes)
                    {
                        pBuffer[position++] = quoteByte;

                        var start = 0;

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

                                    position += encoding.GetBytes(&pValue[start], i - start, &pBuffer[position], length - position);

                                    var code = StringHelper.LowUnicodeIntToEncodedHex[c];
                                    fixed (char* pCode = code)
                                    {
                                        position += encoding.GetBytes(pCode, code.Length, &pBuffer[position], length - position);
                                    }

                                    start = i + 1;
                                    continue;
                            }

                            position += encoding.GetBytes(&pValue[start], i - start, &pBuffer[position], length - position);

                            pBuffer[position++] = escapeByte;
                            pBuffer[position++] = escapedByte;
                            start = i + 1;
                        }

                        if (value.Length > start)
                        {
                            position += encoding.GetBytes(&pValue[start], value.Length - start, &pBuffer[position], length - position);
                        }

                        pBuffer[position++] = quoteByte;
                    }
                }

                return true;
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

                    if (!needsEscaped)
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
                        return true;
                    }

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

                    return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteEscapedQuoted(char value, out int sizeNeeded)
        {
            if (useBytes)
            {
                if (value < 128)
                {
                    byte escapedByte;
                    switch (value)
                    {
                        case '"':
                            escapedByte = quoteByte;
                            break;
                        case '\\':
                            escapedByte = escapeByte;
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
                    bufferBytes[position++] = escapedByte;
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