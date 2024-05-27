// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    public struct ReadState
    {
        private ReadFrame[] stack;
        private int stackCount;
        private int stashCount;
        public int StackSize => stackCount;

        private void EnsureStackSize()
        {
            if (stack == null)
                stack = new ReadFrame[4];
            else if (stackCount == stack.Length)
                Array.Resize(ref stack, stack.Length * 2);
        }

        public bool IncludePropertyTypes;
        public bool UsePropertyNames;
        public bool IgnoreIndexAttribute;
        public bool IndexSizeUInt16;

        public ReadFrame Current;
        public bool Ended;
        public int BytesNeeded;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushFrame(bool nullFlags)
        {
            if (stashCount > 0)
            {
                Current = stack[stackCount++];
                stashCount--;
            }
            else
            {
                EnsureStackSize();
                if (stackCount > 0)
                    stack[stackCount++ - 1] = Current;
                else
                    stackCount++;
                Current = new ReadFrame()
                {
                    NullFlags = nullFlags
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StashFrame()
        {
            Current = stack[stackCount-- - 1];
            stashCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndFrame()
        {
            Current = stack[stackCount-- - 1];
            if (stackCount == 0)
                Ended = true;
        }
    }
}