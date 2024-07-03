// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization.Json.IO
{
    public ref partial struct JsonWriter
    {        //https://www.rfc-editor.org/rfc/rfc4627

        //last byte 128 to 191
        //1 bytes: 0 to 127
        //2 bytes: 192 to ?
        //3 bytes: 224 to ?
        //4 bytes: 240 to ?

        private static readonly byte zeroByte = (byte)'0';
        private static readonly byte oneByte = (byte)'1';
        private static readonly byte twoByte = (byte)'2';
        private static readonly byte threeByte = (byte)'3';
        private static readonly byte fourByte = (byte)'4';
        private static readonly byte fiveByte = (byte)'5';
        private static readonly byte sixByte = (byte)'6';
        private static readonly byte sevenByte = (byte)'7';
        private static readonly byte eightByte = (byte)'8';
        private static readonly byte nineByte = (byte)'9';
        private static readonly byte negativeByte = (byte)'-';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(byte value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded)
                return false;
            if (useBytes)
                WriteUInt64Bytes(value);
            else
                WriteUInt64Chars(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(sbyte value, out int sizeNeeded)
        {
            sizeNeeded = 3;
            if (length - position < sizeNeeded)
                return false;
            if (useBytes)
                WriteInt64Bytes(value);
            else
                WriteInt64Chars(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(short value, out int sizeNeeded)
        {
            sizeNeeded = 6;
            if (length - position < sizeNeeded)
                return false;
            if (useBytes)
                WriteInt64Bytes(value);
            else
                WriteInt64Chars(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(ushort value, out int sizeNeeded)
        {
            sizeNeeded = 5;
            if (length - position < sizeNeeded)
                return false;
            if (useBytes)
                WriteUInt64Bytes(value);
            else
                WriteUInt64Chars(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(int value, out int sizeNeeded)
        {
            sizeNeeded = 11;
            if (length - position < sizeNeeded)
                return false;
            if (useBytes)
                WriteInt64Bytes(value);
            else
                WriteInt64Chars(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(uint value, out int sizeNeeded)
        {
            sizeNeeded = 10;
            if (length - position < sizeNeeded)
                return false;
            if (useBytes)
                WriteUInt64Bytes(value);
            else
                WriteUInt64Chars(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(long value, out int sizeNeeded)
        {
            sizeNeeded = 20;
            if (length - position < sizeNeeded)
                return false;
            if (useBytes)
                WriteInt64Bytes(value);
            else
                WriteInt64Chars(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(ulong value, out int sizeNeeded)
        {
            sizeNeeded = 20;
            if (length - position < sizeNeeded)
                return false;
            if (useBytes)
                WriteUInt64Bytes(value);
            else
                WriteUInt64Chars(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(float value, out int sizeNeeded)
        {
            sizeNeeded = 16; //min
            return TryWrite(value.ToString(), out sizeNeeded);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(double value, out int sizeNeeded)
        {
            sizeNeeded = 32; //min
            return TryWrite(value.ToString(), out sizeNeeded);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(decimal value, out int sizeNeeded)
        {
            sizeNeeded = 31;
            return TryWrite(value.ToString(), out sizeNeeded);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(char value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
                return false;
            bufferChars[position++] = value;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(string? value, out int sizeNeeded)
        {
            if (value == null)
            {
                sizeNeeded = 0;
                return true;
            }
            if (value.Length == 0)
            {
                sizeNeeded = 0;
                return true;
            }

            if (useBytes)
            {
                sizeNeeded = encoding.GetMaxByteCount(value.Length);
                if (length - position < sizeNeeded)
                    return false;

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
                    return false;

                var pCount = value.Length;
                fixed (char* pSource = value, pBuffer = &bufferChars[position])
                {
                    for (var p = 0; p < pCount; p++)
                    {
                        pBuffer[p] = pSource[p];
                    }
                }
                position += pCount;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateTime value, out int sizeNeeded)
        {
            //ISO8601
            //yyyy-MM-ddTHH:mm:ss.fffffff+00:00
            sizeNeeded = 33;
            if (length - position < sizeNeeded)
                return false;

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

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateTimeOffset value, out int sizeNeeded)
        {
            //ISO8601
            //yyyy-MM-ddTHH:mm:ss.fffffff+00:00
            sizeNeeded = 33;
            if (length - position < sizeNeeded)
                return false;

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

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(TimeSpan value, out int sizeNeeded)
        {
            //ISO8601
            //(-)dddddddd.HH:mm:ss.fffffff
            sizeNeeded = 26;
            if (length - position < sizeNeeded)
                return false;

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

            return true;
        }

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateOnly value, out int sizeNeeded)
        {
            //ISO8601
            //yyyy-MM-dd
            sizeNeeded = 10;
            if (length - position < sizeNeeded)
                return false;

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

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(TimeOnly value, out int sizeNeeded)
        {
            //ISO8601
            //HH:mm:ss.fffffff
            sizeNeeded = 16;
            if (length - position < sizeNeeded)
                return false;

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

            return true;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(Guid value, out int sizeNeeded)
        {
            return TryWrite(value.ToString(), out sizeNeeded);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(ReadOnlySpan<char> value, out int sizeNeeded)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length == 0)
            {
                sizeNeeded = 0;
                return true;
            }
            sizeNeeded = value.Length;
            if (length - position < sizeNeeded)
                return false;

            var pCount = value.Length;
            fixed (char* pSource = value, pBuffer = &bufferChars[position])
            {
                for (var p = 0; p < pCount; p++)
                {
                    pBuffer[p] = pSource[p];
                }
            }
            position += pCount;
            return true;
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
                    EnsureBufferSize(20);
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
                    EnsureBufferSize(1);
                    goto L1;
                }
                if (num1 < 100)
                {
                    EnsureBufferSize(2);
                    goto L2;
                }
                if (num1 < 1000)
                {
                    EnsureBufferSize(3);
                    goto L3;
                }
                EnsureBufferSize(4);
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
                        EnsureBufferSize(5);
                        goto L5;
                    }
                    if (num2 < 100)
                    {
                        EnsureBufferSize(6);
                        goto L6;
                    }
                    if (num2 < 1000)
                    {
                        EnsureBufferSize(7);
                        goto L7;
                    }
                    EnsureBufferSize(8);
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
                            EnsureBufferSize(9);
                            goto L9;
                        }
                        if (num3 < 100)
                        {
                            EnsureBufferSize(10);
                            goto L10;
                        }
                        if (num3 < 1000)
                        {
                            EnsureBufferSize(11);
                            goto L11;
                        }
                        EnsureBufferSize(12);
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
                                EnsureBufferSize(13);
                                goto L13;
                            }
                            if (num4 < 100)
                            {
                                EnsureBufferSize(14);
                                goto L14;
                            }
                            if (num4 < 1000)
                            {
                                EnsureBufferSize(15);
                                goto L15;
                            }
                            EnsureBufferSize(16);
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
                                    EnsureBufferSize(17);
                                    goto L17;
                                }
                                if (num5 < 100)
                                {
                                    EnsureBufferSize(18);
                                    goto L18;
                                }
                                if (num5 < 1000)
                                {
                                    EnsureBufferSize(19);
                                    goto L19;
                                }
                                EnsureBufferSize(20);
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
                    EnsureBufferSize(1);
                    goto L1;
                }
                if (num1 < 100)
                {
                    EnsureBufferSize(2);
                    goto L2;
                }
                if (num1 < 1000)
                {
                    EnsureBufferSize(3);
                    goto L3;
                }
                EnsureBufferSize(4);
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
                        EnsureBufferSize(5);
                        goto L5;
                    }
                    if (num2 < 100)
                    {
                        EnsureBufferSize(6);
                        goto L6;
                    }
                    if (num2 < 1000)
                    {
                        EnsureBufferSize(7);
                        goto L7;
                    }
                    EnsureBufferSize(8);
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
                            EnsureBufferSize(9);
                            goto L9;
                        }
                        if (num3 < 100)
                        {
                            EnsureBufferSize(10);
                            goto L10;
                        }
                        if (num3 < 1000)
                        {
                            EnsureBufferSize(11);
                            goto L11;
                        }
                        EnsureBufferSize(12);
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
                                EnsureBufferSize(13);
                                goto L13;
                            }
                            if (num4 < 100)
                            {
                                EnsureBufferSize(14);
                                goto L14;
                            }
                            if (num4 < 1000)
                            {
                                EnsureBufferSize(15);
                                goto L15;
                            }
                            EnsureBufferSize(16);
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
                                    EnsureBufferSize(17);
                                    goto L17;
                                }
                                if (num5 < 100)
                                {
                                    EnsureBufferSize(18);
                                    goto L18;
                                }
                                if (num5 < 1000)
                                {
                                    EnsureBufferSize(19);
                                    goto L19;
                                }
                                EnsureBufferSize(20);
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
        private void WriteInt64Bytes(long value)
        {
            long num1 = value, num2, num3, num4, num5, div;

            if (value < 0)
            {
                if (value == Int64.MinValue)
                {
                    //Min value is one less than max, can't invert signs
                    EnsureBufferSize(20);
                    bufferBytes[position++] = negativeByte;
                    bufferBytes[position++] = nineByte;
                    bufferBytes[position++] = twoByte;
                    bufferBytes[position++] = twoByte;
                    bufferBytes[position++] = threeByte;
                    bufferBytes[position++] = threeByte;
                    bufferBytes[position++] = sevenByte;
                    bufferBytes[position++] = twoByte;
                    bufferBytes[position++] = zeroByte;
                    bufferBytes[position++] = threeByte;
                    bufferBytes[position++] = sixByte;
                    bufferBytes[position++] = eightByte;
                    bufferBytes[position++] = fiveByte;
                    bufferBytes[position++] = fourByte;
                    bufferBytes[position++] = sevenByte;
                    bufferBytes[position++] = sevenByte;
                    bufferBytes[position++] = fiveByte;
                    bufferBytes[position++] = eightByte;
                    bufferBytes[position++] = zeroByte;
                    bufferBytes[position++] = eightByte;
                    return;
                }
                bufferBytes[position++] = negativeByte;
                num1 = unchecked(-value);
            }

            if (num1 < 10000)
            {
                if (num1 < 10)
                {
                    EnsureBufferSize(1);
                    goto L1;
                }
                if (num1 < 100)
                {
                    EnsureBufferSize(2);
                    goto L2;
                }
                if (num1 < 1000)
                {
                    EnsureBufferSize(3);
                    goto L3;
                }
                EnsureBufferSize(4);
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
                        EnsureBufferSize(5);
                        goto L5;
                    }
                    if (num2 < 100)
                    {
                        EnsureBufferSize(6);
                        goto L6;
                    }
                    if (num2 < 1000)
                    {
                        EnsureBufferSize(7);
                        goto L7;
                    }
                    EnsureBufferSize(8);
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
                            EnsureBufferSize(9);
                            goto L9;
                        }
                        if (num3 < 100)
                        {
                            EnsureBufferSize(10);
                            goto L10;
                        }
                        if (num3 < 1000)
                        {
                            EnsureBufferSize(11);
                            goto L11;
                        }
                        EnsureBufferSize(12);
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
                                EnsureBufferSize(13);
                                goto L13;
                            }
                            if (num4 < 100)
                            {
                                EnsureBufferSize(14);
                                goto L14;
                            }
                            if (num4 < 1000)
                            {
                                EnsureBufferSize(15);
                                goto L15;
                            }
                            EnsureBufferSize(16);
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
                                    EnsureBufferSize(17);
                                    goto L17;
                                }
                                if (num5 < 100)
                                {
                                    EnsureBufferSize(18);
                                    goto L18;
                                }
                                if (num5 < 1000)
                                {
                                    EnsureBufferSize(19);
                                    goto L19;
                                }
                                EnsureBufferSize(20);
                                goto L20;
                            }
                        L20:
                            bufferBytes[position++] = (byte)(zeroByte + (div = (num5 * 8389L) >> 23));
                            num5 -= div * 1000;
                        L19:
                            bufferBytes[position++] = (byte)(zeroByte + (div = (num5 * 5243L) >> 19));
                            num5 -= div * 100;
                        L18:
                            bufferBytes[position++] = (byte)(zeroByte + (div = (num5 * 6554L) >> 16));
                            num5 -= div * 10;
                        L17:
                            bufferBytes[position++] = (byte)(zeroByte + (num5));
                        }
                    L16:
                        bufferBytes[position++] = (byte)(zeroByte + (div = (num4 * 8389L) >> 23));
                        num4 -= div * 1000;
                    L15:
                        bufferBytes[position++] = (byte)(zeroByte + (div = (num4 * 5243L) >> 19));
                        num4 -= div * 100;
                    L14:
                        bufferBytes[position++] = (byte)(zeroByte + (div = (num4 * 6554L) >> 16));
                        num4 -= div * 10;
                    L13:
                        bufferBytes[position++] = (byte)(zeroByte + (num4));
                    }
                L12:
                    bufferBytes[position++] = (byte)(zeroByte + (div = (num3 * 8389L) >> 23));
                    num3 -= div * 1000;
                L11:
                    bufferBytes[position++] = (byte)(zeroByte + (div = (num3 * 5243L) >> 19));
                    num3 -= div * 100;
                L10:
                    bufferBytes[position++] = (byte)(zeroByte + (div = (num3 * 6554L) >> 16));
                    num3 -= div * 10;
                L9:
                    bufferBytes[position++] = (byte)(zeroByte + (num3));
                }
            L8:
                bufferBytes[position++] = (byte)(zeroByte + (div = (num2 * 8389L) >> 23));
                num2 -= div * 1000;
            L7:
                bufferBytes[position++] = (byte)(zeroByte + (div = (num2 * 5243L) >> 19));
                num2 -= div * 100;
            L6:
                bufferBytes[position++] = (byte)(zeroByte + (div = (num2 * 6554L) >> 16));
                num2 -= div * 10;
            L5:
                bufferBytes[position++] = (byte)(zeroByte + (num2));
            }
        L4:
            bufferBytes[position++] = (byte)(zeroByte + (div = (num1 * 8389L) >> 23));
            num1 -= div * 1000;
        L3:
            bufferBytes[position++] = (byte)(zeroByte + (div = (num1 * 5243L) >> 19));
            num1 -= div * 100;
        L2:
            bufferBytes[position++] = (byte)(zeroByte + (div = (num1 * 6554L) >> 16));
            num1 -= div * 10;
        L1:
            bufferBytes[position++] = (byte)(zeroByte + (num1));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteUInt64Bytes(ulong value)
        {
            ulong num1 = value, num2, num3, num4, num5, div;

            if (num1 < 10000)
            {
                if (num1 < 10)
                {
                    EnsureBufferSize(1);
                    goto L1;
                }
                if (num1 < 100)
                {
                    EnsureBufferSize(2);
                    goto L2;
                }
                if (num1 < 1000)
                {
                    EnsureBufferSize(3);
                    goto L3;
                }
                EnsureBufferSize(4);
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
                        EnsureBufferSize(5);
                        goto L5;
                    }
                    if (num2 < 100)
                    {
                        EnsureBufferSize(6);
                        goto L6;
                    }
                    if (num2 < 1000)
                    {
                        EnsureBufferSize(7);
                        goto L7;
                    }
                    EnsureBufferSize(8);
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
                            EnsureBufferSize(9);
                            goto L9;
                        }
                        if (num3 < 100)
                        {
                            EnsureBufferSize(10);
                            goto L10;
                        }
                        if (num3 < 1000)
                        {
                            EnsureBufferSize(11);
                            goto L11;
                        }
                        EnsureBufferSize(12);
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
                                EnsureBufferSize(13);
                                goto L13;
                            }
                            if (num4 < 100)
                            {
                                EnsureBufferSize(14);
                                goto L14;
                            }
                            if (num4 < 1000)
                            {
                                EnsureBufferSize(15);
                                goto L15;
                            }
                            EnsureBufferSize(16);
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
                                    EnsureBufferSize(17);
                                    goto L17;
                                }
                                if (num5 < 100)
                                {
                                    EnsureBufferSize(18);
                                    goto L18;
                                }
                                if (num5 < 1000)
                                {
                                    EnsureBufferSize(19);
                                    goto L19;
                                }
                                EnsureBufferSize(20);
                                goto L20;
                            }
                        L20:
                            bufferBytes[position++] = (byte)(zeroByte + (div = (num5 * 8389UL) >> 23));
                            num5 -= div * 1000;
                        L19:
                            bufferBytes[position++] = (byte)(zeroByte + (div = (num5 * 5243UL) >> 19));
                            num5 -= div * 100;
                        L18:
                            bufferBytes[position++] = (byte)(zeroByte + (div = (num5 * 6554UL) >> 16));
                            num5 -= div * 10;
                        L17:
                            bufferBytes[position++] = (byte)(zeroByte + (num5));
                        }
                    L16:
                        bufferBytes[position++] = (byte)(zeroByte + (div = (num4 * 8389UL) >> 23));
                        num4 -= div * 1000;
                    L15:
                        bufferBytes[position++] = (byte)(zeroByte + (div = (num4 * 5243UL) >> 19));
                        num4 -= div * 100;
                    L14:
                        bufferBytes[position++] = (byte)(zeroByte + (div = (num4 * 6554UL) >> 16));
                        num4 -= div * 10;
                    L13:
                        bufferBytes[position++] = (byte)(zeroByte + (num4));
                    }
                L12:
                    bufferBytes[position++] = (byte)(zeroByte + (div = (num3 * 8389UL) >> 23));
                    num3 -= div * 1000;
                L11:
                    bufferBytes[position++] = (byte)(zeroByte + (div = (num3 * 5243UL) >> 19));
                    num3 -= div * 100;
                L10:
                    bufferBytes[position++] = (byte)(zeroByte + (div = (num3 * 6554UL) >> 16));
                    num3 -= div * 10;
                L9:
                    bufferBytes[position++] = (byte)(zeroByte + (num3));
                }
            L8:
                bufferBytes[position++] = (byte)(zeroByte + (div = (num2 * 8389UL) >> 23));
                num2 -= div * 1000;
            L7:
                bufferBytes[position++] = (byte)(zeroByte + (div = (num2 * 5243UL) >> 19));
                num2 -= div * 100;
            L6:
                bufferBytes[position++] = (byte)(zeroByte + (div = (num2 * 6554UL) >> 16));
                num2 -= div * 10;
            L5:
                bufferBytes[position++] = (byte)(zeroByte + (num2));
            }
        L4:
            bufferBytes[position++] = (byte)(zeroByte + (div = (num1 * 8389UL) >> 23));
            num1 -= div * 1000;
        L3:
            bufferBytes[position++] = (byte)(zeroByte + (div = (num1 * 5243UL) >> 19));
            num1 -= div * 100;
        L2:
            bufferBytes[position++] = (byte)(zeroByte + (div = (num1 * 6554UL) >> 16));
            num1 -= div * 10;
        L1:
            bufferBytes[position++] = (byte)(zeroByte + (num1));
        }
    }
}