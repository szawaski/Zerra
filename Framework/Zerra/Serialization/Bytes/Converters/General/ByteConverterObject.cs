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

        private readonly Dictionary<ushort, ByteConverterObjectMember> membersByIndex = new();
        private readonly Dictionary<ushort, ByteConverterObjectMember> membersByIndexIngoreAttributes = new();
        private readonly Dictionary<string, ByteConverterObjectMember> membersByName = new();
        private bool indexSizeUInt16Only;

        private bool collectValues;
        private ConstructorDetail<TValue>? parameterConstructor = null;

        protected override sealed void Setup()
        {
            var memberSets = new List<Tuple<MemberDetail, SerializerIndexAttribute?>>();
            foreach (var member in typeDetail.SerializableMemberDetails)
            {
                if (member.Attributes.Any(x => x is NonSerializedAttribute))
                    continue;
                var indexAttribute = member.Attributes.Select(x => x as SerializerIndexAttribute).Where(x => x != null).FirstOrDefault();
                memberSets.Add(new Tuple<MemberDetail, SerializerIndexAttribute?>(member, indexAttribute));
            }

            //Members by Index with Attributes
            foreach (var member in memberSets.Where(x => x.Item2 != null))
            {
                if (member.Item2?.Index > byte.MaxValue - indexOffset)
                    indexSizeUInt16Only = true;
                if (member.Item2?.Index > ushort.MaxValue - indexOffset)
                    throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members to serialize");

                var index = (ushort)(member.Item2!.Index + indexOffset);

                var detail = ByteConverterObjectMember.New(typeDetail, member.Item1);
                membersByIndex.Add(index, detail);
            }

            //Members by Index and Name
            var hasAttributes = membersByIndex.Count > 0;
            var orderIndex = 0;
            foreach (var member in memberSets)
            {
                if (member.Item2?.Index > byte.MaxValue - indexOffset)
                    indexSizeUInt16Only = true;
                if (member.Item2?.Index > ushort.MaxValue - indexOffset)
                    throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members to serialize");

                var index = (ushort)(orderIndex + indexOffset);

                var detail = ByteConverterObjectMember.New(typeDetail, member.Item1);
                if (!hasAttributes)
                    membersByIndex.Add(index, detail);
                membersByIndexIngoreAttributes.Add(index, detail);
                membersByName.Add(member.Item1.Name, detail);

                orderIndex++;
            }

            if (typeDetail.Type.IsValueType || !typeDetail.HasCreator)
            {
                //find best constructor
                foreach (var constructor in typeDetail.ConstructorDetails.OrderByDescending(x => x.ParametersInfo.Count))
                {
                    var skip = false;
                    foreach (var parameter in constructor.ParametersInfo)
                    {
                        //cannot have argument of itself or a null name
                        if (parameter.ParameterType == typeDetail.Type || parameter.Name == null)
                        {
                            skip = true;
                            break;
                        }
                        //must have a matching a member
                        if (!membersByName.Values.Any(x => x.Member.Type == parameter.ParameterType && MemberNameComparer.Instance.Equals(x.Member.Name, parameter.Name)))
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
                collectValues = parameterConstructor != null;
            }
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TValue? value)
        {
            if (indexSizeUInt16Only && !state.IndexSizeUInt16 && !state.UsePropertyNames)
                throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members for index size");

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
                ByteConverterObjectMember? property;
                if (!state.Current.HasReadProperty)
                {
                    if (state.UsePropertyNames)
                    {
                        if (!reader.TryRead(out string? name, out state.BytesNeeded))
                        {
                            if (collectValues)
                                state.Current.Object = collectedValues;
                            else
                                state.Current.Object = value;
                            return false;
                        }

                        if (name == String.Empty)
                        {
                            break;
                        }

                        property = null;
                        _ = membersByName?.TryGetValue(name!, out property);
                    }
                    else
                    {
                        ushort propertyIndex;
                        if (state.IndexSizeUInt16)
                        {
                            if (!reader.TryRead(out propertyIndex, out state.BytesNeeded))
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
                            if (!reader.TryRead(out byte propertyIndexValue, out state.BytesNeeded))
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

                        property = null;
                        if (state.IgnoreIndexAttribute)
                            _ = membersByIndexIngoreAttributes?.TryGetValue(propertyIndex, out property);
                        else
                            _ = membersByIndex?.TryGetValue(propertyIndex, out property);
                    }
                }
                else
                {
                    property = (ByteConverterObjectMember?)state.Current.Property;
                }

                if (property is null)
                {
                    if (!state.UsePropertyNames && !state.UseTypes)
                        throw new Exception($"Cannot deserialize with property undefined and no types.");

                    //consume bytes but object does not have property
                    var converter = ByteConverterFactory<TValue>.GetTypeRequired();
                    var read = converter.TryReadFromParent(ref reader, ref state, default, false, true);
                    if (!read)
                    {
                        state.Current.HasReadProperty = true;
                        state.Current.Property = property;
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
                        var read = property.ConverterSetCollectedValues.TryReadFromParent(ref reader, ref state, collectedValues, false);
                        if (!read)
                        {
                            state.Current.HasReadProperty = true;
                            state.Current.Property = property;
                            if (collectValues)
                                state.Current.Object = collectedValues;
                            else
                                state.Current.Object = value;
                            return false;
                        }
                    }
                    else
                    {
                        var read = property.Converter.TryReadFromParent(ref reader, ref state, value, false);
                        if (!read)
                        {
                            state.Current.HasReadProperty = true;
                            state.Current.Property = property;
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
                var args = new object?[parameterConstructor!.ParametersInfo.Count];
                for (var i = 0; i < args.Length; i++)
                {
#if NETSTANDARD2_0
                    if (collectedValues!.TryGetValue(parameterConstructor.ParametersInfo[i].Name!, out var parameter))
                    {
                        collectedValues.Remove(parameterConstructor.ParametersInfo[i].Name!);
                        args[i] = parameter;
                    }
#else
                    if (collectedValues!.Remove(parameterConstructor.ParametersInfo[i].Name!, out var parameter))
                        args[i] = parameter;
#endif
                }
                if (typeDetail.Type.IsValueType)
                {
                    value = (TValue?)parameterConstructor.CreatorBoxed(args);
                }
                else
                {
                    value = parameterConstructor.Creator(args);
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

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TValue value)
        {
            if (indexSizeUInt16Only && !state.IndexSizeUInt16 && !state.UsePropertyNames)
                throw new NotSupportedException($"{typeDetail.Type.GetNiceName()} has too many members for index size");

            if (value is null) throw new InvalidOperationException($"{nameof(ByteSerializer)} should not be in this state");

            IEnumerator<KeyValuePair<ushort, ByteConverterObjectMember>> enumerator;
            if (state.Current.Enumerator == null)
            {
                if (state.IgnoreIndexAttribute)
                    enumerator = membersByIndexIngoreAttributes.GetEnumerator();
                else
                    enumerator = membersByIndex.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<KeyValuePair<ushort, ByteConverterObjectMember>>)state.Current.Enumerator;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                //Base will write the property name or index if the value is not null.
                //Done this way so we don't have to extract the value twice due to null checking.
                var write = enumerator.Current.Value.Converter.TryWriteFromParent(ref writer, ref state, value, false, enumerator.Current.Key, enumerator.Current.Value.Member.Name);

                if (!write)
                {
                    state.Current.Enumerator = enumerator;
                    state.Current.EnumeratorInProgress = true;
                    return false;
                }

                state.Current.EnumeratorInProgress = false;
            }

            if (state.UsePropertyNames)
            {
                if (!writer.TryWrite(0, out state.BytesNeeded))
                {
                    state.Current.Enumerator = enumerator;
                    return false;
                }
            }
            else
            {
                if (state.IndexSizeUInt16)
                {
                    if (!writer.TryWrite(endObjectFlagUInt16, out state.BytesNeeded))
                    {
                        state.Current.Enumerator = enumerator;
                        return false;
                    }
                }
                else
                {
                    if (!writer.TryWrite(endObjectFlagByte, out state.BytesNeeded))
                    {
                        state.Current.Enumerator = enumerator;
                        return false;
                    }
                }
            }

            return true;
        }
    }
}