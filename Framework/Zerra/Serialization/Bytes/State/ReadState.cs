// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    internal struct ReadState
    {
        //        private static readonly Stack<ReadFrame> framePool = new();
        //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //        private static ReadFrame RentFrame()
        //        {
        //#if NETSTANDARD2_0
        //            if (framePool.Count > 0)
        //                return framePool.Pop();
        //            else
        //                return new();
        //#else
        //            if (framePool.TryPop(out var frame))
        //                return frame;
        //            return new();
        //#endif
        //        }
        //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //        private static void ReturnFrame(ReadFrame frame)
        //        {
        //            //frame.Converter = default;
        //            //frame.NullFlags = default;
        //            frame.HasNullChecked = default;
        //            frame.HasObjectStarted = default;
        //            frame.ResultObject = default;
        //            //frame.Parent = default;
        //            frame.StringLength = default;
        //            frame.EnumerableLength = default;
        //            frame.DrainBytes = default;
        //            framePool.Push(frame);
        //        }

        private Stack<ReadFrame> stack;
        public ReadFrame Current;
        public object? Result;
        public bool Ended;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushFrame(ByteConverter converter, bool nullFlags, object? parent)
        {
            var frame = new ReadFrame();
            frame.Converter = converter;
            frame.NullFlags = nullFlags;
            frame.Parent = parent;

            stack ??= new Stack<ReadFrame>();
            stack.Push(Current);
            Current = frame;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndFrame(object? result)
        {
            //ReturnFrame(CurrentFrame);
            Current = stack.Pop();
            if (stack.Count == 0)
            {
                Ended = true;
                Result = result;
            }
        }

        public int BytesNeeded;
    }
}