// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Zerra.Reflection;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.Converters.General
{
    internal sealed partial class JsonConverterObject<TValue> : JsonConverter<TValue>
    {
        private static readonly ConcurrentStack<Dictionary<string, object?>> collectedValuesPool = new();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<string, object?> RentCollectedValues()
        {
            if (collectedValuesPool.TryPop(out var collectedValues))
                return collectedValues;
            return new(MemberAndParameterNameComparer.Instance);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnCollectedValues(Dictionary<string, object?> collectedValues)
        {
            collectedValues.Clear();
            collectedValuesPool.Push(collectedValues);
        }

        private readonly Dictionary<string, JsonConverterObjectMember> membersByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<JsonConverterObjectMember> members = new();
        private MemberKey[] membersKeyed = null!;

        private bool collectValues;
        private ConstructorDetail<TValue>? parameterConstructor = null;

        protected override sealed void Setup()
        {
            foreach (var member in TypeDetail.SerializableMemberDetails)
            {
                var found = false;
                var ignoreCondition = JsonIgnoreCondition.Never;
                foreach (var attribute in member.Attributes)
                {
                    if (attribute is JsonIgnoreAttribute jsonIgnore)
                    {
                        ignoreCondition = jsonIgnore.Condition;
                    }
                    else if (attribute is System.Text.Json.Serialization.JsonIgnoreAttribute jsonIgnore2)
                    {
#if NET5_0_OR_GREATER
                        switch (jsonIgnore2.Condition)
                        {
                            case System.Text.Json.Serialization.JsonIgnoreCondition.Never:
                                ignoreCondition = JsonIgnoreCondition.Never;
                                break;
                            case System.Text.Json.Serialization.JsonIgnoreCondition.Always:
                                ignoreCondition = JsonIgnoreCondition.Always;
                                break;
                            case System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault:
                                ignoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                                break;
                            case System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull:
                                ignoreCondition = JsonIgnoreCondition.WhenWritingNull;
                                break;
                            default:
                                ignoreCondition = JsonIgnoreCondition.Always;
                                break;
                        }
#else
                        ignoreCondition = JsonIgnoreCondition.Always;
#endif
                    }
                    else if (attribute is NonSerializedAttribute)
                    {
                        ignoreCondition = JsonIgnoreCondition.Always;
                    }

                    if (ignoreCondition == JsonIgnoreCondition.Always)
                        break;

                    if (attribute is JsonPropertyNameAttribute jsonPropertyName)
                    {
                        var detail = new JsonConverterObjectMember(TypeDetail, member, jsonPropertyName.Name, ignoreCondition);
                        membersByName.Add(jsonPropertyName.Name, detail);
                        members.Add(detail);
                        found = true;
                        break;
                    }
                    else if (attribute is System.Text.Json.Serialization.JsonPropertyNameAttribute jsonPropertyName2)
                    {
                        var detail = new JsonConverterObjectMember(TypeDetail, member, jsonPropertyName2.Name, ignoreCondition);
                        membersByName.Add(jsonPropertyName2.Name, detail);
                        members.Add(detail);
                        found = true;
                        break;
                    }
                }
                if (ignoreCondition == JsonIgnoreCondition.Always)
                    continue;

                if (!found)
                {
                    var detail = new JsonConverterObjectMember(TypeDetail, member, member.Name, ignoreCondition);
                    membersByName.Add(member.Name, detail);
                    members.Add(detail);
                }

                membersKeyed = members.Select(x => new MemberKey(x)).ToArray();
            }

            if (TypeDetail.Type.IsValueType || !TypeDetail.HasCreator)
            {
                //find best constructor
                foreach (var constructor in TypeDetail.Constructors.OrderByDescending(x => x.Parameters.Count))
                {
                    var skip = false;
                    foreach (var parameter in constructor.Parameters)
                    {
                        //cannot have argument of itself or a null name
                        if (parameter.Type == TypeDetail.Type || parameter.Name is null)
                        {
                            skip = true;
                            break;
                        }
                        //must have a matching a member
                        if (!members.Any(x => x.Member.Type == parameter.Type && MemberAndParameterNameComparer.Instance.Equals(x.Member.Name, parameter.Name)))
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

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out TValue? value)
        {
            Dictionary<string, object?>? collectedValues;

            if (state.Nameless)
            {
                if (token != JsonToken.ArrayStart)
                {
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);

                    value = default;
                    return Drain(ref reader, ref state, token);
                }

                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    state.Current.HasReadFirstToken = true;

                    if (collectValues)
                    {
                        value = default;
                        collectedValues = RentCollectedValues();
                    }
                    else if (TypeDetail.HasCreator)
                    {
                        value = TypeDetail.Creator!();
                        collectedValues = null;
                    }
                    else
                    {
                        value = default;
                        collectedValues = null;
                    }

                    if (reader.Token == JsonToken.ArrayEnd)
                        return true;

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
                    if (state.Current.EnumeratorIndex == members.Count)
                        throw reader.CreateException("Unexpected value");

                    if (!state.Current.HasReadFirstToken)
                    {
                        if (!reader.TryReadToken(out state.SizeNeeded))
                        {
                            if (collectValues)
                                state.Current.Object = collectedValues;
                            else
                                state.Current.Object = value;
                            return false;
                        }
                    }

                    var current = members[state.Current.EnumeratorIndex];

                    if (!state.Current.HasReadValue)
                    {
                        if (collectValues)
                        {
                            if (!current.ConverterSetCollectedValues.TryReadFromParentMember(ref reader, ref state, collectedValues, current.Member.Name, false))
                            {
                                state.Current.HasReadFirstToken = true;
                                state.Current.HasReadProperty = true;
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
                            if (!current.Converter.TryReadFromParentMember(ref reader, ref state, value, current.Member.Name, false))
                            {
                                state.Current.HasReadFirstToken = true;
                                state.Current.HasReadProperty = true;
                                state.Current.HasReadSeperator = true;
                                if (collectValues)
                                    state.Current.Object = collectedValues;
                                else
                                    state.Current.Object = value;
                                return false;
                            }
                        }
                    }

                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasReadFirstToken = true;
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        state.Current.HasReadValue = true;
                        if (collectValues)
                            state.Current.Object = collectedValues;
                        else
                            state.Current.Object = value;
                        return false;
                    }

                    if (reader.Token == JsonToken.ArrayEnd)
                        break;

                    if (reader.Token != JsonToken.NextItem)
                        throw reader.CreateException();

                    state.Current.HasReadFirstToken = false;
                    state.Current.HasReadProperty = false;
                    state.Current.HasReadSeperator = false;
                    state.Current.HasReadValue = false;
                    state.Current.EnumeratorIndex++;
                }
            }
            else
            {
                if (token != JsonToken.ObjectStart)
                {
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);

                    value = default;
                    return Drain(ref reader, ref state, token);
                }

                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        value = default;
                        return false;
                    }
                    state.Current.HasReadFirstToken = true;

                    if (collectValues)
                    {
                        value = default;
                        collectedValues = RentCollectedValues();
                    }
                    else if (TypeDetail.HasCreator)
                    {
                        value = TypeDetail.Creator!();
                        collectedValues = null;
                    }
                    else
                    {
                        value = default;
                        collectedValues = null;
                    }

                    if (reader.Token == JsonToken.ObjectEnd)
                        return true;

                    state.Current.HasCreated = true;

                    if (state.IncludeReturnGraph)
                        state.Current.ReturnGraph = new Graph();
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
                    JsonConverterObjectMember? member;
                    MemberKey memberKey;
                    var memberLength = members.Count;
                    int nextIndex = state.Current.EnumeratorIndex;
                    int prevIndex = state.Current.EnumeratorIndex - 1;

                    if (!state.Current.HasReadProperty)
                    {
                        if (!state.Current.HasReadFirstToken)
                        {
                            if (!reader.TryReadToken(out state.SizeNeeded))
                            {
                                if (collectValues)
                                    state.Current.Object = collectedValues;
                                else
                                    state.Current.Object = value;
                                return false;
                            }
                        }

                        if (reader.Token != JsonToken.String)
                            throw reader.CreateException();

                        if (reader.UseBytes)
                        {
                            if (state.IgnoreCase || collectValues)
                            {
                                //slow path
                                if (reader.ValueBytes.Length == 0)
                                    throw reader.CreateException();

                                var name = System.Text.Encoding.UTF8.GetString(reader.ValueBytes);

                                if (!membersByName.TryGetValue(name, out member))
                                    member = null;
                            }
                            else
                            {
                                var nameKey = MemberKey.GetHashCode(reader.ValueBytes);

                                for (; ; )
                                {
                                    if (nextIndex < memberLength)
                                    {
                                        memberKey = membersKeyed[nextIndex];
                                        if (MemberKey.IsEqual(memberKey, reader.ValueBytes, nameKey))
                                        {
                                            member = memberKey.Member;
                                            if (member.IgnoreCondition == JsonIgnoreCondition.WhenReading || (state.Current.Graph is not null && !state.Current.Graph.HasMember(member.Member.Name)))
                                            {
                                                nextIndex++;
                                                continue;
                                            }
                                            state.Current.EnumeratorIndex = nextIndex + 1;
                                            break;
                                        }

                                        if (prevIndex >= 0)
                                        {
                                            memberKey = membersKeyed[prevIndex];
                                            if (MemberKey.IsEqual(memberKey, reader.ValueBytes, nameKey))
                                            {
                                                member = memberKey.Member;
                                                if (member.IgnoreCondition == JsonIgnoreCondition.WhenReading || (state.Current.Graph is not null && !state.Current.Graph.HasMember(member.Member.Name)))
                                                {
                                                    prevIndex--;
                                                    nextIndex++;
                                                    continue;
                                                }
                                                state.Current.EnumeratorIndex = prevIndex + 1;
                                                break;
                                            }
                                        }

                                        prevIndex--;
                                        nextIndex++;
                                    }
                                    else if (prevIndex >= 0)
                                    {
                                        memberKey = membersKeyed[prevIndex];
                                        if (MemberKey.IsEqual(memberKey, reader.ValueBytes, nameKey))
                                        {
                                            member = memberKey.Member;
                                            if (member.IgnoreCondition == JsonIgnoreCondition.WhenReading || (state.Current.Graph is not null && !state.Current.Graph.HasMember(member.Member.Name)))
                                            {
                                                prevIndex--;
                                                continue;
                                            }
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
                            if (state.IgnoreCase || collectValues)
                            {
                                //slow path
                                if (reader.ValueChars.Length == 0)
                                    throw reader.CreateException();

                                if (!membersByName.TryGetValue(reader.ValueChars.ToString(), out member))
                                    member = null;
                            }
                            else
                            {
                                if (reader.ValueChars.Length == 0)
                                    throw reader.CreateException();

                                var nameKey = MemberKey.GetHashCode(reader.ValueChars);

                                for (; ; )
                                {
                                    if (nextIndex < memberLength)
                                    {
                                        memberKey = membersKeyed[nextIndex];
                                        if (MemberKey.IsEqual(memberKey, reader.ValueChars, nameKey))
                                        {
                                            member = memberKey.Member;
                                            if (member.IgnoreCondition == JsonIgnoreCondition.WhenReading || (state.Current.Graph is not null && !state.Current.Graph.HasMember(member.Member.Name)))
                                            {
                                                nextIndex++;
                                                continue;
                                            }
                                            state.Current.EnumeratorIndex = nextIndex + 1;
                                            break;
                                        }

                                        if (prevIndex >= 0)
                                        {
                                            memberKey = membersKeyed[prevIndex];
                                            if (MemberKey.IsEqual(memberKey, reader.ValueChars, nameKey))
                                            {
                                                member = memberKey.Member;
                                                if (member.IgnoreCondition == JsonIgnoreCondition.WhenReading || (state.Current.Graph is not null && !state.Current.Graph.HasMember(member.Member.Name)))
                                                {
                                                    prevIndex--;
                                                    nextIndex++;
                                                    continue;
                                                }
                                                state.Current.EnumeratorIndex = prevIndex + 1;
                                                break;
                                            }
                                        }

                                        prevIndex--;
                                        nextIndex++;
                                    }
                                    else if (prevIndex >= 0)
                                    {
                                        memberKey = membersKeyed[prevIndex];
                                        if (MemberKey.IsEqual(memberKey, reader.ValueChars, nameKey))
                                        {
                                            member = memberKey.Member;
                                            if (member.IgnoreCondition == JsonIgnoreCondition.WhenReading || (state.Current.Graph is not null && !state.Current.Graph.HasMember(member.Member.Name)))
                                            {
                                                prevIndex--;
                                                continue;
                                            }
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
                    }
                    else
                    {
                        member = (JsonConverterObjectMember?)state.Current.Property;
                    }

                    if (!state.Current.HasReadSeperator)
                    {
                        if (!reader.TryReadToken(out state.SizeNeeded))
                        {
                            state.Current.HasReadFirstToken = true;
                            state.Current.HasReadProperty = true;
                            state.Current.Property = member;
                            if (collectValues)
                                state.Current.Object = collectedValues;
                            else
                                state.Current.Object = value;
                            return false;
                        }
                        if (reader.Token != JsonToken.PropertySeperator)
                            throw reader.CreateException();
                    }

                    if (!state.Current.HasReadValue)
                    {
                        if (member is null)
                        {
                            if (!DrainFromParentMember(ref reader, ref state))
                            {
                                state.Current.HasReadFirstToken = true;
                                state.Current.HasReadProperty = true;
                                state.Current.Property = member;
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
                                if (!member.ConverterSetCollectedValues.TryReadFromParentMember(ref reader, ref state, collectedValues, member.Member.Name, true))
                                {
                                    state.Current.HasReadFirstToken = true;
                                    state.Current.HasReadProperty = true;
                                    state.Current.HasReadSeperator = true;
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
                                if (!member.Converter.TryReadFromParentMember(ref reader, ref state, value, member.Member.Name, true))
                                {
                                    state.Current.HasReadFirstToken = true;
                                    state.Current.HasReadProperty = true;
                                    state.Current.HasReadSeperator = true;
                                    state.Current.Property = member;
                                    state.Current.Object = value;
                                    return false;
                                }
                            }
                        }
                    }

                    if (!reader.TryReadToken(out state.SizeNeeded))
                    {
                        state.Current.HasReadFirstToken = true;
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        state.Current.HasReadValue = true;
                        if (collectValues)
                            state.Current.Object = collectedValues;
                        else
                            state.Current.Object = value;
                        return false;
                    }

                    if (reader.Token == JsonToken.ObjectEnd)
                        break;
                    if (reader.Token != JsonToken.NextItem)
                        throw reader.CreateException();

                    state.Current.HasReadFirstToken = false;
                    state.Current.HasReadProperty = false;
                    state.Current.HasReadSeperator = false;
                    state.Current.HasReadValue = false;
                }
            }

            if (collectValues)
            {
                var args = new object?[parameterConstructor!.Parameters.Count];
                for (var i = 0; i < args.Length; i++)
                {
#if NETSTANDARD2_0
                    if (collectedValues!.TryGetValue(parameterConstructor.ParameterDetails[i].Name!, out var parameter))
                    {
                        collectedValues.Remove(parameterConstructor.ParameterDetails[i].Name!);
                        args[i] = parameter;
                    }
#else
                    if (collectedValues!.Remove(parameterConstructor.Parameters[i].Name!, out var parameter))
                        args[i] = parameter;
#endif
                }

                if (TypeDetail.Type.IsValueType)
                    value = (TValue?)parameterConstructor.CreatorBoxed(args);
                else
                    value = parameterConstructor.Creator(args);

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

        protected override sealed bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TValue value)
        {
            if (state.Nameless)
            {
                if (membersByName.Count == 0)
                {
                    if (!writer.TryWriteEmptyBracket(out state.SizeNeeded))
                    {
                        return false;
                    }
                    return true;
                }

                if (!state.Current.HasWrittenStart)
                {
                    if (!writer.TryWriteOpenBracket(out state.SizeNeeded))
                    {
                        return false;
                    }
                }

                while (state.Current.EnumeratorIndex < members.Count)
                {
                    var current = members[state.Current.EnumeratorIndex];
                    if (current.IgnoreCondition == JsonIgnoreCondition.WhenWriting)
                    {
                        state.Current.EnumeratorIndex++;
                        continue;
                    }
                    if (state.Current.Graph is not null && !state.Current.Graph.HasMember(current.Member.Name))
                    {
                        state.Current.EnumeratorIndex++;
                        continue;
                    }

                    if (state.Current.HasWrittenFirst && !state.Current.HasWrittenSeperator)
                    {
                        if (!writer.TryWriteComma(out state.SizeNeeded))
                        {
                            state.Current.HasWrittenStart = true;
                            return false;
                        }
                    }

                    if (!current.Converter.TryWriteFromParentMember(ref writer, ref state, value!, null, default, default, current.IgnoreCondition, true))
                    {
                        state.Current.HasWrittenStart = true;
                        state.Current.HasWrittenSeperator = true;
                        return false;
                    }

                    if (!state.Current.HasWrittenFirst)
                        state.Current.HasWrittenFirst = true;
                    if (state.Current.HasWrittenSeperator)
                        state.Current.HasWrittenSeperator = false;

                    state.Current.EnumeratorIndex++;
                }

                if (!writer.TryWriteCloseBracket(out state.SizeNeeded))
                {
                    state.Current.HasWrittenStart = true;
                    return false;
                }
                return true;
            }
            else
            {
                if (membersByName.Count == 0)
                {
                    if (!writer.TryWriteEmptyBrace(out state.SizeNeeded))
                    {
                        return false;
                    }
                    return true;
                }

                if (!state.Current.HasWrittenStart)
                {
                    if (!writer.TryWriteOpenBrace(out state.SizeNeeded))
                    {
                        return false;
                    }
                }

                while (state.Current.EnumeratorIndex < members.Count)
                {
                    var current = members[state.Current.EnumeratorIndex];
                    if (current.IgnoreCondition == JsonIgnoreCondition.WhenWriting)
                    {
                        state.Current.EnumeratorIndex++;
                        continue;
                    }
                    if (state.Current.Graph is not null && !state.Current.Graph.HasMember(current.Member.Name))
                    {
                        state.Current.EnumeratorIndex++;
                        continue;
                    }

                    if (!current.Converter.TryWriteFromParentMember(ref writer, ref state, value!, current.Member.Name, current.JsonNameSegmentChars, current.JsonNameSegmentBytes, current.IgnoreCondition, false))
                    {
                        state.Current.HasWrittenStart = true;
                        return false;
                    }
                    state.Current.EnumeratorIndex++;
                }

                if (!writer.TryWriteCloseBrace(out state.SizeNeeded))
                {
                    state.Current.HasWrittenStart = true;
                    return false;
                }
                return true;
            }
        }
    }
}