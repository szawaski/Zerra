﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Reflection;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed partial class JsonConverterObject<TParent, TValue>
    {
        private sealed class JsonConverterObjectMember<TValue2> : JsonConverterObjectMember
        {
            public JsonConverterObjectMember(TypeDetail parentTypeDetail, MemberDetail member)
                : base(parentTypeDetail, member) { }

            private void SetterForConverterSetValues(Dictionary<string, object?> parent, TValue2? value) => parent.Add(Member.Name.TrimStart('_'), value);

            private JsonConverter<Dictionary<string, object?>>? converterSetValues;
            public override sealed JsonConverter<Dictionary<string, object?>> ConverterSetCollectedValues
            {
                get
                {
                    if (converterSetValues == null)
                    {
                        lock (this)
                        {
                            converterSetValues ??= ByteConverterFactory<Dictionary<string, object?>>.Get(Member.TypeDetail, memberKey, null, SetterForConverterSetValues);
                        }
                    }
                    return converterSetValues;
                }
            }
        }
    }
}