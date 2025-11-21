// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed partial class JsonConverterObject<TParent, TValue>
    {
        private sealed class JsonConverterObjectMember
        {
            private readonly string memberKey;

            public readonly MemberDetail Member;

            public readonly string JsonName;

            public readonly JsonIgnoreCondition IgnoreCondition;

            private byte[]? jsonNameBytes = null;
            public ReadOnlySpan<byte> JsonNameBytes
            {
                get
                {
                    jsonNameBytes ??= StringHelper.EscapeAndEncodeString(JsonName, false);
                    return jsonNameBytes;
                }
            }

            private byte[]? jsonNameSegmentBytes = null;
            public ReadOnlySpan<byte> JsonNameSegmentBytes
            {
                get
                {
                    jsonNameSegmentBytes ??= StringHelper.EscapeAndEncodeString(JsonName, true);
                    return jsonNameSegmentBytes;
                }
            }

            private char[]? jsonNameChars = null;
            public ReadOnlySpan<char> JsonNameChars
            {
                get
                {
                    jsonNameChars ??= StringHelper.EscapeString(JsonName, false);
                    return jsonNameChars;
                }
            }

            private char[]? jsonNameSegmentChars = null;
            public ReadOnlySpan<char> JsonNameSegmentChars
            {
                get
                {
                    jsonNameSegmentChars ??= StringHelper.EscapeString(JsonName, true);
                    return jsonNameSegmentChars;
                }
            }

            public JsonConverterObjectMember(TypeDetail parentTypeDetail, MemberDetail member, string jsonName, JsonIgnoreCondition ignoreCondition)
            {
                this.memberKey = $"{parentTypeDetail.Type.FullName}.{member.Name}";
                this.Member = member;
                this.JsonName = jsonName;
                this.IgnoreCondition = ignoreCondition;
            }

            private JsonConverter<TValue>? converter = null;
            public JsonConverter<TValue> Converter
            {
                get
                {
                    if (converter is null)
                    {
                        lock (this)
                        {
                            converter ??= JsonConverterFactory<TValue>.Get(Member.TypeDetailBoxed, memberKey, Member.HasGetterBoxed ? Member.GetterTyped : null, Member.HasSetterBoxed ? Member.SetterTyped : null);
                        }
                    }
                    return converter;
                }
            }

            private void SetterForConverterSetValues(Dictionary<string, object?> parent, object? value) => parent.Add(Member.Name.TrimStart('_'), value);

            private JsonConverter<Dictionary<string, object?>>? converterSetValues;
            public JsonConverter<Dictionary<string, object?>> ConverterSetCollectedValues
            {
                get
                {
                    if (converterSetValues is null)
                    {
                        lock (this)
                        {
                            converterSetValues ??= JsonConverterFactory<Dictionary<string, object?>>.Get(Member.TypeDetailBoxed, memberKey, null, SetterForConverterSetValues);
                        }
                    }
                    return converterSetValues;
                }
            }

            //helps with debug
            public override sealed string ToString()
            {
                return Member.Name;
            }
        }
    }
}