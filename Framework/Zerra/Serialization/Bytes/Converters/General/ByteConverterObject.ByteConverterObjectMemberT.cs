// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Reflection;

namespace Zerra.Serialization.Bytes.Converters.General
{
    internal sealed partial class ByteConverterObject<TParent, TValue>
    {
        private sealed class ByteConverterObjectMember<TValue2> : ByteConverterObjectMember
        {
            public ByteConverterObjectMember(TypeDetail parentTypeDetail, MemberDetail member, ushort index)
                : base(parentTypeDetail, member, index) { }

            private void SetterForConverterSetValues(Dictionary<string, object?> parent, TValue2? value) => parent.Add(Member.Name.TrimStart('_'), value);

            private ByteConverter<Dictionary<string, object?>>? converterSetValues;
            public override sealed ByteConverter<Dictionary<string, object?>> ConverterSetCollectedValues
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
        }
    }
}