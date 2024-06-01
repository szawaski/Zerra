// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.InteropServices;

namespace Zerra.Serialization.Bytes.State
{
    [StructLayout(LayoutKind.Auto)]
    public struct WriteFrame
    {
        public bool NullFlags;

        public Type WriteType;
        public object? Object;

        public bool HasWrittenIsNull;
        public int? EnumerableLength;

        public object? Enumerator;
        public bool EnumeratorInProgress;

        public bool HasWrittenPropertyIndex;
        public ushort IndexProperty;
        public string IndexPropertyName;
    }
}