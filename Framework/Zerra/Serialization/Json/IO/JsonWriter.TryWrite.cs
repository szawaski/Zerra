// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Zerra.Serialization.Json.IO
{
    public ref partial struct JsonWriter
    {
        /// <summary>
        /// Attempts to write a byte value to the buffer.
        /// </summary>
        /// <param name="value">The byte value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(byte value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
#if !NETSTANDARD2_0
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, default, CultureInfo.InvariantCulture);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString(CultureInfo.InvariantCulture);
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
                position += consumed;
#endif
            }
            return true;
        }

        /// <summary>
        /// Attempts to write a signed byte value to the buffer.
        /// </summary>
        /// <param name="value">The signed byte value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(sbyte value, out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
#if !NETSTANDARD2_0
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, default, CultureInfo.InvariantCulture);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString(CultureInfo.InvariantCulture);
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
                position += consumed;
#endif
            }
            return true;
        }

        /// <summary>
        /// Attempts to write a short value to the buffer.
        /// </summary>
        /// <param name="value">The short value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(short value, out int sizeNeeded)
        {
            sizeNeeded = 6;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
#if !NETSTANDARD2_0
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, default, CultureInfo.InvariantCulture);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString(CultureInfo.InvariantCulture);
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
                position += consumed;
#endif
            }
            return true;
        }

        /// <summary>
        /// Attempts to write an unsigned short value to the buffer.
        /// </summary>
        /// <param name="value">The unsigned short value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(ushort value, out int sizeNeeded)
        {
            sizeNeeded = 5;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
#if !NETSTANDARD2_0
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, default, CultureInfo.InvariantCulture);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString(CultureInfo.InvariantCulture);
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
                position += consumed;
#endif
            }
            return true;
        }

        /// <summary>
        /// Attempts to write an int value to the buffer.
        /// </summary>
        /// <param name="value">The int value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(int value, out int sizeNeeded)
        {
            sizeNeeded = 11;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
#if !NETSTANDARD2_0
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, default, CultureInfo.InvariantCulture);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString(CultureInfo.InvariantCulture);
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
                position += consumed;
#endif
            }
            return true;
        }

        /// <summary>
        /// Attempts to write an unsigned int value to the buffer.
        /// </summary>
        /// <param name="value">The unsigned int value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(uint value, out int sizeNeeded)
        {
            sizeNeeded = 10;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
#if !NETSTANDARD2_0
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, default, CultureInfo.InvariantCulture);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString(CultureInfo.InvariantCulture);
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
                position += consumed;
#endif
            }
            return true;
        }

        /// <summary>
        /// Attempts to write a long value to the buffer.
        /// </summary>
        /// <param name="value">The long value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(long value, out int sizeNeeded)
        {
            sizeNeeded = 20;
            if (length - position < sizeNeeded && !Grow(sizeNeeded))
                return false;

            if (useBytes)
            {
#if !NETSTANDARD2_0
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, default, CultureInfo.InvariantCulture);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString(CultureInfo.InvariantCulture);
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
                position += consumed;
#endif
            }
            return true;
        }

        /// <summary>
        /// Attempts to write an unsigned long value to the buffer.
        /// </summary>
        /// <param name="value">The unsigned long value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(ulong value, out int sizeNeeded)
        {
            sizeNeeded = 20;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
#if !NETSTANDARD2_0
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, default, CultureInfo.InvariantCulture);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString(CultureInfo.InvariantCulture);
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
                position += consumed;
#endif
            }
            return true;
        }

        /// <summary>
        /// Attempts to write a float value to the buffer.
        /// </summary>
        /// <param name="value">The float value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(float value, out int sizeNeeded)
        {
            sizeNeeded = 16; //min
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

#if NETSTANDARD2_0
            //.NET Framework's default format doesn't round trip, and its R can be a digit short, so it's checked
            var str = value.ToString("R", CultureInfo.InvariantCulture);
            if (!Single.IsNaN(value) && Single.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture) != value)
                str = value.ToString("G9", CultureInfo.InvariantCulture);
            if (useBytes)
            {
                for (var i = 0; i < str.Length; i++)
                    bufferBytes[position + i] = (byte)str[i];
            }
            else
            {
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
            }
            position += str.Length;
#else
            if (useBytes)
            {
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, default, CultureInfo.InvariantCulture);
                position += written;
            }
            else
            {
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
                position += consumed;
            }
#endif

            return true;
        }

        /// <summary>
        /// Attempts to write a double value to the buffer.
        /// </summary>
        /// <param name="value">The double value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(double value, out int sizeNeeded)
        {
            sizeNeeded = 32; //min
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

#if NETSTANDARD2_0
            //.NET Framework's default format doesn't round trip, and its R can be a digit short, so it's checked
            var str = value.ToString("R", CultureInfo.InvariantCulture);
            if (!Double.IsNaN(value) && Double.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture) != value)
                str = value.ToString("G17", CultureInfo.InvariantCulture);
            if (useBytes)
            {
                for (var i = 0; i < str.Length; i++)
                    bufferBytes[position + i] = (byte)str[i];
            }
            else
            {
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
            }
            position += str.Length;
#else
            if (useBytes)
            {
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, default, CultureInfo.InvariantCulture);
                position += written;
            }
            else
            {
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
                position += consumed;
            }
#endif

            return true;
        }

        /// <summary>
        /// Attempts to write a decimal value to the buffer.
        /// </summary>
        /// <param name="value">The decimal value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(decimal value, out int sizeNeeded)
        {
            sizeNeeded = 31;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
#if !NETSTANDARD2_0
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, default, CultureInfo.InvariantCulture);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString(CultureInfo.InvariantCulture);
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
                position += consumed;
#endif
            }

            return true;
        }

        /// <summary>
        /// Attempts to write a quoted string value to the buffer.
        /// </summary>
        /// <param name="value">The string value to write. If null, returns true without writing.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteQuoted(string? value, out int sizeNeeded)
        {
            if (value is null)
            {
                sizeNeeded = 0;
                return true;
            }
            if (value.Length == 0)
            {
                sizeNeeded = 2;
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (DebugShouldReturn())
                    return false;
#endif

                if (useBytes)
                {
                    bufferBytes[position++] = quoteByte;
                    bufferBytes[position++] = quoteByte;
                }
                else
                {
                    bufferChars[position++] = '"';
                    bufferChars[position++] = '"';
                }
                return true;
            }

            if (useBytes)
            {
                sizeNeeded = encoding.GetMaxByteCount(value.Length) + 2;
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (DebugShouldReturn())
                    return false;
#endif

                bufferBytes[position++] = quoteByte;
                fixed (char* pSource = value)
                fixed (byte* pBuffer = &bufferBytes[position])
                {
                    position += encoding.GetBytes(pSource, value.Length, pBuffer, bufferBytes.Length - position);
                }
                bufferBytes[position++] = quoteByte;

                return true;
            }
            else
            {
                sizeNeeded = value.Length + 2;
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (DebugShouldReturn())
                    return false;
#endif

                bufferChars[position++] = '"';
                fixed (char* pSource = value, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, value.Length * 2);
                }
                position += value.Length;
                bufferChars[position++] = '"';

                return true;
            }
        }

        /// <summary>
        /// Attempts to write a DateTime value in ISO8601 format to the buffer.
        /// </summary>
        /// <param name="value">The DateTime value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateTime value, out int sizeNeeded)
        {
            //ISO8601
            //"yyyy-MM-ddTHH:mm:ss.fffffff+00:00"
            sizeNeeded = 35;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written, new StandardFormat('O'));

                var trim = 0;
                while (trim < 7 && bufferBytes[position + 26 - trim] == zeroByte)
                    trim++;
                if (trim > 0)
                {
                    if (trim == 7)
                        trim = 8;
                    bufferBytes.Slice(position + 27, written - 27).CopyTo(bufferBytes.Slice(position + 27 - trim));
                    written -= trim;
                }
                position += written;

                bufferBytes[position++] = quoteByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '"';
#if NETSTANDARD2_0
                var str = value.ToString("O");
                str.AsSpan().CopyTo(bufferChars.Slice(position));
                var written = str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var written, "O");
#endif

                var trim = 0;
                while (trim < 7 && bufferChars[position + 26 - trim] == '0')
                    trim++;
                if (trim > 0)
                {
                    if (trim == 7)
                        trim = 8;
                    bufferChars.Slice(position + 27, written - 27).CopyTo(bufferChars.Slice(position + 27 - trim));
                    written -= trim;
                }
                position += written;

                bufferChars[position++] = '"';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write a DateTimeOffset value in ISO8601 format to the buffer.
        /// </summary>
        /// <param name="value">The DateTimeOffset value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateTimeOffset value, out int sizeNeeded)
        {
            //ISO8601
            //"yyyy-MM-ddTHH:mm:ss.fffffff+00:00"
            sizeNeeded = 35;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written, new StandardFormat('O'));

                var trim = 0;
                while (trim < 7 && bufferBytes[position + 26 - trim] == zeroByte)
                    trim++;
                if (trim > 0)
                {
                    if (trim == 7)
                        trim = 8;
                    bufferBytes.Slice(position + 27, written - 27).CopyTo(bufferBytes.Slice(position + 27 - trim));
                    written -= trim;
                }
                position += written;

                bufferBytes[position++] = quoteByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '"';
#if NETSTANDARD2_0
                var str = value.ToString("O");
                str.AsSpan().CopyTo(bufferChars.Slice(position));
                var written = str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var written, "O");
#endif

                var trim = 0;
                while (trim < 7 && bufferChars[position + 26 - trim] == '0')
                    trim++;
                if (trim > 0)
                {
                    if (trim == 7)
                        trim = 8;
                    bufferChars.Slice(position + 27, written - 27).CopyTo(bufferChars.Slice(position + 27 - trim));
                    written -= trim;
                }
                position += written;

                bufferChars[position++] = '"';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write a TimeSpan value in ISO8601 format to the buffer.
        /// </summary>
        /// <param name="value">The TimeSpan value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(TimeSpan value, out int sizeNeeded)
        {
            //ISO8601
            //(-)dddddddd.HH:mm:ss.fffffff
            sizeNeeded = 28;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;
#if NETSTANDARD2_0
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written, new StandardFormat('c'));
#else
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, "c");
#endif
                position += written;
                bufferBytes[position++] = quoteByte;
                return true;
            }
#if !NETSTANDARD2_0
            else
            {
                bufferChars[position++] = '"';
                _ = value.TryFormat(bufferChars.Slice(position), out var written, "c");
                position += written;
                bufferChars[position++] = '"';
                return true;
            }
#else
            else
            {
                bufferChars[position++] = '"';

                if (value.Ticks < 0)
                    bufferChars[position++] = '-';

                if (value.Days > 0)
                {
                    WriteInt32Chars(value.Days);
                    bufferChars[position++] = '.';
                }
                else if (value.Days < 0)
                {
                    WriteInt32Chars(-value.Days);
                    bufferChars[position++] = '.';
                }

                if (value.Hours > -1)
                {
                    if (value.Hours < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(value.Hours);
                    bufferChars[position++] = ':';
                }
                else
                {
                    if (-value.Hours < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(-value.Hours);
                    bufferChars[position++] = ':';
                }

                if (value.Minutes > -1)
                {
                    if (value.Minutes < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(value.Minutes);
                    bufferChars[position++] = ':';
                }
                else
                {
                    if (-value.Minutes < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(-value.Minutes);
                    bufferChars[position++] = ':';
                }

                if (value.Seconds > -1)
                {
                    if (value.Seconds < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(value.Seconds);
                }
                else
                {
                    if (-value.Seconds < 10)
                        bufferChars[position++] = '0';
                    WriteInt32Chars(-value.Seconds);
                }

                long fraction;
                if (value.Ticks > -1)
                    fraction = value.Ticks - (value.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
                else
                    fraction = -value.Ticks - (-value.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond;
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

                bufferChars[position++] = '"';

                return true;
            }
#endif
        }

#if !NETSTANDARD2_0
        /// <summary>
        /// Attempts to write a DateOnly value in ISO8601 format to the buffer.
        /// </summary>
        /// <param name="value">The DateOnly value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(DateOnly value, out int sizeNeeded)
        {
            //ISO8601
            //"yyyy-MM-dd"
            sizeNeeded = 12;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif


            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;
                _ = value.TryFormat(bufferBytes.Slice(position), out var written, "O");
                position += written;
                bufferBytes[position++] = quoteByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '"';
                _ = value.TryFormat(bufferChars.Slice(position), out var written, "O");
                position += written;
                bufferChars[position++] = '"';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write a TimeOnly value in ISO8601 format to the buffer.
        /// </summary>
        /// <param name="value">The TimeOnly value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(TimeOnly value, out int sizeNeeded)
        {
            //ISO8601
            //"HH:mm:ss.fffffff"
            sizeNeeded = 18;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif


            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;
                _ = value.ToTimeSpan().TryFormat(bufferBytes.Slice(position), out var written, "c");
                position += written;
                bufferBytes[position++] = quoteByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '"';
                _ = value.ToTimeSpan().TryFormat(bufferChars.Slice(position), out var written, "c");
                position += written;
                bufferChars[position++] = '"';
                return true;
            }
        }
#endif

        /// <summary>
        /// Attempts to write a GUID value to the buffer.
        /// </summary>
        /// <param name="value">The GUID value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(Guid value, out int sizeNeeded)
        {
            sizeNeeded = 38;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
                bufferBytes[position++] = quoteByte;
            else
                bufferChars[position++] = '"';

            if (useBytes)
            {
#if !NETSTANDARD2_0
                _ = value.TryFormat(bufferBytes.Slice(position), out var written);
#else
                _ = Utf8Formatter.TryFormat(value, bufferBytes.Slice(position), out var written);
#endif
                position += written;
            }
            else
            {
#if NETSTANDARD2_0
                var str = value.ToString();
                fixed (char* pSource = str, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
                }
                position += str.Length;
#else
                _ = value.TryFormat(bufferChars.Slice(position), out var consumed);
                position += consumed;
#endif
            }

            if (useBytes)
                bufferBytes[position++] = quoteByte;
            else
                bufferChars[position++] = '"';

            return true;
        }

        /// <summary>
        /// Attempts to write a character span to the buffer.
        /// </summary>
        /// <param name="value">The character span to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWrite(ReadOnlySpan<char> value, out int sizeNeeded)
        {
            if (value.Length == 0)
            {
                sizeNeeded = 0;
                return true;
            }

            if (useBytes)
            {
                sizeNeeded = encoding.GetMaxByteCount(value.Length);
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (DebugShouldReturn())
                    return false;
#endif

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
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (DebugShouldReturn())
                    return false;
#endif

                fixed (char* pSource = value, pBuffer = &bufferChars[position])
                {
                    Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, value.Length * 2);
                }
                position += value.Length;
                return true;
            }
        }

#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteInt32Chars(int value)
        {
#if NETSTANDARD2_0
            var str = value.ToString(CultureInfo.InvariantCulture);
            fixed (char* pSource = str, pBuffer = &bufferChars[position])
            {
                Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
            }
            position += str.Length;
#else
            _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
            position += consumed;
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteInt64Chars(long value)
        {
#if NETSTANDARD2_0
            var str = value.ToString(CultureInfo.InvariantCulture);
            fixed (char* pSource = str, pBuffer = &bufferChars[position])
            {
                Buffer.MemoryCopy(pSource, pBuffer, (bufferChars.Length - position) * 2, str.Length * 2);
            }
            position += str.Length;
#else
            _ = value.TryFormat(bufferChars.Slice(position), out var consumed, default, CultureInfo.InvariantCulture);
            position += consumed;
#endif
        }
#endif

        /// <summary>
        /// Attempts to write a null value to the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteNull(out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = nByte;
                bufferBytes[position++] = uByte;
                bufferBytes[position++] = lByte;
                bufferBytes[position++] = lByte;
                return true;
            }
            else
            {
                bufferChars[position++] = 'n';
                bufferChars[position++] = 'u';
                bufferChars[position++] = 'l';
                bufferChars[position++] = 'l';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write the literal "true" to the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteTrue(out int sizeNeeded)
        {
            sizeNeeded = 4;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bufferBytes.Slice(position), 0x65757274);
                position += 4;
                return true;
            }
            else
            {
                bufferChars[position++] = 't';
                bufferChars[position++] = 'r';
                bufferChars[position++] = 'u';
                bufferChars[position++] = 'e';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write the literal "false" to the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteFalse(out int sizeNeeded)
        {
            sizeNeeded = 5;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bufferBytes.Slice(position), 0x736C6166);
                bufferBytes[position + 4] = eByte;
                position += 5;
                return true;
            }
            else
            {
                bufferChars[position++] = 'f';
                bufferChars[position++] = 'a';
                bufferChars[position++] = 'l';
                bufferChars[position++] = 's';
                bufferChars[position++] = 'e';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write a property name segment to the buffer, optionally prefixed with a comma.
        /// </summary>
        /// <param name="value">The property name segment to write.</param>
        /// <param name="startWithComma">Whether to prefix the segment with a comma.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the segment was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteNameSegment(ReadOnlySpan<char> value, bool startWithComma, out int sizeNeeded)
        {
            if (useBytes)
                throw new InvalidOperationException($"{nameof(TryWriteNameSegment)} {nameof(useBytes)} is the wrong setting for this call");

            sizeNeeded = value.Length + (startWithComma ? 1 : 0);
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (startWithComma)
                bufferChars[position++] = ',';

            //a span copy instead of fixed and Buffer.MemoryCopy, the JIT won't inline a method with pinned locals
            value.CopyTo(bufferChars.Slice(position));
            position += value.Length;

            return true;
        }

        /// <summary>
        /// Attempts to write a property name segment to the buffer, optionally prefixed with a comma.
        /// </summary>
        /// <param name="value">The property name segment to write.</param>
        /// <param name="startWithComma">Whether to prefix the segment with a comma.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the segment was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteNameSegment(ReadOnlySpan<byte> value, bool startWithComma, out int sizeNeeded)
        {
            if (!useBytes)
                throw new InvalidOperationException($"{nameof(TryWriteNameSegment)} {nameof(useBytes)} is the wrong setting for this call");

            sizeNeeded = value.Length + (startWithComma ? 1 : 0);
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (startWithComma)
                bufferBytes[position++] = commaByte;

            //a span copy instead of fixed and Buffer.MemoryCopy, the JIT won't inline a method with pinned locals
            value.CopyTo(bufferBytes.Slice(position));
            position += value.Length;

            return true;
        }

        /// <summary>
        /// Attempts to write a comma to the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the comma was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteComma(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = commaByte;
                return true;
            }
            else
            {
                bufferChars[position++] = ',';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write an opening bracket to the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the bracket was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteOpenBracket(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = openBracketByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '[';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write a closing bracket to the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the bracket was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteCloseBracket(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = closeBracketByte;
                return true;
            }
            else
            {
                bufferChars[position++] = ']';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write an empty bracket pair to the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the brackets were successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteEmptyBracket(out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = openBracketByte;
                bufferBytes[position++] = closeBracketByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '[';
                bufferChars[position++] = ']';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write an opening brace to the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the brace was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteOpenBrace(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = openBraceByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '{';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write a closing brace to the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the brace was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteCloseBrace(out int sizeNeeded)
        {
            sizeNeeded = 1;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = closeBraceByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '}';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write an empty brace pair to the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the braces were successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteEmptyBrace(out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = openBraceByte;
                bufferBytes[position++] = closeBraceByte;
                return true;
            }
            else
            {
                bufferChars[position++] = '{';
                bufferChars[position++] = '}';
                return true;
            }
        }

        /// <summary>
        /// Attempts to write an empty quoted string to the buffer.
        /// </summary>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the empty string was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteEmptyString(out int sizeNeeded)
        {
            sizeNeeded = 2;
            if (length - position < sizeNeeded)
            {
                if (!Grow(sizeNeeded))
                    return false;
            }
#if DEBUG
            if (DebugShouldReturn())
                return false;
#endif

            if (useBytes)
            {
                bufferBytes[position++] = quoteByte;
                bufferBytes[position++] = quoteByte;
            }
            else
            {
                bufferChars[position++] = '"';
                bufferChars[position++] = '"';
            }
            return true;
        }

        /// <summary>
        /// Attempts to write a string value to the buffer with escape character handling.
        /// </summary>
        /// <param name="value">The string value to write. If null, returns true without writing.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteEscapedQuoted(string? value, out int sizeNeeded)
        {
            //no fixed in this fast path, the JIT won't inline a method with pinned locals
#if !NETSTANDARD2_0
            if (value is not null && value.Length > 0 && value.AsSpan().IndexOfAny(escapeChars) < 0)
            {
                if (useBytes)
                {
                    sizeNeeded = encoding.GetMaxByteCount(value.Length + 2);
                    if (length - position < sizeNeeded)
                    {
                        if (!Grow(sizeNeeded))
                            return false;
                    }
                    if (Ascii.FromUtf16(value, bufferBytes.Slice(position + 1), out var written) == OperationStatus.Done)
                    {
#if DEBUG
                        if (DebugShouldReturn())
                            return false;
#endif
                        bufferBytes[position] = quoteByte;
                        position += written + 1;
                        bufferBytes[position++] = quoteByte;
                        return true;
                    }
                    if (value.AsSpan().IndexOfAnyInRange(lowerSurrogate, upperSurrogate) < 0)
                    {
#if DEBUG
                        if (DebugShouldReturn())
                            return false;
#endif
                        bufferBytes[position++] = quoteByte;
                        position += encoding.GetBytes(value, bufferBytes.Slice(position));
                        bufferBytes[position++] = quoteByte;
                        return true;
                    }
                }
                else if (value.AsSpan().IndexOfAnyInRange(lowerSurrogate, upperSurrogate) < 0)
                {
                    sizeNeeded = value.Length + 2;
                    if (length - position < sizeNeeded)
                    {
                        if (!Grow(sizeNeeded))
                            return false;
                    }
#if DEBUG
                    if (DebugShouldReturn())
                        return false;
#endif
                    bufferChars[position++] = '"';
                    value.AsSpan().CopyTo(bufferChars.Slice(position));
                    position += value.Length;
                    bufferChars[position++] = '"';
                    return true;
                }
            }
#endif
            return TryWriteEscapedQuotedSlow(value, out sizeNeeded);
        }

        /// <summary>
        /// Attempts to write a property name from a string to the buffer with escape character handling, optionally prefixed with a comma.
        /// </summary>
        /// <param name="value">The property name to write.</param>
        /// <param name="startWithComma">Whether to prefix the property name with a comma.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the property name was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWritePropertyName(string? value, bool startWithComma, out int sizeNeeded)
        {
#if !NETSTANDARD2_0
            if (value is not null && value.AsSpan().IndexOfAny(escapeChars) < 0)
            {
                if (useBytes)
                {
                    sizeNeeded = encoding.GetMaxByteCount(value.Length) + 4;
                    if (length - position < sizeNeeded)
                    {
                        if (!Grow(sizeNeeded))
                            return false;
                    }
                    var start = startWithComma ? 2 : 1;
                    if (Ascii.FromUtf16(value, bufferBytes.Slice(position + start), out var written) == OperationStatus.Done)
                    {
#if DEBUG
                        if (DebugShouldReturn())
                            return false;
#endif
                        if (startWithComma)
                            bufferBytes[position++] = commaByte;
                        bufferBytes[position] = quoteByte;
                        position += written + 1;
                        bufferBytes[position++] = quoteByte;
                        bufferBytes[position++] = colonByte;
                        return true;
                    }
                    if (value.AsSpan().IndexOfAnyInRange(lowerSurrogate, upperSurrogate) < 0)
                    {
#if DEBUG
                        if (DebugShouldReturn())
                            return false;
#endif
                        if (startWithComma)
                            bufferBytes[position++] = commaByte;
                        bufferBytes[position++] = quoteByte;
                        position += encoding.GetBytes(value, bufferBytes.Slice(position));
                        bufferBytes[position++] = quoteByte;
                        bufferBytes[position++] = colonByte;
                        return true;
                    }
                }
                else if (value.AsSpan().IndexOfAnyInRange(lowerSurrogate, upperSurrogate) < 0)
                {
                    sizeNeeded = value.Length + 4;
                    if (length - position < sizeNeeded)
                    {
                        if (!Grow(sizeNeeded))
                            return false;
                    }
#if DEBUG
                    if (DebugShouldReturn())
                        return false;
#endif
                    if (startWithComma)
                        bufferChars[position++] = ',';
                    bufferChars[position++] = '"';
                    value.AsSpan().CopyTo(bufferChars.Slice(position));
                    position += value.Length;
                    bufferChars[position++] = '"';
                    bufferChars[position++] = ':';
                    return true;
                }
            }
#endif
            if (useBytes)
                return TryWriteNameSegment(StringHelper.EscapeAndEncodeString(value, true), startWithComma, out sizeNeeded);
            else
                return TryWriteNameSegment(StringHelper.EscapeString(value, true), startWithComma, out sizeNeeded);
        }

        private unsafe bool TryWriteEscapedQuotedSlow(string? value, out int sizeNeeded)
        {
            const int maxEscapeCharacterSize = 6;

            if (value is null)
            {
                sizeNeeded = 0;
                return true;
            }
            if (value.Length == 0)
            {
                sizeNeeded = 2;
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (DebugShouldReturn())
                    return false;
#endif
                if (useBytes)
                {
                    bufferBytes[position++] = quoteByte;
                    bufferBytes[position++] = quoteByte;
                }
                else
                {
                    bufferChars[position++] = '"';
                    bufferChars[position++] = '"';
                }
                return true;
            }

            if (useBytes)
            {
                fixed (char* pValue = value)
                {
#if NETSTANDARD2_0
                    bool needsEscaped = false;
                    var i = 0;
                    for (; i < value.Length; i++)
                    {
                        var c = pValue[i];
                        if (c < ' ' || c == '"' || c == '\\' || (c >= lowerSurrogate && c <= upperSurrogate))
                        {
                            needsEscaped = true;
                            break;
                        }
                    }
#else
                    var i = value.AsSpan().IndexOfAny(escapeChars);
                    var surrogateIndex = (i < 0 ? value.AsSpan() : value.AsSpan(0, i)).IndexOfAnyInRange(lowerSurrogate, upperSurrogate);
                    if (surrogateIndex >= 0)
                        i = surrogateIndex;
                    var needsEscaped = i >= 0;
#endif

                    if (!needsEscaped)
                    {
                        sizeNeeded = encoding.GetMaxByteCount(value.Length + 2);
                        if (length - position < sizeNeeded)
                        {
                            if (!Grow(sizeNeeded))
                                return false;
                        }
#if DEBUG
                        if (DebugShouldReturn())
                            return false;
#endif

                        fixed (byte* pBuffer = bufferBytes)
                        {
                            pBuffer[position++] = quoteByte;
                            position += encoding.GetBytes(pValue, value.Length, &pBuffer[position], length - position);
                            pBuffer[position++] = quoteByte;
                        }
                        return true;
                    }

                    sizeNeeded = encoding.GetMaxByteCount((i + ((value.Length - i) * maxEscapeCharacterSize)) + 2);
                    if (length - position < sizeNeeded)
                    {
                        if (!Grow(sizeNeeded))
                            return false;
                    }
#if DEBUG
                    if (DebugShouldReturn())
                        return false;
#endif

                    fixed (byte* pBuffer = bufferBytes)
                    {
                        pBuffer[position++] = quoteByte;

                        var start = 0;

                        for (; i < value.Length; i++)
                        {
                            var c = pValue[i];
                            byte escapedByte;
                            switch (c)
                            {
                                case '"':
                                    escapedByte = quoteByte;
                                    break;
                                case '\\':
                                    escapedByte = escapeByte;
                                    break;
                                case >= ' ':
                                    if (c >= lowerSurrogate && c <= upperSurrogate)
                                    {
                                        position += encoding.GetBytes(&pValue[start], i - start, &pBuffer[position], length - position);

                                        var surrogateCode = StringHelper.SurrogateIntToEncodedHexBytes[c];
                                        fixed (byte* pCode = surrogateCode)
                                        {
                                            Buffer.MemoryCopy(pCode, &pBuffer[position], (length - position) * 2, surrogateCode.Length);
                                            position += surrogateCode.Length;
                                        }

                                        start = i + 1;
                                        continue;
                                    }
                                    continue;
                                case '\b':
                                    escapedByte = bByte;
                                    break;
                                case '\f':
                                    escapedByte = fByte;
                                    break;
                                case '\n':
                                    escapedByte = nByte;
                                    break;
                                case '\r':
                                    escapedByte = rByte;
                                    break;
                                case '\t':
                                    escapedByte = tByte;
                                    break;
                                default:

                                    position += encoding.GetBytes(&pValue[start], i - start, &pBuffer[position], length - position);

                                    var code = StringHelper.LowUnicodeIntToEncodedHexBytes[c];
                                    fixed (byte* pCode = code)
                                    {
                                        Buffer.MemoryCopy(pCode, &pBuffer[position], (length - position) * 2, code.Length);
                                        position += code.Length;
                                    }

                                    start = i + 1;
                                    continue;
                            }

                            position += encoding.GetBytes(&pValue[start], i - start, &pBuffer[position], length - position);

                            pBuffer[position++] = escapeByte;
                            pBuffer[position++] = escapedByte;
                            start = i + 1;
                        }

                        if (value.Length > start)
                        {
                            position += encoding.GetBytes(&pValue[start], value.Length - start, &pBuffer[position], length - position);
                        }

                        pBuffer[position++] = quoteByte;
                    }
                }

                return true;
            }
            else
            {
                fixed (char* pValue = value)
                {
#if NETSTANDARD2_0
                    bool needsEscaped = false;
                    var i = 0;
                    for (; i < value.Length; i++)
                    {
                        var c = pValue[i];
                        if (c < ' ' || c == '"' || c == '\\' || (c >= lowerSurrogate && c <= upperSurrogate))
                        {
                            needsEscaped = true;
                            break;
                        }
                    }
#else
                    var i = value.AsSpan().IndexOfAny(escapeChars);
                    var surrogateIndex = (i < 0 ? value.AsSpan() : value.AsSpan(0, i)).IndexOfAnyInRange(lowerSurrogate, upperSurrogate);
                    if (surrogateIndex >= 0)
                        i = surrogateIndex;
                    var needsEscaped = i >= 0;
#endif

                    if (!needsEscaped)
                    {
                        sizeNeeded = value.Length + 2;
                        if (length - position < sizeNeeded)
                        {
                            if (!Grow(sizeNeeded))
                                return false;
                        }
#if DEBUG
                        if (DebugShouldReturn())
                            return false;
#endif

                        bufferChars[position++] = '\"';
                        value.AsSpan().CopyTo(bufferChars.Slice(position));
                        position += value.Length;
                        bufferChars[position++] = '\"';
                        return true;
                    }

                    sizeNeeded = i + ((value.Length - i) * maxEscapeCharacterSize) + 2;
                    if (length - position < sizeNeeded)
                    {
                        if (!Grow(sizeNeeded))
                            return false;
                    }
#if DEBUG
                    if (DebugShouldReturn())
                        return false;
#endif

                    fixed (char* pBuffer = bufferChars)
                    {
                        pBuffer[position++] = '\"';

                        var start = 0;

                        for (; i < value.Length; i++)
                        {
                            var c = pValue[i];
                            char escapedChar;
                            switch (c)
                            {
                                case '"':
                                    escapedChar = '"';
                                    break;
                                case '\\':
                                    escapedChar = '\\';
                                    break;
                                case >= ' ':
                                    if (c >= lowerSurrogate && c <= upperSurrogate)
                                    {
                                        Buffer.MemoryCopy(&pValue[start], &pBuffer[position], (length - position) * 2, (i - start) * 2);
                                        position += i - start;

                                        var surrogateCode = StringHelper.SurrogateIntToEncodedHexChars[c];
                                        fixed (char* pCode = surrogateCode)
                                        {
                                            Buffer.MemoryCopy(pCode, &pBuffer[position], (length - position) * 2, surrogateCode.Length * 2);
                                        }
                                        position += surrogateCode.Length;

                                        start = i + 1;
                                        continue;
                                    }
                                    continue;
                                case '\b':
                                    escapedChar = 'b';
                                    break;
                                case '\f':
                                    escapedChar = 'f';
                                    break;
                                case '\n':
                                    escapedChar = 'n';
                                    break;
                                case '\r':
                                    escapedChar = 'r';
                                    break;
                                case '\t':
                                    escapedChar = 't';
                                    break;
                                default:

                                    Buffer.MemoryCopy(&pValue[start], &pBuffer[position], (length - position) * 2, (i - start) * 2);
                                    position += i - start;

                                    var code = StringHelper.LowUnicodeIntToEncodedHexChars[c];
                                    fixed (char* pCode = code)
                                    {
                                        Buffer.MemoryCopy(pCode, &pBuffer[position], (length - position) * 2, code.Length * 2);
                                    }
                                    position += code.Length;

                                    start = i + 1;
                                    continue;
                            }

                            Buffer.MemoryCopy(&pValue[start], &pBuffer[position], (length - position) * 2, (i - start) * 2);
                            position += i - start;

                            pBuffer[position++] = '\\';
                            pBuffer[position++] = escapedChar;
                            start = i + 1;
                        }

                        if (value.Length > start)
                        {
                            Buffer.MemoryCopy(&pValue[start], &pBuffer[position], (length - position) * 2, (value.Length - start) * 2);
                            position += value.Length - start;
                        }

                        pBuffer[position++] = '\"';
                    }

                    return true;
                }
            }
        }

        /// <summary>
        /// Attempts to write a char value to the buffer with escape character handling.
        /// </summary>
        /// <param name="value">The char value to write.</param>
        /// <param name="sizeNeeded">The number of bytes needed if the operation cannot complete.</param>
        /// <returns><c>true</c> if the value was successfully written; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryWriteEscapedQuoted(char value, out int sizeNeeded)
        {
            if (useBytes)
            {
                if (value < 128)
                {
                    byte escapedByte;
                    switch (value)
                    {
                        case '"':
                            escapedByte = quoteByte;
                            break;
                        case '\\':
                            escapedByte = escapeByte;
                            break;
                        case >= ' ': //32

                            sizeNeeded = 3;
                            if (length - position < sizeNeeded)
                            {
                                if (!Grow(sizeNeeded))
                                    return false;
                            }
#if DEBUG
                            if (DebugShouldReturn())
                                return false;
#endif

                            bufferBytes[position++] = quoteByte;
                            bufferBytes[position++] = (byte)value;
                            bufferBytes[position++] = quoteByte;
                            return true;

                        case '\b':
                            escapedByte = bByte;
                            break;
                        case '\f':
                            escapedByte = fByte;
                            break;
                        case '\n':
                            escapedByte = nByte;
                            break;
                        case '\r':
                            escapedByte = rByte;
                            break;
                        case '\t':
                            escapedByte = tByte;
                            break;
                        default:
                            var code = StringHelper.LowUnicodeIntToEncodedHexBytes[value];

                            sizeNeeded = code.Length + 2;
                            if (length - position < sizeNeeded)
                            {
                                if (!Grow(sizeNeeded))
                                    return false;
                            }
#if DEBUG
                            if (DebugShouldReturn())
                                return false;
#endif
                            fixed (byte* pCode = code, pBuffer = bufferBytes)
                            {
                                pBuffer[position++] = quoteByte;
                                Buffer.MemoryCopy(pCode, &pBuffer[position], length - position, code.Length);
                                position += code.Length;
                                pBuffer[position++] = quoteByte;
                            }
                            return true;
                    }

                    sizeNeeded = 4;
                    if (length - position < sizeNeeded)
                    {
                        if (!Grow(sizeNeeded))
                            return false;
                    }
#if DEBUG
                    if (DebugShouldReturn())
                        return false;
#endif

                    bufferBytes[position++] = quoteByte;
                    bufferBytes[position++] = escapeByte;
                    bufferBytes[position++] = escapedByte;
                    bufferBytes[position++] = quoteByte;
                    return true;
                }
                else
                {
                    if (value >= lowerSurrogate && value <= upperSurrogate)
                    {
                        var surrogateCode = StringHelper.SurrogateIntToEncodedHexBytes[value];

                        sizeNeeded = surrogateCode.Length + 2;
                        if (length - position < sizeNeeded)
                        {
                            if (!Grow(sizeNeeded))
                                return false;
                        }
#if DEBUG
                        if (DebugShouldReturn())
                            return false;
#endif
                        fixed (byte* pCode = surrogateCode, pBuffer = bufferBytes)
                        {
                            pBuffer[position++] = quoteByte;
                            Buffer.MemoryCopy(pCode, &pBuffer[position], length - position, surrogateCode.Length);
                            position += surrogateCode.Length;
                            pBuffer[position++] = quoteByte;
                        }
                        return true;
                    }

                    sizeNeeded = 6;
                    if (length - position < sizeNeeded)
                    {
                        if (!Grow(sizeNeeded))
                            return false;
                    }
#if DEBUG
                    if (DebugShouldReturn())
                        return false;
#endif

                    bufferBytes[position++] = quoteByte;
                    var valueArray = stackalloc char[] { value };
                    fixed (byte* pBuffer = &bufferBytes[position])
                    {
                        position += encoding.GetBytes(valueArray, 1, pBuffer, length - position);
                    }
                    bufferBytes[position++] = quoteByte;

                    return true;
                }
            }
            else
            {
                char escapedChar;
                switch (value)
                {
                    case '"':
                        escapedChar = '"';
                        break;
                    case '\\':
                        escapedChar = '\\';
                        break;
                    case >= ' ':
                        if (value >= lowerSurrogate && value <= upperSurrogate)
                        {
                            var surrogateCode = StringHelper.SurrogateIntToEncodedHexChars[value];

                            sizeNeeded = surrogateCode.Length + 2;
                            if (length - position < sizeNeeded)
                            {
                                if (!Grow(sizeNeeded))
                                    return false;
                            }
#if DEBUG
                            if (DebugShouldReturn())
                                return false;
#endif

                            fixed (char* pCode = surrogateCode, pBuffer = bufferChars)
                            {
                                pBuffer[position++] = '"';
                                Buffer.MemoryCopy(pCode, &pBuffer[position], (length - position) * 2, surrogateCode.Length * 2);
                                position += surrogateCode.Length;
                                pBuffer[position++] = '"';
                            }
                            return true;
                        }

                        sizeNeeded = 3;
                        if (length - position < sizeNeeded)
                        {
                            if (!Grow(sizeNeeded))
                                return false;
                        }
#if DEBUG
                        if (DebugShouldReturn())
                            return false;
#endif

                        bufferChars[position++] = '"';
                        bufferChars[position++] = value;
                        bufferChars[position++] = '"';

                        return true;

                    case '\b':
                        escapedChar = 'b';
                        break;
                    case '\f':
                        escapedChar = 'f';
                        break;
                    case '\n':
                        escapedChar = 'n';
                        break;
                    case '\r':
                        escapedChar = 'r';
                        break;
                    case '\t':
                        escapedChar = 't';
                        break;
                    default:
                        var code = StringHelper.LowUnicodeIntToEncodedHexChars[value];

                        sizeNeeded = code.Length + 2;
                        if (length - position < sizeNeeded)
                        {
                            if (!Grow(sizeNeeded))
                                return false;
                        }
#if DEBUG
                        if (DebugShouldReturn())
                            return false;
#endif

                        fixed (char* pCode = code, pBuffer = bufferChars)
                        {
                            pBuffer[position++] = '"';
                            Buffer.MemoryCopy(pCode, &pBuffer[position], (length - position) * 2, code.Length * 2);
                            position += code.Length;
                            pBuffer[position++] = '"';
                        }
                        return true;
                }

                sizeNeeded = 4;
                if (length - position < sizeNeeded)
                {
                    if (!Grow(sizeNeeded))
                        return false;
                }
#if DEBUG
                if (DebugShouldReturn())
                    return false;
#endif

                bufferChars[position++] = '"';
                bufferChars[position++] = '\\';
                bufferChars[position++] = escapedChar;
                bufferChars[position++] = '"';

                return true;
            }
        }
    }
}