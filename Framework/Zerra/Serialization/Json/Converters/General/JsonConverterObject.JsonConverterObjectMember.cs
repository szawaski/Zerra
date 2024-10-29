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
        private abstract class JsonConverterObjectMember
        {
            protected readonly string memberKey;

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

            public abstract JsonConverter<Dictionary<string, object?>> ConverterSetCollectedValues { get; }

            //helps with debug
            public override sealed string ToString()
            {
                return Member.Name;
            }

            private static readonly Type byteConverterObjectMemberT = typeof(JsonConverterObjectMember<>);
            public static JsonConverterObjectMember New(TypeDetail parentTypeDetail, MemberDetail member, string jsonName, JsonIgnoreCondition ignoreCondition)
            {
                var generic = byteConverterObjectMemberT.GetGenericTypeDetail(typeof(TParent), typeof(TValue), member.Type);
                var obj = generic.ConstructorDetailsBoxed[0].CreatorWithArgsBoxed([parentTypeDetail, member, jsonName, ignoreCondition]);
                return (JsonConverterObjectMember)obj;
            }
        }
    }
}