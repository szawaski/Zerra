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
        public bool TryWrite(byte value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded)
                return false;
            WriteUInt64(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(sbyte value, out int sizeNeeded)
        {
            sizeNeeded = 3;
            if (length - position < sizeNeeded)
                return false;
            WriteInt64(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(short value, out int sizeNeeded)
        {
            sizeNeeded = 6;
            if (length - position < sizeNeeded)
                return false;
            WriteInt64(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(ushort value, out int sizeNeeded)
        {
            sizeNeeded = 5;
            if (length - position < sizeNeeded)
                return false;
            WriteUInt64(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(int value, out int sizeNeeded)
        {
            sizeNeeded = 11;
            if (length - position < sizeNeeded)
                return false;
            WriteInt64(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(uint value, out int sizeNeeded)
        {
            sizeNeeded = 10;
            if (length - position < sizeNeeded)
                return false;
            WriteUInt64(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(long value, out int sizeNeeded)
        {
            sizeNeeded = 20;
            if (length - position < sizeNeeded)
                return false;
            WriteInt64(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(ulong value, out int sizeNeeded)
        {
            sizeNeeded = 20;
            if (length - position < sizeNeeded)
                return false;
            WriteUInt64(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(float value, out int sizeNeeded)
        {
            sizeNeeded = 16; //min
            if (length - position < sizeNeeded)
                return false;
            Write(value.ToString());
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(double value, out int sizeNeeded)
        {
            sizeNeeded = 32; //min
            if (length - position < sizeNeeded)
                return false;
            Write(value.ToString());
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(decimal value, out int sizeNeeded)
        {
            sizeNeeded = 31;
            if (length - position < sizeNeeded)
                return false;
            Write(value.ToString());
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(char value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
                return false;
            buffer[position++] = value;
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

            sizeNeeded = value.Length;
            if (length - position < sizeNeeded)
            {
                return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(char[] value, int index, int count, out int sizeNeeded)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            sizeNeeded = count;
            if (length - position < sizeNeeded)
                return false;

            var pCount = value.Length;
            fixed (char* pSource = value, pBuffer = &buffer[position])
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
        public bool TryWrite(DateTime value, DateTimeFormat format, out int sizeNeeded)
        {
            switch (format)
            {
                case DateTimeFormat.ISO8601:
                    {
                        //yyyy-MM-ddTHH:mm:ss.fffffff+00:00
                        sizeNeeded = 33;
                        if (length - position < sizeNeeded)
                            return false;

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

                        return true;
                    }
                case DateTimeFormat.MsSql:
                    {
                        //yyyy-MM-dd HH:mm:ss.fff
                        sizeNeeded = 23;
                        if (length - position < sizeNeeded)
                            return false;

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

                        return true;
                    }
                case DateTimeFormat.MySql:
                    {
                        //yyyy-MM-dd HH:mm:ss.ffffff
                        sizeNeeded = 26;
                        if (length - position < sizeNeeded)
                            return false;

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

                        return true;
                    }
                case DateTimeFormat.PostgreSql:
                    {
                        //yyyy-MM-dd HH:mm:ss.ffffff
                        sizeNeeded = 26;
                        if (length - position < sizeNeeded)
                            return false;

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

                        return true;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateTimeOffset value, DateTimeFormat format, out int sizeNeeded)
        {
            switch (format)
            {
                case DateTimeFormat.ISO8601:
                    {
                        //yyyy-MM-ddTHH:mm:ss.fffffff+00:00
                        sizeNeeded = 33;
                        if (length - position < sizeNeeded)
                            return false;

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

                        return true;
                    }
                case DateTimeFormat.MsSql:
                    {
                        //yyyy-MM-dd HH:mm:ss.fff+00:00
                        sizeNeeded = 29;
                        if (length - position < sizeNeeded)
                            return false;

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

                        return true;
                    }
                case DateTimeFormat.MySql:
                    {
                        //yyyy-MM-dd HH:mm:ss.ffffff
                        sizeNeeded = 26;
                        if (length - position < sizeNeeded)
                            return false;

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

                        return true;
                    }
                case DateTimeFormat.PostgreSql:
                    {
                        //yyyy-MM-dd HH:mm:ss.ffffff+00:00
                        sizeNeeded = 32;
                        if (length - position < sizeNeeded)
                            return false;

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

                        return true;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(TimeSpan value, TimeFormat format, out int sizeNeeded)
        {
            switch (format)
            {
                case TimeFormat.ISO8601:
                    {
                        //HH:mm:ss.fffffff
                        sizeNeeded = 16;
                        if (length - position < sizeNeeded)
                            return false;

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
                            //while (fraction % 10 == 0)  System.Text.Json does all figures
                            //    fraction /= 10;
                            WriteInt64(fraction);
                        }

                        return true;
                    }
                case TimeFormat.MsSql:
                    {
                        //HH:mm:ss.fffffff
                        sizeNeeded = 16;
                        if (length - position < sizeNeeded)
                            return false;

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

                        return true;
                    }
                case TimeFormat.MySql:
                case TimeFormat.PostgreSql:
                    {
                        //HH:mm:ss.ffffff
                        sizeNeeded = 16;
                        if (length - position < sizeNeeded)
                            return false;

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
                            if (fraction < 1000000)
                                buffer[position++] = '0';
                            while (fraction % 10 == 0)
                                fraction /= 10;
                            WriteInt64(fraction);
                        }

                        return true;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(Guid value, out int sizeNeeded)
        {
            return TryWrite(value.ToString(), out sizeNeeded);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(byte[] value, ByteFormat format, out int sizeNeeded)
        {
            switch (format)
            {
                case ByteFormat.Hex:
                    sizeNeeded = 2 * value.Length;
                    if (length - position < sizeNeeded)
                        return false;

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

                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(in CharWriter value, out int sizeNeeded)
        {
            sizeNeeded = value.position;
            if (length - position < sizeNeeded)
                return false;

            var pCount = value.position;

            fixed (char* pSource = value.buffer, pBuffer = &buffer[position])
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
            fixed (char* pSource = value, pBuffer = &buffer[position])
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
}