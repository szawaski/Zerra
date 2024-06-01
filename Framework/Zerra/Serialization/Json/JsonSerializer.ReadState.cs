// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Serialization.Json
{
    public static partial class JsonSerializer
    {
        private struct ReadState
        {
            private Stack<ReadFrame> stack;
            public ReadFrame CurrentFrame;
            public object? LastFrameResultObject;
            public string? LastFrameResultString;
            public bool Ended;
            public bool Nameless;
            public bool IsFinalBlock;

            public int Count
            {
                get
                {
                    stack ??= new Stack<ReadFrame>();
                    return stack.Count;
                }
            }

            public void PushFrame(ReadFrame frame)
            {
                stack ??= new Stack<ReadFrame>();
                stack.Push(CurrentFrame);
                CurrentFrame = frame;
            }
            public void EndFrame()
            {
                stack ??= new Stack<ReadFrame>();
                LastFrameResultObject = CurrentFrame.ResultObject;
                LastFrameResultString = CurrentFrame.ResultString;
                if (stack.Count > 0)
                    CurrentFrame = stack.Pop();
                else
                    Ended = true;
            }

            public int CharsNeeded;

            public long LiteralNumberInt64;
            public ulong LiteralNumberUInt64;
            public double LiteralNumberDouble;
            public decimal LiteralNumberDecimal;
            public bool LiteralNumberIsNegative;

            public double LiteralNumberWorkingDouble;
            public decimal LiteralNumberWorkingDecimal;
            public bool LiteralNumberWorkingIsNegative;
        }
    }
}