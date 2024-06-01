// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.InteropServices;

namespace Zerra.Serialization.Bytes.State
{
    [StructLayout(LayoutKind.Auto)]
    public struct ReadFrame
    {
        public bool NullFlags;

        public Type ReadType;
        public bool HasNullChecked;
        public bool HasCreated;
        public object? Object;

        public int? EnumerableLength;

        public bool HasReadProperty;
        public object? Property;

        public bool DrainBytes;
    }
}