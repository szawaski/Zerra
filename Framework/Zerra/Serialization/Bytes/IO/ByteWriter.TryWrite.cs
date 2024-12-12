// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization.Bytes.IO
{
    public ref partial struct ByteWriter
    {
#if DEBUG
        public static bool Testing = false;

        private static bool Alternate = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Skip()
        {
            if (!ByteWriter.Testing)
                return false;
            if (ByteWriter.Alternate)
            {
                ByteWriter.Alternate = false;
                return false;
            }
            else
            {
                ByteWriter.Alternate = true;
                return true;
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteRaw(byte[] bytes, out int sizeNeeded)
        {
            sizeNeeded = bytes.Length;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            fixed (byte* pBuffer = &buffer[position], pBytes = bytes)
            {
                Buffer.MemoryCopy(pBytes, pBuffer, buffer.Length - position, bytes.LongLength);
            }
            position += bytes.Length;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWritePropertyName(ReadOnlySpan<byte> bytes, out int sizeNeeded)
        {
            sizeNeeded = bytes.Length + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            var byteLength = bytes.Length;

            buffer[position++] = (byte)byteLength;
            buffer[position++] = (byte)(byteLength >> 8);
            buffer[position++] = (byte)(byteLength >> 16);
            buffer[position++] = (byte)(byteLength >> 24);

            if (byteLength == 0)
                return true;

            fixed (byte* pBuffer = &buffer[position], pBytes = bytes)
            {
                Buffer.MemoryCopy(pBytes, pBuffer, buffer.Length - position, byteLength);
            }
            position += byteLength;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteNull(out int sizeNeeded)
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

            buffer[position++] = nullByte;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteNotNull(out int sizeNeeded)
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

            buffer[position++] = notNullByte;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(bool value, out int sizeNeeded)
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

            buffer[position++] = (byte)(value ? 1 : 0);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<bool> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            foreach (var value in values)
            {
                buffer[position++] = (byte)(value ? 1 : 0);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<bool?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 2 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(byte value, out int sizeNeeded)
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

            buffer[position++] = value;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<byte> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            foreach (var value in values)
            {
                buffer[position++] = value;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<byte?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 2 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(sbyte value, out int sizeNeeded)
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

            buffer[position++] = (byte)value;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<sbyte> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<sbyte?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 2 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(short value, out int sizeNeeded)
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

            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<short> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 2 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<short?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 3 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(ushort value, out int sizeNeeded)
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

            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<ushort> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 2 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<ushort?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 3 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(int value, out int sizeNeeded)
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

            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<int> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 4 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
                buffer[position++] = (byte)(value >> 16);
                buffer[position++] = (byte)(value >> 24);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<int?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 5 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(uint value, out int sizeNeeded)
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

            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<uint> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 4 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
                buffer[position++] = (byte)(value >> 16);
                buffer[position++] = (byte)(value >> 24);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<uint?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 5 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(long value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 24);
            buffer[position++] = (byte)(value >> 32);
            buffer[position++] = (byte)(value >> 40);
            buffer[position++] = (byte)(value >> 48);
            buffer[position++] = (byte)(value >> 56);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<long> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 8 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<long?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 9 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(ulong value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 24);
            buffer[position++] = (byte)(value >> 32);
            buffer[position++] = (byte)(value >> 40);
            buffer[position++] = (byte)(value >> 48);
            buffer[position++] = (byte)(value >> 56);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<ulong> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 8 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<ulong?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 9 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(float value, out int sizeNeeded)
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

            var tmpValue = *(uint*)&value;
            buffer[position++] = (byte)tmpValue;
            buffer[position++] = (byte)(tmpValue >> 8);
            buffer[position++] = (byte)(tmpValue >> 16);
            buffer[position++] = (byte)(tmpValue >> 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(IEnumerable<float> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 4 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            foreach (var value in values)
            {
                var tmpValue = *(uint*)&value;
                buffer[position++] = (byte)tmpValue;
                buffer[position++] = (byte)(tmpValue >> 8);
                buffer[position++] = (byte)(tmpValue >> 16);
                buffer[position++] = (byte)(tmpValue >> 24);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(IEnumerable<float?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 5 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(double value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            var tmpValue = *(ulong*)&value;
            buffer[position++] = (byte)tmpValue;
            buffer[position++] = (byte)(tmpValue >> 8);
            buffer[position++] = (byte)(tmpValue >> 16);
            buffer[position++] = (byte)(tmpValue >> 24);
            buffer[position++] = (byte)(tmpValue >> 32);
            buffer[position++] = (byte)(tmpValue >> 40);
            buffer[position++] = (byte)(tmpValue >> 48);
            buffer[position++] = (byte)(tmpValue >> 56);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(IEnumerable<double> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 8 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(IEnumerable<double?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 9 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(decimal value, out int sizeNeeded)
        {
            sizeNeeded = 16;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<decimal> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 16 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<decimal?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 17 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateTime value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            if (value.Kind == DateTimeKind.Local)
                value = value.ToUniversalTime();

            buffer[position++] = (byte)value.Ticks;
            buffer[position++] = (byte)(value.Ticks >> 8);
            buffer[position++] = (byte)(value.Ticks >> 16);
            buffer[position++] = (byte)(value.Ticks >> 24);
            buffer[position++] = (byte)(value.Ticks >> 32);
            buffer[position++] = (byte)(value.Ticks >> 40);
            buffer[position++] = (byte)(value.Ticks >> 48);
            buffer[position++] = (byte)(value.Ticks >> 56);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<DateTime> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 8 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            DateTime value;
            foreach (var v in values)
            {
                value = v;
                if (value.Kind != DateTimeKind.Local)
                    value = value.ToUniversalTime();

                buffer[position++] = (byte)value.Ticks;
                buffer[position++] = (byte)(value.Ticks >> 8);
                buffer[position++] = (byte)(value.Ticks >> 16);
                buffer[position++] = (byte)(value.Ticks >> 24);
                buffer[position++] = (byte)(value.Ticks >> 32);
                buffer[position++] = (byte)(value.Ticks >> 40);
                buffer[position++] = (byte)(value.Ticks >> 48);
                buffer[position++] = (byte)(value.Ticks >> 56);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<DateTime?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 9 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            DateTime value;
            foreach (var v in values)
            {
                if (v.HasValue)
                {
                    value = v.Value;
                    if (value.Kind != DateTimeKind.Local)
                        value = value.ToUniversalTime();

                    buffer[position++] = notNullByte;
                    buffer[position++] = (byte)value.Ticks;
                    buffer[position++] = (byte)(value.Ticks >> 8);
                    buffer[position++] = (byte)(value.Ticks >> 16);
                    buffer[position++] = (byte)(value.Ticks >> 24);
                    buffer[position++] = (byte)(value.Ticks >> 32);
                    buffer[position++] = (byte)(value.Ticks >> 40);
                    buffer[position++] = (byte)(value.Ticks >> 48);
                    buffer[position++] = (byte)(value.Ticks >> 56);
                }
                else
                {
                    buffer[position++] = nullByte;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateTimeOffset value, out int sizeNeeded)
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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<DateTimeOffset> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 10 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<DateTimeOffset?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 11 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(TimeSpan value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)value.Ticks;
            buffer[position++] = (byte)(value.Ticks >> 8);
            buffer[position++] = (byte)(value.Ticks >> 16);
            buffer[position++] = (byte)(value.Ticks >> 24);
            buffer[position++] = (byte)(value.Ticks >> 32);
            buffer[position++] = (byte)(value.Ticks >> 40);
            buffer[position++] = (byte)(value.Ticks >> 48);
            buffer[position++] = (byte)(value.Ticks >> 56);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<TimeSpan> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 8 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<TimeSpan?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 9 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateOnly value, out int sizeNeeded)
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

            buffer[position++] = (byte)value.DayNumber;
            buffer[position++] = (byte)(value.DayNumber >> 8);
            buffer[position++] = (byte)(value.DayNumber >> 16);
            buffer[position++] = (byte)(value.DayNumber >> 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<DateOnly> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 4 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            foreach (var value in values)
            {
                buffer[position++] = (byte)value.DayNumber;
                buffer[position++] = (byte)(value.DayNumber >> 8);
                buffer[position++] = (byte)(value.DayNumber >> 16);
                buffer[position++] = (byte)(value.DayNumber >> 24);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<DateOnly?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 5 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(TimeOnly value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)value.Ticks;
            buffer[position++] = (byte)(value.Ticks >> 8);
            buffer[position++] = (byte)(value.Ticks >> 16);
            buffer[position++] = (byte)(value.Ticks >> 24);
            buffer[position++] = (byte)(value.Ticks >> 32);
            buffer[position++] = (byte)(value.Ticks >> 40);
            buffer[position++] = (byte)(value.Ticks >> 48);
            buffer[position++] = (byte)(value.Ticks >> 56);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<TimeOnly> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 8 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<TimeOnly?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 9 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(Guid value, out int sizeNeeded)
        {
            sizeNeeded = 16;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            var bytes = value.ToByteArray();
            fixed (byte* pBuffer = &buffer[position], pBytes = &bytes[0])
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    pBuffer[i] = pBytes[i];
                }
            }
            position += 16;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(IEnumerable<Guid> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 16 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(IEnumerable<Guid?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 17 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(char value, out int sizeNeeded)
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

            Unsafe.As<byte, char>(ref buffer[position]) = value;
            position += 2;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<char> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 2 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

            foreach (var value in values)
            {
                Unsafe.As<byte, char>(ref buffer[position]) = value;
                position += 2;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<char?> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = collectionLength * 3 + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

            buffer[position++] = (byte)collectionLength;
            buffer[position++] = (byte)(collectionLength >> 8);
            buffer[position++] = (byte)(collectionLength >> 16);
            buffer[position++] = (byte)(collectionLength >> 24);

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(string value, out int sizeNeeded)
        {
            sizeNeeded = encoding.GetMaxByteCount(value.Length) + 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

#if NETSTANDARD2_0
            var charBytes = encoding.GetBytes(value);
            charBytes.AsSpan().CopyTo(buffer.Slice(position + 4));
            var byteLength = charBytes.Length;
#else

            var byteLength = encoding.GetBytes(value.AsSpan(), buffer.Slice(position + 4));
#endif

            buffer[position++] = (byte)byteLength;
            buffer[position++] = (byte)(byteLength >> 8);
            buffer[position++] = (byte)(byteLength >> 16);
            buffer[position++] = (byte)(byteLength >> 24);

            position += byteLength;

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<string> values, int collectionLength, out int sizeNeeded)
        {
            sizeNeeded = 0;
            foreach (var value in values)
                sizeNeeded += value is null ? 1 : encoding.GetMaxByteCount(value.Length) + 5;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (Skip())
                return false;
#endif

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
                    buffer[position++] = nullByte;
                }
            }
            return true;
        }
    }
}
