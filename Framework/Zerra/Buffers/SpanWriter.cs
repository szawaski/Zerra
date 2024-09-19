// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.Buffers
{
    /// <summary>
    /// Does sequential writing to a <see cref="Span{byte}"/>.
    /// The position is tracked from the end of the last write for the next.
    /// </summary>
    public ref struct SpanWriter<T>
    {
        private readonly Span<T> span;

        private int position;
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
    }
}