// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Collections.Generic;
using Zerra.Reflection;

namespace Zerra.Serialization.Json
{
    public static partial class JsonSerializer
    {
        private sealed class WriteFrame
        {
            public TypeDetail? TypeDetail;
            public WriteFrameType FrameType;

            public Graph? Graph;

            public object? Object;

            public byte State;

            public MemberDetail? ObjectProperty;
            public object? ObjectPropertyValue;

            public int PropertyIndexForNameless;

            public IEnumerator<MemberDetail>? MemberEnumerator;
            public IEnumerator? Enumerator;
            public bool EnumeratorPassedFirstProperty;
            public bool EnumeratorPassedFirstProperty2;
        }
    }
}