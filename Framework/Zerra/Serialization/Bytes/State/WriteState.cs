// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    internal struct WriteState
    {
//        private static readonly Queue<WriteFrame> framePool = new();
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static WriteFrame RentFrame()
//        {
//#if NETSTANDARD2_0
//            if (framePool.Count > 0)
//                return framePool.Dequeue();
//            else
//                return new();
//#else
//            if (framePool.TryDequeue(out var frame))
//                return frame;
//            return new();
//#endif
//        }
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void ReturnFrame(WriteFrame frame)
//        {
//            frame.Converter = default;
//            frame.NullFlags = default;
//            frame.Parent = default;
//            frame.Object = default;
//            frame.HasWrittenIsNull = default;
//            frame.EnumerableLength = default;
//            frame.ObjectInProgress = default;
//            frame.Enumerator = default;
//            frame.EnumeratorInProgress = default;
//            framePool.Enqueue(frame);
//        }

        private Stack<WriteFrame> stack;
        public WriteFrame CurrentFrame;
        public bool Ended;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushFrame(ByteConverter converter, bool nullFlags, object? parent)
        {
            var frame = new WriteFrame();
            frame.Converter = converter;
            frame.NullFlags = nullFlags;
            frame.Parent = parent;

            stack ??= new Stack<WriteFrame>();
            stack.Push(CurrentFrame);
            CurrentFrame = frame;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndFrame()
        {
            //ReturnFrame(CurrentFrame);
            if (stack.Count > 0)
            {
                CurrentFrame = stack.Pop();
                if (stack.Count == 0)
                    Ended = true;
            }
            else
            {
                CurrentFrame = null!;
            }
        }

        public int BytesNeeded;
    }
}