// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;
using Zerra.SourceGeneration.Types;

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
            return new(MemberNameComparer.Instance);
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
            foreach (var member in typeDetail.SerializableMemberDetails)
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
                        var detail = new JsonConverterObjectMember(typeDetail, member, jsonPropertyName.Name, ignoreCondition);
                        membersByName.Add(jsonPropertyName.Name, detail);
                        members.Add(detail);
                        found = true;
                        break;
                    }
                    else if (attribute is System.Text.Json.Serialization.JsonPropertyNameAttribute jsonPropertyName2)
                    {
                        var detail = new JsonConverterObjectMember(typeDetail, member, jsonPropertyName2.Name, ignoreCondition);
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
                    var detail = new JsonConverterObjectMember(typeDetail, member, member.Name, ignoreCondition);
                    membersByName.Add(member.Name, detail);
                    members.Add(detail);
                }

                membersKeyed = members.Select(x => new MemberKey(x)).ToArray();
            }

            if (typeDetail.Type.IsValueType || !typeDetail.HasCreator)
            {
                //find best constructor
                foreach (var constructor in typeDetail.Constructors.OrderByDescending(x => x.Parameters.Count))
                {
                    var skip = false;
                    foreach (var parameter in constructor.Parameters)
                    {
                        //cannot have argument of itself or a null name
                        if (parameter.Type == typeDetail.Type || parameter.Name is null)
                        {
                            skip = true;
                            break;
                        }
                        //must have a matching a member
                        if (!members.Any(x => x.Member.Type == parameter.Type && MemberNameComparer.Instance.Equals(x.Member.Name, parameter.Name)))
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

        protected override sealed bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out TValue? value)
        {
            Dictionary<string, object?>? collectedValues;
            char c;

            if (state.Nameless)
            {
                if (valueType != JsonValueType.Array)
                {
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);

                    value = default;
                    return Drain(ref reader, ref state, valueType);
                }

                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.SizeNeeded = 1;
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
                        value = typeDetail.Creator!();
                        collectedValues = null;
                    }
                    else
                    {
                        value = default;
                        collectedValues = null;
                    }

                    if (c == ']')
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
                    if (state.Current.EnumeratorIndex == members.Count)
                        throw reader.CreateException("Unexpected value");

                    var current = members[state.Current.EnumeratorIndex];

                    if (!state.Current.HasReadValue)
                    {
                        if (collectValues)
                        {
                            if (!current.ConverterSetCollectedValues.TryReadFromParent(ref reader, ref state, collectedValues, current.Member.Name))
                            {
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
                            if (!current.Converter.TryReadFromParent(ref reader, ref state, value, current.Member.Name))
                            {
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

                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.SizeNeeded = 1;
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        state.Current.HasReadValue = true;
                        if (collectValues)
                            state.Current.Object = collectedValues;
                        else
                            state.Current.Object = value;
                        return false;
                    }

                    if (c == ']')
                        break;

                    if (c != ',')
                        throw reader.CreateException("Unexpected character");

                    state.Current.HasReadProperty = false;
                    state.Current.HasReadSeperator = false;
                    state.Current.HasReadValue = false;
                    state.Current.EnumeratorIndex++;
                }
            }
            else
            {
                if (valueType != JsonValueType.Object)
                {
                    if (state.ErrorOnTypeMismatch)
                        ThrowCannotConvert(ref reader);

                    value = default;
                    return Drain(ref reader, ref state, valueType);
                }

                if (!state.Current.HasCreated)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.SizeNeeded = 1;
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
                        value = typeDetail.Creator!();
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
                        if (reader.UseBytes)
                        {
                            if (state.IgnoreCase || collectValues)
                            {
                                //slow path
                                if (!reader.TryReadStringEscapedQuoted(false, out var name, out state.SizeNeeded))
                                {
                                    if (collectValues)
                                        state.Current.Object = collectedValues;
                                    else
                                        state.Current.Object = value;
                                    return false;
                                }

                                if (name.Length == 0)
                                    throw reader.CreateException("Unexpected character");

                                if (!membersByName.TryGetValue(name, out member))
                                    member = null;
                            }
                            else
                            {
                                if (!reader.TryReadStringQuotedBytes(false, out var name, out state.SizeNeeded))
                                {
                                    if (collectValues)
                                        state.Current.Object = collectedValues;
                                    else
                                        state.Current.Object = value;
                                    return false;
                                }

                                if (name.Length == 0)
                                    throw reader.CreateException("Unexpected character");

                                var nameKey = MemberKey.GetHashCode(name);

                                for (; ; )
                                {
                                    if (nextIndex < memberLength)
                                    {
                                        memberKey = membersKeyed[nextIndex];
                                        if (MemberKey.IsEqual(memberKey, name, nameKey))
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
                                            if (MemberKey.IsEqual(memberKey, name, nameKey))
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
                                        if (MemberKey.IsEqual(memberKey, name, nameKey))
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
                                if (!reader.TryReadStringEscapedQuoted(false, out var name, out state.SizeNeeded))
                                {
                                    if (collectValues)
                                        state.Current.Object = collectedValues;
                                    else
                                        state.Current.Object = value;
                                    return false;
                                }

                                if (name.Length == 0)
                                    throw reader.CreateException("Unexpected character");

                                if (!membersByName.TryGetValue(name, out member))
                                    member = null;
                            }
                            else
                            {
                                if (!reader.TryReadStringQuotedChars(false, out var name, out state.SizeNeeded))
                                {
                                    if (collectValues)
                                        state.Current.Object = collectedValues;
                                    else
                                        state.Current.Object = value;
                                    return false;
                                }

                                if (name.Length == 0)
                                    throw reader.CreateException("Unexpected character");

                                var nameKey = MemberKey.GetHashCode(name);

                                for (; ; )
                                {
                                    if (nextIndex < memberLength)
                                    {
                                        memberKey = membersKeyed[nextIndex];
                                        if (MemberKey.IsEqual(memberKey, name, nameKey))
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
                                            if (MemberKey.IsEqual(memberKey, name, nameKey))
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
                                        if (MemberKey.IsEqual(memberKey, name, nameKey))
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
                        if (!reader.TryReadNextSkipWhiteSpace(out c))
                        {
                            state.SizeNeeded = 1;
                            state.Current.HasReadProperty = true;
                            state.Current.Property = member;
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
                        if (member is null)
                        {
                            if (!DrainFromParent(ref reader, ref state))
                            {
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
                                if (!member.ConverterSetCollectedValues.TryReadFromParent(ref reader, ref state, collectedValues, member.Member.Name))
                                {
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
                                if (!member.Converter.TryReadFromParent(ref reader, ref state, value, member.Member.Name))
                                {
                                    state.Current.HasReadProperty = true;
                                    state.Current.HasReadSeperator = true;
                                    state.Current.Property = member;
                                    state.Current.Object = value;
                                    return false;
                                }
                            }
                        }
                    }

                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.SizeNeeded = 1;
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

                if (typeDetail.Type.IsValueType)
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

                    if (!current.Converter.TryWriteFromParent(ref writer, ref state, value!, null, default, default, current.IgnoreCondition, true))
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

                    if (!current.Converter.TryWriteFromParent(ref writer, ref state, value!, current.Member.Name, current.JsonNameSegmentChars, current.JsonNameSegmentBytes, current.IgnoreCondition))
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