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
        public Type? ChildWriteType;
        public object? Object;

        public bool ChildHasWrittenIsNull;

        public bool HasWrittenLength;

        public object? Enumerator;
        public bool EnumeratorInProgress;

        public bool HasWrittenPropertyIndex;
    }
}