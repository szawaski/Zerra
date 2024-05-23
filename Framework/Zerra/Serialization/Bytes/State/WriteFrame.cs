// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Collections.Generic;

namespace Zerra.Serialization
{
    internal sealed class WriteFrame
    {
        public ByteConverter Converter;
        //public ByteConverter? TypeDetail;
        public bool NullFlags;
        //public WriteFrameType FrameType;

        public object? Parent;

        public bool HasWrittenPropertyType;
        public bool HasWrittenIsNull;
        public int? EnumerableLength;
        public bool ObjectInProgress;

        public IEnumerator? Enumerator;
        public bool EnumeratorInProgress;
    }
}