// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        private class ReadFrame
        {
            public TypeDetail TypeDetail;
            public ReadFrameType FrameType;

            public Graph Graph;

            public object ResultObject;
            public string ResultString;

            public int State;

            public MemberDetail ObjectProperty;

            public MethodDetail ArrayAddMethod;
            public object[] ArrayAddMethodArgs;
            public TypeDetail ArrayElementType;

            public char FirstLiteralChar;

            public bool LiteralNumberIsNegative;
            public object LiteralNumberWorking;
            public bool LiteralNumberWorkingIsNegative;

            public int PropertyIndexForNameless;
        }
    }
}