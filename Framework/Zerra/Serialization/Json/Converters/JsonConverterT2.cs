// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.State;
using Zerra.Serialization.Json.IO;

namespace Zerra.Serialization.Json.Converters
{
    public abstract class JsonConverter<TParent, TValue> : JsonConverter<TParent>,
        IJsonConverterHandles<TValue>
    {
        protected virtual bool StackRequired { get; } = true;

        protected TypeDetail<TValue> typeDetail { get; private set; } = null!;
        private string memberKey = null!;
        private Func<TParent, TValue?>? getter;
        private Action<TParent, TValue?>? setter;

        private bool canBeNull;
        private bool isObject;
        private bool isInterfacedObject;

        public override void Setup(TypeDetail typeDetail, string memberKey, Delegate? getterDelegate, Delegate? setterDelegate)
        {
            this.typeDetail = (TypeDetail<TValue>)typeDetail;
            this.memberKey = memberKey;
            if (getterDelegate is not null)
            {
                getter = getterDelegate as Func<TParent, TValue?>;
                getter ??= (parent) => (TValue?)getterDelegate.DynamicInvoke(parent);
            }
            if (setterDelegate is not null)
            {
                setter = setterDelegate as Action<TParent, TValue?>;
                setter ??= (parent, value) => setterDelegate.DynamicInvoke(parent, value);
            }

            canBeNull = typeDetail.IsNullable || !typeDetail.Type.IsValueType;
            isObject = typeDetail.Type == typeof(object);
            isInterfacedObject = typeDetail.Type.IsInterface && !typeDetail.HasIEnumerableGeneric && !typeDetail.HasIEnumerable;

            Setup();
        }

        protected virtual void Setup() { }

        public override sealed bool TryReadBoxed(ref JsonReader reader, ref ReadState state, out object? returnValue)
        {
            if (state.EntryValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.EntryValueType, out state.SizeNeeded))
                {
                    returnValue = default;
                    return false;
                }
                if (state.EntryValueType == JsonValueType.Null_Completed)
                {
                    returnValue = default;
                    return true;
                }
            }
            var valueType = state.EntryValueType;

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(state.Graph);
            }

            if (isObject && valueType != JsonValueType.Object)
            {
                TypeDetail newTypeDetail = valueType switch
                {
                    JsonValueType.Object => TypeAnalyzer<object>.GetTypeDetail(),
                    JsonValueType.Array => TypeAnalyzer<object[]>.GetTypeDetail(),
                    JsonValueType.String => TypeAnalyzer<string>.GetTypeDetail(),
                    JsonValueType.Number => TypeAnalyzer<decimal>.GetTypeDetail(),
                    JsonValueType.True_Completed or JsonValueType.False_Completed => TypeAnalyzer<bool>.GetTypeDetail(),
                    _ => throw new NotSupportedException(),
                };

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, valueType, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    returnValue = default;
                    return false;
                }

                if (StackRequired)
                    state.EndFrame();
                returnValue = valueObject;
                return true;
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, valueType, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    returnValue = default;
                    return false;
                }

                if (StackRequired)
                    state.EndFrame();
                returnValue = valueObject;
                return true;
            }

            if (!TryReadValue(ref reader, ref state, valueType, out var value))
            {
                if (StackRequired)
                    state.StashFrame();
                returnValue = value;
                return false;
            }

            if (StackRequired)
                state.EndFrame();
            returnValue = value;
            return true;
        }
        public override sealed bool TryWriteBoxed(ref JsonWriter writer, ref WriteState state, in object? value)
        {
            if (canBeNull)
            {
                if (value is null)
                {
                    if (!writer.TryWriteNull(out state.SizeNeeded))
                    {
                        return false;
                    }
                    return true;
                }
            }

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(state.Graph);
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = JsonConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value!))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        return false;
                    }

                    if (StackRequired)
                        state.EndFrame();
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, (TValue?)value!))
            {
                if (StackRequired)
                    state.StashFrame();
                return false;
            }

            if (StackRequired)
                state.EndFrame();
            return true;
        }

        public bool TryRead(ref JsonReader reader, ref ReadState state, out TValue? returnValue)
        {
            if (state.EntryValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.EntryValueType, out state.SizeNeeded))
                {
                    returnValue = default;
                    return false;
                }
                if (state.EntryValueType == JsonValueType.Null_Completed)
                {
                    returnValue = default;
                    return true;
                }
            }
            var valueType = state.EntryValueType;

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(state.Graph);
            }

            if (isObject && valueType != JsonValueType.Object)
            {
                TypeDetail newTypeDetail = valueType switch
                {
                    JsonValueType.Object => TypeAnalyzer<object>.GetTypeDetail(),
                    JsonValueType.Array => TypeAnalyzer<object[]>.GetTypeDetail(),
                    JsonValueType.String => TypeAnalyzer<string>.GetTypeDetail(),
                    JsonValueType.Number => TypeAnalyzer<decimal>.GetTypeDetail(),
                    JsonValueType.True_Completed or JsonValueType.False_Completed => TypeAnalyzer<bool>.GetTypeDetail(),
                    _ => throw new NotSupportedException(),
                };

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, valueType, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    returnValue = default;
                    return false;
                }

                if (StackRequired)
                    state.EndFrame();
                returnValue = (TValue?)valueObject;
                return true;
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, valueType, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    returnValue = default;
                    return false;
                }

                if (StackRequired)
                    state.EndFrame();
                returnValue = (TValue?)valueObject;
                return true;
            }

            if (!TryReadValue(ref reader, ref state, valueType, out var value))
            {
                if (StackRequired)
                    state.StashFrame();
                returnValue = value;
                return false;
            }

            if (StackRequired)
                state.EndFrame();
            returnValue = value;
            return true;
        }
        public bool TryWrite(ref JsonWriter writer, ref WriteState state, TValue? value)
        {
            if (canBeNull)
            {
                if (value is null)
                {
                    if (!writer.TryWriteNull(out state.SizeNeeded))
                    {
                        return false;
                    }
                    return true;
                }
            }

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(state.Graph);
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = JsonConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value!))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        return false;
                    }
                    if (StackRequired)
                        state.EndFrame();
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, value!))
            {
                if (StackRequired)
                    state.StashFrame();
                return false;
            }
            if (StackRequired)
                state.EndFrame();
            return true;
        }

        public override sealed bool TryReadFromParent(ref JsonReader reader, ref ReadState state, TParent? parent, string? propertyName)
        {
            if (state.Current.ChildValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.Current.ChildValueType, out state.SizeNeeded))
                {
                    return false;
                }
                if (state.Current.ChildValueType == JsonValueType.Null_Completed)
                {
                    if (setter is not null && parent is not null)
                        setter(parent, default);

                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                }
            }
            var valueType = state.Current.ChildValueType;

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                if (propertyName is not null)
                {
                    var childGraph = state.Current.Graph?.GetChildGraph(propertyName);
                    state.PushFrame(childGraph);
                }
                else
                {
                    state.PushFrame(null);
                }
            }

            if (isObject && valueType != JsonValueType.Object)
            {
                TypeDetail newTypeDetail = valueType switch
                {
                    JsonValueType.Object => TypeAnalyzer<object>.GetTypeDetail(),
                    JsonValueType.Array => TypeAnalyzer<object[]>.GetTypeDetail(),
                    JsonValueType.String => TypeAnalyzer<string>.GetTypeDetail(),
                    JsonValueType.Number => TypeAnalyzer<decimal>.GetTypeDetail(),
                    JsonValueType.True_Completed or JsonValueType.False_Completed => TypeAnalyzer<bool>.GetTypeDetail(),
                    _ => throw new NotSupportedException(),
                };

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, valueType, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    return false;
                }

                if (setter is not null && parent is not null)
                    setter(parent, (TValue?)valueObject);
                if (StackRequired)
                    state.EndFrame();
                state.Current.ChildValueType = JsonValueType.NotDetermined;
                return true;
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, valueType, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    return false;
                }

                if (setter is not null && parent is not null)
                    setter(parent, (TValue?)valueObject);
                if (StackRequired)
                    state.EndFrame();
                state.Current.ChildValueType = JsonValueType.NotDetermined;
                return true;
            }

            if (!TryReadValue(ref reader, ref state, valueType, out var value))
            {
                if (StackRequired)
                    state.StashFrame();
                return false;
            }

            if (setter is not null && parent is not null)
                setter(parent, value);
            if (StackRequired)
                state.EndFrame();
            state.Current.ChildValueType = JsonValueType.NotDetermined;
            return true;
        }
        public override sealed bool TryWriteFromParent(ref JsonWriter writer, ref WriteState state, TParent parent, string? propertyName, ReadOnlySpan<char> jsonNameSegmentChars, ReadOnlySpan<byte> jsonNameSegmentBytes, JsonIgnoreCondition ignoreCondition, bool ignoreDoNotWriteNullProperties)
        {
            if (getter is null)
                return true;
            var value = getter(parent);

            if (propertyName is not null)
            {
                if (canBeNull && value is null)
                {
                    if (!ignoreDoNotWriteNullProperties && (ignoreCondition == JsonIgnoreCondition.WhenWritingNull || state.DoNotWriteNullProperties))
                    {
                        return true;
                    }

                    if (!state.Current.HasWrittenPropertyName)
                    {
                        if (writer.UseBytes)
                        {
                            if (!writer.TryWriteNameSegment(jsonNameSegmentBytes, state.Current.HasWrittenFirst, out state.SizeNeeded))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!writer.TryWriteNameSegment(jsonNameSegmentChars, state.Current.HasWrittenFirst, out state.SizeNeeded))
                            {
                                return false;
                            }
                        }
                        if (!state.Current.HasWrittenFirst)
                            state.Current.HasWrittenFirst = true;
                        state.Current.HasWrittenPropertyName = true;
                    }

                    if (!writer.TryWriteNull(out state.SizeNeeded))
                    {
                        return false;
                    }
                    state.Current.HasWrittenPropertyName = false;
                    return true;
                }
                else
                {
                    if (!ignoreDoNotWriteNullProperties && (ignoreCondition == JsonIgnoreCondition.WhenWritingDefault || state.DoNotWriteDefaultProperties) && value.Equals(default(TValue)))
                    {
                        return true;
                    }

                    if (!state.Current.HasWrittenPropertyName)
                    {
                        if (writer.UseBytes)
                        {
                            if (!writer.TryWriteNameSegment(jsonNameSegmentBytes, state.Current.HasWrittenFirst, out state.SizeNeeded))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!writer.TryWriteNameSegment(jsonNameSegmentChars, state.Current.HasWrittenFirst, out state.SizeNeeded))
                            {
                                return false;
                            }
                        }
                        if (!state.Current.HasWrittenFirst)
                            state.Current.HasWrittenFirst = true;
                        state.Current.HasWrittenPropertyName = true;
                    }
                }
            }
            else if (canBeNull && value is null)
            {
                if (!writer.TryWriteNull(out state.SizeNeeded))
                {
                    return false;
                }
                return true;
            }

            if (StackRequired)
            {
                if (state.StackSize >= maxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                if (propertyName is not null)
                {
                    var childGraph = value is null ? state.Current.Graph?.GetChildGraph(propertyName) : state.Current.Graph?.GetChildInstanceGraph(propertyName, value);
                    state.PushFrame(childGraph);
                }
                else
                {
                    state.PushFrame(null);
                }
            }

            if (isInterfacedObject || isObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = JsonConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value!))
                    {
                        if (StackRequired)
                            state.StashFrame();
                        return false;
                    }

                    if (StackRequired)
                        state.EndFrame();
                    state.Current.HasWrittenPropertyName = false;
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, value!))
            {
                if (StackRequired)
                    state.StashFrame();
                return false;
            }

            if (StackRequired)
                state.EndFrame();
            state.Current.HasWrittenPropertyName = false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryReadValueBoxed(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out object? value)
        {
            var read = TryReadValue(ref reader, ref state, valueType, out var v);
            value = v;
            return read;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWriteValueBoxed(ref JsonWriter writer, ref WriteState state, in object value)
            => TryWriteValue(ref writer, ref state, (TValue)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out TValue? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TValue value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed void CollectedValuesSetter(TParent? parent, in object? value)
        {
            if (setter is not null && parent is not null && value is not null)
                setter(parent, (TValue)value);
        }
    }
}