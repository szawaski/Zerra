// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    public struct WriteFrame
    {
        public bool NullFlags;

        public Type WriteType;
        public object? Object;

        public bool HasWrittenIsNull;
        public int? EnumerableLength;

        public object? Enumerator;
        public bool EnumeratorInProgress;
    }
}