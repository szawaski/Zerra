// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zerra.IO
{
    public partial struct ByteReader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadIsNull(out bool value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = buffer[position++] == nullByte;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBoolean(out bool value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = buffer[position++] != 0;

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBooleanNullable(bool nullFlags, out bool? value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            value = buffer[position++] != 0;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBooleanArray(int length, out bool[] value, out int sizeNeeded)
        {
            sizeNeeded = length;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new bool[length];
            for (var i = 0; i < length; i++)
            {
                var item = buffer[position++] != 0;
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBooleanList(int length, out List<bool> value, out int sizeNeeded)
        {
            sizeNeeded = length;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<bool>(length);
            for (var i = 0; i < length; i++)
            {
                var item = buffer[position++] != 0;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBooleanNullableArray(int length, out bool?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new bool?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = buffer[position++] != 0;
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadBooleanNullableList(int length, out List<bool?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<bool?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = buffer[position++] != 0;
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadByte(out byte value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = buffer[position++];
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadByteNullable(bool nullFlags, out byte? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 2 : 1;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            value = buffer[position++];
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadByteArray(int length, out byte[] value, out int sizeNeeded)
        {
            sizeNeeded = length;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new byte[length];
            fixed (byte* pArray = value)
            {
                for (var i = 0; i < length; i++)
                {
                    pArray[i] = buffer[position++];
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadByteList(int length, out List<byte> value, out int sizeNeeded)
        {
            sizeNeeded = length;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<byte>(length);
            for (var i = 0; i < length; i++)
            {
                var item = buffer[position++];
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadByteNullableArray(int length, out byte?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new byte?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = buffer[position++];
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadByteNullableList(int length, out List<byte?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<byte?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = buffer[position++];
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSByte(out sbyte value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = (sbyte)buffer[position++];
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSByteNullable(bool nullFlags, out sbyte? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 2 : 1;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            value = (sbyte)buffer[position++];
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSByteArray(int length, out sbyte[] value, out int sizeNeeded)
        {
            sizeNeeded = length;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new sbyte[length];
            for (var i = 0; i < length; i++)
            {
                var item = (sbyte)buffer[position++];
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSByteList(int length, out List<sbyte> value, out int sizeNeeded)
        {
            sizeNeeded = length;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<sbyte>(length);
            for (var i = 0; i < length; i++)
            {
                var item = (sbyte)buffer[position++];
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSByteNullableArray(int length, out sbyte?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new sbyte?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (sbyte)buffer[position++];
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSByteNullableList(int length, out List<sbyte?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                ;
                return false;
            }

            value = new List<sbyte?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (sbyte)buffer[position++];
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt16(out short value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = (short)(buffer[position++] | buffer[position++] << 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt16Nullable(bool nullFlags, out short? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 3 : 2;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            value = (short)(buffer[position++] | buffer[position++] << 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt16Array(int length, out short[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new short[length];
            for (var i = 0; i < length; i++)
            {
                var item = (short)(buffer[position++] | buffer[position++] << 8);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt16List(int length, out List<short> value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<short>(length);
            for (var i = 0; i < length; i++)
            {
                var item = (short)(buffer[position++] | buffer[position++] << 8);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt16NullableArray(int length, out short?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 3;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new short?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (short)(buffer[position++] | buffer[position++] << 8);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt16NullableList(int length, out List<short?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 3;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<short?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (short)(buffer[position++] | buffer[position++] << 8);
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt16(out ushort value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = (ushort)(buffer[position++] | buffer[position++] << 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt16Nullable(bool nullFlags, out ushort? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 3 : 2;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            value = (ushort)(buffer[position++] | buffer[position++] << 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt16Array(int length, out ushort[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new ushort[length];
            for (var i = 0; i < length; i++)
            {
                var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt16List(int length, out List<ushort> value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<ushort>(length);
            for (var i = 0; i < length; i++)
            {
                var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt16NullableArray(int length, out ushort?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 3;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new ushort?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt16NullableList(int length, out List<ushort?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 3;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<ushort?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt32(out int value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt32Nullable(bool nullFlags, out int? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 5 : 4;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            value = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt32Array(int length, out int[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 4;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new int[length];
            for (var i = 0; i < length; i++)
            {
                var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt32List(int length, out List<int> value, out int sizeNeeded)
        {
            sizeNeeded = length * 4;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<int>(length);
            for (var i = 0; i < length; i++)
            {
                var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt32NullableArray(int length, out int?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 5;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new int?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt32NullableList(int length, out List<int?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 5;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<int?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt32(out uint value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt32Nullable(bool nullFlags, out uint? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 5 : 4;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            value = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt32Array(int length, out uint[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 4;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new uint[length];
            for (var i = 0; i < length; i++)
            {
                var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt32List(int length, out List<uint> value, out int sizeNeeded)
        {
            sizeNeeded = length * 4;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<uint>(length);
            for (var i = 0; i < length; i++)
            {
                var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt32NullableArray(int length, out uint?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 5;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new uint?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt32NullableList(int length, out List<uint?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 5;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<uint?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt64(out long value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            value = (long)((ulong)hi) << 32 | lo;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt64Nullable(bool nullFlags, out long? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 9 : 8;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            value = (long)((ulong)hi) << 32 | lo;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt64Array(int length, out long[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 8;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new long[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = (long)((ulong)hi) << 32 | lo;
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt64List(int length, out List<long> value, out int sizeNeeded)
        {
            sizeNeeded = length * 8;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<long>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = (long)((ulong)hi) << 32 | lo;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt64NullableArray(int length, out long?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 9;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new long?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = (long)((ulong)hi) << 32 | lo;
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadInt64NullableList(int length, out List<long?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 9;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<long?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = (long)((ulong)hi) << 32 | lo;
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt64(out ulong value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            value = ((ulong)hi) << 32 | lo;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt64Nullable(bool nullFlags, out ulong? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 9 : 8;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            value = ((ulong)hi) << 32 | lo;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt64Array(int length, out ulong[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 8;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new ulong[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = ((ulong)hi) << 32 | lo;
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt64List(int length, out List<ulong> value, out int sizeNeeded)
        {
            sizeNeeded = length * 8;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<ulong>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = ((ulong)hi) << 32 | lo;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt64NullableArray(int length, out ulong?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 9;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new ulong?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = ((ulong)hi) << 32 | lo;
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUInt64NullableList(int length, out List<ulong?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 9;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<ulong?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = ((ulong)hi) << 32 | lo;
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadSingle(out float value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            value = *((float*)&tmpBuffer);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadSingleNullable(bool nullFlags, out float? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 5 : 4;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            value = *((float*)&tmpBuffer);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadSingleArray(int length, out float[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 4;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new float[length];
            for (var i = 0; i < length; i++)
            {
                var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = *((float*)&tmpBuffer);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadSingleList(int length, out List<float> value, out int sizeNeeded)
        {
            sizeNeeded = length * 4;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<float>(length);
            for (var i = 0; i < length; i++)
            {
                var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = *((float*)&tmpBuffer);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadSingleNullableArray(int length, out float?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 5;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new float?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = *((float*)&tmpBuffer);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadSingleNullableList(int length, out List<float?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 5;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<float?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = *((float*)&tmpBuffer);
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadDouble(out double value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var tmpBuffer = ((ulong)hi) << 32 | lo;
            value = *((double*)&tmpBuffer);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadDoubleNullable(bool nullFlags, out double? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 9 : 8;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var tmpBuffer = ((ulong)hi) << 32 | lo;
            value = *((double*)&tmpBuffer);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadDoubleArray(int length, out double[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 8;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new double[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var tmpBuffer = ((ulong)hi) << 32 | lo;
                var item = *((double*)&tmpBuffer);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadDoubleList(int length, out List<double> value, out int sizeNeeded)
        {
            sizeNeeded = length * 8;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<double>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var tmpBuffer = ((ulong)hi) << 32 | lo;
                var item = *((double*)&tmpBuffer);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadDoubleNullableArray(int length, out double?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 9;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new double?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var tmpBuffer = ((ulong)hi) << 32 | lo;
                    var item = *((double*)&tmpBuffer);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadDoubleNullableList(int length, out List<double?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 9;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<double?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var tmpBuffer = ((ulong)hi) << 32 | lo;
                    var item = *((double*)&tmpBuffer);
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDecimal(out decimal value, out int sizeNeeded)
        {
            sizeNeeded = 16;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            value = new decimal(new int[] { lo, mid, hi, flags });
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDecimalNullable(bool nullFlags, out decimal? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 17 : 16;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            value = new decimal(new int[] { lo, mid, hi, flags });
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDecimalArray(int length, out decimal[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 16;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new decimal[length];
            for (var i = 0; i < length; i++)
            {
                var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var item = new decimal(new int[] { lo, mid, hi, flags });
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDecimalList(int length, out List<decimal> value, out int sizeNeeded)
        {
            sizeNeeded = length * 16;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<decimal>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var item = new decimal(new int[] { lo, mid, hi, flags });
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDecimalNullableArray(int length, out decimal?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 17;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new decimal?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var item = new decimal(new int[] { lo, mid, hi, flags });
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDecimalNullableList(int length, out List<decimal?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 17;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<decimal?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var item = new decimal(new int[] { lo, mid, hi, flags });
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTime(out DateTime value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            value = new DateTime(ticks);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTimeNullable(bool nullFlags, out DateTime? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 9 : 8;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            value = new DateTime(ticks);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTimeArray(int length, out DateTime[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 8;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new DateTime[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new DateTime(ticks);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTimeList(int length, out List<DateTime> value, out int sizeNeeded)
        {
            sizeNeeded = length * 8;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<DateTime>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new DateTime(ticks);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTimeNullableArray(int length, out DateTime?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 9;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new DateTime?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new DateTime(ticks);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTimeNullableList(int length, out List<DateTime?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 9;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<DateTime?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new DateTime(ticks);
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTimeOffset(out DateTimeOffset value, out int sizeNeeded)
        {
            sizeNeeded = 10;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            var offset = (short)(buffer[position++] | buffer[position++] << 8);
            value = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTimeOffsetNullable(bool nullFlags, out DateTimeOffset? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 11 : 10;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            var offset = (short)(buffer[position++] | buffer[position++] << 8);
            value = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTimeOffsetArray(int length, out DateTimeOffset[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 10;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new DateTimeOffset[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var offset = (short)(buffer[position++] | buffer[position++] << 8);
                var item = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTimeOffsetList(int length, out List<DateTimeOffset> value, out int sizeNeeded)
        {
            sizeNeeded = length * 10;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<DateTimeOffset>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var offset = (short)(buffer[position++] | buffer[position++] << 8);
                var item = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTimeOffsetNullableArray(int length, out DateTimeOffset?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 11;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new DateTimeOffset?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var offset = (short)(buffer[position++] | buffer[position++] << 8);
                    var item = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadDateTimeOffsetNullableList(int length, out List<DateTimeOffset?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 11;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<DateTimeOffset?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var offset = (short)(buffer[position++] | buffer[position++] << 8);
                    var item = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadTimeSpan(out TimeSpan value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            value = new TimeSpan(ticks);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadTimeSpanNullable(bool nullFlags, out TimeSpan? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 9 : 8;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            value = new TimeSpan(ticks);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadTimeSpanArray(int length, out TimeSpan[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 8;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new TimeSpan[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new TimeSpan(ticks);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadTimeSpanList(int length, out List<TimeSpan> value, out int sizeNeeded)
        {
            sizeNeeded = length * 8;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<TimeSpan>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new TimeSpan(ticks);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadTimeSpanNullableArray(int length, out TimeSpan?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 9;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new TimeSpan?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new TimeSpan(ticks);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadTimeSpanNullableList(int length, out List<TimeSpan?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 9;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<TimeSpan?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new TimeSpan(ticks);
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadGuid(out Guid value, out int sizeNeeded)
        {
            sizeNeeded = 16;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            fixed (byte* pBuffer = &buffer[position], pGuidBuffer = guidBuffer)
            {
                for (var p = 0; p < 16; p++)
                {
                    pGuidBuffer[p] = pBuffer[p];
                }
            }
            position += 16;

#if NETSTANDARD2_0
            value = new Guid(guidBuffer.ToArray());
#else
            value = new Guid(guidBuffer);
#endif
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadGuidNullable(bool nullFlags, out Guid? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 17 : 16;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }

            fixed (byte* pBuffer = &buffer[position], pGuidBuffer = guidBuffer)
            {
                for (var p = 0; p < 16; p++)
                {
                    pGuidBuffer[p] = pBuffer[p];
                }
            }
            position += 16;

#if NETSTANDARD2_0
            value = new Guid(guidBuffer.ToArray());
#else
            value = new Guid(guidBuffer);
#endif
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadGuidArray(int length, out Guid[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 16;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new Guid[length];
            for (var i = 0; i < length; i++)
            {
                fixed (byte* pBuffer = &buffer[position], pGuidBuffer = guidBuffer)
                {
                    for (var p = 0; p < 16; p++)
                    {
                        pGuidBuffer[p] = pBuffer[p];
                    }
                }
                position += 16;
#if NETSTANDARD2_0
                var item = new Guid(guidBuffer.ToArray());
#else
                var item = new Guid(guidBuffer);
#endif
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadGuidList(int length, out List<Guid> value, out int sizeNeeded)
        {
            sizeNeeded = length * 16;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<Guid>(length);
            for (var i = 0; i < length; i++)
            {
                fixed (byte* pBuffer = &buffer[position], pGuidBuffer = guidBuffer)
                {
                    for (var p = 0; p < 16; p++)
                    {
                        pGuidBuffer[p] = pBuffer[p];
                    }
                }
                position += 16;
#if NETSTANDARD2_0
                var item = new Guid(guidBuffer.ToArray());
#else
                var item = new Guid(guidBuffer);
#endif
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadGuidNullableArray(int length, out Guid?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 17;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new Guid?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    fixed (byte* pBuffer = &buffer[position], pGuidBuffer = guidBuffer)
                    {
                        for (var p = 0; p < 16; p++)
                        {
                            pGuidBuffer[p] = pBuffer[p];
                        }
                    }
                    position += 16;
#if NETSTANDARD2_0
                    var item = new Guid(guidBuffer.ToArray());
#else
                    var item = new Guid(guidBuffer);
#endif
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadGuidNullableList(int length, out List<Guid?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 17;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<Guid?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    fixed (byte* pBuffer = &buffer[position], pGuidBuffer = guidBuffer)
                    {
                        for (var p = 0; p < 16; p++)
                        {
                            pGuidBuffer[p] = pBuffer[p];
                        }
                    }
                    position += 16;
#if NETSTANDARD2_0
                    var item = new Guid(guidBuffer.ToArray());
#else
                    var item = new Guid(guidBuffer);
#endif
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadChar(out char value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            fixed (byte* pBuffer = &buffer[position])
            {
                value = (char)*(short*)pBuffer;
                position += 2;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadCharNullable(bool nullFlags, out char? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 3 : 2;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                {
                    value = null;
                    return true;
                }
            }
            fixed (byte* pBuffer = &buffer[position])
            {
                value = (char)*(short*)pBuffer;
                position += 2;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadCharArray(int length, out char[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new char[length];
            fixed (char* pArray = value)
            {
                for (var i = 0; i < length; i++)
                {
                    fixed (byte* pBuffer = &buffer[position])
                    {
                        pArray[i] = (char)*(short*)pBuffer;
                    }
                    position += 2;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadCharList(int length, out List<char> value, out int sizeNeeded)
        {
            sizeNeeded = length * 2;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<char>(length);
            for (var i = 0; i < length; i++)
            {
                char item;
                fixed (byte* pBuffer = &buffer[position])
                {
                    item = (char)*(short*)pBuffer;
                }
                position += 2;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadCharNullableArray(int length, out char?[] value, out int sizeNeeded)
        {
            sizeNeeded = length * 3;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new char?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    char item;
                    fixed (byte* pBuffer = &buffer[position])
                    {
                        item = (char)*(short*)pBuffer;
                    }
                    position += 2;
                    value[i] = item;
                }
            }

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadCharNullableList(int length, out List<char?> value, out int sizeNeeded)
        {
            sizeNeeded = length * 3;
            if (!isFinalBlock && this.length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            value = new List<char?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    char item;
                    fixed (byte* pBuffer = &buffer[position])
                    {
                        item = (char)*(short*)pBuffer;
                    }
                    position += 2;
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadStringLength(bool nullFlags, out int? value, out int sizeNeeded)
        {
            sizeNeeded = nullFlags ? 5 : 4;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            if (nullFlags && buffer[position++] == nullByte)
            {
                value = null;
                return true;
            }
            value = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryReadString(int byteLength, out string value, out int sizeNeeded)
        {
            sizeNeeded = byteLength;
            if (!isFinalBlock && length - position < sizeNeeded)
            {
                value = default;
                return false;
            }

            var bytesForString = buffer.Slice(position, byteLength);
            fixed (byte* p = bytesForString)
            {
                value = encoding.GetString(p, sizeNeeded);
            }
            position += sizeNeeded;
            return true;
        }
    }
}
