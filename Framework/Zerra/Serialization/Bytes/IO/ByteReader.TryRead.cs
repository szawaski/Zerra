// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0028 // Simplify collection initialization

namespace Zerra.Serialization.Bytes.IO
{
    public partial struct ByteReader
    {
#if DEBUG
        public static bool Testing = false;

        private static bool Alternate = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Skip()
        {
            if (!ByteReader.Testing)
                return false;
            if (ByteReader.Alternate)
            {
                ByteReader.Alternate = false;
                return false;
            }
            else
            {
                ByteReader.Alternate = true;
                return true;
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadIsNull(out bool value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = buffer[position++] is nullByte;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out bool value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = buffer[position++] != 0;

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out bool? value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = buffer[position++] != 0;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out bool[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<bool>();
                return true;
            }

            sizeNeeded = collectionLength;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new bool[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var item = buffer[position++] != 0;
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<bool>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<bool>(0);
                return true;
            }

            sizeNeeded = collectionLength;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<bool>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var item = buffer[position++] != 0;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<bool>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<bool>();
#else
                value = new HashSet<bool>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<bool>();
#else
            value = new HashSet<bool>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var item = buffer[position++] != 0;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out bool?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<bool?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 1, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new bool?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var item = buffer[position++] != 0;
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<bool?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<bool?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 1, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<bool?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<bool?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<bool?>();
#else
                value = new HashSet<bool?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 1, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<bool?>();
#else
            value = new HashSet<bool?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out byte value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = buffer[position++];
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out byte? value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = buffer[position++];
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryRead(out byte[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<byte>();
                return true;
            }

            sizeNeeded = collectionLength;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new byte[collectionLength];
            fixed (byte* pArray = value)
            {
                for (var i = 0; i < collectionLength; i++)
                {
                    pArray[i] = buffer[position++];
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<byte>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<byte>(0);
                return true;
            }

            sizeNeeded = collectionLength;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<byte>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var item = buffer[position++];
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<byte>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<byte>();
#else
                value = new HashSet<byte>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<byte>();
#else
            value = new HashSet<byte>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var item = buffer[position++];
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out byte?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<byte?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 1, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new byte?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var item = buffer[position++];
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<byte?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<byte?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 1, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<byte?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<byte?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<byte?>();
#else
                value = new HashSet<byte?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 1, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<byte?>();
#else
            value = new HashSet<byte?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out sbyte value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = (sbyte)buffer[position++];
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out sbyte? value, out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = (sbyte)buffer[position++];
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out sbyte[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<sbyte>();
                return true;
            }

            sizeNeeded = collectionLength;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new sbyte[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (sbyte)buffer[position++];
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<sbyte>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<sbyte>(0);
                return true;
            }

            sizeNeeded = collectionLength;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<sbyte>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (sbyte)buffer[position++];
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<sbyte>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<sbyte>();
#else
                value = new HashSet<sbyte>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<sbyte>();
#else
            value = new HashSet<sbyte>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (sbyte)buffer[position++];
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out sbyte?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<sbyte?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 1, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new sbyte?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var item = (sbyte)buffer[position++];
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<sbyte?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<sbyte?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 1, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<sbyte?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<sbyte?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<sbyte?>();
#else
                value = new HashSet<sbyte?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 1, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<sbyte?>();
#else
            value = new HashSet<sbyte?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out short value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = (short)(buffer[position++] | buffer[position++] << 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out short? value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = (short)(buffer[position++] | buffer[position++] << 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out short[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<short>();
                return true;
            }

            sizeNeeded = collectionLength * 2;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new short[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (short)(buffer[position++] | buffer[position++] << 8);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<short>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<short>(0);
                return true;
            }

            sizeNeeded = collectionLength * 2;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<short>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (short)(buffer[position++] | buffer[position++] << 8);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<short>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {

#if NETSTANDARD2_0
                value = new HashSet<short>();
#else
                value = new HashSet<short>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 2;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<short>();
#else
            value = new HashSet<short>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (short)(buffer[position++] | buffer[position++] << 8);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out short?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<short?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 2, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new short?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var item = (short)(buffer[position++] | buffer[position++] << 8);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<short?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<short?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 2, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<short?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<short?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<short?>();
#else
                value = new HashSet<short?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 2, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<short?>();
#else
            value = new HashSet<short?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out ushort value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = (ushort)(buffer[position++] | buffer[position++] << 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out ushort? value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = (ushort)(buffer[position++] | buffer[position++] << 8);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out ushort[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<ushort>();
                return true;
            }

            sizeNeeded = collectionLength * 2;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new ushort[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<ushort>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<ushort>(0);
                return true;
            }

            sizeNeeded = collectionLength * 2;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<ushort>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<ushort>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<ushort>();
#else
                value = new HashSet<ushort>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 2;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<ushort>();
#else
            value = new HashSet<ushort>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out ushort?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<ushort?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 2, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new ushort?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var item = (ushort)(buffer[position++] | buffer[position++] << 8);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<ushort?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<ushort?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 2, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<ushort?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<ushort?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<ushort?>();
#else
                value = new HashSet<ushort?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 2, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<ushort?>();
#else
            value = new HashSet<ushort?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out int value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out int? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out int[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<int>();
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new int[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<int>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<int>(0);
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<int>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<int>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<int>();
#else
                value = new HashSet<int>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<int>();
#else
            value = new HashSet<int>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out int?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<int?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new int?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var item = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<int?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<int?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<int?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<int?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<int?>();
#else
                value = new HashSet<int?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<int?>();
#else
            value = new HashSet<int?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out uint value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out uint? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            value = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out uint[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<uint>();
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new uint[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<uint>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<uint>(0);
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<uint>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<uint>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {

#if NETSTANDARD2_0
                value = new HashSet<uint>();
#else
                value = new HashSet<uint>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<uint>();
#else
            value = new HashSet<uint>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out uint?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<uint?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new uint?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var item = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<uint?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<uint?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<uint?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<uint?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<uint?>();
#else
                value = new HashSet<uint?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<uint?>();
#else
            value = new HashSet<uint?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out long value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public bool TryRead(out long? value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public bool TryRead(out long[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<long>();
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new long[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = (long)((ulong)hi) << 32 | lo;
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<long>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<long>(0);
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<long>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = (long)((ulong)hi) << 32 | lo;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<long>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<long>();
#else
                value = new HashSet<long>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<long>();
#else
            value = new HashSet<long>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = (long)((ulong)hi) << 32 | lo;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out long?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<long?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new long?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out List<long?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<long?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<long?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<long?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<long?>();
#else
                value = new HashSet<long?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<long?>();
#else
            value = new HashSet<long?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out ulong value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public bool TryRead(out ulong? value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public bool TryRead(out ulong[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<ulong>();
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new ulong[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = ((ulong)hi) << 32 | lo;
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<ulong>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<ulong>(0);
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<ulong>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = ((ulong)hi) << 32 | lo;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<ulong>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<ulong>();
#else
                value = new HashSet<ulong>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<ulong>();
#else
            value = new HashSet<ulong>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = ((ulong)hi) << 32 | lo;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out ulong?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<ulong?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new ulong?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out List<ulong?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<ulong?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<ulong?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<ulong?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<ulong?>();
#else
                value = new HashSet<ulong?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<ulong?>();
#else
            value = new HashSet<ulong?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public unsafe bool TryRead(out float value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            value = *((float*)&tmpBuffer);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryRead(out float? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            value = *((float*)&tmpBuffer);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryRead(out float[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<float>();
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new float[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = *((float*)&tmpBuffer);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryRead(out List<float>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<float>(0);
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<float>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = *((float*)&tmpBuffer);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryRead(out HashSet<float>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<float>();
#else
                value = new HashSet<float>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<float>();
#else
            value = new HashSet<float>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = *((float*)&tmpBuffer);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryRead(out float?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<float?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new float?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var tmpBuffer = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = *((float*)&tmpBuffer);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryRead(out List<float?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<float?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<float?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public unsafe bool TryRead(out HashSet<float?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<float?>();
#else
                value = new HashSet<float?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<float?>();
#else
            value = new HashSet<float?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public unsafe bool TryRead(out double value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public unsafe bool TryRead(out double? value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public unsafe bool TryRead(out double[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<double>();
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new double[collectionLength];
            for (var i = 0; i < collectionLength; i++)
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
        public unsafe bool TryRead(out List<double>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<double>(0);
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<double>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
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
        public unsafe bool TryRead(out HashSet<double>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<double>();
#else
                value = new HashSet<double>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<double>();
#else
            value = new HashSet<double>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
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
        public unsafe bool TryRead(out double?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<double?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new double?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public unsafe bool TryRead(out List<double?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<double?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<double?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public unsafe bool TryRead(out HashSet<double?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<double?>();
#else
                value = new HashSet<double?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<double?>();
#else
            value = new HashSet<double?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out decimal value, out int sizeNeeded)
        {
            sizeNeeded = 16;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            value = new decimal([lo, mid, hi, flags]);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out decimal? value, out int sizeNeeded)
        {
            sizeNeeded = 16;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
            value = new decimal([lo, mid, hi, flags]);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out decimal[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<decimal>();
                return true;
            }

            sizeNeeded = collectionLength * 16;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new decimal[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var item = new decimal([lo, mid, hi, flags]);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<decimal>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<decimal>(0);
                return true;
            }

            sizeNeeded = collectionLength * 16;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<decimal>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var item = new decimal([lo, mid, hi, flags]);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<decimal>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<decimal>();
#else
                value = new HashSet<decimal>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 16;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<decimal>();
#else
            value = new HashSet<decimal>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                var item = new decimal([lo, mid, hi, flags]);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out decimal?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<decimal?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 16, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new decimal?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var item = new decimal([lo, mid, hi, flags]);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<decimal?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<decimal?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 16, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<decimal?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var item = new decimal([lo, mid, hi, flags]);
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
        public bool TryRead(out HashSet<decimal?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<decimal?>();
#else
                value = new HashSet<decimal?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 16, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<decimal?>();
#else
            value = new HashSet<decimal?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var lo = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var mid = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var hi = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var flags = ((int)buffer[position++]) | ((int)buffer[position++] << 8) | ((int)buffer[position++] << 16) | ((int)buffer[position++] << 24);
                    var item = new decimal([lo, mid, hi, flags]);
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
        public bool TryRead(out DateTime value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public bool TryRead(out DateTime? value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public bool TryRead(out DateTime[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<DateTime>();
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new DateTime[collectionLength];
            for (var i = 0; i < collectionLength; i++)
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
        public bool TryRead(out List<DateTime>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<DateTime>(0);
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<DateTime>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
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
        public bool TryRead(out HashSet<DateTime>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<DateTime>();
#else
                value = new HashSet<DateTime>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<DateTime>();
#else
            value = new HashSet<DateTime>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
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
        public bool TryRead(out DateTime?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<DateTime?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new DateTime?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out List<DateTime?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<DateTime?>(collectionLength);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<DateTime?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<DateTime?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<DateTime?>();
#else
                value = new HashSet<DateTime?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<DateTime?>();
#else
            value = new HashSet<DateTime?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out DateTimeOffset value, out int sizeNeeded)
        {
            sizeNeeded = 10;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public bool TryRead(out DateTimeOffset? value, out int sizeNeeded)
        {
            sizeNeeded = 10;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public bool TryRead(out DateTimeOffset[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<DateTimeOffset>();
                return true;
            }

            sizeNeeded = collectionLength * 10;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new DateTimeOffset[collectionLength];
            for (var i = 0; i < collectionLength; i++)
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
        public bool TryRead(out List<DateTimeOffset>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<DateTimeOffset>(0);
                return true;
            }

            sizeNeeded = collectionLength * 10;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<DateTimeOffset>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
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
        public bool TryRead(out HashSet<DateTimeOffset>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<DateTimeOffset>();
#else
                value = new HashSet<DateTimeOffset>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 10;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<DateTimeOffset>();
#else
            value = new HashSet<DateTimeOffset>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
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
        public bool TryRead(out DateTimeOffset?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<DateTimeOffset?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 10, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new DateTimeOffset?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out List<DateTimeOffset?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<DateTimeOffset?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 10, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<DateTimeOffset?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<DateTimeOffset?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<DateTimeOffset?>();
#else
                value = new HashSet<DateTimeOffset?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 10, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<DateTimeOffset?>();
#else
            value = new HashSet<DateTimeOffset?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out TimeSpan value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public bool TryRead(out TimeSpan? value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public bool TryRead(out TimeSpan[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<TimeSpan>();
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new TimeSpan[collectionLength];
            for (var i = 0; i < collectionLength; i++)
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
        public bool TryRead(out List<TimeSpan>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<TimeSpan>(0);
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<TimeSpan>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
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
        public bool TryRead(out HashSet<TimeSpan>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<TimeSpan>();
#else
                value = new HashSet<TimeSpan>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<TimeSpan>();
#else
            value = new HashSet<TimeSpan>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
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
        public bool TryRead(out TimeSpan?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<TimeSpan?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new TimeSpan?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out List<TimeSpan?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<TimeSpan?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<TimeSpan?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public bool TryRead(out HashSet<TimeSpan?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<TimeSpan?>();
#else
                value = new HashSet<TimeSpan?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<TimeSpan?>();
#else
            value = new HashSet<TimeSpan?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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

#if NET6_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out DateOnly value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var dayNumber = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            value = DateOnly.FromDayNumber(dayNumber);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out DateOnly? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var dayNumber = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            value = DateOnly.FromDayNumber(dayNumber);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out DateOnly[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<DateOnly>();
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new DateOnly[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var dayNumber = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = DateOnly.FromDayNumber(dayNumber);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<DateOnly>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<DateOnly>(0);
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<DateOnly>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var dayNumber = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = DateOnly.FromDayNumber(dayNumber);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<DateOnly>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
            value = new HashSet<DateOnly>();
#else
                value = new HashSet<DateOnly>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 4;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<DateOnly>();
#else
            value = new HashSet<DateOnly>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var dayNumber = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var item = DateOnly.FromDayNumber(dayNumber);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out DateOnly?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<DateOnly?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new DateOnly?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var dayNumber = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = DateOnly.FromDayNumber(dayNumber);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<DateOnly?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<DateOnly?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<DateOnly?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var dayNumber = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = DateOnly.FromDayNumber(dayNumber);
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
        public bool TryRead(out HashSet<DateOnly?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
            value = new HashSet<DateOnly?>();
#else
                value = new HashSet<DateOnly?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 4, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<DateOnly?>();
#else
            value = new HashSet<DateOnly?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var dayNumber = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var item = DateOnly.FromDayNumber(dayNumber);
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
        public bool TryRead(out TimeOnly value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            value = new TimeOnly(ticks);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out TimeOnly? value, out int sizeNeeded)
        {
            sizeNeeded = 8;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            var ticks = (long)((ulong)hi) << 32 | lo;
            value = new TimeOnly(ticks);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out TimeOnly[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<TimeOnly>();
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new TimeOnly[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new TimeOnly(ticks);
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<TimeOnly>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<TimeOnly>(0);
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<TimeOnly>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new TimeOnly(ticks);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<TimeOnly>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
            value = new HashSet<TimeOnly>();
#else
                value = new HashSet<TimeOnly>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 8;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<TimeOnly>();
#else
            value = new HashSet<TimeOnly>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                var ticks = (long)((ulong)hi) << 32 | lo;
                var item = new TimeOnly(ticks);
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out TimeOnly?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<TimeOnly?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new TimeOnly?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new TimeOnly(ticks);
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<TimeOnly?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<TimeOnly?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<TimeOnly?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new TimeOnly(ticks);
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
        public bool TryRead(out HashSet<TimeOnly?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
            value = new HashSet<TimeOnly?>();
#else
                value = new HashSet<TimeOnly?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 8, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<TimeOnly?>();
#else
            value = new HashSet<TimeOnly?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
                    var lo = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var hi = (uint)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
                    var ticks = (long)((ulong)hi) << 32 | lo;
                    var item = new TimeOnly(ticks);
                    value.Add(item);
                }
                else
                {
                    value.Add(null);
                }
            }
            return true;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out Guid value, out int sizeNeeded)
        {
            sizeNeeded = 16;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new Guid(buffer.Slice(position, 16).ToArray());
#else
            value = new Guid(buffer.Slice(position, 16));
#endif
            position += 16;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out Guid? value, out int sizeNeeded)
        {
            sizeNeeded = 16;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }


#if NETSTANDARD2_0
            value = new Guid(buffer.Slice(position, 16).ToArray());
#else
            value = new Guid(buffer.Slice(position, 16));
#endif
            position += 16;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out Guid[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<Guid>();
                return true;
            }

            sizeNeeded = collectionLength * 16;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new Guid[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
#if NETSTANDARD2_0
                var item = new Guid(buffer.Slice(position, 16).ToArray());
#else
                var item = new Guid(buffer.Slice(position, 16));
#endif
                position += 16;
                value[i] = item;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<Guid>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<Guid>(0);
                return true;
            }

            sizeNeeded = collectionLength * 16;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<Guid>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
#if NETSTANDARD2_0
                var item = new Guid(buffer.Slice(position, 16).ToArray());
#else
                var item = new Guid(buffer.Slice(position, 16));
#endif
                position += 16;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out HashSet<Guid>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<Guid>();
#else
                value = new HashSet<Guid>(0);
#endif
                return true;
            }

            sizeNeeded = collectionLength * 16;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<Guid>();
#else
            value = new HashSet<Guid>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
#if NETSTANDARD2_0
                var item = new Guid(buffer.Slice(position, 16).ToArray());
#else
                var item = new Guid(buffer.Slice(position, 16));
#endif
                position += 16;
                value.Add(item);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out Guid?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<Guid?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 16, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new Guid?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
#if NETSTANDARD2_0
                    var item = new Guid(buffer.Slice(position, 16).ToArray());
#else
                    var item = new Guid(buffer.Slice(position, 16));
#endif
                    position += 16;
                    value[i] = item;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out List<Guid?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<Guid?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 16, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<Guid?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
#if NETSTANDARD2_0
                    var item = new Guid(buffer.Slice(position, 16).ToArray());
#else
                    var item = new Guid(buffer.Slice(position, 16));
#endif
                    position += 16;
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
        public bool TryRead(out HashSet<Guid?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<Guid?>();
#else
                value = new HashSet<Guid?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 16, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<Guid?>();
#else
            value = new HashSet<Guid?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
                {
#if NETSTANDARD2_0
                    var item = new Guid(buffer.Slice(position, 16).ToArray());
#else
                    var item = new Guid(buffer.Slice(position, 16));
#endif
                    position += 16;
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
        public unsafe bool TryRead(out char value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public unsafe bool TryRead(out char? value, out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
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
        public unsafe bool TryRead(out char[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<char>();
                return true;
            }

            sizeNeeded = collectionLength * 2;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new char[collectionLength];
            fixed (char* pArray = value)
            {
                for (var i = 0; i < collectionLength; i++)
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
        public unsafe bool TryRead(out List<char>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<char>(0);
                return true;
            }

            sizeNeeded = collectionLength * 2;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = new List<char>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
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
        public unsafe bool TryRead(out HashSet<char>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<char>();
#else
                value = new HashSet<char>(0);
#endif
                return true;
            }


            sizeNeeded = collectionLength * 2;
            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<char>();
#else
            value = new HashSet<char>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
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
        public unsafe bool TryRead(out char?[]? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = Array.Empty<char?>();
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 2, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new char?[collectionLength];
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public unsafe bool TryRead(out List<char?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
                value = new List<char?>(0);
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 2, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

            value = new List<char?>(collectionLength);
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public unsafe bool TryRead(out HashSet<char?>? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            var collectionLength = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (collectionLength == 0)
            {
#if NETSTANDARD2_0
                value = new HashSet<char?>();
#else
                value = new HashSet<char?>(0);
#endif
                return true;
            }

            if (!SeekNullableSizeNeeded(collectionLength, 2, ref sizeNeeded))
            {
                position -= 4;
                value = default;
                return false;
            }

#if NETSTANDARD2_0
            value = new HashSet<char?>();
#else
            value = new HashSet<char?>(collectionLength);
#endif
            for (var i = 0; i < collectionLength; i++)
            {
                if (buffer[position++] is not nullByte)
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
        public unsafe bool TryRead(out string? value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            sizeNeeded = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (sizeNeeded == 0)
            {
                value = String.Empty;
                return true;
            }

            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            var bytesForString = buffer.Slice(position, sizeNeeded);
            fixed (byte* p = bytesForString)
            {
                value = encoding.GetString(p, sizeNeeded);
            }
            position += sizeNeeded;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryRead(out ReadOnlySpan<byte> value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded
#if DEBUG
            || Skip()
#endif
            )
            {
                value = default;
                return false;
            }

            sizeNeeded = (int)(buffer[position++] | buffer[position++] << 8 | buffer[position++] << 16 | buffer[position++] << 24);
            if (sizeNeeded == 0)
            {
                value = default;
                return true;
            }

            if (length - position < sizeNeeded)
            {
                sizeNeeded += 4;
                position -= 4;
                value = default;
                return false;
            }

            value = buffer.Slice(position, sizeNeeded);
            position += sizeNeeded;
            return true;
        }
    }
}