// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.General
{
    internal sealed partial class ByteConverterObject<TParent, TValue> : ByteConverter<TParent, TValue>
    {
        private static readonly ConcurrentStack<Dictionary<string, object?>> collectedValuesPool = new();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<string, object?> RentCollectedValues()
        {
            if (collectedValuesPool.TryPop(out var collectedValues))
                return collectedValues;
            return new(MemberNameComparer.Instance);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnCollectedValues(Dictionary<string, object?> collectedValues)
        {
            collectedValues.Clear();
            collectedValuesPool.Push(collectedValues);
        }

        private MemberKey[] members = null!;
        private MemberKey[] membersIngoreAttributes = null!;
        private readonly Dictionary<string, ByteConverterObjectMember> membersByName = new();

        private bool indexSizeUInt16Only;

        private bool collectValues;
        private ConstructorDetail<TValue>? parameterConstructor = null;

        protected override sealed void Setup()
        {
            var validMembers = new List<MemberDetail>();
            foreach (var member in typeDetail.SerializableMemberDetails)
            {
                if (member.Attributes.Any(x => x is NonSerializedAttribute))
                    continue;
                validMembers.Add(member);
            }

            var membersByIndex = new Dictionary<ushort, ByteConverterObjectMember>();
            var membersByIndexIngoreAttributes = new Dictionary<ushort, ByteConverterObjectMember>();

            //Members by Index with Attributes
            foreach (var member in validMembers)
            {
                var indexAttribute = member.Attributes.Select(x => x as SerializerIndexAttribute).Where(x => x is not null).FirstOrDefault();
                if (indexAttribute is null)
                    continue;
                var index = (ushort)(indexAttribute.Index + indexOffset);

                if (index > byte.MaxValue)
                    indexSizeUInt16Only = true;
                if (index > ushort.MaxValue)
                    throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has an {nameof(SerializerIndexAttribute)} index too high");

                var detail = ByteConverterObjectMember.New(typeDetail, member, index);
                membersByIndex.Add(index, detail);
            }

            //Members by Index and Name
            var hasAttributes = membersByIndex.Count > 0;
            var orderIndex = 0;
            foreach (var member in validMembers)
            {
                var index = (ushort)(orderIndex + indexOffset);

                if (index > byte.MaxValue)
                    indexSizeUInt16Only = true;
                if (index > ushort.MaxValue)
                    throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members to serialize");

                var detail = ByteConverterObjectMember.New(typeDetail, member, index);
                if (!hasAttributes)
                {
                    membersByIndex.Add(index, detail);
                }
                membersByIndexIngoreAttributes.Add(index, detail);
                membersByName.Add(member.Name, detail);

                orderIndex++;
            }

            members = membersByIndex.Select(x => new MemberKey(x.Key, x.Value)).ToArray();
            membersIngoreAttributes = membersByIndexIngoreAttributes.Select(x => new MemberKey(x.Key, x.Value)).ToArray();

            if (typeDetail.Type.IsValueType || !typeDetail.HasCreator)
            {
                //find best constructor
                foreach (var constructor in typeDetail.ConstructorDetails.OrderByDescending(x => x.ParameterDetails.Count))
                {
                    var skip = false;
                    foreach (var parameter in constructor.ParameterDetails)
                    {
                        //cannot have argument of itself or a null name
                        if (parameter.Type == typeDetail.Type || parameter.Name is null)
                        {
                            skip = true;
                            break;
                        }
                        //must have a matching a member
                        if (!membersByName.Values.Any(x => x.Member.Type == parameter.Type && MemberNameComparer.Instance.Equals(x.Member.Name, parameter.Name)))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip)
                        continue;
                    parameterConstructor = constructor;
                    break;
                }
                collectValues = parameterConstructor is not null;
            }
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue? value)
        {
            if (indexSizeUInt16Only && !state.UseIndexSizeUInt16 && !state.UseMemberNames)
                throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members or {nameof(SerializerIndexAttribute)} index too high for index size");

            Dictionary<string, object?>? collectedValues;

            if (!state.Current.HasCreated)
            {
                if (!state.Current.DrainBytes)
                {
                    if (collectValues)
                    {
                        value = default;
                        collectedValues = RentCollectedValues();
                    }
                    else if (typeDetail.HasCreator)
                    {
                        value = typeDetail.Creator();
                        collectedValues = null;
                    }
                    else
                    {
                        value = default;
                        collectedValues = null;
                    }
                }
                else
                {
                    value = default;
                    collectedValues = null;
                }

                state.Current.HasCreated = true;
            }
            else
            {
                if (collectValues)
                {
                    value = default;
                    collectedValues = (Dictionary<string, object?>)state.Current.Object!;
                }
                else
                {
                    value = (TValue?)state.Current.Object;
                    collectedValues = null;
                }
            }

            for (; ; )
            {
                ByteConverterObjectMember? member;
                if (!state.Current.HasReadProperty)
                {
                    if (state.UseMemberNames)
                    {
                        if (!reader.TryRead(out ReadOnlySpan<byte> name, out state.SizeNeeded))
                        {
                            if (collectValues)
                                state.Current.Object = collectedValues;
                            else
                                state.Current.Object = value;
                            return false;
                        }

                        if (name.Length == 0)
                        {
                            break;
                        }

                        MemberKey memberKey;
                        var memberLength = members.Length;
                        var nextIndex = state.Current.EnumeratorIndex;
                        var prevIndex = state.Current.EnumeratorIndex - 1;

                        for (; ; )
                        {
                            if (nextIndex < memberLength)
                            {
                                memberKey = members[nextIndex];
                                if (MemberKey.IsEqual(memberKey, name))
                                {
                                    member = memberKey.Member;
                                    state.Current.EnumeratorIndex = nextIndex + 1;
                                    break;
                                }

                                if (prevIndex >= 0)
                                {
                                    memberKey = members[prevIndex];
                                    if (MemberKey.IsEqual(memberKey, name))
                                    {
                                        member = memberKey.Member;
                                        state.Current.EnumeratorIndex = prevIndex + 1;
                                        break;
                                    }
                                }

                                prevIndex--;
                                nextIndex++;
                            }
                            else if (prevIndex >= 0)
                            {
                                memberKey = members[prevIndex];
                                if (MemberKey.IsEqual(memberKey, name))
                                {
                                    member = memberKey.Member;
                                    state.Current.EnumeratorIndex = prevIndex + 1;
                                    break;
                                }

                                prevIndex--;
                            }
                            else
                            {
                                member = null;
                                break;
                            }
                        }
                    }
                    else
                    {
                        ushort propertyIndex;
                        if (state.UseIndexSizeUInt16)
                        {
                            if (!reader.TryRead(out propertyIndex, out state.SizeNeeded))
                            {
                                if (collectValues)
                                    state.Current.Object = collectedValues;
                                else
                                    state.Current.Object = value;
                                return false;
                            }
                        }
                        else
                        {
                            if (!reader.TryRead(out byte propertyIndexValue, out state.SizeNeeded))
                            {
                                if (collectValues)
                                    state.Current.Object = collectedValues;
                                else
                                    state.Current.Object = value;
                                return false;
                            }
                            propertyIndex = propertyIndexValue;
                        }

                        if (propertyIndex == endObjectFlagUShort)
                        {
                            break;
                        }

                        MemberKey memberKey;
                        var selectedMembers = state.IgnoreIndexAttribute ? membersIngoreAttributes : members;
                        var memberLength = members.Length;
                        var nextIndex = state.Current.EnumeratorIndex;
                        var prevIndex = state.Current.EnumeratorIndex - 1;

                        for (; ; )
                        {
                            if (nextIndex < memberLength)
                            {
                                memberKey = selectedMembers[nextIndex];
                                if (memberKey.Index == propertyIndex)
                                {
                                    member = memberKey.Member;
                                    state.Current.EnumeratorIndex = nextIndex + 1;
                                    break;
                                }

                                if (prevIndex >= 0)
                                {
                                    memberKey = selectedMembers[prevIndex];
                                    if (memberKey.Index == propertyIndex)
                                    {
                                        member = memberKey.Member;
                                        state.Current.EnumeratorIndex = prevIndex + 1;
                                        break;
                                    }
                                }

                                prevIndex--;
                                nextIndex++;
                            }
                            else if (prevIndex >= 0)
                            {
                                memberKey = selectedMembers[prevIndex];
                                if (memberKey.Index == propertyIndex)
                                {
                                    member = memberKey.Member;
                                    state.Current.EnumeratorIndex = prevIndex + 1;
                                    break;
                                }

                                prevIndex--;
                            }
                            else
                            {
                                member = null;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    member = (ByteConverterObjectMember?)state.Current.Property;
                }

                if (member is null)
                {
                    if (!state.UseMemberNames && !state.UseTypes)
                        throw new Exception($"Cannot deserialize with property undefined and no types.");

                    //consume bytes but object does not have property
                    var converter = ByteConverterFactory<TValue>.GetDrainBytes();
                    if (!converter.TryReadFromParent(ref reader, ref state, default, false, true))
                    {
                        state.Current.HasReadProperty = true;
                        state.Current.Property = member;
                        if (collectValues)
                            state.Current.Object = collectedValues;
                        else
                            state.Current.Object = value;
                        return false;
                    }
                }
                else
                {
                    if (collectValues)
                    {
                        if (!member.ConverterSetCollectedValues.TryReadFromParent(ref reader, ref state, collectedValues, false))
                        {
                            state.Current.HasReadProperty = true;
                            state.Current.Property = member;
                            if (collectValues)
                                state.Current.Object = collectedValues;
                            else
                                state.Current.Object = value;
                            return false;
                        }
                    }
                    else
                    {
                        if (!member.Converter.TryReadFromParent(ref reader, ref state, value, false))
                        {
                            state.Current.HasReadProperty = true;
                            state.Current.Property = member;
                            if (collectValues)
                                state.Current.Object = collectedValues;
                            else
                                state.Current.Object = value;
                            return false;
                        }
                    }
                }

                state.Current.HasReadProperty = false;
            }

            if (collectValues)
            {
                var args = new object?[parameterConstructor!.ParameterDetails.Count];
                for (var i = 0; i < args.Length; i++)
                {
#if NETSTANDARD2_0
                    if (collectedValues!.TryGetValue(parameterConstructor.ParameterDetails[i].Name!, out var parameter))
                    {
                        collectedValues.Remove(parameterConstructor.ParameterDetails[i].Name!);
                        args[i] = parameter;
                    }
#else
                    if (collectedValues!.Remove(parameterConstructor.ParameterDetails[i].Name!, out var parameter))
                        args[i] = parameter;
#endif
                }
                if (typeDetail.Type.IsValueType)
                {
                    value = (TValue?)parameterConstructor.CreatorWithArgsBoxed(args);
                }
                else
                {
                    value = parameterConstructor.CreatorWithArgs(args);
                }

                foreach (var remaining in collectedValues!)
                {
                    if (membersByName!.TryGetValue(remaining.Key, out var member))
                    {
                        member.Converter.CollectedValuesSetter(value!, remaining.Value);
                    }
                }

                ReturnCollectedValues(collectedValues!);
            }

            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TValue value)
        {
            if (indexSizeUInt16Only && !state.UseIndexSizeUInt16 && !state.UseMemberNames)
                throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members or {nameof(SerializerIndexAttribute)} index too high for index size");

            MemberKey[] enumerator;
            if (state.IgnoreIndexAttribute)
                enumerator = membersIngoreAttributes;
            else
                enumerator = members;

            while (state.Current.EnumeratorIndex < enumerator.Length)
            {
                var current = enumerator[state.Current.EnumeratorIndex].Member;
                //Base will write the property name or index if the value is not null.
                //Done this way so we don't have to extract the value twice due to null checking.
                if (!current.Converter.TryWriteFromParent(ref writer, ref state, value, false, current.Index, state.UseMemberNames ? current.NameAsBytes : null))
                    return false;

                state.Current.EnumeratorIndex++;
            }

            if (state.UseMemberNames)
            {
                if (!writer.TryWrite(0, out state.BytesNeeded))
                    return false;
            }
            else
            {
                if (state.UseIndexSizeUInt16)
                {
                    if (!writer.TryWriteRaw(endObjectFlagUInt16, out state.BytesNeeded))
                        return false;
                }
                else
                {
                    if (!writer.TryWrite(endObjectFlagByte, out state.BytesNeeded))
                        return false;
                }
            }

            return true;
        }
    }
}