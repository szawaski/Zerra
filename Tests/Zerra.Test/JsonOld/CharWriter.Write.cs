// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization.Json
{
    public ref partial struct CharWriterOld
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
            EnsureBufferSize(16); //min
            Write(value.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(double value)
        {
            EnsureBufferSize(32); //min
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
        public unsafe void Write(string? value)
        {
            if (value is null)
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
            if (value is null)
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
        public void Write(DateTime value, DateTimeFormatOld format)
        {
            switch (format)
            {
                case DateTimeFormatOld.ISO8601:
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

                        var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
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
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }

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
                                    if (offset.Offset.Hours < 0)
                                        buffer[position++] = '-';
                                    else
                                        buffer[position++] = '+';
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
                case DateTimeFormatOld.MsSql:
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

                        var fraction = (value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000) / 10000;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
                            if (fraction < 10)
                                buffer[position++] = '0';
                            if (fraction < 100)
                                buffer[position++] = '0';
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }

                        break;
                    }
                case DateTimeFormatOld.MySql:
                    {
                        //yyyy-MM-dd HH:mm:ss.ffffff
                        EnsureBufferSize(26);

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

                        var fraction = (value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000) / 10;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
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
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }

                        break;
                    }
                case DateTimeFormatOld.PostgreSql:
                    {
                        //yyyy-MM-dd HH:mm:ss.ffffff
                        EnsureBufferSize(26);

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

                        var fraction = (value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000) / 10;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
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
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }

                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(DateTimeOffset value, DateTimeFormatOld format)
        {
            switch (format)
            {
                case DateTimeFormatOld.ISO8601:
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
                     
                        var fraction = value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
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
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }

                        if (value.Offset.Hours < 0)
                            buffer[position++] = '-';
                        else
                            buffer[position++] = '+';
                        if (value.Offset.Hours < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Offset.Hours < 0 ? -value.Offset.Hours : value.Offset.Hours);
                        buffer[position++] = ':';

                        if (value.Offset.Minutes < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Offset.Minutes);

                        break;
                    }
                case DateTimeFormatOld.MsSql:
                    {
                        //yyyy-MM-dd HH:mm:ss.fff+00:00
                        EnsureBufferSize(29);

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

                        var fraction = (value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000) / 10000;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
                            if (fraction < 10)
                                buffer[position++] = '0';
                            if (fraction < 100)
                                buffer[position++] = '0';
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }

                        if (value.Offset.Hours < 0)
                            buffer[position++] = '-';
                        else
                            buffer[position++] = '+';
                        if (value.Offset.Hours < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Offset.Hours < 0 ? -value.Offset.Hours : value.Offset.Hours);
                        buffer[position++] = ':';

                        if (value.Offset.Minutes < 10)
                            buffer[position++] = '0';
                        WriteInt64(value.Offset.Minutes);

                        break;
                    }
                case DateTimeFormatOld.MySql:
                    {
                        //yyyy-MM-dd HH:mm:ss.ffffff
                        EnsureBufferSize(26);

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

                        var fraction = (value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000) / 10;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
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
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }

                        break;
                    }
                case DateTimeFormatOld.PostgreSql:
                    {
                        //yyyy-MM-dd HH:mm:ss.ffffff+00:00
                        EnsureBufferSize(32);

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

                        var fraction = (value.TimeOfDay.Ticks - (value.TimeOfDay.Ticks / 10000000) * 10000000) / 10;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
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
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }


                        if (value.Offset.Hours < 0)
                            buffer[position++] = '-';
                        else
                            buffer[position++] = '+';
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
        public void Write(TimeSpan value, TimeFormatOld format)
        {
            switch (format)
            {
                case TimeFormatOld.ISO8601:
                case TimeFormatOld.MsSql:
                    {
                        //(-)dddddddd.HH:mm:ss.fffffff
                        EnsureBufferSize(26);

                        if (value.Ticks < 0)
                            buffer[position++] = '-';

                        if (value.Days > 0)
                        {
                            WriteInt64(value.Days);
                            buffer[position++] = '.';
                        }
                        else if (value.Days < 0)
                        {
                            WriteInt64(-value.Days);
                            buffer[position++] = '.';
                        }

                        if (value.Hours > -1)
                        {
                            if (value.Hours < 10)
                                buffer[position++] = '0';
                            WriteInt64(value.Hours);
                            buffer[position++] = ':';
                        }
                        else
                        {
                            if (-value.Hours < 10)
                                buffer[position++] = '0';
                            WriteInt64(-value.Hours);
                            buffer[position++] = ':';
                        }

                        if (value.Minutes > -1)
                        {
                            if (value.Minutes < 10)
                                buffer[position++] = '0';
                            WriteInt64(value.Minutes);
                            buffer[position++] = ':';
                        }
                        else
                        {
                            if (-value.Minutes < 10)
                                buffer[position++] = '0';
                            WriteInt64(-value.Minutes);
                            buffer[position++] = ':';
                        }

                        if (value.Seconds > -1)
                        {
                            if (value.Seconds < 10)
                                buffer[position++] = '0';
                            WriteInt64(value.Seconds);
                        }
                        else
                        {
                            if (-value.Seconds < 10)
                                buffer[position++] = '0';
                            WriteInt64(-value.Seconds);
                        }

                        long fraction;
                        if (value.Ticks > -1)
                            fraction = value.Ticks - (value.Ticks / 10000000) * 10000000;
                        else
                            fraction = -value.Ticks - (-value.Ticks / 10000000) * 10000000;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
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
                        }

                        break;
                    }
                case TimeFormatOld.MySql:
                case TimeFormatOld.PostgreSql:
                    {
                        //(-)dddddddd.HH:mm:ss.ffffff
                        EnsureBufferSize(25);

                        if (value.Ticks < 0)
                            buffer[position++] = '-';

                        if (value.Days > 0)
                        {
                            WriteInt64(value.Days);
                            buffer[position++] = '.';
                        }
                        else if (value.Days < 0)
                        {
                            WriteInt64(-value.Days);
                            buffer[position++] = '.';
                        }

                        if (value.Hours > -1)
                        {
                            if (value.Hours < 10)
                                buffer[position++] = '0';
                            WriteInt64(value.Hours);
                            buffer[position++] = ':';
                        }
                        else
                        {
                            if (-value.Hours < 10)
                                buffer[position++] = '0';
                            WriteInt64(-value.Hours);
                            buffer[position++] = ':';
                        }

                        if (value.Minutes > -1)
                        {
                            if (value.Minutes < 10)
                                buffer[position++] = '0';
                            WriteInt64(value.Minutes);
                            buffer[position++] = ':';
                        }
                        else
                        {
                            if (-value.Minutes < 10)
                                buffer[position++] = '0';
                            WriteInt64(-value.Minutes);
                            buffer[position++] = ':';
                        }

                        if (value.Seconds > -1)
                        {
                            if (value.Seconds < 10)
                                buffer[position++] = '0';
                            WriteInt64(value.Seconds);
                        }
                        else
                        {
                            if (-value.Seconds < 10)
                                buffer[position++] = '0';
                            WriteInt64(-value.Seconds);
                        }

                        long fraction;
                        if (value.Ticks > -1)
                            fraction = (value.Ticks - (value.Ticks / 10000000) * 10000000) / 10;
                        else
                            fraction = (-value.Ticks - (-value.Ticks / 10000000) * 10000000) / 10;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
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
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }

                        break;
                    }
            }
        }

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(DateOnly value, DateTimeFormatOld format)
        {
            switch (format)
            {
                case DateTimeFormatOld.ISO8601:
                    {
                        //yyyy-MM-dd
                        EnsureBufferSize(10);

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

                        break;
                    }
                case DateTimeFormatOld.MsSql:
                    {
                        //yyyy-MM-dd HH:mm:ss.fff
                        EnsureBufferSize(10);

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

                        break;
                    }
                case DateTimeFormatOld.MySql:
                    {
                        //yyyy-MM-dd HH:mm:ss.ffffff
                        EnsureBufferSize(10);

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

                        break;
                    }
                case DateTimeFormatOld.PostgreSql:
                    {
                        //yyyy-MM-dd HH:mm:ss.ffffff
                        EnsureBufferSize(10);

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

                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(TimeOnly value, TimeFormatOld format)
        {
            switch (format)
            {
                case TimeFormatOld.ISO8601:
                    {
                        //HH:mm:ss.fffffff
                        EnsureBufferSize(16);

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

                        var fraction = value.Ticks - (value.Ticks / 10000000) * 10000000;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
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
                            //while (fraction % 10 == 0) System.Text.Json does all figures
                            //    fraction /= 10;
                            WriteInt64(fraction);
                        }

                        break;
                    }
                case TimeFormatOld.MsSql:
                    {
                        //HH:mm:ss.fffffff
                        EnsureBufferSize(16);

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

                        var fraction = value.Ticks - (value.Ticks / 10000000) * 10000000;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
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
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }

                        break;
                    }
                case TimeFormatOld.MySql:
                case TimeFormatOld.PostgreSql:
                    {
                        //HH:mm:ss.ffffff
                        EnsureBufferSize(15);

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

                        var fraction = (value.Ticks - (value.Ticks / 10000000) * 10000000) / 10;
                        if (fraction > 0)
                        {
                            buffer[position++] = '.';
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
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }

                        break;
                    }
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(Guid value)
        {
            Write(value.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(in CharWriterOld value)
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
        public unsafe void Write(ReadOnlySpan<char> value)
        {
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