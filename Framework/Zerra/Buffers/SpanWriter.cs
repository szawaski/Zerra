// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.Buffers
{
    /// <summary>
    /// Does sequential writing to a <see cref="Span{T}"/>.
    /// The position is tracked from the end of the last write for the next.
    /// </summary>
    public ref struct SpanWriter<T>
    {
        private readonly Span<T> span;

        private int position;
        /// <summary>
        /// The current position of the span.
        /// </summary>
        public readonly int Position => position;

        /// <summary>
        /// Creates an instance with the span
        /// </summary>
        /// <param name="span">The span to which shall be written.</param>
        public SpanWriter(Span<T> span)
        {
            this.span = span;
            position = 0;
        }

        /// <summary>
        /// Writes a set of values to the span.
        /// </summary>
        /// <param name="values">The values to be written.</param>
        /// <exception cref="InvalidOperationException">Throws if the values excede the remaining length of the span.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<T> values)
        {
            values.CopyTo(span.Slice(position));
            position += values.Length;
        }

        /// <summary>
        /// The part of the span not yet written to.
        /// Write to it directly, such as encoding a string, then call <see cref="Advance"/> with the count written.
        /// </summary>
        public readonly Span<T> Remaining => span.Slice(position);

        /// <summary>
        /// Moves the position forward after writing directly to <see cref="Remaining"/>.
        /// </summary>
        /// <param name="count">The number of values written.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws if the count is negative or excedes the remaining length of the span.</exception>
        public void Advance(int count)
        {
            if (count < 0 || count > span.Length - position)
                throw new ArgumentOutOfRangeException(nameof(count));
            position += count;
        }
    }
}