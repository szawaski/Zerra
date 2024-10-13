// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;
using Zerra.Buffers;

namespace Zerra.Repository.IO
{
    public ref partial struct CharWriter
    {
        private const int defaultBufferSize = 1024;

        private char[]? bufferOwner;
        private Span<char> buffer;

        private int position;
        private readonly int length;

        public CharWriter()
        {
            this.bufferOwner = ArrayPoolHelper<char>.Rent(defaultBufferSize);
            this.buffer = bufferOwner;
            this.position = 0;
            this.length = buffer.Length;
        }

        public CharWriter(int initialSize)
        {
            this.bufferOwner = ArrayPoolHelper<char>.Rent(initialSize);
            this.buffer = bufferOwner;
            this.position = 0;
            this.length = buffer.Length;
        }

        public readonly int Length => position;

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

        public void Clear()
        {
            position = 0;
        }

        public readonly Span<char> ToSpan()
        {
            return buffer.Slice(0, position);
        }
        public readonly char[] ToArray()
        {
            return buffer.Slice(0, position).ToArray();
        }
        public override readonly string ToString()
        {
            return buffer.Slice(0, position).ToString();
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteInt64(long value)
        {
            long num1 = value, num2, num3, num4, num5, div;

            if (value < 0)
            {
                if (value == Int64.MinValue)
                {
                    //Min value is one less than max, can't invert signs
                    EnsureBufferSize(20);
                    buffer[position++] = '-';
                    buffer[position++] = '9';
                    buffer[position++] = '2';
                    buffer[position++] = '2';
                    buffer[position++] = '3';
                    buffer[position++] = '3';
                    buffer[position++] = '7';
                    buffer[position++] = '2';
                    buffer[position++] = '0';
                    buffer[position++] = '3';
                    buffer[position++] = '6';
                    buffer[position++] = '8';
                    buffer[position++] = '5';
                    buffer[position++] = '4';
                    buffer[position++] = '7';
                    buffer[position++] = '7';
                    buffer[position++] = '5';
                    buffer[position++] = '8';
                    buffer[position++] = '0';
                    buffer[position++] = '8';
                    return;
                }
                buffer[position++] = '-';
                num1 = unchecked(-value);
            }

            if (num1 < 10000)
            {
                if (num1 < 10)
                {
                    EnsureBufferSize(1);
                    goto L1;
                }
                if (num1 < 100)
                {
                    EnsureBufferSize(2);
                    goto L2;
                }
                if (num1 < 1000)
                {
                    EnsureBufferSize(3);
                    goto L3;
                }
                EnsureBufferSize(4);
                goto L4;
            }
            else
            {
                num2 = num1 / 10000;
                num1 -= num2 * 10000;
                if (num2 < 10000)
                {
                    if (num2 < 10)
                    {
                        EnsureBufferSize(5);
                        goto L5;
                    }
                    if (num2 < 100)
                    {
                        EnsureBufferSize(6);
                        goto L6;
                    }
                    if (num2 < 1000)
                    {
                        EnsureBufferSize(7);
                        goto L7;
                    }
                    EnsureBufferSize(8);
                    goto L8;
                }
                else
                {
                    num3 = num2 / 10000;
                    num2 -= num3 * 10000;
                    if (num3 < 10000)
                    {
                        if (num3 < 10)
                        {
                            EnsureBufferSize(9);
                            goto L9;
                        }
                        if (num3 < 100)
                        {
                            EnsureBufferSize(10);
                            goto L10;
                        }
                        if (num3 < 1000)
                        {
                            EnsureBufferSize(11);
                            goto L11;
                        }
                        EnsureBufferSize(12);
                        goto L12;
                    }
                    else
                    {
                        num4 = num3 / 10000;
                        num3 -= num4 * 10000;
                        if (num4 < 10000)
                        {
                            if (num4 < 10)
                            {
                                EnsureBufferSize(13);
                                goto L13;
                            }
                            if (num4 < 100)
                            {
                                EnsureBufferSize(14);
                                goto L14;
                            }
                            if (num4 < 1000)
                            {
                                EnsureBufferSize(15);
                                goto L15;
                            }
                            EnsureBufferSize(16);
                            goto L16;
                        }
                        else
                        {
                            num5 = num4 / 10000;
                            num4 -= num5 * 10000;
                            if (num5 < 10000)
                            {
                                if (num5 < 10)
                                {
                                    EnsureBufferSize(17);
                                    goto L17;
                                }
                                if (num5 < 100)
                                {
                                    EnsureBufferSize(18);
                                    goto L18;
                                }
                                if (num5 < 1000)
                                {
                                    EnsureBufferSize(19);
                                    goto L19;
                                }
                                EnsureBufferSize(20);
                                goto L20;
                            }
                        L20:
                            buffer[position++] = (char)('0' + (div = (num5 * 8389L) >> 23));
                            num5 -= div * 1000;
                        L19:
                            buffer[position++] = (char)('0' + (div = (num5 * 5243L) >> 19));
                            num5 -= div * 100;
                        L18:
                            buffer[position++] = (char)('0' + (div = (num5 * 6554L) >> 16));
                            num5 -= div * 10;
                        L17:
                            buffer[position++] = (char)('0' + (num5));
                        }
                    L16:
                        buffer[position++] = (char)('0' + (div = (num4 * 8389L) >> 23));
                        num4 -= div * 1000;
                    L15:
                        buffer[position++] = (char)('0' + (div = (num4 * 5243L) >> 19));
                        num4 -= div * 100;
                    L14:
                        buffer[position++] = (char)('0' + (div = (num4 * 6554L) >> 16));
                        num4 -= div * 10;
                    L13:
                        buffer[position++] = (char)('0' + (num4));
                    }
                L12:
                    buffer[position++] = (char)('0' + (div = (num3 * 8389L) >> 23));
                    num3 -= div * 1000;
                L11:
                    buffer[position++] = (char)('0' + (div = (num3 * 5243L) >> 19));
                    num3 -= div * 100;
                L10:
                    buffer[position++] = (char)('0' + (div = (num3 * 6554L) >> 16));
                    num3 -= div * 10;
                L9:
                    buffer[position++] = (char)('0' + (num3));
                }
            L8:
                buffer[position++] = (char)('0' + (div = (num2 * 8389L) >> 23));
                num2 -= div * 1000;
            L7:
                buffer[position++] = (char)('0' + (div = (num2 * 5243L) >> 19));
                num2 -= div * 100;
            L6:
                buffer[position++] = (char)('0' + (div = (num2 * 6554L) >> 16));
                num2 -= div * 10;
            L5:
                buffer[position++] = (char)('0' + (num2));
            }
        L4:
            buffer[position++] = (char)('0' + (div = (num1 * 8389L) >> 23));
            num1 -= div * 1000;
        L3:
            buffer[position++] = (char)('0' + (div = (num1 * 5243L) >> 19));
            num1 -= div * 100;
        L2:
            buffer[position++] = (char)('0' + (div = (num1 * 6554L) >> 16));
            num1 -= div * 10;
        L1:
            buffer[position++] = (char)('0' + (num1));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteUInt64(ulong value)
        {
            ulong num1 = value, num2, num3, num4, num5, div;

            if (num1 < 10000)
            {
                if (num1 < 10)
                {
                    EnsureBufferSize(1);
                    goto L1;
                }
                if (num1 < 100)
                {
                    EnsureBufferSize(2);
                    goto L2;
                }
                if (num1 < 1000)
                {
                    EnsureBufferSize(3);
                    goto L3;
                }
                EnsureBufferSize(4);
                goto L4;
            }
            else
            {
                num2 = num1 / 10000;
                num1 -= num2 * 10000;
                if (num2 < 10000)
                {
                    if (num2 < 10)
                    {
                        EnsureBufferSize(5);
                        goto L5;
                    }
                    if (num2 < 100)
                    {
                        EnsureBufferSize(6);
                        goto L6;
                    }
                    if (num2 < 1000)
                    {
                        EnsureBufferSize(7);
                        goto L7;
                    }
                    EnsureBufferSize(8);
                    goto L8;
                }
                else
                {
                    num3 = num2 / 10000;
                    num2 -= num3 * 10000;
                    if (num3 < 10000)
                    {
                        if (num3 < 10)
                        {
                            EnsureBufferSize(9);
                            goto L9;
                        }
                        if (num3 < 100)
                        {
                            EnsureBufferSize(10);
                            goto L10;
                        }
                        if (num3 < 1000)
                        {
                            EnsureBufferSize(11);
                            goto L11;
                        }
                        EnsureBufferSize(12);
                        goto L12;
                    }
                    else
                    {
                        num4 = num3 / 10000;
                        num3 -= num4 * 10000;
                        if (num4 < 10000)
                        {
                            if (num4 < 10)
                            {
                                EnsureBufferSize(13);
                                goto L13;
                            }
                            if (num4 < 100)
                            {
                                EnsureBufferSize(14);
                                goto L14;
                            }
                            if (num4 < 1000)
                            {
                                EnsureBufferSize(15);
                                goto L15;
                            }
                            EnsureBufferSize(16);
                            goto L16;
                        }
                        else
                        {
                            num5 = num4 / 10000;
                            num4 -= num5 * 10000;
                            if (num5 < 10000)
                            {
                                if (num5 < 10)
                                {
                                    EnsureBufferSize(17);
                                    goto L17;
                                }
                                if (num5 < 100)
                                {
                                    EnsureBufferSize(18);
                                    goto L18;
                                }
                                if (num5 < 1000)
                                {
                                    EnsureBufferSize(19);
                                    goto L19;
                                }
                                EnsureBufferSize(20);
                                goto L20;
                            }
                        L20:
                            buffer[position++] = (char)('0' + (div = (num5 * 8389UL) >> 23));
                            num5 -= div * 1000;
                        L19:
                            buffer[position++] = (char)('0' + (div = (num5 * 5243UL) >> 19));
                            num5 -= div * 100;
                        L18:
                            buffer[position++] = (char)('0' + (div = (num5 * 6554UL) >> 16));
                            num5 -= div * 10;
                        L17:
                            buffer[position++] = (char)('0' + (num5));
                        }
                    L16:
                        buffer[position++] = (char)('0' + (div = (num4 * 8389UL) >> 23));
                        num4 -= div * 1000;
                    L15:
                        buffer[position++] = (char)('0' + (div = (num4 * 5243UL) >> 19));
                        num4 -= div * 100;
                    L14:
                        buffer[position++] = (char)('0' + (div = (num4 * 6554UL) >> 16));
                        num4 -= div * 10;
                    L13:
                        buffer[position++] = (char)('0' + (num4));
                    }
                L12:
                    buffer[position++] = (char)('0' + (div = (num3 * 8389UL) >> 23));
                    num3 -= div * 1000;
                L11:
                    buffer[position++] = (char)('0' + (div = (num3 * 5243UL) >> 19));
                    num3 -= div * 100;
                L10:
                    buffer[position++] = (char)('0' + (div = (num3 * 6554UL) >> 16));
                    num3 -= div * 10;
                L9:
                    buffer[position++] = (char)('0' + (num3));
                }
            L8:
                buffer[position++] = (char)('0' + (div = (num2 * 8389UL) >> 23));
                num2 -= div * 1000;
            L7:
                buffer[position++] = (char)('0' + (div = (num2 * 5243UL) >> 19));
                num2 -= div * 100;
            L6:
                buffer[position++] = (char)('0' + (div = (num2 * 6554UL) >> 16));
                num2 -= div * 10;
            L5:
                buffer[position++] = (char)('0' + (num2));
            }
        L4:
            buffer[position++] = (char)('0' + (div = (num1 * 8389UL) >> 23));
            num1 -= div * 1000;
        L3:
            buffer[position++] = (char)('0' + (div = (num1 * 5243UL) >> 19));
            num1 -= div * 100;
        L2:
            buffer[position++] = (char)('0' + (div = (num1 * 6554UL) >> 16));
            num1 -= div * 10;
        L1:
            buffer[position++] = (char)('0' + (num1));
        }
    }
}