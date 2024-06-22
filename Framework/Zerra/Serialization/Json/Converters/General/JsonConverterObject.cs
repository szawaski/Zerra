﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zerra.Reflection;
using Zerra.IO;
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

        protected override sealed bool TryReadValue(ref CharReader reader, ref ReadState state, out TValue? value)
        {
            if (state.Current.ValueType != JsonValueType.Object)
            {
                if (!Drain(ref reader, ref state))
                {
                    value = default;
                    return false;
                }
                value = default;
                return true;
            }

            Dictionary<string, object?>? collectedValues;

            if (!state.Current.HasCreated)
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

            char c;
            for (; ; )
            {
                JsonConverterObjectMember? property;
                if (!state.Current.HasReadProperty)
                {
                    if (!reader.TryReadSkipWhiteSpace(out c))
                    {
                        state.CharsNeeded = 1;
                        return false;
                    }

                    if (c == '}')
                        break;

                    if (c != '"')
                        throw reader.CreateException("Unexpected character");

                    if (!ReadString(ref reader, ref state, out var name))
                        return false;

                    if (String.IsNullOrWhiteSpace(name))
                        throw reader.CreateException("Unexpected character");

                    property = null;
                    _ = membersByName?.TryGetValue(name!, out property);
                }
                else
                {
                    property = (JsonConverterObjectMember?)state.Current.Property;
                }

                if (!state.Current.HasReadPropertySeperator)
                {
                    if (!reader.TryReadSkipWhiteSpace(out c))
                    {
                        state.Current.HasReadProperty = true;
                        state.CharsNeeded = 1;
                        return false;
                    }
                    if (c != ':')
                        throw reader.CreateException("Unexpected character");
                }

                if (!state.Current.HasReadPropertyValue)
                {
                    if (property is null)
                    {
                        state.PushFrame();
                        if (!Drain(ref reader, ref state))
                        {
                            state.Current.HasReadProperty = true;
                            state.Current.HasReadPropertySeperator = true;
                            state.Current.Property = property;
                            return false;
                        }
                    }
                    else
                    {
                        if (collectValues)
                        {
                            state.PushFrame();
                            if (!property.ConverterSetCollectedValues.TryReadFromParent(ref reader, ref state, collectedValues))
                            {
                                state.Current.HasReadProperty = true;
                                state.Current.HasReadPropertySeperator = true;
                                state.Current.Property = property;
                                return false;
                            }
                        }
                        else
                        {
                            state.PushFrame();
                            if (!property.Converter.TryReadFromParent(ref reader, ref state, value))
                            {
                                state.Current.HasReadProperty = true;
                                state.Current.HasReadPropertySeperator = true;
                                state.Current.Property = property;
                                return false;
                            }
                        }
                    }
                }

                if (!reader.TryReadSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.HasReadProperty = true;
                    state.Current.HasReadPropertySeperator = true;
                    state.Current.HasReadPropertyValue = true;
                    return false;
                }

                if (c == '}')
                    break;

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadProperty = false;
                state.Current.HasReadPropertySeperator = false;
                state.Current.HasReadPropertyValue = false;
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

        protected override sealed bool TryWriteValue(ref CharWriter writer, ref WriteState state, TValue? value)
        {
            throw new NotImplementedException();
        }
    }
}