// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using Zerra.Reflection;

namespace Zerra.Serialization.Bytes
{
    public static partial class ByteSerializerOld
    {
        private sealed class SerializerTypeDetail
        {
            private readonly ByteSerializerIndexSize indexSize;
            private readonly bool ignoreIndexAttribute;

            public Type Type { get; }
            public IReadOnlyDictionary<ushort, SerializerMemberDetail>? PropertiesByIndex { get; }
            public IReadOnlyDictionary<string, SerializerMemberDetail>? PropertiesByName { get; }
            public TypeDetail TypeDetail { get; }
            public Func<int, object> ListCreator { get; }
            public MethodDetail ListAdder { get; }
            public Func<int, object> HashSetCreator { get; }
            public MethodDetail HashSetAdder { get; }

            private SerializerTypeDetail? innerTypeDetails = null;
            public SerializerTypeDetail InnerTypeDetail
            {
                get
                {
                    if (innerTypeDetails is null)
                    {
                        lock (this)
                        {
                            innerTypeDetails ??= GetTypeInformation(TypeDetail.IEnumerableGenericInnerType, indexSize, ignoreIndexAttribute);
                        }
                    }
                    return innerTypeDetails;
                }
            }

            public SerializerTypeDetail(ByteSerializerIndexSize indexSize, bool ignoreIndexAttribute, Type type)
            {
                this.indexSize = indexSize;
                this.ignoreIndexAttribute = ignoreIndexAttribute;
                this.Type = type;

                this.TypeDetail = TypeAnalyzer.GetTypeDetail(type);

                var listTypeDetail = TypeAnalyzer.GetGenericTypeDetail(genericListType, type);
                this.ListAdder = listTypeDetail.GetMethodBoxed("Add");
                var listCreator = listTypeDetail.GetConstructorBoxed([typeof(int)]).CreatorWithArgsBoxed;
                this.ListCreator = (length) => { return listCreator([length]); };

                var hashSetTypeDetail = TypeAnalyzer.GetGenericTypeDetail(genericHashSetType, type);
                this.HashSetAdder = hashSetTypeDetail.GetMethodBoxed("Add");
                var hashSetCreator = hashSetTypeDetail.GetConstructorBoxed([typeof(int)]).CreatorWithArgsBoxed;
                this.HashSetCreator = (length) => { return hashSetCreator([length]); };

                if (!this.Type.IsEnum && !this.TypeDetail.CoreType.HasValue && !this.TypeDetail.SpecialType.HasValue && !this.TypeDetail.IsNullable && !this.TypeDetail.HasIEnumerableGeneric)
                {
                    var memberSets = new List<Tuple<MemberDetail, SerializerIndexAttribute?, NonSerializedAttribute?>>();
                    foreach (var member in this.TypeDetail.SerializableMemberDetails)
                    {
                        var indexAttribute = member.Attributes.Select(x => x as SerializerIndexAttribute).Where(x => x is not null).FirstOrDefault();
                        var nonSerializedAttribute = member.Attributes.Select(x => x as NonSerializedAttribute).Where(x => x is not null).FirstOrDefault();
                        memberSets.Add(new Tuple<MemberDetail, SerializerIndexAttribute?, NonSerializedAttribute?>(member, indexAttribute, nonSerializedAttribute));
                    }

                    var propertiesByIndex = new Dictionary<ushort, SerializerMemberDetail>();
                    var propertiesByName = new Dictionary<string, SerializerMemberDetail>();

                    if (!ignoreIndexAttribute)
                    {
                        var membersWithIndexes = memberSets.Where(x => x.Item2 is not null && x.Item3 is null).ToArray();
                        if (membersWithIndexes.Length > 0)
                        {
                            foreach (var member in membersWithIndexes)
                            {
                                switch (indexSize)
                                {
                                    case ByteSerializerIndexSize.Byte:
                                        if (member.Item2!.Index > Byte.MaxValue - indexOffset)
                                            throw new Exception("Index attribute too large for the index size");
                                        break;
                                    case ByteSerializerIndexSize.UInt16:
                                        if (member.Item2!.Index > UInt16.MaxValue - indexOffset)
                                            throw new Exception("Index attribute too large for the index size");
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                                var index = (ushort)(member.Item2.Index + indexOffset);

                                var detail = new SerializerMemberDetail(indexSize, ignoreIndexAttribute, member.Item1);
                                propertiesByIndex.Add(index, detail);
                                propertiesByName.Add(member.Item1.Name, detail);
                            }
                        }
                    }

                    if (propertiesByIndex.Count == 0)
                    {
                        var orderIndex = 0;
                        foreach (var member in memberSets.Where(x => x.Item3 is null))
                        {
                            switch (indexSize)
                            {
                                case ByteSerializerIndexSize.Byte:
                                    if (orderIndex > Byte.MaxValue - indexOffset)
                                        throw new Exception("Index attribute too large for the index size");
                                    break;
                                case ByteSerializerIndexSize.UInt16:
                                    if (orderIndex > UInt16.MaxValue - indexOffset)
                                        throw new Exception("Index attribute too large for the index size");
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                            var index = (ushort)(orderIndex + indexOffset);

                            var detail = new SerializerMemberDetail(indexSize, ignoreIndexAttribute, member.Item1);
                            propertiesByIndex.Add(index, detail);
                            propertiesByName.Add(member.Item1.Name, detail);

                            orderIndex++;
                        }
                    }

                    this.PropertiesByIndex = propertiesByIndex;
                    this.PropertiesByName = propertiesByName;
                }
            }
        }
    }
}