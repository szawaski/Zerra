﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers;

#if DEBUG && false
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
#endif

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
        public static void Return(T[] buffer)
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
            pool.Return(buffer);
#endif
        }
    }
}
