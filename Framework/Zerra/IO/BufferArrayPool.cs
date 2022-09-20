// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Zerra.IO
{
    public static class BufferArrayPool<T>
        where T : unmanaged
    {
        private static readonly bool isBytes;
        private static readonly int elementSize;
        private static readonly ArrayPool<T> pool;

        unsafe static BufferArrayPool()
        {
            isBytes = typeof(T) == typeof(byte);
            elementSize = sizeof(T);
            pool = ArrayPool<T>.Shared;
        }

        private const int nonByteArrayMaxSize = 2146435071; //0X7FEFFFFF
        private const int byteArrayMaxSize = 2147483591; //0X7FFFFFC7
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
        public static int ElementSize
        {
            get
            {
                return elementSize;
            }
        }

        public static void Grow(ref T[] buffer, int minSize)
        {
            if (buffer == null)
            {
                buffer = pool.Rent(minSize);
                return;
            }

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

#if DEBUG
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

#if DEBUG
        private static readonly HashSet<T[]> rented = new();
#endif
        public static T[] Rent(int minimunLength)
        {
#if DEBUG
            lock (rented)
            {
                var buffer = pool.Rent(minimunLength);
                rented.Add(buffer);
                System.Diagnostics.Debug.WriteLine($"Memory<{typeof(T).Name}> Rented - {rented.Count}: Size {buffer.Length}");
                return buffer;
            }
#else
            return pool.Rent(minimunLength);
#endif
        }
        public static void Return(T[] buffer)
        {
#if DEBUG
            lock (rented)
            {
                if (!rented.Contains(buffer))
                    throw new Exception($"Memory<{typeof(T).Name}> Returned That Was Not Rented: Size {buffer.Length}");
                System.Diagnostics.Debug.WriteLine($"Memory<{typeof(T).Name}> Returned - {rented.Count}: Size {buffer.Length}");
                rented.Remove(buffer);
                pool.Return(buffer);
            }
#else
            pool.Return(buffer);
#endif
        }
    }
}
