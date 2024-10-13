// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization.Bytes.IO
{
    public ref partial struct ByteWriterOld
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(byte[] bytes)
        {
            if (bytes.Length == 0)
                return;
            EnsureBufferSize(bytes.Length);
            fixed (byte* pBuffer = &buffer[position], pBytes = &bytes[0])
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    pBuffer[i] = pBytes[i];
                }
            }
            position += bytes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNull()
        {
            EnsureBufferSize(1);
            buffer[position++] = nullByte;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteNotNull()
        {
            EnsureBufferSize(1);
            buffer[position++] = notNullByte;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(bool value)
        {
            EnsureBufferSize(1);
            buffer[position++] = (byte)(value ? 1 : 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(bool? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(2);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(1);
                }
                buffer[position++] = (byte)(value.Value ? 1 : 0);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<bool> values, int maxLength)
        {
            EnsureBufferSize(maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)(value ? 1 : 0);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<bool?> values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)(value.Value ? 1 : 0);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBoolCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(maxLength);
            foreach (var value in values)
            {
                var cast = (bool)value;
                buffer[position++] = (byte)(cast ? 1 : 0);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBoolNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                var cast = (bool?)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)(cast.Value ? 1 : 0);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            EnsureBufferSize(1);
            buffer[position++] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(2);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(1);
                }
                buffer[position++] = value.Value;
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<byte> values, int maxLength)
        {
            EnsureBufferSize(maxLength);
            foreach (var value in values)
            {
                buffer[position++] = value;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<byte?> values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = value.Value;
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByteCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(maxLength);
            foreach (var value in values)
            {
                var cast = (byte)value;
                buffer[position++] = cast;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByteNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (byte?)(byte)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = cast.Value;
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(sbyte value)
        {
            EnsureBufferSize(1);
            buffer[position++] = (byte)value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(sbyte? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(2);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(1);
                }
                buffer[position++] = (byte)value;
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<sbyte> values, int maxLength)
        {
            EnsureBufferSize(maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<sbyte?> values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value;
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSByteCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(maxLength);
            foreach (var value in values)
            {
                var cast = (sbyte)value;
                buffer[position++] = (byte)cast;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSByteNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (sbyte?)(sbyte)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value;
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(short value)
        {
            EnsureBufferSize(2);
            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(short? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(3);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(2);
                }
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<short> values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<short?> values, int maxLength)
        {
            EnsureBufferSize(3 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value;
                    buffer[position++] = (byte)(value.Value >> 8);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16Cast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                var cast = (short)value;
                buffer[position++] = (byte)cast;
                buffer[position++] = (byte)(cast >> 8);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16NullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(3 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (short?)(short)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value;
                    buffer[position++] = (byte)(cast.Value >> 8);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort value)
        {
            EnsureBufferSize(2);
            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(3);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(2);
                }
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<ushort> values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<ushort?> values, int maxLength)
        {
            EnsureBufferSize(3 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value;
                    buffer[position++] = (byte)(value.Value >> 8);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16Cast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                var cast = (ushort)value;
                buffer[position++] = (byte)cast;
                buffer[position++] = (byte)(cast >> 8);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16NullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(3 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (ushort?)(ushort)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value;
                    buffer[position++] = (byte)(cast.Value >> 8);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            EnsureBufferSize(4);
            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 24);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(5);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(4);
                }
                buffer[position++] = (byte)value.Value;
                buffer[position++] = (byte)(value.Value >> 8);
                buffer[position++] = (byte)(value.Value >> 16);
                buffer[position++] = (byte)(value.Value >> 24);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<int> values, int maxLength)
        {
            EnsureBufferSize(4 * maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
                buffer[position++] = (byte)(value >> 16);
                buffer[position++] = (byte)(value >> 24);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<int?> values, int maxLength)
        {
            EnsureBufferSize(5 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value;
                    buffer[position++] = (byte)(value.Value >> 8);
                    buffer[position++] = (byte)(value.Value >> 16);
                    buffer[position++] = (byte)(value.Value >> 24);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32Cast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(4 * maxLength);
            foreach (var value in values)
            {
                var cast = (int)value;
                buffer[position++] = (byte)cast;
                buffer[position++] = (byte)(cast >> 8);
                buffer[position++] = (byte)(cast >> 16);
                buffer[position++] = (byte)(cast >> 24);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32NullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(5 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (int?)(int)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value;
                    buffer[position++] = (byte)(cast.Value >> 8);
                    buffer[position++] = (byte)(cast.Value >> 16);
                    buffer[position++] = (byte)(cast.Value >> 24);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value)
        {
            EnsureBufferSize(4);
            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 24);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(5);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(4);
                }
                buffer[position++] = (byte)value.Value;
                buffer[position++] = (byte)(value.Value >> 8);
                buffer[position++] = (byte)(value.Value >> 16);
                buffer[position++] = (byte)(value.Value >> 24);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<uint> values, int maxLength)
        {
            EnsureBufferSize(4 * maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
                buffer[position++] = (byte)(value >> 16);
                buffer[position++] = (byte)(value >> 24);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<uint?> values, int maxLength)
        {
            EnsureBufferSize(5 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value;
                    buffer[position++] = (byte)(value.Value >> 8);
                    buffer[position++] = (byte)(value.Value >> 16);
                    buffer[position++] = (byte)(value.Value >> 24);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32Cast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(4 * maxLength);
            foreach (var value in values)
            {
                var cast = (uint)value;
                buffer[position++] = (byte)cast;
                buffer[position++] = (byte)(cast >> 8);
                buffer[position++] = (byte)(cast >> 16);
                buffer[position++] = (byte)(cast >> 24);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32NullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(5 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (uint?)(uint)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value;
                    buffer[position++] = (byte)(cast.Value >> 8);
                    buffer[position++] = (byte)(cast.Value >> 16);
                    buffer[position++] = (byte)(cast.Value >> 24);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long value)
        {
            EnsureBufferSize(8);
            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 24);
            buffer[position++] = (byte)(value >> 32);
            buffer[position++] = (byte)(value >> 40);
            buffer[position++] = (byte)(value >> 48);
            buffer[position++] = (byte)(value >> 56);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(9);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(8);
                }
                buffer[position++] = (byte)value.Value;
                buffer[position++] = (byte)(value.Value >> 8);
                buffer[position++] = (byte)(value.Value >> 16);
                buffer[position++] = (byte)(value.Value >> 24);
                buffer[position++] = (byte)(value.Value >> 32);
                buffer[position++] = (byte)(value.Value >> 40);
                buffer[position++] = (byte)(value.Value >> 48);
                buffer[position++] = (byte)(value.Value >> 56);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<long> values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
                buffer[position++] = (byte)(value >> 16);
                buffer[position++] = (byte)(value >> 24);
                buffer[position++] = (byte)(value >> 32);
                buffer[position++] = (byte)(value >> 40);
                buffer[position++] = (byte)(value >> 48);
                buffer[position++] = (byte)(value >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<long?> values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value;
                    buffer[position++] = (byte)(value.Value >> 8);
                    buffer[position++] = (byte)(value.Value >> 16);
                    buffer[position++] = (byte)(value.Value >> 24);
                    buffer[position++] = (byte)(value.Value >> 32);
                    buffer[position++] = (byte)(value.Value >> 40);
                    buffer[position++] = (byte)(value.Value >> 48);
                    buffer[position++] = (byte)(value.Value >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64Cast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                var cast = (long)value;
                buffer[position++] = (byte)cast;
                buffer[position++] = (byte)(cast >> 8);
                buffer[position++] = (byte)(cast >> 16);
                buffer[position++] = (byte)(cast >> 24);
                buffer[position++] = (byte)(cast >> 32);
                buffer[position++] = (byte)(cast >> 40);
                buffer[position++] = (byte)(cast >> 48);
                buffer[position++] = (byte)(cast >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64NullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (long?)(long)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value;
                    buffer[position++] = (byte)(cast.Value >> 8);
                    buffer[position++] = (byte)(cast.Value >> 16);
                    buffer[position++] = (byte)(cast.Value >> 24);
                    buffer[position++] = (byte)(cast.Value >> 32);
                    buffer[position++] = (byte)(cast.Value >> 40);
                    buffer[position++] = (byte)(cast.Value >> 48);
                    buffer[position++] = (byte)(cast.Value >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong value)
        {
            EnsureBufferSize(8);
            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 24);
            buffer[position++] = (byte)(value >> 32);
            buffer[position++] = (byte)(value >> 40);
            buffer[position++] = (byte)(value >> 48);
            buffer[position++] = (byte)(value >> 56);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(9);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(8);
                }
                buffer[position++] = (byte)value.Value;
                buffer[position++] = (byte)(value.Value >> 8);
                buffer[position++] = (byte)(value.Value >> 16);
                buffer[position++] = (byte)(value.Value >> 24);
                buffer[position++] = (byte)(value.Value >> 32);
                buffer[position++] = (byte)(value.Value >> 40);
                buffer[position++] = (byte)(value.Value >> 48);
                buffer[position++] = (byte)(value.Value >> 56);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<ulong> values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
                buffer[position++] = (byte)(value >> 16);
                buffer[position++] = (byte)(value >> 24);
                buffer[position++] = (byte)(value >> 32);
                buffer[position++] = (byte)(value >> 40);
                buffer[position++] = (byte)(value >> 48);
                buffer[position++] = (byte)(value >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<ulong?> values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value;
                    buffer[position++] = (byte)(value.Value >> 8);
                    buffer[position++] = (byte)(value.Value >> 16);
                    buffer[position++] = (byte)(value.Value >> 24);
                    buffer[position++] = (byte)(value.Value >> 32);
                    buffer[position++] = (byte)(value.Value >> 40);
                    buffer[position++] = (byte)(value.Value >> 48);
                    buffer[position++] = (byte)(value.Value >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64Cast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                var cast = (ulong)value;
                buffer[position++] = (byte)cast;
                buffer[position++] = (byte)(cast >> 8);
                buffer[position++] = (byte)(cast >> 16);
                buffer[position++] = (byte)(cast >> 24);
                buffer[position++] = (byte)(cast >> 32);
                buffer[position++] = (byte)(cast >> 40);
                buffer[position++] = (byte)(cast >> 48);
                buffer[position++] = (byte)(cast >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64NullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (ulong?)(ulong)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value;
                    buffer[position++] = (byte)(cast.Value >> 8);
                    buffer[position++] = (byte)(cast.Value >> 16);
                    buffer[position++] = (byte)(cast.Value >> 24);
                    buffer[position++] = (byte)(cast.Value >> 32);
                    buffer[position++] = (byte)(cast.Value >> 40);
                    buffer[position++] = (byte)(cast.Value >> 48);
                    buffer[position++] = (byte)(cast.Value >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(float value)
        {
            EnsureBufferSize(4);
            var tmpValue = *(uint*)&value;
            buffer[position++] = (byte)tmpValue;
            buffer[position++] = (byte)(tmpValue >> 8);
            buffer[position++] = (byte)(tmpValue >> 16);
            buffer[position++] = (byte)(tmpValue >> 24);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(float? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(5);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(4);
                }
                var temp = value.Value;
                var tmpValue = *(uint*)&temp;
                buffer[position++] = (byte)tmpValue;
                buffer[position++] = (byte)(tmpValue >> 8);
                buffer[position++] = (byte)(tmpValue >> 16);
                buffer[position++] = (byte)(tmpValue >> 24);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(IEnumerable<float> values, int maxLength)
        {
            EnsureBufferSize(4 * maxLength);
            foreach (var value in values)
            {
                var tmpValue = *(uint*)&value;
                buffer[position++] = (byte)tmpValue;
                buffer[position++] = (byte)(tmpValue >> 8);
                buffer[position++] = (byte)(tmpValue >> 16);
                buffer[position++] = (byte)(tmpValue >> 24);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(IEnumerable<float?> values, int maxLength)
        {
            EnsureBufferSize(5 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    var temp = value.Value;
                    var tmpValue = *(uint*)&temp;
                    buffer[position++] = (byte)tmpValue;
                    buffer[position++] = (byte)(tmpValue >> 8);
                    buffer[position++] = (byte)(tmpValue >> 16);
                    buffer[position++] = (byte)(tmpValue >> 24);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteSingleCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(4 * maxLength);
            foreach (var value in values)
            {
                var cast = (float)value;
                var tmpValue = *(uint*)&cast;
                buffer[position++] = (byte)tmpValue;
                buffer[position++] = (byte)(tmpValue >> 8);
                buffer[position++] = (byte)(tmpValue >> 16);
                buffer[position++] = (byte)(tmpValue >> 24);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteSingleNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(5 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (float?)(float)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    var temp = cast.Value;
                    var tmpValue = *(uint*)&temp;
                    buffer[position++] = (byte)tmpValue;
                    buffer[position++] = (byte)(tmpValue >> 8);
                    buffer[position++] = (byte)(tmpValue >> 16);
                    buffer[position++] = (byte)(tmpValue >> 24);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(double value)
        {
            EnsureBufferSize(8);
            var tmpValue = *(ulong*)&value;
            buffer[position++] = (byte)tmpValue;
            buffer[position++] = (byte)(tmpValue >> 8);
            buffer[position++] = (byte)(tmpValue >> 16);
            buffer[position++] = (byte)(tmpValue >> 24);
            buffer[position++] = (byte)(tmpValue >> 32);
            buffer[position++] = (byte)(tmpValue >> 40);
            buffer[position++] = (byte)(tmpValue >> 48);
            buffer[position++] = (byte)(tmpValue >> 56);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(double? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(9);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(8);
                }
                var temp = value.Value;
                var tmpValue = *(ulong*)&temp;
                buffer[position++] = (byte)tmpValue;
                buffer[position++] = (byte)(tmpValue >> 8);
                buffer[position++] = (byte)(tmpValue >> 16);
                buffer[position++] = (byte)(tmpValue >> 24);
                buffer[position++] = (byte)(tmpValue >> 32);
                buffer[position++] = (byte)(tmpValue >> 40);
                buffer[position++] = (byte)(tmpValue >> 48);
                buffer[position++] = (byte)(tmpValue >> 56);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(IEnumerable<double> values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                var tmpValue = *(ulong*)&value;
                buffer[position++] = (byte)tmpValue;
                buffer[position++] = (byte)(tmpValue >> 8);
                buffer[position++] = (byte)(tmpValue >> 16);
                buffer[position++] = (byte)(tmpValue >> 24);
                buffer[position++] = (byte)(tmpValue >> 32);
                buffer[position++] = (byte)(tmpValue >> 40);
                buffer[position++] = (byte)(tmpValue >> 48);
                buffer[position++] = (byte)(tmpValue >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(IEnumerable<double?> values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    var temp = value.Value;
                    var tmpValue = *(ulong*)&temp;
                    buffer[position++] = (byte)tmpValue;
                    buffer[position++] = (byte)(tmpValue >> 8);
                    buffer[position++] = (byte)(tmpValue >> 16);
                    buffer[position++] = (byte)(tmpValue >> 24);
                    buffer[position++] = (byte)(tmpValue >> 32);
                    buffer[position++] = (byte)(tmpValue >> 40);
                    buffer[position++] = (byte)(tmpValue >> 48);
                    buffer[position++] = (byte)(tmpValue >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteDoubleCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                var cast = (double)value;
                var tmpValue = *(ulong*)&cast;
                buffer[position++] = (byte)tmpValue;
                buffer[position++] = (byte)(tmpValue >> 8);
                buffer[position++] = (byte)(tmpValue >> 16);
                buffer[position++] = (byte)(tmpValue >> 24);
                buffer[position++] = (byte)(tmpValue >> 32);
                buffer[position++] = (byte)(tmpValue >> 40);
                buffer[position++] = (byte)(tmpValue >> 48);
                buffer[position++] = (byte)(tmpValue >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteDoubleNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (double?)(double)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    var temp = cast.Value;
                    var tmpValue = *(ulong*)&temp;
                    buffer[position++] = (byte)tmpValue;
                    buffer[position++] = (byte)(tmpValue >> 8);
                    buffer[position++] = (byte)(tmpValue >> 16);
                    buffer[position++] = (byte)(tmpValue >> 24);
                    buffer[position++] = (byte)(tmpValue >> 32);
                    buffer[position++] = (byte)(tmpValue >> 40);
                    buffer[position++] = (byte)(tmpValue >> 48);
                    buffer[position++] = (byte)(tmpValue >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(decimal value)
        {
            EnsureBufferSize(16);
            var bits = Decimal.GetBits(value);
            var lo = bits[0];
            var mid = bits[1];
            var hi = bits[2];
            var flags = bits[3];
            buffer[position++] = (byte)lo;
            buffer[position++] = (byte)(lo >> 8);
            buffer[position++] = (byte)(lo >> 16);
            buffer[position++] = (byte)(lo >> 24);
            buffer[position++] = (byte)mid;
            buffer[position++] = (byte)(mid >> 8);
            buffer[position++] = (byte)(mid >> 16);
            buffer[position++] = (byte)(mid >> 24);
            buffer[position++] = (byte)hi;
            buffer[position++] = (byte)(hi >> 8);
            buffer[position++] = (byte)(hi >> 16);
            buffer[position++] = (byte)(hi >> 24);
            buffer[position++] = (byte)flags;
            buffer[position++] = (byte)(flags >> 8);
            buffer[position++] = (byte)(flags >> 16);
            buffer[position++] = (byte)(flags >> 24);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(decimal? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(17);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(16);
                }
                var bits = Decimal.GetBits(value.Value);
                var lo = bits[0];
                var mid = bits[1];
                var hi = bits[2];
                var flags = bits[3];
                buffer[position++] = (byte)lo;
                buffer[position++] = (byte)(lo >> 8);
                buffer[position++] = (byte)(lo >> 16);
                buffer[position++] = (byte)(lo >> 24);
                buffer[position++] = (byte)mid;
                buffer[position++] = (byte)(mid >> 8);
                buffer[position++] = (byte)(mid >> 16);
                buffer[position++] = (byte)(mid >> 24);
                buffer[position++] = (byte)hi;
                buffer[position++] = (byte)(hi >> 8);
                buffer[position++] = (byte)(hi >> 16);
                buffer[position++] = (byte)(hi >> 24);
                buffer[position++] = (byte)flags;
                buffer[position++] = (byte)(flags >> 8);
                buffer[position++] = (byte)(flags >> 16);
                buffer[position++] = (byte)(flags >> 24);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<decimal> values, int maxLength)
        {
            EnsureBufferSize(16 * maxLength);
            foreach (var value in values)
            {
                var bits = Decimal.GetBits(value);
                var lo = bits[0];
                var mid = bits[1];
                var hi = bits[2];
                var flags = bits[3];
                buffer[position++] = (byte)lo;
                buffer[position++] = (byte)(lo >> 8);
                buffer[position++] = (byte)(lo >> 16);
                buffer[position++] = (byte)(lo >> 24);
                buffer[position++] = (byte)mid;
                buffer[position++] = (byte)(mid >> 8);
                buffer[position++] = (byte)(mid >> 16);
                buffer[position++] = (byte)(mid >> 24);
                buffer[position++] = (byte)hi;
                buffer[position++] = (byte)(hi >> 8);
                buffer[position++] = (byte)(hi >> 16);
                buffer[position++] = (byte)(hi >> 24);
                buffer[position++] = (byte)flags;
                buffer[position++] = (byte)(flags >> 8);
                buffer[position++] = (byte)(flags >> 16);
                buffer[position++] = (byte)(flags >> 24);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<decimal?> values, int maxLength)
        {
            EnsureBufferSize(17 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    var bits = Decimal.GetBits(value.Value);
                    var lo = bits[0];
                    var mid = bits[1];
                    var hi = bits[2];
                    var flags = bits[3];
                    buffer[position++] = (byte)lo;
                    buffer[position++] = (byte)(lo >> 8);
                    buffer[position++] = (byte)(lo >> 16);
                    buffer[position++] = (byte)(lo >> 24);
                    buffer[position++] = (byte)mid;
                    buffer[position++] = (byte)(mid >> 8);
                    buffer[position++] = (byte)(mid >> 16);
                    buffer[position++] = (byte)(mid >> 24);
                    buffer[position++] = (byte)hi;
                    buffer[position++] = (byte)(hi >> 8);
                    buffer[position++] = (byte)(hi >> 16);
                    buffer[position++] = (byte)(hi >> 24);
                    buffer[position++] = (byte)flags;
                    buffer[position++] = (byte)(flags >> 8);
                    buffer[position++] = (byte)(flags >> 16);
                    buffer[position++] = (byte)(flags >> 24);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDecimalCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(16 * maxLength);
            foreach (var value in values)
            {
                var cast = (decimal)value;
                var bits = Decimal.GetBits(cast);
                var lo = bits[0];
                var mid = bits[1];
                var hi = bits[2];
                var flags = bits[3];
                buffer[position++] = (byte)lo;
                buffer[position++] = (byte)(lo >> 8);
                buffer[position++] = (byte)(lo >> 16);
                buffer[position++] = (byte)(lo >> 24);
                buffer[position++] = (byte)mid;
                buffer[position++] = (byte)(mid >> 8);
                buffer[position++] = (byte)(mid >> 16);
                buffer[position++] = (byte)(mid >> 24);
                buffer[position++] = (byte)hi;
                buffer[position++] = (byte)(hi >> 8);
                buffer[position++] = (byte)(hi >> 16);
                buffer[position++] = (byte)(hi >> 24);
                buffer[position++] = (byte)flags;
                buffer[position++] = (byte)(flags >> 8);
                buffer[position++] = (byte)(flags >> 16);
                buffer[position++] = (byte)(flags >> 24);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDecimalNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(17 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (decimal?)(decimal)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    var bits = Decimal.GetBits(cast.Value);
                    var lo = bits[0];
                    var mid = bits[1];
                    var hi = bits[2];
                    var flags = bits[3];
                    buffer[position++] = (byte)lo;
                    buffer[position++] = (byte)(lo >> 8);
                    buffer[position++] = (byte)(lo >> 16);
                    buffer[position++] = (byte)(lo >> 24);
                    buffer[position++] = (byte)mid;
                    buffer[position++] = (byte)(mid >> 8);
                    buffer[position++] = (byte)(mid >> 16);
                    buffer[position++] = (byte)(mid >> 24);
                    buffer[position++] = (byte)hi;
                    buffer[position++] = (byte)(hi >> 8);
                    buffer[position++] = (byte)(hi >> 16);
                    buffer[position++] = (byte)(hi >> 24);
                    buffer[position++] = (byte)flags;
                    buffer[position++] = (byte)(flags >> 8);
                    buffer[position++] = (byte)(flags >> 16);
                    buffer[position++] = (byte)(flags >> 24);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(DateTime value)
        {
            EnsureBufferSize(8);
            buffer[position++] = (byte)value.Ticks;
            buffer[position++] = (byte)(value.Ticks >> 8);
            buffer[position++] = (byte)(value.Ticks >> 16);
            buffer[position++] = (byte)(value.Ticks >> 24);
            buffer[position++] = (byte)(value.Ticks >> 32);
            buffer[position++] = (byte)(value.Ticks >> 40);
            buffer[position++] = (byte)(value.Ticks >> 48);
            buffer[position++] = (byte)(value.Ticks >> 56);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(DateTime? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(9);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(8);
                }
                buffer[position++] = (byte)value.Value.Ticks;
                buffer[position++] = (byte)(value.Value.Ticks >> 8);
                buffer[position++] = (byte)(value.Value.Ticks >> 16);
                buffer[position++] = (byte)(value.Value.Ticks >> 24);
                buffer[position++] = (byte)(value.Value.Ticks >> 32);
                buffer[position++] = (byte)(value.Value.Ticks >> 40);
                buffer[position++] = (byte)(value.Value.Ticks >> 48);
                buffer[position++] = (byte)(value.Value.Ticks >> 56);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<DateTime> values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value.Ticks;
                buffer[position++] = (byte)(value.Ticks >> 8);
                buffer[position++] = (byte)(value.Ticks >> 16);
                buffer[position++] = (byte)(value.Ticks >> 24);
                buffer[position++] = (byte)(value.Ticks >> 32);
                buffer[position++] = (byte)(value.Ticks >> 40);
                buffer[position++] = (byte)(value.Ticks >> 48);
                buffer[position++] = (byte)(value.Ticks >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<DateTime?> values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value.Ticks;
                    buffer[position++] = (byte)(value.Value.Ticks >> 8);
                    buffer[position++] = (byte)(value.Value.Ticks >> 16);
                    buffer[position++] = (byte)(value.Value.Ticks >> 24);
                    buffer[position++] = (byte)(value.Value.Ticks >> 32);
                    buffer[position++] = (byte)(value.Value.Ticks >> 40);
                    buffer[position++] = (byte)(value.Value.Ticks >> 48);
                    buffer[position++] = (byte)(value.Value.Ticks >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDateTimeCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                var cast = (DateTime)value;
                buffer[position++] = (byte)cast.Ticks;
                buffer[position++] = (byte)(cast.Ticks >> 8);
                buffer[position++] = (byte)(cast.Ticks >> 16);
                buffer[position++] = (byte)(cast.Ticks >> 24);
                buffer[position++] = (byte)(cast.Ticks >> 32);
                buffer[position++] = (byte)(cast.Ticks >> 40);
                buffer[position++] = (byte)(cast.Ticks >> 48);
                buffer[position++] = (byte)(cast.Ticks >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDateTimeNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (DateTime?)(DateTime)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value.Ticks;
                    buffer[position++] = (byte)(cast.Value.Ticks >> 8);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 16);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 24);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 32);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 40);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 48);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(DateTimeOffset value)
        {
            EnsureBufferSize(10);
            buffer[position++] = (byte)value.Ticks;
            buffer[position++] = (byte)(value.Ticks >> 8);
            buffer[position++] = (byte)(value.Ticks >> 16);
            buffer[position++] = (byte)(value.Ticks >> 24);
            buffer[position++] = (byte)(value.Ticks >> 32);
            buffer[position++] = (byte)(value.Ticks >> 40);
            buffer[position++] = (byte)(value.Ticks >> 48);
            buffer[position++] = (byte)(value.Ticks >> 56);
            var castedOffset = (short)value.Offset.TotalMinutes;
            buffer[position++] = (byte)castedOffset;
            buffer[position++] = (byte)(castedOffset >> 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(DateTimeOffset? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(11);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(10);
                }
                buffer[position++] = (byte)value.Value.Ticks;
                buffer[position++] = (byte)(value.Value.Ticks >> 8);
                buffer[position++] = (byte)(value.Value.Ticks >> 16);
                buffer[position++] = (byte)(value.Value.Ticks >> 24);
                buffer[position++] = (byte)(value.Value.Ticks >> 32);
                buffer[position++] = (byte)(value.Value.Ticks >> 40);
                buffer[position++] = (byte)(value.Value.Ticks >> 48);
                buffer[position++] = (byte)(value.Value.Ticks >> 56);
                var castedOffset = (short)value.Value.Offset.TotalMinutes;
                buffer[position++] = (byte)castedOffset;
                buffer[position++] = (byte)(castedOffset >> 8);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<DateTimeOffset> values, int maxLength)
        {
            EnsureBufferSize(10 * maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value.Ticks;
                buffer[position++] = (byte)(value.Ticks >> 8);
                buffer[position++] = (byte)(value.Ticks >> 16);
                buffer[position++] = (byte)(value.Ticks >> 24);
                buffer[position++] = (byte)(value.Ticks >> 32);
                buffer[position++] = (byte)(value.Ticks >> 40);
                buffer[position++] = (byte)(value.Ticks >> 48);
                buffer[position++] = (byte)(value.Ticks >> 56);
                var castedOffset = (short)value.Offset.TotalMinutes;
                buffer[position++] = (byte)castedOffset;
                buffer[position++] = (byte)(castedOffset >> 8);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<DateTimeOffset?> values, int maxLength)
        {
            EnsureBufferSize(11 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value.Ticks;
                    buffer[position++] = (byte)(value.Value.Ticks >> 8);
                    buffer[position++] = (byte)(value.Value.Ticks >> 16);
                    buffer[position++] = (byte)(value.Value.Ticks >> 24);
                    buffer[position++] = (byte)(value.Value.Ticks >> 32);
                    buffer[position++] = (byte)(value.Value.Ticks >> 40);
                    buffer[position++] = (byte)(value.Value.Ticks >> 48);
                    buffer[position++] = (byte)(value.Value.Ticks >> 56);
                    var castedOffset = (short)value.Value.Offset.TotalMinutes;
                    buffer[position++] = (byte)castedOffset;
                    buffer[position++] = (byte)(castedOffset >> 8);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDateTimeOffsetCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(10 * maxLength);
            foreach (var value in values)
            {
                var cast = (DateTimeOffset)value;
                buffer[position++] = (byte)cast.Ticks;
                buffer[position++] = (byte)(cast.Ticks >> 8);
                buffer[position++] = (byte)(cast.Ticks >> 16);
                buffer[position++] = (byte)(cast.Ticks >> 24);
                buffer[position++] = (byte)(cast.Ticks >> 32);
                buffer[position++] = (byte)(cast.Ticks >> 40);
                buffer[position++] = (byte)(cast.Ticks >> 48);
                buffer[position++] = (byte)(cast.Ticks >> 56);
                var castedOffset = (short)cast.Offset.TotalMinutes;
                buffer[position++] = (byte)castedOffset;
                buffer[position++] = (byte)(castedOffset >> 8);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDateTimeOffsetNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(11 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (DateTimeOffset?)(DateTimeOffset)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value.Ticks;
                    buffer[position++] = (byte)(cast.Value.Ticks >> 8);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 16);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 24);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 32);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 40);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 48);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 56);
                    var castedOffset = (short)cast.Value.Offset.TotalMinutes;
                    buffer[position++] = (byte)castedOffset;
                    buffer[position++] = (byte)(castedOffset >> 8);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(TimeSpan value)
        {
            EnsureBufferSize(8);
            buffer[position++] = (byte)value.Ticks;
            buffer[position++] = (byte)(value.Ticks >> 8);
            buffer[position++] = (byte)(value.Ticks >> 16);
            buffer[position++] = (byte)(value.Ticks >> 24);
            buffer[position++] = (byte)(value.Ticks >> 32);
            buffer[position++] = (byte)(value.Ticks >> 40);
            buffer[position++] = (byte)(value.Ticks >> 48);
            buffer[position++] = (byte)(value.Ticks >> 56);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(TimeSpan? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(9);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(8);
                }
                buffer[position++] = (byte)value.Value.Ticks;
                buffer[position++] = (byte)(value.Value.Ticks >> 8);
                buffer[position++] = (byte)(value.Value.Ticks >> 16);
                buffer[position++] = (byte)(value.Value.Ticks >> 24);
                buffer[position++] = (byte)(value.Value.Ticks >> 32);
                buffer[position++] = (byte)(value.Value.Ticks >> 40);
                buffer[position++] = (byte)(value.Value.Ticks >> 48);
                buffer[position++] = (byte)(value.Value.Ticks >> 56);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<TimeSpan> values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value.Ticks;
                buffer[position++] = (byte)(value.Ticks >> 8);
                buffer[position++] = (byte)(value.Ticks >> 16);
                buffer[position++] = (byte)(value.Ticks >> 24);
                buffer[position++] = (byte)(value.Ticks >> 32);
                buffer[position++] = (byte)(value.Ticks >> 40);
                buffer[position++] = (byte)(value.Ticks >> 48);
                buffer[position++] = (byte)(value.Ticks >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<TimeSpan?> values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value.Ticks;
                    buffer[position++] = (byte)(value.Value.Ticks >> 8);
                    buffer[position++] = (byte)(value.Value.Ticks >> 16);
                    buffer[position++] = (byte)(value.Value.Ticks >> 24);
                    buffer[position++] = (byte)(value.Value.Ticks >> 32);
                    buffer[position++] = (byte)(value.Value.Ticks >> 40);
                    buffer[position++] = (byte)(value.Value.Ticks >> 48);
                    buffer[position++] = (byte)(value.Value.Ticks >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTimeSpanCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                var cast = (TimeSpan)value;
                buffer[position++] = (byte)cast.Ticks;
                buffer[position++] = (byte)(cast.Ticks >> 8);
                buffer[position++] = (byte)(cast.Ticks >> 16);
                buffer[position++] = (byte)(cast.Ticks >> 24);
                buffer[position++] = (byte)(cast.Ticks >> 32);
                buffer[position++] = (byte)(cast.Ticks >> 40);
                buffer[position++] = (byte)(cast.Ticks >> 48);
                buffer[position++] = (byte)(cast.Ticks >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTimeSpanNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (TimeSpan?)(TimeSpan)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value.Ticks;
                    buffer[position++] = (byte)(cast.Value.Ticks >> 8);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 16);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 24);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 32);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 40);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 48);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(DateOnly value)
        {
            EnsureBufferSize(4);
            buffer[position++] = (byte)value.DayNumber;
            buffer[position++] = (byte)(value.DayNumber >> 8);
            buffer[position++] = (byte)(value.DayNumber >> 16);
            buffer[position++] = (byte)(value.DayNumber >> 24);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(DateOnly? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(5);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(4);
                }
                buffer[position++] = (byte)value.Value.DayNumber;
                buffer[position++] = (byte)(value.Value.DayNumber >> 8);
                buffer[position++] = (byte)(value.Value.DayNumber >> 16);
                buffer[position++] = (byte)(value.Value.DayNumber >> 24);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<DateOnly> values, int maxLength)
        {
            EnsureBufferSize(4 * maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value.DayNumber;
                buffer[position++] = (byte)(value.DayNumber >> 8);
                buffer[position++] = (byte)(value.DayNumber >> 16);
                buffer[position++] = (byte)(value.DayNumber >> 24);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<DateOnly?> values, int maxLength)
        {
            EnsureBufferSize(5 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value.DayNumber;
                    buffer[position++] = (byte)(value.Value.DayNumber >> 8);
                    buffer[position++] = (byte)(value.Value.DayNumber >> 16);
                    buffer[position++] = (byte)(value.Value.DayNumber >> 24);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDateOnlyCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(4 * maxLength);
            foreach (var value in values)
            {
                var cast = (DateOnly)value;
                buffer[position++] = (byte)cast.DayNumber;
                buffer[position++] = (byte)(cast.DayNumber >> 8);
                buffer[position++] = (byte)(cast.DayNumber >> 16);
                buffer[position++] = (byte)(cast.DayNumber >> 24);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDateOnlyNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(5 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (DateOnly?)(DateOnly)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value.DayNumber;
                    buffer[position++] = (byte)(cast.Value.DayNumber >> 8);
                    buffer[position++] = (byte)(cast.Value.DayNumber >> 16);
                    buffer[position++] = (byte)(cast.Value.DayNumber >> 24);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(TimeOnly value)
        {
            EnsureBufferSize(8);
            buffer[position++] = (byte)value.Ticks;
            buffer[position++] = (byte)(value.Ticks >> 8);
            buffer[position++] = (byte)(value.Ticks >> 16);
            buffer[position++] = (byte)(value.Ticks >> 24);
            buffer[position++] = (byte)(value.Ticks >> 32);
            buffer[position++] = (byte)(value.Ticks >> 40);
            buffer[position++] = (byte)(value.Ticks >> 48);
            buffer[position++] = (byte)(value.Ticks >> 56);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(TimeOnly? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(9);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(8);
                }
                buffer[position++] = (byte)value.Value.Ticks;
                buffer[position++] = (byte)(value.Value.Ticks >> 8);
                buffer[position++] = (byte)(value.Value.Ticks >> 16);
                buffer[position++] = (byte)(value.Value.Ticks >> 24);
                buffer[position++] = (byte)(value.Value.Ticks >> 32);
                buffer[position++] = (byte)(value.Value.Ticks >> 40);
                buffer[position++] = (byte)(value.Value.Ticks >> 48);
                buffer[position++] = (byte)(value.Value.Ticks >> 56);
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<TimeOnly> values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                buffer[position++] = (byte)value.Ticks;
                buffer[position++] = (byte)(value.Ticks >> 8);
                buffer[position++] = (byte)(value.Ticks >> 16);
                buffer[position++] = (byte)(value.Ticks >> 24);
                buffer[position++] = (byte)(value.Ticks >> 32);
                buffer[position++] = (byte)(value.Ticks >> 40);
                buffer[position++] = (byte)(value.Ticks >> 48);
                buffer[position++] = (byte)(value.Ticks >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<TimeOnly?> values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Value.Ticks;
                    buffer[position++] = (byte)(value.Value.Ticks >> 8);
                    buffer[position++] = (byte)(value.Value.Ticks >> 16);
                    buffer[position++] = (byte)(value.Value.Ticks >> 24);
                    buffer[position++] = (byte)(value.Value.Ticks >> 32);
                    buffer[position++] = (byte)(value.Value.Ticks >> 40);
                    buffer[position++] = (byte)(value.Value.Ticks >> 48);
                    buffer[position++] = (byte)(value.Value.Ticks >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTimeOnlyCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(8 * maxLength);
            foreach (var value in values)
            {
                var cast = (TimeOnly)value;
                buffer[position++] = (byte)cast.Ticks;
                buffer[position++] = (byte)(cast.Ticks >> 8);
                buffer[position++] = (byte)(cast.Ticks >> 16);
                buffer[position++] = (byte)(cast.Ticks >> 24);
                buffer[position++] = (byte)(cast.Ticks >> 32);
                buffer[position++] = (byte)(cast.Ticks >> 40);
                buffer[position++] = (byte)(cast.Ticks >> 48);
                buffer[position++] = (byte)(cast.Ticks >> 56);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTimeOnlyNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(9 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (TimeOnly?)(TimeOnly)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)cast.Value.Ticks;
                    buffer[position++] = (byte)(cast.Value.Ticks >> 8);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 16);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 24);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 32);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 40);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 48);
                    buffer[position++] = (byte)(cast.Value.Ticks >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(Guid value)
        {
            EnsureBufferSize(16);
            var bytes = value.ToByteArray();
            fixed (byte* pBuffer = &buffer[position], pBytes = &bytes[0])
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    pBuffer[i] = pBytes[i];
                }
            }
            position += 16;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(Guid? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(17);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(16);
                }
                var bytes = value.Value.ToByteArray();
                fixed (byte* pBuffer = &buffer[position], pBytes = &bytes[0])
                {
                    for (var i = 0; i < bytes.Length; i++)
                    {
                        pBuffer[i] = pBytes[i];
                    }
                }
                position += 16;
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(IEnumerable<Guid> values, int maxLength)
        {
            EnsureBufferSize(16 * maxLength);
            foreach (var value in values)
            {
                var bytes = value.ToByteArray();
                fixed (byte* pBuffer = &buffer[position], pBytes = &bytes[0])
                {
                    for (var i = 0; i < bytes.Length; i++)
                    {
                        pBuffer[i] = pBytes[i];
                    }
                }
                position += 16;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(IEnumerable<Guid?> values, int maxLength)
        {
            EnsureBufferSize(17 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    var bytes = value.Value.ToByteArray();
                    fixed (byte* pBuffer = &buffer[position], pBytes = &bytes[0])
                    {
                        for (var i = 0; i < bytes.Length; i++)
                        {
                            pBuffer[i] = pBytes[i];
                        }
                    }
                    position += 16;
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteGuidCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(16 * maxLength);
            foreach (var value in values)
            {
                var cast = (Guid)value;
                var bytes = cast.ToByteArray();
                fixed (byte* pBuffer = &buffer[position], pBytes = &bytes[0])
                {
                    for (var i = 0; i < bytes.Length; i++)
                    {
                        pBuffer[i] = pBytes[i];
                    }
                }
                position += 16;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteGuidNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(17 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (Guid?)(Guid)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    var bytes = cast.Value.ToByteArray();
                    fixed (byte* pBuffer = &buffer[position], pBytes = &bytes[0])
                    {
                        for (var i = 0; i < bytes.Length; i++)
                        {
                            pBuffer[i] = pBytes[i];
                        }
                    }
                    position += 16;
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(char value)
        {
            EnsureBufferSize(2);
            Unsafe.As<byte, char>(ref buffer[position]) = value;
            position += 2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(char? value, bool nullFlags)
        {
            if (value.HasValue)
            {
                if (nullFlags)
                {
                    EnsureBufferSize(3);
                    buffer[position++] = notNullByte;
                }
                else
                {
                    EnsureBufferSize(2);
                }
                Unsafe.As<byte, char>(ref buffer[position]) = value.Value;
                position += 2;
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<char> values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                Unsafe.As<byte, char>(ref buffer[position]) = value;
                position += 2;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<char?> values, int maxLength)
        {
            EnsureBufferSize(3 * maxLength);
            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    buffer[position++] = notNullByte;
                    Unsafe.As<byte, char>(ref buffer[position]) = value.Value;
                    position += 2;
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteCharCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(2 * maxLength);
            foreach (var value in values)
            {
                var cast = (char)value;
                Unsafe.As<byte, char>(ref buffer[position]) = cast;
                position += 2;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteCharNullableCast(IEnumerable values, int maxLength)
        {
            EnsureBufferSize(3 * maxLength);
            foreach (var value in values)
            {
                var cast = value is null ? null : (char?)(char)value;
                if (cast.HasValue)
                {
                    buffer[position++] = notNullByte;
                    Unsafe.As<byte, char>(ref buffer[position]) = cast.Value;
                    position += 2;
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string? value, bool nullFlags)
        {
            if (value is not null)
            {
                var sizeNeeded = encoding.GetMaxByteCount(value.Length);
                EnsureBufferSize(sizeNeeded + 4 + (nullFlags ? 1 : 0));
#if NETSTANDARD2_0
                var charBytes = encoding.GetBytes(value);
                charBytes.AsSpan().CopyTo(buffer.Slice(position + 4 + (nullFlags ? 1 : 0)));
                var byteLength = charBytes.Length;
#else
                var byteLength = encoding.GetBytes(value.AsSpan(), buffer.Slice(position + 4 + (nullFlags ? 1 : 0)));
#endif

                if (nullFlags)
                    buffer[position++] = notNullByte;
                buffer[position++] = (byte)byteLength;
                buffer[position++] = (byte)(byteLength >> 8);
                buffer[position++] = (byte)(byteLength >> 16);
                buffer[position++] = (byte)(byteLength >> 24);
                position += byteLength;
            }
            else if (nullFlags)
            {
                EnsureBufferSize(1);
                buffer[position++] = nullByte;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(IEnumerable<string> values, int maxLength)
        {
            var sizeNeeded = 0;
            foreach (var value in values)
                sizeNeeded += value is null ? 1 : encoding.GetMaxByteCount(value.Length) + 5;
            EnsureBufferSize(sizeNeeded);

            foreach (var value in values)
            {
                if (value is not null)
                {
#if NETSTANDARD2_0
                    var charBytes = encoding.GetBytes(value);
                    charBytes.AsSpan().CopyTo(buffer.Slice(position + 5));
                    var byteLength = charBytes.Length;
#else

                    var byteLength = encoding.GetBytes(value.AsSpan(), buffer.Slice(position + 5));
#endif
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)byteLength;
                    buffer[position++] = (byte)(byteLength >> 8);
                    buffer[position++] = (byte)(byteLength >> 16);
                    buffer[position++] = (byte)(byteLength >> 24);
                    position += byteLength;
                }
                else
                {
                    EnsureBufferSize(1);
                    buffer[position++] = nullByte;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteStringCast(IEnumerable values, int maxLength)
        {
            var sizeNeeded = 0;
            foreach (string value in values)
                sizeNeeded += encoding.GetMaxByteCount(value.Length) + 5;
            EnsureBufferSize(sizeNeeded);

            foreach (string value in values)
            {
                if (value is not null)
                {
#if NETSTANDARD2_0
                    var charBytes = encoding.GetBytes(value);
                    charBytes.AsSpan().CopyTo(buffer.Slice(position + 5));
                    var byteLength = charBytes.Length;
#else

                    var byteLength = encoding.GetBytes(value.AsSpan(), buffer.Slice(position + 5));
#endif
                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)byteLength;
                    buffer[position++] = (byte)(byteLength >> 8);
                    buffer[position++] = (byte)(byteLength >> 16);
                    buffer[position++] = (byte)(byteLength >> 24);
                    position += byteLength;
                }
                else
                {
                    EnsureBufferSize(1);
                    buffer[position++] = nullByte;
                }
            }
        }
    }
}