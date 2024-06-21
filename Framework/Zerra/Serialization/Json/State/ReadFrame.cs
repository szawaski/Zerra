// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.InteropServices;

namespace Zerra.Serialization.Json.State
{
    [StructLayout(LayoutKind.Auto)]
    public struct ReadFrame
    {
        public Graph? Graph;
        public JsonValueType ValueType;
        public char FirstChar;

        public byte State;

        public bool HasCreated;
        public object? Object;

        public bool HasReadProperty;
        public bool HasReadPropertySeperator;
        public bool HasReadPropertyValue;
        public object? Property;
    }
}