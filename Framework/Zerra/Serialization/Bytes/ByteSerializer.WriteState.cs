// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Serialization
{
    public sealed partial class ByteSerializer
    {
        private struct WriteState
        {
            private Stack<WriteFrame> stack;
            public WriteFrame CurrentFrame;
            public bool Ended;

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

            public int BytesNeeded;
        }
    }
}