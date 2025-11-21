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
        private sealed class ByteConverterObjectMember
        {
            private readonly string memberKey;

            public readonly MemberDetail Member;

            public readonly ushort Index;

            private byte[]? nameAsBytes = null;
            public ReadOnlySpan<byte> NameAsBytes
            {
                get
                {
                    nameAsBytes ??= ByteWriter.encoding.GetBytes(Member.Name);
                    return nameAsBytes;
                }
            }

            public ByteConverterObjectMember(TypeDetail parentTypeDetail, MemberDetail member, ushort index)
            {
                this.memberKey = $"{parentTypeDetail.Type.FullName}.{member.Name}";
                this.Member = member;
                this.Index = index;
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

            private void SetterForConverterSetValues(Dictionary<string, object?> parent, object? value) => parent.Add(Member.Name.TrimStart('_'), value);

            private ByteConverter<Dictionary<string, object?>>? converterSetValues;
            public ByteConverter<Dictionary<string, object?>> ConverterSetCollectedValues
            {
                get
                {
                    if (converterSetValues is null)
                    {
                        lock (this)
                        {
                            converterSetValues ??= ByteConverterFactory<Dictionary<string, object?>>.Get(Member.TypeDetailBoxed, memberKey, null, SetterForConverterSetValues);
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