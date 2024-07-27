// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Serialization.Json
{
    public static partial class JsonSerializerOld
    {
        private sealed class ReadFrame
        {
            public TypeDetail? TypeDetail;
            public ReadFrameType FrameType;

            public Graph? Graph;

            public object? ResultObject;
            public string? ResultString;

            public byte State;

            public MemberDetail? ObjectProperty;

            public MethodDetail? AddMethod;
            public object?[]? AddMethodArgs;
            public TypeDetail? ArrayElementType;

            public char FirstLiteralChar;

            public int PropertyIndexForNameless;
            public object? DictionaryKey;
        }
    }
}