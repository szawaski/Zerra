// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    public struct WriteState
    {
        private WriteFrame[] stack;
        private int stackCount;
        private int stashCount;
        public int StackSize => stackCount;

        public bool IncludePropertyTypes;
        public bool UsePropertyNames;
        public bool IgnoreIndexAttribute;
        public bool IndexSizeUInt16;

        public WriteFrame Current;
        public bool Ended;
        public int BytesNeeded;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureStackSize()
        {
            if (stack == null)
                stack = new WriteFrame[4];
            else if (stackCount == stack.Length)
                Array.Resize(ref stack, stack.Length * 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushFrame(bool nullFlags)
        {
            if (stashCount > 0)
            {
                Current = stack[++stackCount - 1];
                stashCount--;
            }
            else
            {
                EnsureStackSize();
                if (stackCount > 0)
                    stack[stackCount++ - 1] = Current;
                else
                    stackCount++;
                Current = new WriteFrame()
                {
                    NullFlags = nullFlags
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StashFrame()
        {
            if (stackCount == 1)
                return;
            EnsureStackSize();
            stack[stackCount - 1] = Current;
            Current = stack[--stackCount - 1];
            stashCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndFrame()
        {
            if (stackCount == 1)
            {
                Ended = true;
                return;
            }
            Current = stack[--stackCount - 1];
        }
    }
}