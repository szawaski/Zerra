// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        private class WriteFrame
        {
            public TypeDetail TypeDetail;
            public ReadFrameType FrameType;

            public Graph Graph;

            public object Object;

            public byte State;

            public MemberDetail ObjectProperty;

            public int PropertyIndexForNameless;
        }
    }
}