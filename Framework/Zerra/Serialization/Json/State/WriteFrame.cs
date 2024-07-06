// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.InteropServices;

namespace Zerra.Serialization.Json.State
{
    [StructLayout(LayoutKind.Auto)]
    public struct WriteFrame
    {
        public bool HasWrittenIsNull;
        public bool HasWrittenPropertyName;
    }
}