// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.State;
using Zerra.Serialization.Json.IO;
using Zerra.Reflection;

namespace Zerra.Serialization.Json.Converters
{
    /// <summary>
    /// Generic abstract base class for type-specific JSON serialization and deserialization converters.
    /// </summary>
    /// <typeparam name="TValue">The type of value this converter handles.</typeparam>
    /// <remarks>
    /// This generic class provides the foundation for converting strongly-typed values to and from JSON format.
    /// It manages type details, nullable type handling, and stack depth tracking during serialization/deserialization.
    /// Derived classes implement specific conversion logic for different value types.
    /// Supports special handling for interface types and the object type with dynamic type resolution.
    /// </remarks>
    public abstract class JsonConverter<TValue> : JsonConverter
    {
        /// <summary>
        /// Gets a value indicating whether stack frame management is required for this converter.
        /// </summary>
        /// <remarks>
        /// Set to <c>true</c> if the converter needs to track stack depth to prevent stack overflow during nested conversions.
        /// </remarks>
        protected virtual bool StackRequired { get; } = true;

        /// <summary>
        /// Gets the type detail information for the value type being converted.
        /// </summary>
        protected TypeDetail<TValue> TypeDetail { get; private set; } = null!;
        private string memberKey = null!;
        private Func<object, TValue?>? getter;
        private Action<object, TValue?>? setter;

        private bool canBeNull;
        private bool isObject;
        private bool isInterfacedObject;

        /// <inheritdoc/>
        public override sealed void Setup(string memberKey, Delegate? getterDelegate, Delegate? setterDelegate)
        {
            this.TypeDetail = TypeAnalyzer<TValue>.GetTypeDetail();
            this.memberKey = memberKey;
            if (getterDelegate is not null)
            {
                getter = getterDelegate as Func<object, TValue?>;
                getter ??= (parent) => (TValue?)getterDelegate.DynamicInvoke(parent);
            }
            if (setterDelegate is not null)
            {
                setter = setterDelegate as Action<object, TValue?>;
                setter ??= (parent, value) => setterDelegate.DynamicInvoke(parent, value);
            }

            canBeNull = TypeDetail.IsNullable || !TypeDetail.Type.IsValueType;
            isObject = TypeDetail.Type == typeof(object);
            isInterfacedObject = TypeDetail.Type.IsInterface && !TypeDetail.HasIEnumerableGeneric && !TypeDetail.HasIEnumerable;

            Setup();
        }

        /// <summary>
        /// Provides a hook for derived classes to perform additional setup after initialization.
        /// </summary>
        protected virtual void Setup() { }

        /// <inheritdoc/>
        public override sealed bool TryReadBoxed(ref JsonReader reader, ref ReadState state, out object? value)
        {
            if (state.EntryToken == JsonToken.NotDetermined)
            {
                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    value = default;
                    return false;
                }
                if (reader.Token == JsonToken.Null)
                {
                    value = default;
                    return true;
                }
                state.EntryToken = reader.Token;
            }


            if (StackRequired)
            {
                if (state.StackSize >= MaxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(state.Graph);
            }

            if (isObject && state.EntryToken != JsonToken.ObjectStart)
            {
                TypeDetail newTypeDetail = state.EntryToken switch
                {
                    JsonToken.ObjectStart => TypeAnalyzer<object>.GetTypeDetail(),
                    JsonToken.ArrayStart => TypeAnalyzer<object[]>.GetTypeDetail(),
                    JsonToken.String => TypeAnalyzer<string>.GetTypeDetail(),
                    JsonToken.Number => TypeAnalyzer<decimal>.GetTypeDetail(),
                    JsonToken.True or JsonToken.False => TypeAnalyzer<bool>.GetTypeDetail(),
                    _ => throw new NotSupportedException(),
                };

                var newConverter = JsonConverterFactory.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, state.EntryToken, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    value = default;
                    return false;
                }

                if (StackRequired)
                    state.EndFrame();
                value = valueObject;
                return true;
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetType(TypeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, state.EntryToken, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    value = default;
                    return false;
                }

                if (StackRequired)
                    state.EndFrame();
                value = valueObject;
                return true;
            }

            if (!TryReadValue(ref reader, ref state, state.EntryToken, out var returnValue))
            {
                if (StackRequired)
                    state.StashFrame();
                value = returnValue;
                return false;
            }

            if (StackRequired)
                state.EndFrame();
            value = returnValue;
            return true;
        }
        /// <inheritdoc/>
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
                if (state.StackSize >= MaxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(state.Graph);
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? TypeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != TypeDetail.Type)
                {
                    var newConverter = JsonConverterFactory.Get(typeFromValue, memberKey, getter, setter);
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

        /// <summary>
        /// Attempts to read a strongly-typed value from the JSON reader.
        /// </summary>
        /// <param name="reader">The JSON reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="returnValue">The deserialized value if successful; otherwise, the default value for <typeparamref name="TValue"/>.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        public bool TryRead(ref JsonReader reader, ref ReadState state, out TValue? returnValue)
        {
            if (state.EntryToken == JsonToken.NotDetermined)
            {
                if (!reader.TryReadToken(out state.SizeNeeded))
                {
                    returnValue = default;
                    return false;
                }
                if (reader.Token == JsonToken.Null)
                {
                    returnValue = default;
                    return true;
                }
                state.EntryToken = reader.Token;
            }

            if (StackRequired)
            {
                if (state.StackSize >= MaxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(state.Graph);
            }

            if (isObject && state.EntryToken != JsonToken.ObjectStart)
            {
                TypeDetail newTypeDetail = state.EntryToken switch
                {
                    JsonToken.ObjectStart => TypeAnalyzer<object>.GetTypeDetail(),
                    JsonToken.ArrayStart => TypeAnalyzer<object[]>.GetTypeDetail(),
                    JsonToken.String => TypeAnalyzer<string>.GetTypeDetail(),
                    JsonToken.Number => TypeAnalyzer<decimal>.GetTypeDetail(),
                    JsonToken.True or JsonToken.False => TypeAnalyzer<bool>.GetTypeDetail(),
                    _ => throw new NotSupportedException(),
                };

                var newConverter = JsonConverterFactory.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, state.EntryToken, out var valueObject))
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
                var emptyImplementationType = EmptyImplementations.GetType(TypeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, state.EntryToken, out var valueObject))
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

            if (!TryReadValue(ref reader, ref state, state.EntryToken, out var value))
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
        /// <summary>
        /// Attempts to write a strongly-typed value to the JSON writer.
        /// </summary>
        /// <param name="writer">The JSON writer to write to.</param>
        /// <param name="state">The current write state.</param>
        /// <param name="value">The value to serialize.</param>
        /// <returns><c>true</c> if the write operation completed successfully; <c>false</c> if more bytes are needed.</returns>
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
                if (state.StackSize >= MaxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(state.Graph);
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? TypeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != TypeDetail.Type)
                {
                    var newConverter = JsonConverterFactory.Get(typeFromValue, memberKey, getter, setter);
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

        /// <inheritdoc/>
        public override sealed bool TryReadFromParent(ref JsonReader reader, ref ReadState state, object? parent)
        {
            if (state.Current.ChildJsonToken == JsonToken.NotDetermined)
            {
                if (reader.Token == JsonToken.Null)
                {
                    if (setter is not null && parent is not null)
                        setter(parent, default);

                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                }
                state.Current.ChildJsonToken = reader.Token;
            }
            var token = state.Current.ChildJsonToken;

            if (StackRequired)
            {
                if (state.StackSize >= MaxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(null);
            }

            if (isObject && token != JsonToken.ObjectStart)
            {
                TypeDetail newTypeDetail = token switch
                {
                    JsonToken.ObjectStart => TypeAnalyzer<object>.GetTypeDetail(),
                    JsonToken.ArrayStart => TypeAnalyzer<object[]>.GetTypeDetail(),
                    JsonToken.String => TypeAnalyzer<string>.GetTypeDetail(),
                    JsonToken.Number => TypeAnalyzer<decimal>.GetTypeDetail(),
                    JsonToken.True or JsonToken.False => TypeAnalyzer<bool>.GetTypeDetail(),
                    _ => throw new NotSupportedException(),
                };

                var newConverter = JsonConverterFactory.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, token, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    return false;
                }

                if (setter is not null && parent is not null)
                    setter(parent, (TValue?)valueObject);
                if (StackRequired)
                    state.EndFrame();
                state.Current.ChildJsonToken = JsonToken.NotDetermined;
                return true;
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetType(TypeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, token, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    return false;
                }

                if (setter is not null && parent is not null)
                    setter(parent, (TValue?)valueObject);
                if (StackRequired)
                    state.EndFrame();
                state.Current.ChildJsonToken = JsonToken.NotDetermined;
                return true;
            }

            if (!TryReadValue(ref reader, ref state, token, out var value))
            {
                if (StackRequired)
                    state.StashFrame();
                return false;
            }

            if (setter is not null && parent is not null)
                setter(parent, value);
            if (StackRequired)
            {
                state.EndFrame();
            }
            state.Current.ChildJsonToken = JsonToken.NotDetermined;
            return true;
        }
        /// <inheritdoc/>
        public override sealed bool TryWriteFromParent(ref JsonWriter writer, ref WriteState state, object parent)
        {
            if (getter is null)
                return true;
            var value = getter(parent);

            if (canBeNull && value is null)
            {
                if (!writer.TryWriteNull(out state.SizeNeeded))
                {
                    return false;
                }
                return true;
            }

            if (StackRequired)
            {
                if (state.StackSize >= MaxStackDepth)
                    throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
                state.PushFrame(null);
            }

            if (isInterfacedObject || isObject)
            {
                var typeFromValue = value is null ? TypeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != TypeDetail.Type)
                {
                    var newConverter = JsonConverterFactory.Get(typeFromValue, memberKey, getter, setter);
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

        /// <inheritdoc/>
        public override sealed bool TryReadFromParentMember(ref JsonReader reader, ref ReadState state, object? parent, string? propertyName, bool readToken)
        {
            if (state.Current.ChildJsonToken == JsonToken.NotDetermined)
            {
                if (readToken)
                {
                    if (!reader.TryReadToken(out state.SizeNeeded))
                        return false;
                }
                if (reader.Token == JsonToken.Null)
                {
                    if (setter is not null && parent is not null)
                        setter(parent, default);

                    if (state.IncludeReturnGraph && state.Current.ReturnGraph is not null && propertyName is not null)
                        state.Current.ReturnGraph.AddMember(propertyName);

                    state.Current.ChildJsonToken = JsonToken.NotDetermined;
                    return true;
                }
                state.Current.ChildJsonToken = reader.Token;
            }
            var token = state.Current.ChildJsonToken;

            if (StackRequired)
            {
                if (state.StackSize >= MaxStackDepth)
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

            if (isObject && token != JsonToken.ObjectStart)
            {
                TypeDetail newTypeDetail = token switch
                {
                    JsonToken.ObjectStart => TypeAnalyzer<object>.GetTypeDetail(),
                    JsonToken.ArrayStart => TypeAnalyzer<object[]>.GetTypeDetail(),
                    JsonToken.String => TypeAnalyzer<string>.GetTypeDetail(),
                    JsonToken.Number => TypeAnalyzer<decimal>.GetTypeDetail(),
                    JsonToken.True or JsonToken.False => TypeAnalyzer<bool>.GetTypeDetail(),
                    _ => throw new NotSupportedException(),
                };

                var newConverter = JsonConverterFactory.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, token, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    return false;
                }

                if (setter is not null && parent is not null)
                    setter(parent, (TValue?)valueObject);
                if (StackRequired)
                    state.EndFrame();
                state.Current.ChildJsonToken = JsonToken.NotDetermined;
                return true;
            }

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetType(TypeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, token, out var valueObject))
                {
                    if (StackRequired)
                        state.StashFrame();
                    return false;
                }

                if (setter is not null && parent is not null)
                    setter(parent, (TValue?)valueObject);
                if (StackRequired)
                    state.EndFrame();
                state.Current.ChildJsonToken = JsonToken.NotDetermined;
                return true;
            }

            if (!TryReadValue(ref reader, ref state, token, out var value))
            {
                if (StackRequired)
                    state.StashFrame();
                return false;
            }

            if (setter is not null && parent is not null)
                setter(parent, value);
            if (StackRequired)
            {
                if (state.IncludeReturnGraph && propertyName is not null)
                {
                    var childReturnGraph = state.Current.ReturnGraph;
                    state.EndFrame();
                    if (state.Current.ReturnGraph is not null)
                    {
                        if (childReturnGraph is not null)
                            state.Current.ReturnGraph.AddChildGraph(propertyName, childReturnGraph);
                        else
                            state.Current.ReturnGraph.AddMember(propertyName);
                    }
                }
                else
                {
                    state.EndFrame();
                }
            }
            else
            {
                if (state.IncludeReturnGraph && state.Current.ReturnGraph is not null && propertyName is not null)
                    state.Current.ReturnGraph.AddMember(propertyName);
            }
            state.Current.ChildJsonToken = JsonToken.NotDetermined;
            return true;
        }
        /// <inheritdoc/>
        public override sealed bool TryWriteFromParentMember(ref JsonWriter writer, ref WriteState state, object parent, string? propertyName, ReadOnlySpan<char> jsonNameSegmentChars, ReadOnlySpan<byte> jsonNameSegmentBytes, JsonIgnoreCondition ignoreCondition, bool ignoreDoNotWriteNullProperties)
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
                    if (!ignoreDoNotWriteNullProperties && (ignoreCondition == JsonIgnoreCondition.WhenWritingDefault || state.DoNotWriteDefaultProperties) && value!.Equals(default(TValue)))
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
                if (state.StackSize >= MaxStackDepth)
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
                var typeFromValue = value is null ? TypeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != TypeDetail.Type)
                {
                    var newConverter = JsonConverterFactory.Get(typeFromValue, memberKey, getter, setter);
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

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryReadValueBoxed(ref JsonReader reader, ref ReadState state, JsonToken token, out object? value)
        {
            var read = TryReadValue(ref reader, ref state, token, out var returnValue);
            value = returnValue;
            return read;
        }
        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWriteValueBoxed(ref JsonWriter writer, ref WriteState state, in object value)
            => TryWriteValue(ref writer, ref state, (TValue)value);

        /// <summary>
        /// When implemented in a derived class, attempts to read a strongly-typed value from the JSON reader.
        /// </summary>
        /// <param name="reader">The JSON reader to read from.</param>
        /// <param name="state">The current read state.</param>
        /// <param name="token">The determined JSON token type.</param>
        /// <param name="value">The deserialized value if successful; otherwise, the default value for <typeparamref name="TValue"/>.</param>
        /// <returns><c>true</c> if the read operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryReadValue(ref JsonReader reader, ref ReadState state, JsonToken token, out TValue? value);

        /// <summary>
        /// When implemented in a derived class, attempts to write a strongly-typed value to the JSON writer.
        /// </summary>
        /// <param name="writer">The JSON writer to write to.</param>
        /// <param name="state">The current write state.</param>
        /// <param name="value">The value to serialize.</param>
        /// <returns><c>true</c> if the write operation completed successfully; <c>false</c> if more bytes are needed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryWriteValue(ref JsonWriter writer, ref WriteState state, in TValue value);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed void CollectedValuesSetter(object? parent, in object? value)
        {
            if (setter is not null && parent is not null && value is not null)
                setter(parent, (TValue)value);
        }

        /// <summary>
        /// Throws an exception indicating that a value cannot be converted to the target type.
        /// </summary>
        /// <param name="reader">The JSON reader from which the exception context is derived.</param>
        /// <exception cref="InvalidOperationException">Always thrown with a message indicating the conversion failure.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowCannotConvert(ref JsonReader reader) => throw reader.CreateException($"Cannot convert to {TypeDetail.Type.Name} (disable {nameof(ReadState.ErrorOnTypeMismatch)} to prevent this exception)");
    }
}