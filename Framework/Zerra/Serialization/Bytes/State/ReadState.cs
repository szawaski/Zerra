// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    public struct ReadState
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

        public Stack<ReadFrame> Stack;

        public bool IncludePropertyTypes;
        public bool UsePropertyNames;
        public bool IgnoreIndexAttribute;
        public bool IndexSizeUInt16;

        public ReadFrame Current;
        public object? Result;
        public bool Ended;
        public int BytesNeeded;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushFrame(ByteConverter converter, bool nullFlags, object? parent)
        {
            var frame = new ReadFrame();
            frame.Converter = converter;
            frame.NullFlags = nullFlags;
            frame.Parent = parent;

            Stack.Push(Current);
            Current = frame;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndFrame(object? result)
        {
            //ReturnFrame(CurrentFrame);
            Current = Stack.Pop();
            if (Stack.Count == 0)
            {
                Ended = true;
                Result = result;
            }
        }
    }
}