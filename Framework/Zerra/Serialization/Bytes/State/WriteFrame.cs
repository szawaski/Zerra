﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization
{
    public sealed class WriteFrame
    {
        public ByteConverter Converter;
        public bool NullFlags;

        public object? Parent;
        public object? Object;

        public bool HasTypeWritten;
        public bool HasWrittenIsNull;
        public int? EnumerableLength;

        public object? Enumerator;
        public bool EnumeratorInProgress;
    }
}