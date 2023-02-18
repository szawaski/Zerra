// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Serialization
{
    public partial class ByteSerializer
    {
        private struct ReadState
        {
            private Stack<ReadFrame> stack;
            public ReadFrame CurrentFrame;
            public object LastFrameResult;
            public bool Ended;

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
                LastFrameResult = CurrentFrame.ResultObject;
                if (stack.Count > 0)
                    CurrentFrame = stack.Pop();
                else
                    Ended = true;
            }

            public int BytesNeeded;
            public int BufferPostion;
        }
    }
}