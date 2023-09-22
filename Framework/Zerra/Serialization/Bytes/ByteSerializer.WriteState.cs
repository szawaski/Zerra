// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;

namespace Zerra.Serialization
{
    public static partial class ByteSerializer
    {
        private struct WriteState
        {
            public bool UsePropertyNames;
            public bool IncludePropertyTypes;
            public bool IgnoreIndexAttribute;
            public ByteSerializerIndexSize IndexSize;

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