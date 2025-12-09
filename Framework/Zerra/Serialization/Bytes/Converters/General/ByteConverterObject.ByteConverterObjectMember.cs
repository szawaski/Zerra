// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;

namespace Zerra.Serialization.Bytes.Converters.General
{
    partial class ByteConverterObject<TValue>
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

            private ByteConverter? converter = null;
            public ByteConverter Converter
            {
                get
                {
                    if (converter is null)
                    {
                        lock (this)
                        {
                            converter ??= ByteConverterFactory.Get(Member.TypeDetail, memberKey, Member.HasGetter ? Member.Getter : null, Member.HasSetter ? Member.Setter : null);
                        }
                    }
                    return converter;
                }
            }

            private void SetterForConverterSetValues(object parent, object? value) => ((Dictionary<string, object?>)parent).Add(Member.Name.TrimStart('_'), value);

            private ByteConverter? converterSetValues;
            public ByteConverter ConverterSetCollectedValues
            {
                get
                {
                    if (converterSetValues is null)
                    {
                        lock (this)
                        {
                            converterSetValues ??= ByteConverterFactory.Get(Member.TypeDetail, $"{memberKey}_CollectedValues", null, SetterForConverterSetValues);
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