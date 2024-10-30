// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Zerra.Serialization.Bytes.State
{
    [StructLayout(LayoutKind.Auto)]
    public struct ReadState
    {
        private ReadFrame[] stack;
        private int stackCount;
        private int stashCount;
        public int StackSize => stackCount;

        public bool UseTypes;
        public bool IgnoreIndexAttribute;
        public bool UseMemberNames;
        public bool UseIndexSizeUInt16;

        public ReadFrame Current;
        public int SizeNeeded;

        public bool EntryHasNullChecked;
        public Type? EntryReadType;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public ReadState(ByteSerializerOptions options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            UseTypes = options.UseTypes;
            IgnoreIndexAttribute = options.IgnoreIndexAttribute;
            UseMemberNames = options.IndexType == ByteSerializerIndexType.MemberNames;
            UseIndexSizeUInt16 = options.IndexType == ByteSerializerIndexType.UInt16;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureStackSize()
        {
            if (stack is null)
                stack = new ReadFrame[4];
            else if (stackCount == stack.Length)
                Array.Resize(ref stack, stack.Length * 2);
        }

        public void PushFrame(bool drainBytes)
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
                    DrainBytes = drainBytes
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
            Debug.Assert(stackCount > 0);
            if (stashCount == 0)
            {
                stashCount = stackCount;
                EnsureStackSize();
            }
            if (stackCount > 1)
            {
                stack[stackCount - 1] = Current;
                Current = stack[--stackCount - 1];
            }
            else
            {
                stackCount = 0;
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