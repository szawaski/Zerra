// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

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
        public int SizeNeeded;

        public bool IsFinalBlock;
        public bool IncludeReturnGraph;

        public JsonToken EntryToken;

        public bool Nameless { get; set; }
        public bool EnumAsNumber { get; set; }
        public bool ErrorOnTypeMismatch { get; set; }
        public bool IgnoreCase { get; set; }

        public char[]? StringBuffer;
        public int StringPosition;

        public bool ReadStringStart;
        public bool ReadStringEscape;
        public bool ReadStringEscapeUnicode;

        public ReadNumberStage NumberStage;
        public long NumberInt64;
        public ulong NumberUInt64;
        public double NumberDouble;
        public decimal NumberDecimal;
        public bool NumberIsNegative;

        public bool NumberParseFailed;
        public double NumberWorkingDouble;
        public decimal NumberWorkingDecimal;
        public bool NumberWorkingIsNegative;

        public Graph? Graph;

        public JsonToken LastReaderToken;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
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