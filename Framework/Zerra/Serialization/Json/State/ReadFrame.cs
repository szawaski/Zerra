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

        public bool HasReadFirstArrayElement;
        public bool HasReadProperty;
        public char? WorkingFirstChar;
        public bool HasReadSeperator;
        public bool HasReadValue;
        public object? Property;
    }
}