// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Serialization
{
    internal sealed class WriteFrame
    {
        public ByteConverter Converter;
        public bool NullFlags;

        public object? Parent;
        public object? Object;

        public bool HasWrittenIsNull;
        public int? EnumerableLength;
        public bool ObjectInProgress;

        public IEnumerator? Enumerator;
        public bool EnumeratorInProgress;
    }
}