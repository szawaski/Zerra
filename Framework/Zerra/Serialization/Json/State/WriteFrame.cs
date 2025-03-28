﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.InteropServices;

namespace Zerra.Serialization.Json.State
{
    [StructLayout(LayoutKind.Auto)]
    public struct WriteFrame
    {
        public bool HasWrittenPropertyName;

        public object? Object;
        public bool HasWrittenStart;
        public bool HasWrittenFirst;
        public bool HasWrittenSeperator;
        public int EnumeratorIndex;
        public bool EnumeratorInProgress;

        public Graph? Graph;
    }
}