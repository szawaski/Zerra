// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Linq
{
    public static class LinqBatchExtensions
    {
#if !NET6_0_OR_GREATER
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int size)
        {
            var buffer = new T[size];
            var count = 0;

            foreach (var item in source)
            {
                buffer[count++] = item;

                if (count != size)
                    continue;

                yield return buffer;

                count = 0;
            }

            if (count > 0)
            {
                Array.Resize(ref buffer, count);
                yield return buffer;
            }
        }
#endif

        public static IEnumerable<ICollection<T>> Chunk<T>(this ICollection<T> source, int size)
        {
            if (source.Count == 0)
                yield break;
            if (source.Count <= size)
            {
                yield return source;
                yield break;
            }

            var buffer = new T[size];
            var count = 0;

            foreach (var item in source)
            {
                buffer[count++] = item;

                if (count != size)
                    continue;

                yield return buffer;

                count = 0;
            }

            if (count > 0)
            {
                Array.Resize(ref buffer, count);
                yield return buffer;
            }
        }

        public static IEnumerable<T[]> Chunk<T>(this T[] source, int size)
        {
            if (source.Length == 0)
                yield break;
            if (source.Length <= size)
            {
                yield return source;
                yield break;
            }

            var buffer = new T[size];

            for (var i = 0; i < source.Length; i += size)
            {
                var blockSize = Math.Min(source.Length - i, size);
                Array.Copy(source, i, buffer, 0, blockSize);
                if (blockSize < size)
                    Array.Resize(ref buffer, blockSize);
                yield return buffer;
            }
        }
    }
}
