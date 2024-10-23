// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed partial class JsonConverterObject<TParent, TValue>
    {
        private abstract class JsonConverterObjectMember
        {
            protected readonly string memberKey;

            public readonly MemberDetail Member;

            private readonly string name;

            private byte[]? nameAsBytes = null;
            public ReadOnlySpan<byte> NameAsBytes
            {
                get
                {
                    nameAsBytes ??= StringHelper.EscapeAndEncodeString(name);
                    return nameAsBytes;
                }
            }

            public JsonConverterObjectMember(TypeDetail parentTypeDetail, MemberDetail member, string name)
            {
                this.memberKey = $"{parentTypeDetail.Type.FullName}.{member.Name}";
                this.Member = member;
                this.name = name;
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
            public static JsonConverterObjectMember New(TypeDetail parentTypeDetail, MemberDetail member, string name)
            {
                var generic = byteConverterObjectMemberT.GetGenericTypeDetail(typeof(TParent), typeof(TValue), member.Type);
                var obj = generic.ConstructorDetailsBoxed[0].CreatorWithArgsBoxed([parentTypeDetail, member, name]);
                return (JsonConverterObjectMember)obj;
            }
        }
    }
}