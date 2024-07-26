// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed partial class JsonConverterObject<TParent, TValue> : JsonConverter<TParent, TValue>
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

        private readonly Dictionary<string, JsonConverterObjectMember> membersByName = new();

        private bool collectValues;
        private ConstructorDetail<TValue>? parameterConstructor = null;

        protected override sealed void Setup()
        {
            foreach (var member in typeDetail.SerializableMemberDetails)
            {
                var detail = JsonConverterObjectMember.New(typeDetail, member);
                membersByName.Add(member.Name, detail);
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

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out TValue? value)
        {
            if (valueType != JsonValueType.Object)
            {
                if (state.ErrorOnTypeMismatch)
                    throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");

                value = default;
                return Drain(ref reader, ref state, valueType);
            }

            Dictionary<string, object?>? collectedValues;
            char c;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    value = default;
                    return false;
                }

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

                if (c == '}')
                    return true;

                reader.BackOne();

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
                JsonConverterObjectMember? property;
                if (!state.Current.HasReadProperty)
                {
                    if (!ReadString(ref reader, ref state, false, out var name))
                    {
                        if (collectValues)
                            state.Current.Object = collectedValues;
                        else
                            state.Current.Object = value;
                        return false;
                    }

                    if (String.IsNullOrWhiteSpace(name))
                        throw reader.CreateException("Unexpected character");

                    property = null;
                    _ = membersByName?.TryGetValue(name!, out property);
                }
                else
                {
                    property = (JsonConverterObjectMember?)state.Current.Property;
                }

                if (!state.Current.HasReadSeperator)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        state.Current.HasReadProperty = true;
                        state.Current.Property = property;
                        if (collectValues)
                            state.Current.Object = collectedValues;
                        else
                            state.Current.Object = value;
                        return false;
                    }
                    if (c != ':')
                        throw reader.CreateException("Unexpected character");
                }

                if (!state.Current.HasReadValue)
                {
                    if (property is null)
                    {
                        if (!DrainFromParent(ref reader, ref state))
                        {
                            state.Current.HasReadProperty = true;
                            state.Current.Property = property;
                            state.Current.HasReadSeperator = true;
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
                            if (!property.ConverterSetCollectedValues.TryReadFromParent(ref reader, ref state, collectedValues))
                            {
                                state.Current.HasReadProperty = true;
                                state.Current.HasReadSeperator = true;
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
                            if (!property.Converter.TryReadFromParent(ref reader, ref state, value))
                            {
                                state.Current.HasReadProperty = true;
                                state.Current.HasReadSeperator = true;
                                state.Current.Property = property;
                                if (collectValues)
                                    state.Current.Object = collectedValues;
                                else
                                    state.Current.Object = value;
                                return false;
                            }
                        }
                    }
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.HasReadProperty = true;
                    state.Current.HasReadSeperator = true;
                    state.Current.HasReadValue = true;
                    if (collectValues)
                        state.Current.Object = collectedValues;
                    else
                        state.Current.Object = value;
                    return false;
                }

                if (c == '}')
                    break;

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadProperty = false;
                state.Current.HasReadSeperator = false;
                state.Current.HasReadValue = false;
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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, TValue value)
        {
            if (membersByName.Count == 0)
            {
                if (!writer.TryWriteEmptyBrace(out state.CharsNeeded))
                {
                    return false;
                }
                return true;
            }

            IEnumerator<KeyValuePair<string, JsonConverterObjectMember>> enumerator;
            if (!state.Current.HasWrittenStart)
            {
                if (!writer.TryWriteOpenBrace(out state.CharsNeeded))
                {
                    return false;
                }
                enumerator = membersByName.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<KeyValuePair<string, JsonConverterObjectMember>>)state.Current.Enumerator!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                if (!enumerator.Current.Value.Converter.TryWriteFromParent(ref writer, ref state, value, enumerator.Current.Key, false))
                {
                    state.Current.HasWrittenStart = true;
                    state.Current.Enumerator = enumerator;
                    return false;
                }

                if (state.Current.EnumeratorInProgress)
                    state.Current.EnumeratorInProgress = false;
            }

            if (!writer.TryWriteCloseBrace(out state.CharsNeeded))
            {
                state.Current.HasWrittenStart = true;
                return false;
            }
            return true;
        }
    }
}