// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Zerra.Serialization.Json.State
{
    /// <summary>
    /// Represents the state of JSON deserialization, managing a stack of read frames, parsing state, and deserialization options.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct ReadState
    {
        private ReadFrame[] stack;
        private int stackCount;
        private int stashCount;

        /// <summary>
        /// Gets the current size of the stack.
        /// </summary>
        public int StackSize => stackCount;

        /// <summary>
        /// Gets or sets the current read frame.
        /// </summary>
        public ReadFrame Current;

        /// <summary>
        /// Gets or sets the number of bytes needed for the current operation.
        /// </summary>
        public int SizeNeeded;

        /// <summary>
        /// Gets or sets a value indicating whether this is the final block of data to process.
        /// </summary>
        public bool IsFinalBlock;

        /// <summary>
        /// Gets or sets a value indicating whether the return graph should be included in the result.
        /// </summary>
        public bool IncludeReturnGraph;

        /// <summary>
        /// Gets or sets the current JSON token being processed.
        /// </summary>
        public JsonToken EntryToken;

        /// <summary>
        /// Gets or sets a value indicating whether object members should be treated as nameless.
        /// </summary>
        public bool Nameless { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether enums should be deserialized as numbers.
        /// </summary>
        public bool EnumAsNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to throw an error on type mismatch.
        /// </summary>
        public bool ErrorOnTypeMismatch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether comparison should be case-insensitive.
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// Gets or sets the buffer used for storing string values during deserialization.
        /// </summary>
        public char[]? StringBuffer;

        /// <summary>
        /// Gets or sets the current position in the string buffer.
        /// </summary>
        public int StringPosition;

        /// <summary>
        /// Gets or sets a value indicating whether the start of a string has been read.
        /// </summary>
        public bool ReadStringStart;

        /// <summary>
        /// Gets or sets a value indicating whether a string escape sequence has been encountered.
        /// </summary>
        public bool ReadStringEscape;

        /// <summary>
        /// Gets or sets a value indicating whether a Unicode escape sequence in a string is being processed.
        /// </summary>
        public bool ReadStringEscapeUnicode;

        /// <summary>
        /// Gets or sets the current stage of number parsing.
        /// </summary>
        public ReadNumberStage NumberStage;

        /// <summary>
        /// Gets or sets the parsed 64-bit signed integer value.
        /// </summary>
        public long NumberInt64;

        /// <summary>
        /// Gets or sets the parsed 64-bit unsigned integer value.
        /// </summary>
        public ulong NumberUInt64;

        /// <summary>
        /// Gets or sets the parsed double value.
        /// </summary>
        public double NumberDouble;

        /// <summary>
        /// Gets or sets the parsed decimal value.
        /// </summary>
        public decimal NumberDecimal;

        /// <summary>
        /// Gets or sets a value indicating whether the current number is negative.
        /// </summary>
        public bool NumberIsNegative;

        /// <summary>
        /// Gets or sets a value indicating whether number parsing failed.
        /// </summary>
        public bool NumberParseFailed;

        /// <summary>
        /// Gets or sets the working double value during number parsing.
        /// </summary>
        public double NumberWorkingDouble;

        /// <summary>
        /// Gets or sets the working decimal value during number parsing.
        /// </summary>
        public decimal NumberWorkingDecimal;

        /// <summary>
        /// Gets or sets a value indicating whether the working number is negative.
        /// </summary>
        public bool NumberWorkingIsNegative;

        /// <summary>
        /// Gets or sets the conversion graph that specifies which members should be included or excluded from processing.
        /// </summary>
        public Graph? Graph;

        /// <summary>
        /// Gets or sets the last JSON token read by the reader.
        /// </summary>
        public JsonToken LastReaderToken;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadState"/> struct with the specified deserialization options.
        /// </summary>
        /// <param name="options">The JSON serializer options to configure the read state.</param>
        /// <param name="graph">The optional conversion graph for tracking circular references.</param>
        /// <param name="isFinalBlock">A value indicating whether this is the final block of data.</param>
        /// <param name="hasReturnGraph">A value indicating whether the return graph should be included.</param>
        public ReadState(JsonSerializerOptions options, Graph? graph, bool isFinalBlock, bool hasReturnGraph)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            Nameless = options.Nameless;
            EnumAsNumber = options.EnumAsNumber;
            ErrorOnTypeMismatch = options.ErrorOnTypeMismatch;
            Graph = graph;
            IsFinalBlock = isFinalBlock;
            IncludeReturnGraph = hasReturnGraph;
            IgnoreCase = options.IgnoreCase;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureStackSize()
        {
            if (stack is null)
                stack = new ReadFrame[4];
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