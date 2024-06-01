// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Serialization.Json
{
    public static partial class JsonSerializer
    {
        private struct WriteState
        {
            public bool Nameless;
            public bool DoNotWriteNull;
            public bool EnumAsNumber;

            private Stack<WriteFrame> stack;
            public WriteFrame CurrentFrame;
            public bool Ended;

            public readonly int Count => stack.Count;

            public void PushFrame(WriteFrame frame)
            {
                stack ??= new Stack<WriteFrame>();
                stack.Push(CurrentFrame);
                CurrentFrame = frame;
            }
            public void EndFrame()
            {
                stack ??= new Stack<WriteFrame>();
                if (stack.Count > 0)
                    CurrentFrame = stack.Pop();
                else
                    Ended = true;
            }

            public int CharsNeeded;

            public byte WorkingStringState;
            public ReadOnlyMemory<char> WorkingString;
            public int WorkingStringIndex;
            public int WorkingStringStart;
        }
    }
}