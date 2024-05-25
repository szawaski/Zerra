﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    internal sealed class ReadFrame
    {
        public ByteConverter Converter;
        public bool NullFlags;

        public bool HasTypeRead;
        public bool HasNullChecked;
        public bool HasCreated;
        public object? Object;
        public object? Parent;

        public int? StringLength;
        public int? EnumerableLength;

        public bool DrainBytes;
    }
}