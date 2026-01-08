// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Buffers;

namespace Zerra.Serialization.Bytes.IO
{
    /// <summary>
    /// A ref struct for efficiently writing binary data to either a pooled byte buffer or a provided buffer.
    /// Provides buffering and automatic growth capabilities for building binary content.
    /// </summary>
    public ref partial struct ByteWriter
    {
        internal static readonly Encoding encoding = Encoding.UTF8;

        private const byte nullByte = 0;
        private const byte notNullByte = 1;

        private byte[]? bufferOwner;
        private Span<byte> buffer;

        private int position;
        private int length;

        /// <summary>
        /// Gets the current position in the buffer.
        /// </summary>
        public readonly int Position => position;

        /// <summary>
        /// Gets the total length of the buffer.
        /// </summary>
        public readonly int Length => length;

        /// <summary>
        /// Not supported default constructor.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public ByteWriter()
        {
            throw new NotSupportedException($"{nameof(ByteWriter)} cannot use default constructor");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteWriter"/> struct with a provided buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer to write to.</param>
        public ByteWriter(Span<byte> buffer)
        {
            this.bufferOwner = null;
            this.buffer = buffer;
            this.position = 0;
            this.length = buffer.Length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteWriter"/> struct with a pooled buffer.
        /// </summary>
        /// <param name="initialSize">The initial size of the pooled buffer.</param>
        public ByteWriter(int initialSize)
        {
            this.bufferOwner = ArrayPoolHelper<byte>.Rent(initialSize);
            this.buffer = bufferOwner;
            this.position = 0;
            this.length = buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Grow(int sizeNeeded)
        {
            if (bufferOwner is null)
                return false;

            ArrayPoolHelper<byte>.Grow(ref bufferOwner, Math.Max(bufferOwner.Length * 2, bufferOwner.Length + sizeNeeded));
            buffer = bufferOwner;
            length = buffer.Length;

            return true;
        }

        /// <summary>
        /// Converts the written content to a byte array.
        /// </summary>
        /// <returns>A byte array containing the written data.</returns>
        public readonly byte[] ToArray()
        {
            return buffer.Slice(0, position).ToArray();
        }

        /// <summary>
        /// Releases the pooled buffer back to the array pool if one was used.
        /// </summary>
        public void Dispose()
        {
            if (bufferOwner is not null)
            {
                ArrayPoolHelper<byte>.Return(bufferOwner);
                bufferOwner = null;
                buffer = null;
            }
        }
    }
}