// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;

namespace Zerra.Serialization
{
    public static partial class ByteSerializer
    {
        private sealed class ReadFrame
        {
            public SerializerTypeDetail TypeDetail;
            public bool NullFlags;
            public ReadFrameType FrameType;

            public bool HasReadPropertyType;

            public bool HasNullChecked;
            public bool HasObjectStarted;
            public object ResultObject;
            public SerializerMemberDetail ObjectProperty;

            public int? StringLength;
            public int? EnumerableLength;

            public IList EnumerableList;
            public Array EnumerableArray;

            public int EnumerablePosition;

            public bool DrainBytes;
        }
    }
}