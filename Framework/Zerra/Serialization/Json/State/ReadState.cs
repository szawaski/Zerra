// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Zerra.Serialization.Json.State
{
    [StructLayout(LayoutKind.Auto)]
    public struct ReadState
    {
        private ReadFrame[] stack;
        private int stackCount;
        private int stashCount;
        public int StackSize => stackCount;

        public ReadFrame Current;
        public int CharsNeeded;

        public bool IsFinalBlock;

        public char[]? Buffer;
        public int BufferPosition;

        public long NumberInt64;
        public ulong NumberUInt64;
        public double NumberDouble;
        public decimal NumberDecimal;
        public bool NumberIsNegative;

        public double NumberWorkingDouble;
        public decimal NumberWorkingDecimal;
        public bool NumberWorkingIsNegative;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureStackSize()
        {
            if (stack is null)
                stack = new ReadFrame[4];
            else if (stackCount == stack.Length)
                Array.Resize(ref stack, stack.Length * 2);
        }

        public void PushFrame(bool nullFlags)
        {
            if (stashCount == 0)
            {
                if (stackCount++ > 0)
                {
                    EnsureStackSize();
                    stack[stackCount - 2] = Current;
                }
                Current = new()
                {
                    NullFlags = nullFlags
                };
            }
            else
            {
                if (stackCount++ > 0)
                {
                    Current = stack[stackCount - 1];
                }
                if (stackCount == stashCount)
                    stashCount = 0;
            }
        }

        public void StashFrame()
        {
            if (stackCount > 1)
            {
                if (stashCount == 0)
                {
                    stashCount = stackCount;
                    EnsureStackSize();
                }
                stack[stackCount - 1] = Current;
                Current = stack[--stackCount - 1];
            }
        }

        public void EndFrame()
        {
            Debug.Assert(stashCount == 0);
            if (stackCount > 1)
                Current = stack[--stackCount - 1];
        }
    }
}