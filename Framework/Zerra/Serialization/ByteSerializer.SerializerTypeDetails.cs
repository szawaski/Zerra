// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using Zerra.Reflection;

namespace Zerra.Serialization
{

    public partial class ByteSerializer
    {
        private class SerializerTypeDetails
        {
            private readonly ByteSerializerIndexSize indexSize;
            private readonly bool ignoreIndexAttribute;

            public Type Type { get; private set; }
            public IReadOnlyDictionary<ushort, SerializerMemberDetails> IndexedProperties { get; private set; }
            public TypeDetail TypeDetail { get; private set; }
            public Func<int, object> ListCreator { get; private set; }

            private SerializerTypeDetails innerTypeDetails = null;
            public SerializerTypeDetails InnerTypeDetail
            {
                get
                {
                    if (innerTypeDetails == null)
                    {
                        lock (this)
                        {
                            if (innerTypeDetails == null)
                            {
                                innerTypeDetails = GetTypeInformation(TypeDetail.IEnumerableGenericInnerType, indexSize, ignoreIndexAttribute);
                            }
                        }
                    }
                    return innerTypeDetails;
                }
            }

            public SerializerTypeDetails(ByteSerializerIndexSize indexSize, bool ignoreIndexAttribute, Type type)
            {
                this.indexSize = indexSize;
                this.ignoreIndexAttribute = ignoreIndexAttribute;
                this.Type = type;

                this.TypeDetail = TypeAnalyzer.GetTypeDetail(type);

                var listTypeDetail = TypeAnalyzer.GetGenericTypeDetail(genericListType, type);
                var listCreator = listTypeDetail.GetConstructor(typeof(int)).Creator;
                this.ListCreator = (length) => { return listCreator(new object[] { length }); };

                if (!this.Type.IsEnum && !this.TypeDetail.CoreType.HasValue && !this.TypeDetail.SpecialType.HasValue && !this.TypeDetail.IsNullable && !this.TypeDetail.IsIEnumerableGeneric)
                {
                    var memberSets = new List<Tuple<MemberDetail, SerializerIndexAttribute, NonSerializedAttribute>>();
                    foreach (var member in this.TypeDetail.SerializableMemberDetails)
                    {
                        var indexAttribute = member.Attributes.Select(x => x as SerializerIndexAttribute).Where(x => x != null).FirstOrDefault();
                        var nonSerializedAttribute = member.Attributes.Select(x => x as NonSerializedAttribute).Where(x => x != null).FirstOrDefault();
                        memberSets.Add(new Tuple<MemberDetail, SerializerIndexAttribute, NonSerializedAttribute>(member, indexAttribute, nonSerializedAttribute));
                    }

                    var indexProperties = new Dictionary<ushort, SerializerMemberDetails>();

                    if (!ignoreIndexAttribute)
                    {
                        var membersWithIndexes = memberSets.Where(x => x.Item2 != null && x.Item3 == null).ToArray();
                        if (membersWithIndexes.Length > 0)
                        {
                            foreach (var member in membersWithIndexes)
                            {
                                switch (indexSize)
                                {
                                    case ByteSerializerIndexSize.Byte:
                                        if (member.Item2.Index > Byte.MaxValue - IndexOffset)
                                            throw new Exception("Index attribute too large for the index size");
                                        break;
                                    case ByteSerializerIndexSize.UInt16:
                                        if (member.Item2.Index > UInt16.MaxValue - IndexOffset)
                                            throw new Exception("Index attribute too large for the index size");
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                var index = (ushort)(member.Item2.Index + IndexOffset);

                                indexProperties.Add(index, new SerializerMemberDetails(indexSize, ignoreIndexAttribute, member.Item1));
                            }
                        }
                    }

                    if (indexProperties.Count == 0)
                    {
                        var orderIndex = 0;
                        foreach (var member in memberSets.Where(x => x.Item3 == null))
                        {
                            switch (indexSize)
                            {
                                case ByteSerializerIndexSize.Byte:
                                    if (orderIndex > Byte.MaxValue - IndexOffset)
                                        throw new Exception("Index attribute too large for the index size");
                                    break;
                                case ByteSerializerIndexSize.UInt16:
                                    if (orderIndex > UInt16.MaxValue - IndexOffset)
                                        throw new Exception("Index attribute too large for the index size");
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                            var index = (ushort)(orderIndex + IndexOffset);

                            indexProperties.Add(index, new SerializerMemberDetails(indexSize, ignoreIndexAttribute, member.Item1));
                            orderIndex++;
                        }
                    }

                    this.IndexedProperties = indexProperties;
                }
            }
        }
    }
}