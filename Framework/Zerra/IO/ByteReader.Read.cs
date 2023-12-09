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
        public bool ReadIsNull()
        {
            return buffer[position++] == nullByte;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            return buffer[position++] != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool? ReadBooleanNullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            return buffer[position++] != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool[] ReadBooleanArray(int length)
        {
            var array = new bool[length];
            for (var i = 0; i < length; i++)
            {
                var item = buffer[position++] != 0;
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<bool> ReadBooleanList(int length)
        {
            var list = new List<bool>(length);
            for (var i = 0; i < length; i++)
            {
                var item = buffer[position++] != 0;
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<bool> ReadBooleanHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<bool>();
#else
            var set = new HashSet<bool>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var item = buffer[position++] != 0;
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool?[] ReadBooleanNullableArray(int length)
        {
            var array = new bool?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = buffer[position++] != 0;
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<bool?> ReadBooleanNullableList(int length)
        {
            var list = new List<bool?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = buffer[position++] != 0;
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<bool?> ReadBooleanNullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<bool?>();
#else
            var set = new HashSet<bool?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = buffer[position++] != 0;
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            return buffer[position++];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte? ReadByteNullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            return buffer[position++];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte[] ReadByteArray(int length)
        {
            var array = new byte[length];
            fixed (byte* pArray = array)
            {
                for (var i = 0; i < length; i++)
                {
                    pArray[i] = buffer[position++];
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<byte> ReadByteList(int length)
        {
            var list = new List<byte>(length);
            for (var i = 0; i < length; i++)
            {
                var item = buffer[position++];
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<byte> ReadByteHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<byte>();
#else
            var set = new HashSet<byte>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var item = buffer[position++];
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte?[] ReadByteNullableArray(int length)
        {
            var array = new byte?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = buffer[position++];
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<byte?> ReadByteNullableList(int length)
        {
            var list = new List<byte?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = buffer[position++];
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<byte?> ReadByteNullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<byte?>();
#else
            var set = new HashSet<byte?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = buffer[position++];
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte()
        {
            return (sbyte)buffer[position++];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte? ReadSByteNullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            return (sbyte)buffer[position++];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte[] ReadSByteArray(int length)
        {
            var array = new sbyte[length];
            for (var i = 0; i < length; i++)
            {
                var item = (sbyte)buffer[position++];
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<sbyte> ReadSByteList(int length)
        {
            var list = new List<sbyte>(length);
            for (var i = 0; i < length; i++)
            {
                var item = (sbyte)buffer[position++];
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<sbyte> ReadSByteHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<sbyte>();
#else
            var set = new HashSet<sbyte>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var item = (sbyte)buffer[position++];
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte?[] ReadSByteNullableArray(int length)
        {
            var array = new sbyte?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (sbyte)buffer[position++];
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<sbyte?> ReadSByteNullableList(int length)
        {
            var list = new List<sbyte?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (sbyte)buffer[position++];
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<sbyte?> ReadSByteNullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<sbyte?>();
#else
            var set = new HashSet<sbyte?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (sbyte)buffer[position++];
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            return (short)(buffer[position++] | buffer[position++] << 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short? ReadInt16Nullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            return (short)(buffer[position++] | buffer[position++] << 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short[] ReadInt16Array(int length)
        {
            var array = new short[length];
            for (var i = 0; i < length; i++)
            {
                var item = (short)(buffer[position++] | buffer[position++] << 8);
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<short> ReadInt16List(int length)
        {
            var list = new List<short>(length);
            for (var i = 0; i < length; i++)
            {
                var item = (short)(buffer[position++] | buffer[position++] << 8);
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<short> ReadInt16HashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<short>();
#else
            var set = new HashSet<short>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var item = (short)(buffer[position++] | buffer[position++] << 8);
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short?[] ReadInt16NullableArray(int length)
        {
            var array = new short?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (short)(buffer[position++] | buffer[position++] << 8);
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<short?> ReadInt16NullableList(int length)
        {
            var list = new List<short?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (short)(buffer[position++] | buffer[position++] << 8);
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<short?> ReadInt16NullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<short?>();
#else
            var set = new HashSet<short?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (short)(buffer[position++] | buffer[position++] << 8);
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            return (ushort)(buffer[position++] | buffer[position++] << 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort? ReadUInt16Nullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            return (ushort)(buffer[position++] | buffer[position++] << 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort[] ReadUInt16Array(int length)
        {
            var array = new ushort[length];
            for (var i = 0; i < length; i++)
            {
                var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<ushort> ReadUInt16List(int length)
        {
            var list = new List<ushort>(length);
            for (var i = 0; i < length; i++)
            {
                var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<ushort> ReadUInt16HashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<ushort>();
#else
            var set = new HashSet<ushort>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort?[] ReadUInt16NullableArray(int length)
        {
            var array = new ushort?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<ushort?> ReadUInt16NullableList(int length)
        {
            var list = new List<ushort?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<ushort?> ReadUInt16NullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<ushort?>();
#else
            var set = new HashSet<ushort?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            return (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int? ReadInt32Nullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            return (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] ReadInt32Array(int length)
        {
            var array = new int[length];
            for (var i = 0; i < length; i++)
            {
                var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<int> ReadInt32List(int length)
        {
            var list = new List<int>(length);
            for (var i = 0; i < length; i++)
            {
                var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<int> ReadInt32HashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<int>();
#else
            var set = new HashSet<int>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int?[] ReadInt32NullableArray(int length)
        {
            var array = new int?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<int?> ReadInt32NullableList(int length)
        {
            var list = new List<int?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<int?> ReadInt32NullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<int?>();
#else
            var set = new HashSet<int?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            return (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint? ReadUInt32Nullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            return (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint[] ReadUInt32Array(int length)
        {
            var array = new uint[length];
            for (var i = 0; i < length; i++)
            {
                var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<uint> ReadUInt32List(int length)
        {
            var list = new List<uint>(length);
            for (var i = 0; i < length; i++)
            {
                var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<uint> ReadUInt32HashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<uint>();
#else
            var set = new HashSet<uint>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint?[] ReadUInt32NullableArray(int length)
        {
            var array = new uint?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<uint?> ReadUInt32NullableList(int length)
        {
            var list = new List<uint?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<uint?> ReadUInt32NullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<uint?>();
#else
            var set = new HashSet<uint?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long? ReadInt64Nullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long[] ReadInt64Array(int length)
        {
            var array = new long[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = (long)((ulong)hi) << 32 | lo;
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<long> ReadInt64List(int length)
        {
            var list = new List<long>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = (long)((ulong)hi) << 32 | lo;
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<long> ReadInt64HashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<long>();
#else
            var set = new HashSet<long>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = (long)((ulong)hi) << 32 | lo;
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long?[] ReadInt64NullableArray(int length)
        {
            var array = new long?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = (long)((ulong)hi) << 32 | lo;
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<long?> ReadInt64NullableList(int length)
        {
            var list = new List<long?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = (long)((ulong)hi) << 32 | lo;
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<long?> ReadInt64NullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<long?>();
#else
            var set = new HashSet<long?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = (long)((ulong)hi) << 32 | lo;
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return ((ulong)hi) << 32 | lo;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong? ReadUInt64Nullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return ((ulong)hi) << 32 | lo;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong[] ReadUInt64Array(int length)
        {
            var array = new ulong[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = ((ulong)hi) << 32 | lo;
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<ulong> ReadUInt64List(int length)
        {
            var list = new List<ulong>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = ((ulong)hi) << 32 | lo;
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<ulong> ReadUInt64HashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<ulong>();
#else
            var set = new HashSet<ulong>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = ((ulong)hi) << 32 | lo;
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong?[] ReadUInt64NullableArray(int length)
        {
            var array = new ulong?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = ((ulong)hi) << 32 | lo;
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<ulong?> ReadUInt64NullableList(int length)
        {
            var list = new List<ulong?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = ((ulong)hi) << 32 | lo;
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<ulong?> ReadUInt64NullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<ulong?>();
#else
            var set = new HashSet<ulong?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = ((ulong)hi) << 32 | lo;
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe float ReadSingle()
        {
            var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return *((float*)&tmpBuffer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe float? ReadSingleNullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return *((float*)&tmpBuffer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe float[] ReadSingleArray(int length)
        {
            var array = new float[length];
            for (var i = 0; i < length; i++)
            {
                var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = *((float*)&tmpBuffer);
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe List<float> ReadSingleList(int length)
        {
            var list = new List<float>(length);
            for (var i = 0; i < length; i++)
            {
                var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = *((float*)&tmpBuffer);
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe HashSet<float> ReadSingleHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<float>();
#else
            var set = new HashSet<float>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = *((float*)&tmpBuffer);
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe float?[] ReadSingleNullableArray(int length)
        {
            var array = new float?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = *((float*)&tmpBuffer);
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe List<float?> ReadSingleNullableList(int length)
        {
            var list = new List<float?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = *((float*)&tmpBuffer);
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe HashSet<float?> ReadSingleNullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<float?>();
#else
            var set = new HashSet<float?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = *((float*)&tmpBuffer);
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe double ReadDouble()
        {
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var tmpBuffer = ((ulong)hi) << 32 | lo;
            return *((double*)&tmpBuffer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe double? ReadDoubleNullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var tmpBuffer = ((ulong)hi) << 32 | lo;
            return *((double*)&tmpBuffer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe double[] ReadDoubleArray(int length)
        {
            var array = new double[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var tmpBuffer = ((ulong)hi) << 32 | lo;
                var item = *((double*)&tmpBuffer);
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe List<double> ReadDoubleList(int length)
        {
            var list = new List<double>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var tmpBuffer = ((ulong)hi) << 32 | lo;
                var item = *((double*)&tmpBuffer);
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe HashSet<double> ReadDoubleHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<double>();
#else
            var set = new HashSet<double>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var tmpBuffer = ((ulong)hi) << 32 | lo;
                var item = *((double*)&tmpBuffer);
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe double?[] ReadDoubleNullableArray(int length)
        {
            var array = new double?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var tmpBuffer = ((ulong)hi) << 32 | lo;
                    var item = *((double*)&tmpBuffer);
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe List<double?> ReadDoubleNullableList(int length)
        {
            var list = new List<double?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var tmpBuffer = ((ulong)hi) << 32 | lo;
                    var item = *((double*)&tmpBuffer);
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe HashSet<double?> ReadDoubleNullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<double?>();
#else
            var set = new HashSet<double?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var tmpBuffer = ((ulong)hi) << 32 | lo;
                    var item = *((double*)&tmpBuffer);
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal ReadDecimal()
        {
            var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            return new decimal(new int[] { lo, mid, hi, flags });
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal? ReadDecimalNullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            return new decimal(new int[] { lo, mid, hi, flags });
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal[] ReadDecimalArray(int length)
        {
            var array = new decimal[length];
            for (var i = 0; i < length; i++)
            {
                var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var item = new decimal(new int[] { lo, mid, hi, flags });
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<decimal> ReadDecimalList(int length)
        {
            var list = new List<decimal>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var item = new decimal(new int[] { lo, mid, hi, flags });
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<decimal> ReadDecimalHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<decimal>();
#else
            var set = new HashSet<decimal>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var item = new decimal(new int[] { lo, mid, hi, flags });
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal?[] ReadDecimalNullableArray(int length)
        {
            var array = new decimal?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var item = new decimal(new int[] { lo, mid, hi, flags });
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<decimal?> ReadDecimalNullableList(int length)
        {
            var list = new List<decimal?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var item = new decimal(new int[] { lo, mid, hi, flags });
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<decimal?> ReadDecimalNullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<decimal?>();
#else
            var set = new HashSet<decimal?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var item = new decimal(new int[] { lo, mid, hi, flags });
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ReadDateTime()
        {
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            return new DateTime(ticks);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime? ReadDateTimeNullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            return new DateTime(ticks);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime[] ReadDateTimeArray(int length)
        {
            var array = new DateTime[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new DateTime(ticks);
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<DateTime> ReadDateTimeList(int length)
        {
            var list = new List<DateTime>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new DateTime(ticks);
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<DateTime> ReadDateTimeHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<DateTime>();
#else
            var set = new HashSet<DateTime>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new DateTime(ticks);
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime?[] ReadDateTimeNullableArray(int length)
        {
            var array = new DateTime?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new DateTime(ticks);
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<DateTime?> ReadDateTimeNullableList(int length)
        {
            var list = new List<DateTime?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new DateTime(ticks);
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<DateTime?> ReadDateTimeNullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<DateTime?>();
#else
            var set = new HashSet<DateTime?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new DateTime(ticks);
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTimeOffset ReadDateTimeOffset()
        {
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            var offset = (short)(buffer[position++] | buffer[position++] << 8);
            return new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTimeOffset? ReadDateTimeOffsetNullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            var offset = (short)(buffer[position++] | buffer[position++] << 8);
            return new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTimeOffset[] ReadDateTimeOffsetArray(int length)
        {
            var array = new DateTimeOffset[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var offset = (short)(buffer[position++] | buffer[position++] << 8);
                var item = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<DateTimeOffset> ReadDateTimeOffsetList(int length)
        {
            var list = new List<DateTimeOffset>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var offset = (short)(buffer[position++] | buffer[position++] << 8);
                var item = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<DateTimeOffset> ReadDateTimeOffsetHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<DateTimeOffset>();
#else
            var set = new HashSet<DateTimeOffset>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var offset = (short)(buffer[position++] | buffer[position++] << 8);
                var item = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTimeOffset?[] ReadDateTimeOffsetNullableArray(int length)
        {
            var array = new DateTimeOffset?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var offset = (short)(buffer[position++] | buffer[position++] << 8);
                    var item = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<DateTimeOffset?> ReadDateTimeOffsetNullableList(int length)
        {
            var list = new List<DateTimeOffset?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var offset = (short)(buffer[position++] | buffer[position++] << 8);
                    var item = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<DateTimeOffset?> ReadDateTimeOffsetNullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<DateTimeOffset?>();
#else
            var set = new HashSet<DateTimeOffset?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var offset = (short)(buffer[position++] | buffer[position++] << 8);
                    var item = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offset));
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpan ReadTimeSpan()
        {
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            return new TimeSpan(ticks);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpan? ReadTimeSpanNullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            return new TimeSpan(ticks);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpan[] ReadTimeSpanArray(int length)
        {
            var array = new TimeSpan[length];
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new TimeSpan(ticks);
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<TimeSpan> ReadTimeSpanList(int length)
        {
            var list = new List<TimeSpan>(length);
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new TimeSpan(ticks);
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<TimeSpan> ReadTimeSpanHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<TimeSpan>();
#else
            var set = new HashSet<TimeSpan>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new TimeSpan(ticks);
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpan?[] ReadTimeSpanNullableArray(int length)
        {
            var array = new TimeSpan?[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new TimeSpan(ticks);
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<TimeSpan?> ReadTimeSpanNullableList(int length)
        {
            var list = new List<TimeSpan?>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new TimeSpan(ticks);
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HashSet<TimeSpan?> ReadTimeSpanNullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<TimeSpan?>();
#else
            var set = new HashSet<TimeSpan?>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new TimeSpan(ticks);
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Guid ReadGuid()
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
            return new Guid(guidBuffer.ToArray());
#else
            return new Guid(guidBuffer);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Guid? ReadGuidNullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
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
            return new Guid(guidBuffer.ToArray());
#else
            return new Guid(guidBuffer);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Guid[] ReadGuidArray(int length)
        {
            var array = new Guid[length];
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
                array[i] = item;
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe List<Guid> ReadGuidList(int length)
        {
            var list = new List<Guid>(length);
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
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe HashSet<Guid> ReadGuidHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<Guid>();
#else
            var set = new HashSet<Guid>(length);
#endif
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
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Guid?[] ReadGuidNullableArray(int length)
        {
            var array = new Guid?[length];
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
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe List<Guid?> ReadGuidNullableList(int length)
        {
            var list = new List<Guid?>(length);
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
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe HashSet<Guid?> ReadGuidNullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<Guid?>();
#else
            var set = new HashSet<Guid?>(length);
#endif
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
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe char ReadChar()
        {
            fixed (byte* pBuffer = &buffer[position])
            {
                var value = (char)*(short*)pBuffer;
                position += 2;
                return value;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe char? ReadCharNullable(bool nullFlags)
        {
            if (nullFlags)
            {
                if (buffer[position++] == nullByte)
                    return null;
            }
            fixed (byte* pBuffer = &buffer[position])
            {
                var value = (char)*(short*)pBuffer;
                position += 2;
                return value;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe char[] ReadCharArray(int length)
        {
            var array = new char[length];
            fixed (char* pArray = array)
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
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe List<char> ReadCharList(int length)
        {
            var list = new List<char>(length);
            for (var i = 0; i < length; i++)
            {
                char item;
                fixed (byte* pBuffer = &buffer[position])
                {
                    item = (char)*(short*)pBuffer;
                }
                position += 2;
                list.Add(item);
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe HashSet<char> ReadCharHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<char>();
#else
            var set = new HashSet<char>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                char item;
                fixed (byte* pBuffer = &buffer[position])
                {
                    item = (char)*(short*)pBuffer;
                }
                position += 2;
                set.Add(item);
            }
            return set;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe char?[] ReadCharNullableArray(int length)
        {
            var array = new char?[length];
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
                    array[i] = item;
                }
            }

            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe List<char?> ReadCharNullableList(int length)
        {
            var list = new List<char?>(length);
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
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe HashSet<char?> ReadCharNullableHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<char?>();
#else
            var set = new HashSet<char?>(length);
#endif
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
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe string ReadString(bool nullFlags)
        {
            if (nullFlags && buffer[position++] == nullByte)
                return null;
            var byteLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (byteLength == 0)
                return String.Empty;
            var bytesForString = buffer.Slice(position, byteLength);
            string value;
            fixed (byte* p = bytesForString)
            {
                value = encoding.GetString(p, byteLength);
            }
            position += byteLength;
            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe string[] ReadStringArray(int length)
        {
            var array = new string[length];
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var byteLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    string item;
                    if (byteLength == 0)
                    {
                        item = String.Empty;
                    }
                    else
                    {
                        var bytesForString = buffer.Slice(position, byteLength);
                        fixed (byte* p = &bytesForString[0])
                        {
                            item = encoding.GetString(p, byteLength);
                        }
                    }
                    position += byteLength;
                    array[i] = item;
                }
            }
            return array;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe List<string> ReadStringList(int length)
        {
            var list = new List<string>(length);
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var byteLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    string item;
                    if (byteLength == 0)
                    {
                        item = String.Empty;
                    }
                    else
                    {
                        var bytesForString = buffer.Slice(position, byteLength);
                        fixed (byte* p = bytesForString)
                        {
                            item = encoding.GetString(p, byteLength);
                        }
                    }
                    position += byteLength;
                    list.Add(item);
                }
                else
                {
                    list.Add(null);
                }
            }
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe HashSet<string> ReadStringHashSet(int length)
        {
#if NETSTANDARD2_0
            var set = new HashSet<string>();
#else
            var set = new HashSet<string>(length);
#endif
            for (var i = 0; i < length; i++)
            {
                if (buffer[position++] != nullByte)
                {
                    var byteLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    string item;
                    if (byteLength == 0)
                    {
                        item = String.Empty;
                    }
                    else
                    {
                        var bytesForString = buffer.Slice(position, byteLength);
                        fixed (byte* p = bytesForString)
                        {
                            item = encoding.GetString(p, byteLength);
                        }
                    }
                    position += byteLength;
                    set.Add(item);
                }
                else
                {
                    set.Add(null);
                }
            }
            return set;
        }
    }
}
