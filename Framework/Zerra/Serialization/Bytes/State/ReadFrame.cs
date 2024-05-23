// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ReadFrame
    {
        public ByteConverter Converter;
        public bool NullFlags;
        //public ReadFrameType FrameType;

        //public bool HasReadPropertyType;

        public bool HasNullChecked;
        public bool HasObjectStarted;
        public object? ResultObject;
        //public ByteConverterMember? ObjectProperty;

        public int? StringLength;
        public int? EnumerableLength;

        public Array? EnumerableArray;

        public int EnumerablePosition;

        public bool DrainBytes;
    }
}