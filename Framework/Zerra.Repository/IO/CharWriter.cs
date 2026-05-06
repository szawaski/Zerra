// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Buffers;

namespace Zerra.Repository.IO
{
    /// <summary>
    /// A high-performance, stack-allocated character buffer writer that uses pooled arrays for storage.
    /// Must be disposed to return the rented array to the pool.
    /// </summary>
    public ref partial struct CharWriter
    {
        private const int defaultBufferSize = 1024;

        private char[]? bufferOwner;
        private Span<char> buffer;

        private int position;
        private readonly int length;

        /// <summary>
        /// Initializes a new <see cref="CharWriter"/> with a default internal buffer size.
        /// </summary>
        public CharWriter()
        {
            this.bufferOwner = ArrayPoolHelper<char>.Rent(defaultBufferSize);
            this.buffer = bufferOwner;
            this.position = 0;
            this.length = buffer.Length;
        }

        /// <summary>
        /// Initializes a new <see cref="CharWriter"/> with a buffer of at least <paramref name="initialSize"/> characters.
        /// </summary>
        /// <param name="initialSize">The minimum initial capacity of the internal buffer.</param>
        public CharWriter(int initialSize)
        {
            this.bufferOwner = ArrayPoolHelper<char>.Rent(initialSize);
            this.buffer = bufferOwner;
            this.position = 0;
            this.length = buffer.Length;
        }

        /// <summary>Gets the number of characters that have been written to the buffer.</summary>
        public readonly int Length => position;

        /// <summary>Gets the underlying rented array. This reference is only valid until <see cref="Dispose"/> is called.</summary>
        public readonly char[]? BufferOwner => bufferOwner;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBufferSize(int additionalSize)
        {
            if (position + additionalSize <= buffer.Length)
                return;

            if (bufferOwner is null)
                throw new InvalidOperationException($"{nameof(CharWriter)} has reached it's buffer limit");

            var minSize = position + additionalSize;
            ArrayPoolHelper<char>.Grow(ref bufferOwner, minSize);
            buffer = bufferOwner;
        }

        /// <summary>
        /// Resets the write position to zero without releasing the underlying buffer.
        /// </summary>
        public void Clear()
        {
            position = 0;
        }

        /// <summary>
        /// Returns a <see cref="Span{T}"/> over the characters written so far.
        /// The span is only valid until the next write or <see cref="Dispose"/>.
        /// </summary>
        public readonly Span<char> ToSpan()
        {
            return buffer.Slice(0, position);
        }
        /// <summary>
        /// Copies the written characters into a new <see cref="char"/> array and returns it.
        /// </summary>
        public readonly char[] ToArray()
        {
            return buffer.Slice(0, position).ToArray();
        }
        /// <summary>
        /// Copies the written characters into a new <see cref="string"/> and returns it.
        /// </summary>
        public override readonly string ToString()
        {
            return buffer.Slice(0, position).ToString();
        }

        /// <summary>
        /// Clears the buffer and returns the rented array to the pool.
        /// </summary>
        public void Dispose()
        {
            if (bufferOwner is not null)
            {
                buffer.Clear();
                ArrayPoolHelper<char>.Return(bufferOwner);
                bufferOwner = null;
                buffer = null;
            }
        }
    }
}