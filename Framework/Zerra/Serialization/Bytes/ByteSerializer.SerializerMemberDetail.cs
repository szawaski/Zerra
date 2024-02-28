// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class ByteSerializer
    {
        private sealed class SerializerMemberDetail
        {
            private readonly ByteSerializerIndexSize indexSize;
            private readonly bool ignoreBinaryIndexAttribute;

            public string Name { get; }
            public Type Type { get; }
            public CoreType? CoreType { get; }

            private SerializerTypeDetail? serializerTypeDetails = null;
            public SerializerTypeDetail SerailzierTypeDetails
            {
                get
                {
                    if (serializerTypeDetails == null)
                    {
                        lock (this)
                        {
                            serializerTypeDetails ??= GetTypeInformation(Type, indexSize, ignoreBinaryIndexAttribute);
                        }
                    }
                    return serializerTypeDetails;
                }
            }

            public Func<object, object?> Getter { get; }
            public Action<object, object?> Setter { get; }

            public SerializerMemberDetail(ByteSerializerIndexSize indexSize, bool ignoreBinaryIndexAttribute, MemberDetail member)
            {
                this.indexSize = indexSize;
                this.ignoreBinaryIndexAttribute = ignoreBinaryIndexAttribute;

                this.Name = member.Name;
                this.Type = member.Type;
                this.CoreType = member.TypeDetail.CoreType;

                this.Getter = member.Getter;
                this.Setter = member.Setter;
            }

            public override string ToString()
            {
                return $"{Type.Name} {Name}";
            }
        }
    }
}