// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Zerra.Serialization.Bytes.State
{
    /// <summary>
    /// Represents the state of bytes serialization, managing a stack of write frames and serialization options.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct WriteState
    {
        private WriteFrame[] stack;
        private int stackCount;
        private int stashCount;

        /// <summary>
        /// Gets the current size of the stack.
        /// </summary>
        public int StackSize => stackCount;

        /// <summary>
        /// Gets or sets a value indicating whether type information should be used during serialization.
        /// </summary>
        public bool UseTypes;

        /// <summary>
        /// Gets or sets a value indicating whether index attributes should be ignored during serialization.
        /// </summary>
        public bool IgnoreIndexAttribute;

        /// <summary>
        /// Gets or sets a value indicating whether member names should be used for indexing.
        /// </summary>
        public bool UseMemberNames;

        /// <summary>
        /// Gets or sets a value indicating whether a 16-bit unsigned integer is used for index size.
        /// </summary>
        public bool UseIndexSizeUInt16;

        /// <summary>
        /// Gets or sets the current write frame.
        /// </summary>
        public WriteFrame Current;

        /// <summary>
        /// Gets or sets the number of bytes needed for the current operation.
        /// </summary>
        public int SizeNeeded;

        /// <summary>
        /// Gets or sets a value indicating whether a null indicator has been written for the entry.
        /// </summary>
        public bool EntryHasWrittenIsNull;

        /// <summary>
        /// Gets or sets the type of the entry being written.
        /// </summary>
        public Type? EntryWriteType;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteState"/> struct with the specified serialization options.
        /// </summary>
        /// <param name="options">The byte serializer options to configure the write state.</param>
        public WriteState(ByteSerializerOptions options)
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
                stack = new WriteFrame[4];
            else if (stackCount == stack.Length)
                Array.Resize(ref stack, stack.Length * 2);
        }

        /// <summary>
        /// Pushes a new frame onto the stack.
        /// </summary>
        public void PushFrame()
        {
            if (stashCount == 0)
            {
                if (stackCount++ > 0)
                {
                    EnsureStackSize();
                    stack[stackCount - 2] = Current;
                }
                Current = new();
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

        /// <summary>
        /// Stashes the current frame and restores the previous frame from the stack.
        /// </summary>
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

        /// <summary>
        /// Ends the current frame and restores the previous frame from the stack.
        /// </summary>
        public void EndFrame()
        {
            Debug.Assert(stashCount == 0);
            if (stackCount > 1)
                Current = stack[--stackCount - 1];
        }
    }
}