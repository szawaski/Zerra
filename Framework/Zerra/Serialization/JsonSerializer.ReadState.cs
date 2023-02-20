// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.IO;

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        private struct ReadState
        {
            private Stack<ReadFrame> stack;
            public ReadFrame CurrentFrame;
            public object LastFrameResultObject;
            public string LastFrameResultString;
            public bool Ended;
            public bool Nameless;

            public int Count => stack.Count;

            public void PushFrame(ReadFrame frame)
            {
                if (stack == null)
                    stack = new Stack<ReadFrame>();
                stack.Push(CurrentFrame);
                CurrentFrame = frame;
            }
            public void EndFrame()
            {
                if (stack == null)
                    stack = new Stack<ReadFrame>();
                LastFrameResultObject = CurrentFrame.ResultObject;
                LastFrameResultString = CurrentFrame.ResultString;
                if (stack.Count > 0)
                    CurrentFrame = stack.Pop();
                else
                    Ended = true;
            }

            public int BytesNeeded;
            public int BufferPostion;

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