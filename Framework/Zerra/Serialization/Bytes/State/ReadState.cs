// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Serialization
{
    internal ref struct ReadState
    {
        private Stack<ReadFrame> stack;
        public ReadFrame CurrentFrame;
        public object? LastFrameResultObject;
        public bool Ended;

        public void PushFrame(ByteConverter converter, bool nullFlags)
        {
            var frame = new ReadFrame()
            {
                Converter = converter,
                NullFlags = nullFlags
            };
            stack ??= new Stack<ReadFrame>();
            stack.Push(CurrentFrame);
            CurrentFrame = frame;
        }
        public void EndFrame()
        {
            stack ??= new Stack<ReadFrame>();
            LastFrameResultObject = CurrentFrame.ResultObject;
            if (stack.Count > 0)
                CurrentFrame = stack.Pop();
            else
                Ended = true;
        }

        public int BytesNeeded;
    }
}