// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;

namespace Zerra.Serialization.Bytes.Converters.General
{
    internal sealed partial class ByteConverterObject<TParent, TValue>
    {
        private abstract class ByteConverterObjectMember
        {
            protected readonly string memberKey;

            public readonly MemberDetail Member;

            private byte[]? name = null;
            public byte[] Name
            {
                get
                {
                    name ??= ByteWriter.encoding.GetBytes(Member.Name);
                    return name;
                }
            }

            public ByteConverterObjectMember(TypeDetail parentTypeDetail, MemberDetail member)
            {
                this.memberKey = $"{parentTypeDetail.Type.FullName}.{member.Name}";
                this.Member = member;
            }

            private ByteConverter<TValue>? converter = null;
            public ByteConverter<TValue> Converter
            {
                get
                {
                    if (converter is null)
                    {
                        lock (this)
                        {
                            converter ??= ByteConverterFactory<TValue>.Get(Member.TypeDetailBoxed, memberKey, Member.HasGetterBoxed ? Member.GetterTyped : null, Member.HasSetterBoxed ? Member.SetterTyped : null);
                        }
                    }
                    return converter;
                }
            }

            public abstract ByteConverter<Dictionary<string, object?>> ConverterSetCollectedValues { get; }

            //helps with debug
            public override sealed string ToString()
            {
                return Member.Name;
            }

            private static readonly Type byteConverterObjectMemberT = typeof(ByteConverterObjectMember<>);
            public static ByteConverterObjectMember New(TypeDetail parentTypeDetail, MemberDetail member)
            {
                var generic = byteConverterObjectMemberT.GetGenericTypeDetail(typeof(TParent), typeof(TValue), member.Type);
                var obj = generic.ConstructorDetailsBoxed[0].CreatorWithArgsBoxed([parentTypeDetail, member]);
                return (ByteConverterObjectMember)obj;
            }
        }
    }
}