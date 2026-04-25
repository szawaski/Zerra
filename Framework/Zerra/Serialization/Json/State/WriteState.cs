// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Zerra.Serialization.Json.State
{
    /// <summary>
    /// Represents the state of JSON serialization, managing a stack of write frames, string writing state, and serialization options.
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
        /// Gets or sets the current write frame.
        /// </summary>
        public WriteFrame Current;

        /// <summary>
        /// Gets or sets the number of bytes needed for the current operation.
        /// </summary>
        public int SizeNeeded;

        /// <summary>
        /// Gets or sets a value indicating whether object members should be written without names.
        /// </summary>
        public bool Nameless;

        /// <summary>
        /// Gets or sets a value indicating whether properties with null values should not be written.
        /// </summary>
        public bool DoNotWriteNullProperties;

        /// <summary>
        /// Gets or sets a value indicating whether properties with default values should not be written.
        /// </summary>
        public bool DoNotWriteDefaultProperties;

        /// <summary>
        /// Gets or sets a value indicating whether enums should be serialized as numbers.
        /// </summary>
        public bool EnumAsNumber;

        /// <summary>
        /// Gets or sets the current stage of string writing.
        /// </summary>
        public byte WorkingStringStage;

        /// <summary>
        /// Gets or sets the string being written.
        /// </summary>
        public ReadOnlyMemory<char> WorkingString;

        /// <summary>
        /// Gets or sets the current index in the string being written.
        /// </summary>
        public int WorkingStringIndex;

        /// <summary>
        /// Gets or sets the starting position in the string being written.
        /// </summary>
        public int WorkingStringStart;

        /// <summary>
        /// Gets or sets the conversion graph that specifies which members should be included or excluded from processing.
        /// </summary>
        public Graph? Graph;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteState"/> struct with the specified serialization options.
        /// </summary>
        /// <param name="options">The JSON serializer options to configure the write state.</param>
        /// <param name="graph">The optional conversion graph for tracking circular references.</param>
        public WriteState(JsonSerializerOptions options, Graph? graph)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            Nameless = options.Nameless;
            DoNotWriteNullProperties = options.DoNotWriteNullProperties;
            DoNotWriteDefaultProperties = options.DoNotWriteDefaultProperties;
            EnumAsNumber = options.EnumAsNumber;
            Graph = graph;
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
        /// Pushes a new frame onto the stack with the specified graph.
        /// </summary>
        /// <param name="graph">The optional conversion graph for the new frame.</param>
        public void PushFrame(Graph? graph)
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
                    Graph = graph
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