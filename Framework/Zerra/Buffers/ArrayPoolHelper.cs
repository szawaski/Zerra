// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers;

#if DEBUG && false
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
#endif

namespace Zerra.Buffers
{
    /// <summary>
    /// This wraps <see cref="ArrayPool{T}"/> to prevent memory leaks in framework development
    /// and provides a <see cref="Grow(ref T[], int)"/> function to expand array sizes sourced from the pool.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class ArrayPoolHelper<T>
        where T : unmanaged
    {
        private static readonly bool isBytes;
        private static readonly int elementSize;
        private static readonly ArrayPool<T> pool;

        unsafe static ArrayPoolHelper()
        {
            isBytes = typeof(T) == typeof(byte);
            elementSize = sizeof(T);
            pool = ArrayPool<T>.Shared;
        }

        private const int nonByteArrayMaxSize = 2146435071; //0X7FEFFFFF
        private const int byteArrayMaxSize = 2147483591; //0X7FFFFFC7
        /// <summary>
        /// The maximum allowed size of the array.
        /// </summary>
        public static int MaxArraySize
        {
            get
            {
                if (isBytes)
                    return byteArrayMaxSize;
                else
                    return nonByteArrayMaxSize;
            }
        }
        /// <summary>
        /// The byte size of the array element.
        /// </summary>
        public static int ElementSize
        {
            get
            {
                return elementSize;
            }
        }

        /// <summary>
        /// Increase the size of an array that was sourced from <see cref="ArrayPool{T}"/>.
        /// </summary>
        /// <param name="buffer">The array to resize.</param>
        /// <param name="minSize">The minimum size required for the array.</param>
        /// <exception cref="ArgumentNullException">Throws if the array is null.</exception>
        /// <exception cref="OverflowException">Throws when the array tries to exceede the maximum length.</exception>
        public static void Grow(ref T[] buffer, int minSize)
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            if (minSize < buffer.Length)
                return;

            var maxSize = MaxArraySize;
            if (minSize > maxSize)
                throw new OverflowException($"Buffer size cannot exceede {maxSize}");

            var newBufferLength = unchecked(buffer.Length * 2);
            if (newBufferLength < 0)
                newBufferLength = maxSize;
            else if (newBufferLength < minSize)
                newBufferLength = minSize;

#if DEBUG && false
            var newBuffer = Rent(newBufferLength);
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length * elementSize);
            Array.Clear(buffer, 0, buffer.Length);
            Return(buffer);
#else
            var newBuffer = pool.Rent(newBufferLength);
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length * elementSize);
            Array.Clear(buffer, 0, buffer.Length);
            pool.Return(buffer);
#endif

            buffer = newBuffer;
        }

#if DEBUG && false
        private static readonly Dictionary<T[], string> rented = new();
#endif
        /// <summary>
        /// Rents an array from <see cref="ArrayPool{T}"/>. The array should be returned when no longer used.
        /// </summary>
        /// <param name="minimunLength">The minimum length of the array.</param>
        /// <returns>The rented array.</returns>
        public static T[] Rent(int minimunLength)
        {
#if DEBUG && false
            lock (rented)
            {
                var line = Regex.Split(Environment.StackTrace, "\r\n|\r|\n").Skip(2).Select(x => x.Trim()).First();

                var buffer = pool.Rent(minimunLength);
                rented.Add(buffer, line);
                System.Diagnostics.Debug.WriteLine($"Memory<{typeof(T).Name}> Rented - {rented.Count}: Size {buffer.Length} {line}");
                return buffer;
            }
#else
            return pool.Rent(minimunLength);
#endif
        }
        /// <summary>
        /// Returns an array to the <see cref="ArrayPool{T}"/>. The array must not be used afterwards.
        /// </summary>
        /// <param name="buffer">The rented array to return.</param>
        /// <param name="lengthToClear">The length of the buffer to clear. Less than zero will clear all.  Default is -1.</param>
        public static void Return(T[] buffer, int lengthToClear = -1)
        {
#if DEBUG && false
            lock (rented)
            {
                if (!rented.TryGetValue(buffer, out var line))
                    throw new Exception($"Memory<{typeof(T).Name}> Returned That Was Not Rented: Size {buffer.Length}");

                rented.Remove(buffer);
                pool.Return(buffer);
                System.Diagnostics.Debug.WriteLine($"Memory<{typeof(T).Name}> Returned - {rented.Count}: Size {buffer.Length} {line}");
            }
#else
            if (lengthToClear < 0)
                lengthToClear = buffer.Length;
            if (lengthToClear > 0)
                Array.Clear(buffer, 0, lengthToClear);
            pool.Return(buffer);
#endif
        }
    }
}
