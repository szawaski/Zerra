// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
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
        public unsafe bool TryWrite(byte[] bytes, out int sizeNeeded)
        {
            sizeNeeded = bytes.Length;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            if (bytes.Length == 0)
                return true;

            fixed (byte* pBuffer = &buffer[position], pBytes = bytes)
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    pBuffer[i] = pBytes[i];
                }
            }
            position += bytes.Length;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteNull(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            buffer[position++] = nullByte;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteNotNull(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            buffer[position++] = notNullByte;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(bool value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            buffer[position++] = (byte)(value ? 1 : 0);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<bool> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                buffer[position++] = (byte)(value ? 1 : 0);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<bool?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteBoolCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                var cast = (bool)value;
                buffer[position++] = (byte)(cast ? 1 : 0);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteBoolNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(byte value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            buffer[position++] = value;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<byte> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                buffer[position++] = value;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<byte?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteByteCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                var cast = (byte)value;
                buffer[position++] = cast;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteByteNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(sbyte value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            buffer[position++] = (byte)value;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<sbyte> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<sbyte?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteSByteCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                var cast = (sbyte)value;
                buffer[position++] = (byte)cast;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteSByteNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(short value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<short> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<short?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 3;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteInt16Cast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                var cast = (short)value;
                buffer[position++] = (byte)cast;
                buffer[position++] = (byte)(cast >> 8);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteInt16NullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 3;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(ushort value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<ushort> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                buffer[position++] = (byte)value;
                buffer[position++] = (byte)(value >> 8);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<ushort?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 3;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteUInt16Cast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                var cast = (ushort)value;
                buffer[position++] = (byte)cast;
                buffer[position++] = (byte)(cast >> 8);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteUInt16NullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 3;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(int value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<int> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<int?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 5;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteInt32Cast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                var cast = (int)value;
                buffer[position++] = (byte)cast;
                buffer[position++] = (byte)(cast >> 8);
                buffer[position++] = (byte)(cast >> 16);
                buffer[position++] = (byte)(cast >> 24);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteInt32NullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 5;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(uint value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<uint> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<uint?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 5;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteUInt32Cast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                var cast = (uint)value;
                buffer[position++] = (byte)cast;
                buffer[position++] = (byte)(cast >> 8);
                buffer[position++] = (byte)(cast >> 16);
                buffer[position++] = (byte)(cast >> 24);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteUInt32NullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 5;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(long value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<long> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<long?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteInt64Cast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;

            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteInt64NullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(ulong value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<ulong> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<ulong?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteUInt64Cast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteUInt64NullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(float value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            var tmpValue = *(uint*)&value;
            buffer[position++] = (byte)tmpValue;
            buffer[position++] = (byte)(tmpValue >> 8);
            buffer[position++] = (byte)(tmpValue >> 16);
            buffer[position++] = (byte)(tmpValue >> 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(IEnumerable<float> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public unsafe bool TryWrite(IEnumerable<float?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 5;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public unsafe bool TryWriteSingleCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                var cast = (float)value;
                var tmpValue = *(uint*)&cast;
                buffer[position++] = (byte)tmpValue;
                buffer[position++] = (byte)(tmpValue >> 8);
                buffer[position++] = (byte)(tmpValue >> 16);
                buffer[position++] = (byte)(tmpValue >> 24);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteSingleNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 5;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(double value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public unsafe bool TryWrite(IEnumerable<double> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public unsafe bool TryWrite(IEnumerable<double?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public unsafe bool TryWriteDoubleCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteDoubleNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(decimal value, out int sizeNeeded)
        {
            sizeNeeded = 16;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<decimal> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 16;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<decimal?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 17;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteDecimalCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 16;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteDecimalNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 17;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateTime value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<DateTime> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<DateTime?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteDateTimeCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteDateTimeNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateTimeOffset value, out int sizeNeeded)
        {
            sizeNeeded = 10;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<DateTimeOffset> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 10;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<DateTimeOffset?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 11;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteDateTimeOffsetCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 10;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteDateTimeOffsetNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 11;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(TimeSpan value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<TimeSpan> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<TimeSpan?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteTimeSpanCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteTimeSpanNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateOnly value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            buffer[position++] = (byte)value.DayNumber;
            buffer[position++] = (byte)(value.DayNumber >> 8);
            buffer[position++] = (byte)(value.DayNumber >> 16);
            buffer[position++] = (byte)(value.DayNumber >> 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<DateOnly> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<DateOnly?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 5;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteDateOnlyCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                var cast = (DateOnly)value;
                buffer[position++] = (byte)cast.DayNumber;
                buffer[position++] = (byte)(cast.DayNumber >> 8);
                buffer[position++] = (byte)(cast.DayNumber >> 16);
                buffer[position++] = (byte)(cast.DayNumber >> 24);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteDateOnlyNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(TimeOnly value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<TimeOnly> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<TimeOnly?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteTimeOnlyCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteTimeOnlyNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 9;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(Guid value, out int sizeNeeded)
        {
            sizeNeeded = 16;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public unsafe bool TryWrite(IEnumerable<Guid> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 16;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public unsafe bool TryWrite(IEnumerable<Guid?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 17;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public unsafe bool TryWriteGuidCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 16;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteGuidNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 17;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(char value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            Unsafe.As<byte, char>(ref buffer[position]) = value;
            position += 2;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<char> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                Unsafe.As<byte, char>(ref buffer[position]) = value;
                position += 2;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(IEnumerable<char?> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 3;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWriteCharCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

            foreach (var value in values)
            {
                var cast = (char)value;
                Unsafe.As<byte, char>(ref buffer[position]) = cast;
                position += 2;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteCharNullableCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = maxLength * 3;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(string value, out int sizeNeeded)
        {
            sizeNeeded = encoding.GetMaxByteCount(value.Length) + 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        public bool TryWrite(IEnumerable<string> values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = 0;
            foreach (var value in values)
                sizeNeeded += value is null ? 1 : encoding.GetMaxByteCount(value.Length) + 5;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteStringCast(IEnumerable values, int maxLength, out int sizeNeeded)
        {
            sizeNeeded = 0;
            foreach (string value in values)
                sizeNeeded += encoding.GetMaxByteCount(value.Length) + 5;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }

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
                    buffer[position++] = nullByte;
                }
            }
            return true;
        }
    }
}
