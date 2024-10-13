// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Linq
{
    public static class LinqChunkExtensions
    {
#if !NET6_0_OR_GREATER
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int size)
        {
            T[]? buffer = null;
            var count = 0;

            foreach (var item in source)
            {
                buffer ??= new T[size];

                buffer[count++] = item;

                if (count != size)
                    continue;

                yield return buffer;

                buffer = null;
                count = 0;
            }

            if (buffer is not null && count > 0)
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

            T[]? buffer = null;
            var count = 0;

            foreach (var item in source)
            {
                buffer ??= new T[size];

                buffer[count++] = item;

                if (count != size)
                    continue;

                yield return buffer;

                buffer = null;
                count = 0;
            }

            if (buffer is not null && count > 0)
            {
                Array.Resize(ref buffer, count);
                yield return buffer;
            }
        }

        public static IEnumerable<IReadOnlyCollection<T>> Chunk<T>(this IReadOnlyCollection<T> source, int size)
        {
            if (source.Count == 0)
                yield break;
            if (source.Count <= size)
            {
                yield return source;
                yield break;
            }

            T[]? buffer = null;
            var count = 0;

            foreach (var item in source)
            {
                buffer ??= new T[size];

                buffer[count++] = item;

                if (count != size)
                    continue;

                yield return buffer;

                buffer = null;
                count = 0;
            }

            if (buffer is not null && count > 0)
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

            for (var i = 0; i < source.Length; i += size)
            {
                var blockSize = Math.Min(source.Length - i, size);
                var buffer = new T[blockSize];
                Array.Copy(source, i, buffer, 0, blockSize);
                yield return buffer;
            }
        }
    }
}
