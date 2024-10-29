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
        public Type? ChildReadType;
        public bool ChildHasNullChecked;

        public bool HasCreated;
        public object? Object;

        public int EnumeratorIndex;

        public int? EnumerableLength;

        public bool HasReadProperty;
        public object? Property;

        public bool DrainBytes;
    }
}