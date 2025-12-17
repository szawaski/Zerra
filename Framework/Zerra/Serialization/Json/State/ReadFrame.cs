// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.InteropServices;

namespace Zerra.Serialization.Json.State
{
    [StructLayout(LayoutKind.Auto)]
    public struct ReadFrame
    {
        public JsonToken ChildJsonToken;

        public byte State;

        public bool HasCreated;
        public object? Object;
        public int EnumeratorIndex;

        public bool HasReadFirstToken;
        public bool HasReadProperty;
        public bool HasReadSeperator;
        public bool HasReadValue;
        public object? Property;

        public Graph? Graph;
        public Graph? ReturnGraph;
    }
}