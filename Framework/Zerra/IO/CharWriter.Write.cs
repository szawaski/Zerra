// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.IO
{
    public ref partial struct CharWriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            EnsureBufferSize(4);
            WriteUInt64(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(sbyte value)
        {
            EnsureBufferSize(3);
            WriteInt64(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(short value)
        {
            EnsureBufferSize(6);
            WriteInt64(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort value)
        {
            EnsureBufferSize(5);
            WriteUInt64(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            EnsureBufferSize(11);
            WriteInt64(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value)
        {
            EnsureBufferSize(10);
            WriteUInt64(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long value)
        {
            EnsureBufferSize(20);
            WriteInt64(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong value)
        {
            EnsureBufferSize(20);
            WriteUInt64(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(float value)
        {
            EnsureBufferSize(16); //approx
            Write(value.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(double value)
        {
            EnsureBufferSize(32); //approx
            Write(value.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(decimal value)
        {
            EnsureBufferSize(31);
            Write(value.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(char value)
        {
            EnsureBufferSize(1);
            buffer[position++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(string value)
        {
            if (value == null)
                return;
            var length = value.Length;
            if (length == 0)
                return;

            EnsureBufferSize(length);

            var valueSpan = value.AsSpan();
            var pCount = value.Length;
            fixed (char* pSource = value, pBuffer = &buffer[position])
            {
                for (var p = 0; p < pCount; p++)
                {
                    pBuffer[p] = pSource[p];
                }
            }
            position += pCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(char[] value, int index, int count)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            EnsureBufferSize(count);
            var pCount = value.Length;
            fixed (char* pSource = value, pBuffer = &buffer[position])
            {
                for (var p = 0; p < pCount; p++)
                {
                    pBuffer[p] = pSource[p];
                }
            }
            position += pCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(DateTime value, DateTimeFormat format)
        {
            switch (format)
            {
                case DateTimeFormat.ISO8601:
                    {
                        //yyyy-MM-ddTHH:mm:ss.fffffff+00:00
                        EnsureBufferSize(33);

                        if (value.Year < 10)
                            buffer[position++] = '0';
                        if (value.Year < 100)
                            buffer[position++] = '0';
                        if (value.Year < 1000)
                            buffer[position++] = '0';
                        WriteInt64(value.Year);
                        buffer[position++] = '-';

                        if (value.Month < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Month);
                        buffer[position++] = '-';

                        if (value.Day < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Day);

                        buffer[position++] = 'T';

                        if (value.Hour < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Hour);
                        buffer[position++] = ':';

                        if (value.Minute < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Minute);
                        buffer[position++] = ':';

                        if (value.Second < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Second);
                        buffer[position++] = '.';

                        var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000;
                        if (fraction < 10)
                            buffer[position++] = '0';
                        if (fraction < 100)
                            buffer[position++] = '0';
                        if (fraction < 1000)
                            buffer[position++] = '0';
                        if (fraction < 10000)
                            buffer[position++] = '0';
                        if (fraction < 100000)
                            buffer[position++] = '0';
                        if (fraction < 1000000)
                            buffer[position++] = '0';
                        WriteInt64(fraction);

                        switch (value.Kind)
                        {
                            case DateTimeKind.Utc:
                                {
                                    buffer[position++] = 'Z';
                                    break;
                                }
                            case DateTimeKind.Local:
                                {
                                    var offset = (DateTimeOffset)value;
                                    if (offset.Offset.Hours > 0)
                                        buffer[position++] = '+';
                                    else
                                        buffer[position++] = '-';
                                    if (offset.Offset.Hours < 10)
                                        buffer[position++] = '0';
                                    WriteInt64(offset.Offset.Hours < 0 ? -offset.Offset.Hours : offset.Offset.Hours);
                                    buffer[position++] = ':';

                                    if (offset.Offset.Minutes < 10)
                                        buffer[position++] = '0';
                                    WriteInt64(offset.Offset.Minutes);
                                    break;
                                }
                            case DateTimeKind.Unspecified:
                                {
                                    //nothing
                                    break;
                                }
                            default: throw new NotImplementedException();
                        }
                        break;
                    }
                case DateTimeFormat.MsSql:
                    {
                        //yyyy-MM-dd HH:mm:ss.fff
                        EnsureBufferSize(23);

                        if (value.Year < 10)
                            buffer[position++] = '0';
                        if (value.Year < 100)
                            buffer[position++] = '0';
                        if (value.Year < 1000)
                            buffer[position++] = '0';
                        WriteInt64(value.Year);
                        buffer[position++] = '-';

                        if (value.Month < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Month);
                        buffer[position++] = '-';

                        if (value.Day < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Day);

                        buffer[position++] = ' ';

                        if (value.Hour < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Hour);
                        buffer[position++] = ':';

                        if (value.Minute < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Minute);
                        buffer[position++] = ':';

                        if (value.Second < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Second);
                        buffer[position++] = '.';

                        var fraction = (value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000) / 10000;
                        if (fraction < 10)
                            buffer[position++] = '0';
                        if (fraction < 100)
                            buffer[position++] = '0';
                        WriteInt64(fraction);

                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(DateTimeOffset value, DateTimeFormat format)
        {
            switch (format)
            {
                case DateTimeFormat.ISO8601:
                    {
                        //yyyy-MM-ddTHH:mm:ss.fffffff+00:00
                        EnsureBufferSize(33);

                        if (value.Year < 10)
                            buffer[position++] = '0';
                        if (value.Year < 100)
                            buffer[position++] = '0';
                        if (value.Year < 1000)
                            buffer[position++] = '0';
                        WriteInt64(value.Year);
                        buffer[position++] = '-';

                        if (value.Month < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Month);
                        buffer[position++] = '-';

                        if (value.Day < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Day);

                        buffer[position++] = 'T';

                        if (value.Hour < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Hour);
                        buffer[position++] = ':';

                        if (value.Minute < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Minute);
                        buffer[position++] = ':';

                        if (value.Second < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Second);
                        buffer[position++] = '.';

                        var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000;
                        if (fraction < 10)
                            buffer[position++] = '0';
                        if (fraction < 100)
                            buffer[position++] = '0';
                        if (fraction < 1000)
                            buffer[position++] = '0';
                        if (fraction < 10000)
                            buffer[position++] = '0';
                        if (fraction < 100000)
                            buffer[position++] = '0';
                        if (fraction < 1000000)
                            buffer[position++] = '0';
                        WriteInt64(fraction);

                        if (value.Offset.Hours > 0)
                            buffer[position++] = '+';
                        else
                            buffer[position++] = '-';
                        if (value.Offset.Hours < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Offset.Hours < 0 ? -value.Offset.Hours : value.Offset.Hours);
                        buffer[position++] = ':';

                        if (value.Offset.Minutes < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Offset.Minutes);

                        break;
                    }
                case DateTimeFormat.MsSql:
                    {
                        //yyyy-MM-dd HH:mm:ss.fff zzz
                        EnsureBufferSize(27);

                        if (value.Year < 10)
                            buffer[position++] = '0';
                        if (value.Year < 100)
                            buffer[position++] = '0';
                        if (value.Year < 1000)
                            buffer[position++] = '0';
                        WriteInt64(value.Year);
                        buffer[position++] = '-';

                        if (value.Month < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Month);
                        buffer[position++] = '-';

                        if (value.Day < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Day);

                        buffer[position++] = ' ';

                        if (value.Hour < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Hour);
                        buffer[position++] = ':';

                        if (value.Minute < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Minute);
                        buffer[position++] = ':';

                        if (value.Second < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Second);
                        buffer[position++] = '.';

                        var fraction = (value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000) / 10000;
                        if (fraction < 10)
                            buffer[position++] = '0';
                        if (fraction < 100)
                            buffer[position++] = '0';
                        WriteInt64(fraction);

                        buffer[position++] = ' ';

                        if (value.Offset.Hours > 0)
                            buffer[position++] = '+';
                        else
                            buffer[position++] = '-';
                        if (value.Offset.Hours < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Offset.Hours < 0 ? -value.Offset.Hours : value.Offset.Hours);
                        buffer[position++] = ':';

                        if (value.Offset.Minutes < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Offset.Minutes);

                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(TimeSpan value, TimeFormat format)
        {
            switch (format)
            {
                case TimeFormat.ISO8601:
                case TimeFormat.MsSql:
                    //HH:mm:ss.fffffff
                    EnsureBufferSize(16);

                    if (value.TotalHours < 10)
                        buffer[position++] = '0';
                    WriteInt64(value.Hours);
                    buffer[position++] = ':';

                    if (value.Minutes < 10)
                        buffer[position++] = '0';
                    WriteInt64(value.Minutes);
                    buffer[position++] = ':';

                    if (value.Seconds < 10)
                        buffer[position++] = '0';
                    WriteInt64(value.Seconds);
                    buffer[position++] = '.';

                    var fraction = value.Ticks - (value.Ticks / 10000000) * 10000000;
                    if (fraction < 10)
                        buffer[position++] = '0';
                    if (fraction < 100)
                        buffer[position++] = '0';
                    if (fraction < 1000)
                        buffer[position++] = '0';
                    if (fraction < 10000)
                        buffer[position++] = '0';
                    if (fraction < 100000)
                        buffer[position++] = '0';
                    if (fraction < 1000000)
                        buffer[position++] = '0';
                    WriteInt64(fraction);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(Guid value)
        {
            Write(value.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(byte[] value, ByteFormat format)
        {
            switch (format)
            {
                case ByteFormat.Hex:
                    EnsureBufferSize(2 * value.Length);
                    fixed (byte* pValue = value)
                    fixed (char* pBuffer = &buffer[position])
                    {
                        for (var p = 0; p < value.Length; p++)
                        {
                            var b = pValue[p];
                            var v1 = b / 16;
                            var v2 = b - v1 * 16;
                            switch (v1)
                            {
                                case 0: pBuffer[p * 2] = '0'; break;
                                case 1: pBuffer[p * 2] = '1'; break;
                                case 2: pBuffer[p * 2] = '2'; break;
                                case 3: pBuffer[p * 2] = '3'; break;
                                case 4: pBuffer[p * 2] = '4'; break;
                                case 5: pBuffer[p * 2] = '5'; break;
                                case 6: pBuffer[p * 2] = '6'; break;
                                case 7: pBuffer[p * 2] = '7'; break;
                                case 8: pBuffer[p * 2] = '8'; break;
                                case 9: pBuffer[p * 2] = '9'; break;
                                case 10: pBuffer[p * 2] = 'a'; break;
                                case 11: pBuffer[p * 2] = 'b'; break;
                                case 12: pBuffer[p * 2] = 'c'; break;
                                case 13: pBuffer[p * 2] = 'd'; break;
                                case 14: pBuffer[p * 2] = 'e'; break;
                                case 15: pBuffer[p * 2] = 'f'; break;
                            }
                            switch (v2)
                            {
                                case 0: pBuffer[p * 2 + 1] = '0'; break;
                                case 1: pBuffer[p * 2 + 1] = '1'; break;
                                case 2: pBuffer[p * 2 + 1] = '2'; break;
                                case 3: pBuffer[p * 2 + 1] = '3'; break;
                                case 4: pBuffer[p * 2 + 1] = '4'; break;
                                case 5: pBuffer[p * 2 + 1] = '5'; break;
                                case 6: pBuffer[p * 2 + 1] = '6'; break;
                                case 7: pBuffer[p * 2 + 1] = '7'; break;
                                case 8: pBuffer[p * 2 + 1] = '8'; break;
                                case 9: pBuffer[p * 2 + 1] = '9'; break;
                                case 10: pBuffer[p * 2 + 1] = 'a'; break;
                                case 11: pBuffer[p * 2 + 1] = 'b'; break;
                                case 12: pBuffer[p * 2 + 1] = 'c'; break;
                                case 13: pBuffer[p * 2 + 1] = 'd'; break;
                                case 14: pBuffer[p * 2 + 1] = 'e'; break;
                                case 15: pBuffer[p * 2 + 1] = 'f'; break;
                            }
                        }
                    }
                    position += value.Length * 2;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(in CharWriter value)
        {
            EnsureBufferSize(value.position);
            var pCount = value.position;

            fixed (char* pSource = value.buffer, pBuffer = &buffer[position])
            {
                for (var p = 0; p < pCount; p++)
                {
                    pBuffer[p] = pSource[p];
                }
            }
            position += pCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteInt64(long value)
        {
            long num1 = value, num2, num3, num4, num5, div;

            if (value < 0)
            {
                if (value == Int64.MinValue)
                {
                    //Min value is one less than max, can't invert signs
                    EnsureBufferSize(20);
                    buffer[position++] = '-';
                    buffer[position++] = '9';
                    buffer[position++] = '2';
                    buffer[position++] = '2';
                    buffer[position++] = '3';
                    buffer[position++] = '3';
                    buffer[position++] = '7';
                    buffer[position++] = '2';
                    buffer[position++] = '0';
                    buffer[position++] = '3';
                    buffer[position++] = '6';
                    buffer[position++] = '8';
                    buffer[position++] = '5';
                    buffer[position++] = '4';
                    buffer[position++] = '7';
                    buffer[position++] = '7';
                    buffer[position++] = '5';
                    buffer[position++] = '8';
                    buffer[position++] = '0';
                    buffer[position++] = '8';
                    return;
                }
                buffer[position++] = '-';
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
                            buffer[position++] = (char)('0' + (div = (num5 * 8389L) >> 23));
                            num5 -= div * 1000;
                        L19:
                            buffer[position++] = (char)('0' + (div = (num5 * 5243L) >> 19));
                            num5 -= div * 100;
                        L18:
                            buffer[position++] = (char)('0' + (div = (num5 * 6554L) >> 16));
                            num5 -= div * 10;
                        L17:
                            buffer[position++] = (char)('0' + (num5));
                        }
                    L16:
                        buffer[position++] = (char)('0' + (div = (num4 * 8389L) >> 23));
                        num4 -= div * 1000;
                    L15:
                        buffer[position++] = (char)('0' + (div = (num4 * 5243L) >> 19));
                        num4 -= div * 100;
                    L14:
                        buffer[position++] = (char)('0' + (div = (num4 * 6554L) >> 16));
                        num4 -= div * 10;
                    L13:
                        buffer[position++] = (char)('0' + (num4));
                    }
                L12:
                    buffer[position++] = (char)('0' + (div = (num3 * 8389L) >> 23));
                    num3 -= div * 1000;
                L11:
                    buffer[position++] = (char)('0' + (div = (num3 * 5243L) >> 19));
                    num3 -= div * 100;
                L10:
                    buffer[position++] = (char)('0' + (div = (num3 * 6554L) >> 16));
                    num3 -= div * 10;
                L9:
                    buffer[position++] = (char)('0' + (num3));
                }
            L8:
                buffer[position++] = (char)('0' + (div = (num2 * 8389L) >> 23));
                num2 -= div * 1000;
            L7:
                buffer[position++] = (char)('0' + (div = (num2 * 5243L) >> 19));
                num2 -= div * 100;
            L6:
                buffer[position++] = (char)('0' + (div = (num2 * 6554L) >> 16));
                num2 -= div * 10;
            L5:
                buffer[position++] = (char)('0' + (num2));
            }
        L4:
            buffer[position++] = (char)('0' + (div = (num1 * 8389L) >> 23));
            num1 -= div * 1000;
        L3:
            buffer[position++] = (char)('0' + (div = (num1 * 5243L) >> 19));
            num1 -= div * 100;
        L2:
            buffer[position++] = (char)('0' + (div = (num1 * 6554L) >> 16));
            num1 -= div * 10;
        L1:
            buffer[position++] = (char)('0' + (num1));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteUInt64(ulong value)
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
                            buffer[position++] = (char)('0' + (div = (num5 * 8389UL) >> 23));
                            num5 -= div * 1000;
                        L19:
                            buffer[position++] = (char)('0' + (div = (num5 * 5243UL) >> 19));
                            num5 -= div * 100;
                        L18:
                            buffer[position++] = (char)('0' + (div = (num5 * 6554UL) >> 16));
                            num5 -= div * 10;
                        L17:
                            buffer[position++] = (char)('0' + (num5));
                        }
                    L16:
                        buffer[position++] = (char)('0' + (div = (num4 * 8389UL) >> 23));
                        num4 -= div * 1000;
                    L15:
                        buffer[position++] = (char)('0' + (div = (num4 * 5243UL) >> 19));
                        num4 -= div * 100;
                    L14:
                        buffer[position++] = (char)('0' + (div = (num4 * 6554UL) >> 16));
                        num4 -= div * 10;
                    L13:
                        buffer[position++] = (char)('0' + (num4));
                    }
                L12:
                    buffer[position++] = (char)('0' + (div = (num3 * 8389UL) >> 23));
                    num3 -= div * 1000;
                L11:
                    buffer[position++] = (char)('0' + (div = (num3 * 5243UL) >> 19));
                    num3 -= div * 100;
                L10:
                    buffer[position++] = (char)('0' + (div = (num3 * 6554UL) >> 16));
                    num3 -= div * 10;
                L9:
                    buffer[position++] = (char)('0' + (num3));
                }
            L8:
                buffer[position++] = (char)('0' + (div = (num2 * 8389UL) >> 23));
                num2 -= div * 1000;
            L7:
                buffer[position++] = (char)('0' + (div = (num2 * 5243UL) >> 19));
                num2 -= div * 100;
            L6:
                buffer[position++] = (char)('0' + (div = (num2 * 6554UL) >> 16));
                num2 -= div * 10;
            L5:
                buffer[position++] = (char)('0' + (num2));
            }
        L4:
            buffer[position++] = (char)('0' + (div = (num1 * 8389UL) >> 23));
            num1 -= div * 1000;
        L3:
            buffer[position++] = (char)('0' + (div = (num1 * 5243UL) >> 19));
            num1 -= div * 100;
        L2:
            buffer[position++] = (char)('0' + (div = (num1 * 6554UL) >> 16));
            num1 -= div * 10;
        L1:
            buffer[position++] = (char)('0' + (num1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(ReadOnlySpan<char> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length == 0)
                return;
            EnsureBufferSize(value.Length);

            var pCount = value.Length;
            fixed (char* pSource = value, pBuffer = &buffer[position])
            {
                for (var p = 0; p < pCount; p++)
                {
                    pBuffer[p] = pSource[p];
                }
            }
            position += pCount;
        }
    }
}