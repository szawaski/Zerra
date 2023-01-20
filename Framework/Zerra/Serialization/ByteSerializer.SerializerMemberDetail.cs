// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public partial class ByteSerializer
    {
        private class SerializerMemberDetail
        {
            private readonly ByteSerializerIndexSize indexSize;
            private readonly bool ignoreBinaryIndexAttribute;

            public string Name { get; private set; }
            public Type Type { get; private set; }
            public CoreType? CoreType { get; private set; }

            private SerializerTypeDetail serializerTypeDetails = null;
            public SerializerTypeDetail SerailzierTypeDetails
            {
                get
                {
                    if (serializerTypeDetails == null)
                    {
                        lock (this)
                        {
                            if (serializerTypeDetails == null)
                            {
                                serializerTypeDetails = GetTypeInformation(Type, indexSize, ignoreBinaryIndexAttribute);
                            }
                        }
                    }
                    return serializerTypeDetails;
                }

            }

            public Func<object, object> Getter { get; private set; }
            public Action<object, object> Setter { get; private set; }

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