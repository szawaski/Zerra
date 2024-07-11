// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using System.Runtime.CompilerServices;
using Zerra.IO;
using Zerra.Serialization.Json.State;
using Zerra.Serialization.Json.IO;
using System.Diagnostics.CodeAnalysis;

namespace Zerra.Serialization.Json.Converters
{
    public abstract class JsonConverter<TParent, TValue> : JsonConverter<TParent>,
        IJsonConverterHandles<TValue>
    {
        protected TypeDetail<TValue> typeDetail { get; private set; } = null!;
        private string? memberKey;
        private Func<TParent, TValue?>? getter;
        private Action<TParent, TValue?>? setter;

        private bool isNullable;
        private bool isInterfacedObject;

        public override void Setup(TypeDetail typeDetail, string? memberKey, Delegate? getterDelegate, Delegate? setterDelegate)
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

            isNullable = typeDetail.IsNullable || !typeDetail.Type.IsValueType;
            isInterfacedObject = typeDetail.Type.IsInterface && !typeDetail.HasIEnumerableGeneric && !typeDetail.HasIEnumerable;

            Setup();
        }

        protected virtual void Setup() { }

        public override sealed bool TryReadBoxed(ref JsonReader reader, ref ReadState state, out object? returnValue)
        {
            state.PushFrame();

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                {
                    state.StashFrame();
                    returnValue = default;
                    return false;
                }

                state.EndFrame();
                returnValue = valueObject;
                return true;
            }

            if (!ReadValueType(ref reader, ref state))
            {
                state.StashFrame();
                returnValue = default;
                return false;
            }

            if (!TryReadValue(ref reader, ref state, out var value))
            {
                state.StashFrame();
                returnValue = value;
                return false;
            }

            state.EndFrame();
            returnValue = value;
            return true;
        }
        public override sealed bool TryWriteBoxed(ref JsonWriter writer, ref WriteState state, object? value)
        {
            state.PushFrame();

            if (isNullable)
            {
                if (value is null)
                {
                    if (!writer.TryWriteNull(out state.CharsNeeded))
                    {
                        state.StashFrame();
                        return false;
                    }
                    return true;
                }
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = JsonConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value!))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, (TValue?)value!))
            {
                state.StashFrame();
                return false;
            }
            state.EndFrame();
            return true;
        }

        public bool TryRead(ref JsonReader reader, ref ReadState state, out TValue? returnValue)
        {
            state.PushFrame();

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                {
                    state.StashFrame();
                    returnValue = default;
                    return false;
                }

                state.EndFrame();
                returnValue = (TValue?)valueObject;
                return true;
            }

            if (!ReadValueType(ref reader, ref state))
            {
                state.StashFrame();
                returnValue = default;
                return false;
            }

            if (!TryReadValue(ref reader, ref state, out var value))
            {
                state.StashFrame();
                returnValue = value;
                return false;
            }

            state.EndFrame();
            returnValue = value;
            return true;
        }
        public bool TryWrite(ref JsonWriter writer, ref WriteState state, TValue? value)
        {
            state.PushFrame();

            if (isNullable)
            {
                if (value is null)
                {
                    if (!writer.TryWriteNull(out state.CharsNeeded))
                    {
                        state.StashFrame();
                        return false;
                    }
                    return true;
                }
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = JsonConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, value!))
            {
                state.StashFrame();
                return false;
            }
            state.EndFrame();
            return true;
        }

        public override sealed bool TryReadFromParent(ref JsonReader reader, ref ReadState state, TParent? parent)
        {
            if (state.StackSize > maxStackDepth)
                throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
            state.PushFrame();

            if (isInterfacedObject)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                var newTypeDetail = emptyImplementationType.GetTypeDetail();

                var newConverter = JsonConverterFactory<TParent>.Get(newTypeDetail, memberKey, getter, setter);

                if (!newConverter.TryReadValueBoxed(ref reader, ref state, out var valueObject))
                {
                    state.StashFrame();
                    return false;
                }

                if (setter is not null && parent is not null)
                    setter(parent, (TValue?)valueObject);
                state.EndFrame();
                return true;
            }

            if (!ReadValueType(ref reader, ref state))
            {
                state.StashFrame();
                return false;
            }

            if (!TryReadValue(ref reader, ref state, out var value))
            {
                state.StashFrame();
                return false;
            }

            if (setter is not null && parent is not null)
                setter(parent, value);
            state.EndFrame();
            return true;
        }
        public override sealed bool TryWriteFromParent(ref JsonWriter writer, ref WriteState state, TParent parent, string? propertyName)
        {
            if (state.StackSize >= maxStackDepth)
                throw new StackOverflowException($"{nameof(JsonConverter)} has reach the max depth of {state.StackSize}");
            state.PushFrame();

            if (getter == null)
            {
                state.EndFrame();
                return true;
            }
            var value = getter(parent);

            if (propertyName is not null)
            {
                if (value is null)
                {
                    if (state.DoNotWriteNullProperties)
                    {
                        state.EndFrame();
                        return true;
                    }
                    if (!state.Current.HasWrittenPropertyName)
                    {
                        if (!writer.TryWritePropertyName(propertyName, out state.CharsNeeded))
                        {
                            state.StashFrame();
                            return false;
                        }
                        state.Current.HasWrittenPropertyName = true;
                    }
                    if (isNullable)
                    {
                        if (!writer.TryWriteNull(out state.CharsNeeded))
                        {
                            state.StashFrame();
                            return false;
                        }
                        state.EndFrame();
                        return true;
                    }
                }
                else
                {
                    if (!state.Current.HasWrittenPropertyName)
                    {
                        if (!writer.TryWritePropertyName(propertyName, out state.CharsNeeded))
                        {
                            state.StashFrame();
                            return false;
                        }
                        state.Current.HasWrittenPropertyName = true;
                    }
                }
            }
            else
            {
                if (isNullable)
                {
                    if (value is null)
                    {
                        if (!writer.TryWriteNull(out state.CharsNeeded))
                        {
                            state.StashFrame();
                            return false;
                        }
                        state.EndFrame();
                        return true;
                    }
                }
            }

            if (isInterfacedObject)
            {
                var typeFromValue = value is null ? typeDetail : value.GetType().GetTypeDetail();

                if (typeFromValue.Type != typeDetail.Type)
                {
                    var newConverter = JsonConverterFactory<TParent>.Get(typeFromValue, memberKey, getter, setter);
                    if (!newConverter.TryWriteValueBoxed(ref writer, ref state, value!))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.HasWrittenPropertyName = false;
                    return true;
                }
            }

            if (!TryWriteValue(ref writer, ref state, value!))
            {
                state.StashFrame();
                return false;
            }

            state.EndFrame();
            state.Current.HasWrittenPropertyName = false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryReadValueBoxed(ref JsonReader reader, ref ReadState state, out object? value)
        {
            if (!ReadValueType(ref reader, ref state))
            {
                value = default;
                return false;
            }

            var read = TryReadValue(ref reader, ref state, out var v);
            value = v;
            return read;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed bool TryWriteValueBoxed(ref JsonWriter writer, ref WriteState state, object value)
            => TryWriteValue(ref writer, ref state, (TValue)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryReadValue(ref JsonReader reader, ref ReadState state, out TValue? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool TryWriteValue(ref JsonWriter writer, ref WriteState state, TValue value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void CollectedValuesSetter(TParent? parent, object? value)
        {
            if (setter is not null && parent is not null && value is not null)
                setter(parent, (TValue)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ReadNumberAsString(ref JsonReader reader, ref ReadState state,
#if !NETSTANDARD2_0
           [MaybeNullWhen(false)]
#endif
        out string value)
        {
            scoped Span<char> buffer;

            if (state.StringBuffer == null)
            {
                buffer = stackalloc char[128];
            }
            else
            {
                if (state.StringBuffer.Length > 128)
                {
                    buffer = state.StringBuffer;
                }
                else
                {
                    buffer = stackalloc char[128];
                    state.StringBuffer.CopyTo(buffer);
                    BufferArrayPool<char>.Return(state.StringBuffer);
                }
            }

            char c;
            if (state.NumberStage != ReadNumberStage.Value)
            {
                switch (state.NumberStage)
                {
                    case ReadNumberStage.ValueContinue:
                        goto startValueContinue;
                    case ReadNumberStage.Decimal:
                        goto startDecimal;
                    case ReadNumberStage.Exponent:
                        goto startExponent;
                    case ReadNumberStage.ExponentContinue:
                        goto startExponentContinue;
                }
            }

            //startValue:
            switch (state.Current.FirstChar)
            {
                case '0': break;
                case '1': break;
                case '2': break;
                case '3': break;
                case '4': break;
                case '5': break;
                case '6': break;
                case '7': break;
                case '8': break;
                case '9': break;
                case '-': break;
                default: throw reader.CreateException("Unexpected character");
            }
            if (state.StringPosition + 1 > buffer.Length)
            {
                var oldRented = state.StringBuffer;
                state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length * 2);
                buffer.CopyTo(state.StringBuffer);
                buffer = state.StringBuffer;
                if (oldRented != null)
                    BufferArrayPool<char>.Return(oldRented);
            }
            buffer[state.StringPosition++] = state.Current.FirstChar;

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    if (state.StringBuffer == null)
                    {
                        state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length);
                        buffer.Slice(0, state.StringPosition).CopyTo(state.StringBuffer);
                    }
                    value = default;
                    state.NumberStage = ReadNumberStage.ValueContinue;
                    return false;
                }
                switch (c)
                {
                    case '0': break;
                    case '1': break;
                    case '2': break;
                    case '3': break;
                    case '4': break;
                    case '5': break;
                    case '6': break;
                    case '7': break;
                    case '8': break;
                    case '9': break;
                    case '.':
                        state.NumberStage = ReadNumberStage.Decimal;
                        break;
                    case 'e':
                    case 'E':
                        state.NumberStage = ReadNumberStage.Exponent;
                        break;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    default: throw reader.CreateException("Unexpected character");
                }
                if (state.StringPosition + 1 > buffer.Length)
                {
                    var oldRented = state.StringBuffer;
                    state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length * 2);
                    buffer.CopyTo(state.StringBuffer);
                    buffer = state.StringBuffer;
                    if (oldRented != null)
                        BufferArrayPool<char>.Return(oldRented);
                }
                buffer[state.StringPosition++] = c;
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    if (state.StringBuffer == null)
                    {
                        state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length);
                        buffer.Slice(0, state.StringPosition).CopyTo(state.StringBuffer);
                    }
                    value = default;
                    state.NumberStage = ReadNumberStage.Decimal;
                    return false;
                }
                switch (c)
                {
                    case '0': break;
                    case '1': break;
                    case '2': break;
                    case '3': break;
                    case '4': break;
                    case '5': break;
                    case '6': break;
                    case '7': break;
                    case '8': break;
                    case '9': break;
                    case 'e':
                    case 'E':
                        state.NumberStage = ReadNumberStage.Exponent;
                        break;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    default: throw reader.CreateException("Unexpected character");
                }
                if (state.StringPosition + 1 > buffer.Length)
                {
                    var oldRented = state.StringBuffer;
                    state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length * 2);
                    buffer.CopyTo(state.StringBuffer);
                    buffer = state.StringBuffer;
                    if (oldRented != null)
                        BufferArrayPool<char>.Return(oldRented);
                }
                buffer[state.StringPosition++] = c;
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock)
                {
                    value = buffer.Slice(0, state.StringPosition).ToString();
                    if (state.StringBuffer != null)
                    {
                        BufferArrayPool<char>.Return(state.StringBuffer);
                        state.StringBuffer = null;
                    }
                    state.StringPosition = 0;
                    state.NumberStage = ReadNumberStage.Value;
                    return true;
                }
                state.CharsNeeded = 1;
                if (state.StringBuffer == null)
                {
                    state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length);
                    buffer.Slice(0, state.StringPosition).CopyTo(state.StringBuffer);
                }
                value = default;
                state.NumberStage = ReadNumberStage.Exponent;
                return false;
            }
            switch (c)
            {
                case '0': break;
                case '1': break;
                case '2': break;
                case '3': break;
                case '4': break;
                case '5': break;
                case '6': break;
                case '7': break;
                case '8': break;
                case '9': break;
                case '+': break;
                case '-': break;
                default:
                    throw reader.CreateException("Unexpected character");
            }
            if (state.StringPosition + 1 > buffer.Length)
            {
                var oldRented = state.StringBuffer;
                state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length * 2);
                buffer.CopyTo(state.StringBuffer);
                buffer = state.StringBuffer;
                if (oldRented != null)
                    BufferArrayPool<char>.Return(oldRented);
            }
            buffer[state.StringPosition++] = c;

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    if (state.StringBuffer == null)
                    {
                        state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length);
                        buffer.Slice(0, state.StringPosition).CopyTo(state.StringBuffer);
                    }
                    value = default;
                    state.NumberStage = ReadNumberStage.ExponentContinue;
                    return false;
                }

                switch (c)
                {
                    case '0': break;
                    case '1': break;
                    case '2': break;
                    case '3': break;
                    case '4': break;
                    case '5': break;
                    case '6': break;
                    case '7': break;
                    case '8': break;
                    case '9': break;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    default: throw reader.CreateException("Unexpected character");
                }
                if (state.StringPosition + 1 > buffer.Length)
                {
                    var oldRented = state.StringBuffer;
                    state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length * 2);
                    buffer.CopyTo(state.StringBuffer);
                    buffer = state.StringBuffer;
                    if (oldRented != null)
                        BufferArrayPool<char>.Return(oldRented);
                }
                buffer[state.StringPosition++] = c;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ReadNumberAsInt64(ref JsonReader reader, ref ReadState state, out long value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Value)
            {
                state.NumberInt64 = 0;
                state.NumberWorkingDouble = 0;
                state.NumberIsNegative = false;
                state.NumberWorkingIsNegative = false;
                state.NumberParseFailed = false;
                value = 0;
                workingNumber = 0;
                if (state.Current.ValueType == JsonValueType.String)
                    state.NumberStage = ReadNumberStage.ValueContinue;
                else if (state.Current.ValueType != JsonValueType.Number)
                    reader.CreateException("Bad JsonSerializer state");
            }
            else
            {
                value = state.NumberInt64;
                workingNumber = state.NumberWorkingDouble;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.ValueContinue:
                        goto startValueContinue;
                    case ReadNumberStage.Decimal:
                        goto startDecimal;
                    case ReadNumberStage.Exponent:
                        goto startExponent;
                    case ReadNumberStage.ExponentContinue:
                        goto startExponentContinue;
                }
            }

            //startValue:
            switch (state.Current.FirstChar)
            {
                case '0': value = 0; break;
                case '1': value = 1; break;
                case '2': value = 2; break;
                case '3': value = 3; break;
                case '4': value = 4; break;
                case '5': value = 5; break;
                case '6': value = 6; break;
                case '7': value = 7; break;
                case '8': value = 8; break;
                case '9': value = 9; break;
                case '-':
                    value = 0;
                    state.NumberIsNegative = true;
                    break;
                default:
                    if (state.Current.ValueType == JsonValueType.String)
                    {
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        else
                            state.NumberParseFailed = true;
                    }
                    else
                    {
                        throw reader.CreateException("Unexpected character");
                    }
                    break;
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberInt64 = value;
                    state.NumberStage = ReadNumberStage.ValueContinue;
                    return false;
                }
                switch (c)
                {
                    case '0': value *= 10; break;
                    case '1': value = value * 10 + 1; break;
                    case '2': value = value * 10 + 2; break;
                    case '3': value = value * 10 + 3; break;
                    case '4': value = value * 10 + 4; break;
                    case '5': value = value * 10 + 5; break;
                    case '6': value = value * 10 + 6; break;
                    case '7': value = value * 10 + 7; break;
                    case '8': value = value * 10 + 8; break;
                    case '9': value = value * 10 + 9; break;
                    case '.':
                        workingNumber = 10;
                        goto startDecimal;
                    case 'e':
                    case 'E':
                        goto startExponent;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberInt64 = value;
                    state.NumberStage = ReadNumberStage.Decimal;
                    return false;
                }
                switch (c)
                {
                    case '0': break;
                    case '1': break;
                    case '2': break;
                    case '3': break;
                    case '4': break;
                    case '5': break;
                    case '6': break;
                    case '7': break;
                    case '8': break;
                    case '9': break;
                    case 'e':
                    case 'E':
                        goto startExponent;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock)
                {
                    if (state.Current.ValueType == JsonValueType.String)
                        throw reader.CreateException("Unexpected character");
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Value;
                    if (state.NumberParseFailed)
                        value = default;
                    return true;
                }
                state.CharsNeeded = 1;
                state.NumberInt64 = value;
                state.NumberStage = ReadNumberStage.Exponent;
                return false;
            }
            switch (c)
            {
                case '0': workingNumber = 0; break;
                case '1': workingNumber = 1; break;
                case '2': workingNumber = 2; break;
                case '3': workingNumber = 3; break;
                case '4': workingNumber = 4; break;
                case '5': workingNumber = 5; break;
                case '6': workingNumber = 6; break;
                case '7': workingNumber = 7; break;
                case '8': workingNumber = 8; break;
                case '9': workingNumber = 9; break;
                case '+': workingNumber = 0; break;
                case '-':
                    workingNumber = 0;
                    state.NumberWorkingIsNegative = true;
                    break;
                default:
                    if (state.Current.ValueType == JsonValueType.String)
                    {
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        else
                            state.NumberParseFailed = true;
                    }
                    else
                    {
                        throw reader.CreateException("Unexpected character");
                    }
                    break;
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (long)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberInt64 = value;
                    state.NumberDouble = workingNumber;
                    state.NumberStage = ReadNumberStage.ExponentContinue;
                    return false;
                }

                switch (c)
                {
                    case '0': workingNumber *= 10; break;
                    case '1': workingNumber = workingNumber * 10 + 1; break;
                    case '2': workingNumber = workingNumber * 10 + 2; break;
                    case '3': workingNumber = workingNumber * 10 + 3; break;
                    case '4': workingNumber = workingNumber * 10 + 4; break;
                    case '5': workingNumber = workingNumber * 10 + 5; break;
                    case '6': workingNumber = workingNumber * 10 + 6; break;
                    case '7': workingNumber = workingNumber * 10 + 7; break;
                    case '8': workingNumber = workingNumber * 10 + 8; break;
                    case '9': workingNumber = workingNumber * 10 + 9; break;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (long)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (long)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ReadNumberAsUInt64(ref JsonReader reader, ref ReadState state, out ulong value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Value)
            {
                state.NumberUInt64 = 0;
                state.NumberWorkingDouble = 0;
                state.NumberIsNegative = false;
                state.NumberWorkingIsNegative = false;
                state.NumberParseFailed = false;
                value = 0;
                workingNumber = 0;
                if (state.Current.ValueType == JsonValueType.String)
                    state.NumberStage = ReadNumberStage.ValueContinue;
                else if (state.Current.ValueType != JsonValueType.Number)
                    reader.CreateException("Bad JsonSerializer state");
            }
            else
            {
                value = state.NumberUInt64;
                workingNumber = state.NumberWorkingDouble;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.ValueContinue:
                        goto startValueContinue;
                    case ReadNumberStage.Decimal:
                        goto startDecimal;
                    case ReadNumberStage.Exponent:
                        goto startExponent;
                    case ReadNumberStage.ExponentContinue:
                        goto startExponentContinue;
                }
            }

            //startValue:
            switch (state.Current.FirstChar)
            {
                case '0': value = 0; break;
                case '1': value = 1; break;
                case '2': value = 2; break;
                case '3': value = 3; break;
                case '4': value = 4; break;
                case '5': value = 5; break;
                case '6': value = 6; break;
                case '7': value = 7; break;
                case '8': value = 8; break;
                case '9': value = 9; break;
                case '-':
                    value = 0;
                    break;
                default:
                    if (state.Current.ValueType == JsonValueType.String)
                    {
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        else
                            state.NumberParseFailed = true;
                    }
                    else
                    {
                        throw reader.CreateException("Unexpected character");
                    }
                    break;
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberUInt64 = value;
                    state.NumberStage = ReadNumberStage.ValueContinue;
                    return false;
                }
                switch (c)
                {
                    case '0': value *= 10; break;
                    case '1': value = value * 10 + 1; break;
                    case '2': value = value * 10 + 2; break;
                    case '3': value = value * 10 + 3; break;
                    case '4': value = value * 10 + 4; break;
                    case '5': value = value * 10 + 5; break;
                    case '6': value = value * 10 + 6; break;
                    case '7': value = value * 10 + 7; break;
                    case '8': value = value * 10 + 8; break;
                    case '9': value = value * 10 + 9; break;
                    case '.':
                        workingNumber = 10;
                        goto startDecimal;
                    case 'e':
                    case 'E':
                        goto startExponent;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberUInt64 = value;
                    state.NumberStage = ReadNumberStage.Decimal;
                    return false;
                }
                switch (c)
                {
                    case '0': break;
                    case '1': break;
                    case '2': break;
                    case '3': break;
                    case '4': break;
                    case '5': break;
                    case '6': break;
                    case '7': break;
                    case '8': break;
                    case '9': break;
                    case 'e':
                    case 'E':
                        goto startExponent;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock)
                {
                    if (state.Current.ValueType == JsonValueType.String)
                        throw reader.CreateException("Unexpected character");
                    state.NumberStage = ReadNumberStage.Value;
                    if (state.NumberParseFailed)
                        value = default;
                    return true;
                }
                state.CharsNeeded = 1;
                state.NumberUInt64 = value;
                state.NumberStage = ReadNumberStage.Exponent;
                return false;
            }
            switch (c)
            {
                case '0': workingNumber = 0; break;
                case '1': workingNumber = 1; break;
                case '2': workingNumber = 2; break;
                case '3': workingNumber = 3; break;
                case '4': workingNumber = 4; break;
                case '5': workingNumber = 5; break;
                case '6': workingNumber = 6; break;
                case '7': workingNumber = 7; break;
                case '8': workingNumber = 8; break;
                case '9': workingNumber = 9; break;
                case '+': workingNumber = 0; break;
                case '-':
                    workingNumber = 0;
                    state.NumberWorkingIsNegative = true;
                    break;
                default:
                    if (state.Current.ValueType == JsonValueType.String)
                    {
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        else
                            state.NumberParseFailed = true;
                    }
                    else
                    {
                        throw reader.CreateException("Unexpected character");
                    }
                    break;
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (ulong)Math.Pow(10, workingNumber);
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberUInt64 = value;
                    state.NumberDouble = workingNumber;
                    state.NumberStage = ReadNumberStage.ExponentContinue;
                    return false;
                }

                switch (c)
                {
                    case '0': workingNumber *= 10; break;
                    case '1': workingNumber = workingNumber * 10 + 1; break;
                    case '2': workingNumber = workingNumber * 10 + 2; break;
                    case '3': workingNumber = workingNumber * 10 + 3; break;
                    case '4': workingNumber = workingNumber * 10 + 4; break;
                    case '5': workingNumber = workingNumber * 10 + 5; break;
                    case '6': workingNumber = workingNumber * 10 + 6; break;
                    case '7': workingNumber = workingNumber * 10 + 7; break;
                    case '8': workingNumber = workingNumber * 10 + 8; break;
                    case '9': workingNumber = workingNumber * 10 + 9; break;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (ulong)Math.Pow(10, workingNumber);
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (ulong)Math.Pow(10, workingNumber);
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ReadNumberAsDouble(ref JsonReader reader, ref ReadState state, out double value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Value)
            {
                state.NumberDouble = 0;
                state.NumberWorkingDouble = 0;
                state.NumberIsNegative = false;
                state.NumberWorkingIsNegative = false;
                state.NumberParseFailed = false;
                value = 0;
                workingNumber = 0;
                if (state.Current.ValueType == JsonValueType.String)
                    state.NumberStage = ReadNumberStage.ValueContinue;
                else if (state.Current.ValueType != JsonValueType.Number)
                    reader.CreateException("Bad JsonSerializer state");
            }
            else
            {
                value = state.NumberDouble;
                workingNumber = state.NumberWorkingDouble;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.ValueContinue:
                        goto startValueContinue;
                    case ReadNumberStage.Decimal:
                        goto startDecimal;
                    case ReadNumberStage.Exponent:
                        goto startExponent;
                    case ReadNumberStage.ExponentContinue:
                        goto startExponentContinue;
                }
            }

            //startValue:
            switch (state.Current.FirstChar)
            {
                case '0': value = 0; break;
                case '1': value = 1; break;
                case '2': value = 2; break;
                case '3': value = 3; break;
                case '4': value = 4; break;
                case '5': value = 5; break;
                case '6': value = 6; break;
                case '7': value = 7; break;
                case '8': value = 8; break;
                case '9': value = 9; break;
                case '-':
                    value = 0;
                    state.NumberIsNegative = true;
                    break;
                default:
                    if (state.Current.ValueType == JsonValueType.String)
                    {
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        else
                            state.NumberParseFailed = true;
                    }
                    else
                    {
                        throw reader.CreateException("Unexpected character");
                    }
                    break;
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberDouble = value;
                    state.NumberStage = ReadNumberStage.ValueContinue;
                    return false;
                }
                switch (c)
                {
                    case '0': value *= 10; break;
                    case '1': value = value * 10 + 1; break;
                    case '2': value = value * 10 + 2; break;
                    case '3': value = value * 10 + 3; break;
                    case '4': value = value * 10 + 4; break;
                    case '5': value = value * 10 + 5; break;
                    case '6': value = value * 10 + 6; break;
                    case '7': value = value * 10 + 7; break;
                    case '8': value = value * 10 + 8; break;
                    case '9': value = value * 10 + 9; break;
                    case '.':
                        workingNumber = 10;
                        goto startDecimal;
                    case 'e':
                    case 'E':
                        goto startExponent;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberDouble = value;
                    state.NumberWorkingDouble = workingNumber;
                    state.NumberStage = ReadNumberStage.Decimal;
                    return false;
                }
                switch (c)
                {
                    case '0': break;
                    case '1': value += 1 / workingNumber; break;
                    case '2': value += 2 / workingNumber; break;
                    case '3': value += 3 / workingNumber; break;
                    case '4': value += 4 / workingNumber; break;
                    case '5': value += 5 / workingNumber; break;
                    case '6': value += 6 / workingNumber; break;
                    case '7': value += 7 / workingNumber; break;
                    case '8': value += 8 / workingNumber; break;
                    case '9': value += 9 / workingNumber; break;
                    case 'e':
                    case 'E':
                        goto startExponent;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
                workingNumber *= 10;
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock)
                {
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Value;
                    if (state.NumberParseFailed)
                        value = default;
                    return true;
                }
                state.CharsNeeded = 1;
                state.NumberDouble = value;
                state.NumberStage = ReadNumberStage.Exponent;
                return false;
            }
            switch (c)
            {
                case '0': workingNumber = 0; break;
                case '1': workingNumber = 1; break;
                case '2': workingNumber = 2; break;
                case '3': workingNumber = 3; break;
                case '4': workingNumber = 4; break;
                case '5': workingNumber = 5; break;
                case '6': workingNumber = 6; break;
                case '7': workingNumber = 7; break;
                case '8': workingNumber = 8; break;
                case '9': workingNumber = 9; break;
                case '+': workingNumber = 0; break;
                case '-':
                    workingNumber = 0;
                    state.NumberWorkingIsNegative = true;
                    break;
                default:
                    if (state.Current.ValueType == JsonValueType.String)
                    {
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        else
                            state.NumberParseFailed = true;
                    }
                    else
                    {
                        throw reader.CreateException("Unexpected character");
                    }
                    break;
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (double)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberDouble = value;
                    state.NumberWorkingDouble = workingNumber;
                    state.NumberStage = ReadNumberStage.ExponentContinue;
                    return false;
                }

                switch (c)
                {
                    case '0': workingNumber *= 10; break;
                    case '1': workingNumber = workingNumber * 10 + 1; break;
                    case '2': workingNumber = workingNumber * 10 + 2; break;
                    case '3': workingNumber = workingNumber * 10 + 3; break;
                    case '4': workingNumber = workingNumber * 10 + 4; break;
                    case '5': workingNumber = workingNumber * 10 + 5; break;
                    case '6': workingNumber = workingNumber * 10 + 6; break;
                    case '7': workingNumber = workingNumber * 10 + 7; break;
                    case '8': workingNumber = workingNumber * 10 + 8; break;
                    case '9': workingNumber = workingNumber * 10 + 9; break;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (double)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (double)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ReadNumberAsDecimal(ref JsonReader reader, ref ReadState state, out decimal value)
        {
            decimal workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Value)
            {
                state.NumberDecimal = 0;
                state.NumberWorkingDecimal = 0;
                state.NumberIsNegative = false;
                state.NumberParseFailed = false;
                state.NumberWorkingIsNegative = false;
                value = 0;
                workingNumber = 0m;
                if (state.Current.ValueType == JsonValueType.String)
                    state.NumberStage = ReadNumberStage.ValueContinue;
                else if (state.Current.ValueType != JsonValueType.Number)
                    reader.CreateException("Bad JsonSerializer state");
            }
            else
            {
                value = state.NumberDecimal;
                workingNumber = state.NumberWorkingDecimal;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.ValueContinue:
                        goto startValueContinue;
                    case ReadNumberStage.Decimal:
                        goto startDecimal;
                    case ReadNumberStage.Exponent:
                        goto startExponent;
                    case ReadNumberStage.ExponentContinue:
                        goto startExponentContinue;
                }
            }

            //startValue:
            switch (state.Current.FirstChar)
            {
                case '0': value = 0; break;
                case '1': value = 1; break;
                case '2': value = 2; break;
                case '3': value = 3; break;
                case '4': value = 4; break;
                case '5': value = 5; break;
                case '6': value = 6; break;
                case '7': value = 7; break;
                case '8': value = 8; break;
                case '9': value = 9; break;
                case '-':
                    value = 0;
                    state.NumberIsNegative = true;
                    break;
                default:
                    if (state.Current.ValueType == JsonValueType.String)
                    {
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        else
                            state.NumberParseFailed = true;
                    }
                    else
                    {
                        throw reader.CreateException("Unexpected character");
                    }
                    break;
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberDecimal = value;
                    state.NumberStage = ReadNumberStage.ValueContinue;
                    return false;
                }
                switch (c)
                {
                    case '0': value *= 10; break;
                    case '1': value = value * 10 + 1; break;
                    case '2': value = value * 10 + 2; break;
                    case '3': value = value * 10 + 3; break;
                    case '4': value = value * 10 + 4; break;
                    case '5': value = value * 10 + 5; break;
                    case '6': value = value * 10 + 6; break;
                    case '7': value = value * 10 + 7; break;
                    case '8': value = value * 10 + 8; break;
                    case '9': value = value * 10 + 9; break;
                    case '.':
                        workingNumber = 10;
                        goto startDecimal;
                    case 'e':
                    case 'E':
                        goto startExponent;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberDecimal = value;
                    state.NumberWorkingDecimal = workingNumber;
                    state.NumberStage = ReadNumberStage.Decimal;
                    return false;
                }
                switch (c)
                {
                    case '0': break;
                    case '1': value += 1 / workingNumber; break;
                    case '2': value += 2 / workingNumber; break;
                    case '3': value += 3 / workingNumber; break;
                    case '4': value += 4 / workingNumber; break;
                    case '5': value += 5 / workingNumber; break;
                    case '6': value += 6 / workingNumber; break;
                    case '7': value += 7 / workingNumber; break;
                    case '8': value += 8 / workingNumber; break;
                    case '9': value += 9 / workingNumber; break;
                    case 'e':
                    case 'E':
                        goto startExponent;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
                workingNumber *= 10;
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock)
                {
                    if (state.Current.ValueType == JsonValueType.String)
                        throw reader.CreateException("Unexpected character");
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Value;
                    if (state.NumberParseFailed)
                        value = default;
                    return true;
                }
                state.CharsNeeded = 1;
                state.NumberDecimal = value;
                state.NumberStage = ReadNumberStage.Exponent;
                return false;
            }
            switch (c)
            {
                case '0': workingNumber = 0; break;
                case '1': workingNumber = 1; break;
                case '2': workingNumber = 2; break;
                case '3': workingNumber = 3; break;
                case '4': workingNumber = 4; break;
                case '5': workingNumber = 5; break;
                case '6': workingNumber = 6; break;
                case '7': workingNumber = 7; break;
                case '8': workingNumber = 8; break;
                case '9': workingNumber = 9; break;
                case '+': workingNumber = 0; break;
                case '-':
                    workingNumber = 0;
                    state.NumberWorkingIsNegative = true;
                    break;
                default:
                    if (state.Current.ValueType == JsonValueType.String)
                    {
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        else
                            state.NumberParseFailed = true;
                    }
                    else
                    {
                        throw reader.CreateException("Unexpected character");
                    }
                    break;
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (decimal)Math.Pow(10, (double)workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    state.NumberDecimal = value;
                    state.NumberWorkingDecimal = workingNumber;
                    state.NumberStage = ReadNumberStage.ExponentContinue;
                    return false;
                }

                switch (c)
                {
                    case '0': workingNumber *= 10; break;
                    case '1': workingNumber = workingNumber * 10 + 1; break;
                    case '2': workingNumber = workingNumber * 10 + 2; break;
                    case '3': workingNumber = workingNumber * 10 + 3; break;
                    case '4': workingNumber = workingNumber * 10 + 4; break;
                    case '5': workingNumber = workingNumber * 10 + 5; break;
                    case '6': workingNumber = workingNumber * 10 + 6; break;
                    case '7': workingNumber = workingNumber * 10 + 7; break;
                    case '8': workingNumber = workingNumber * 10 + 8; break;
                    case '9': workingNumber = workingNumber * 10 + 9; break;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (state.Current.ValueType == JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (decimal)Math.Pow(10, (double)workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (decimal)Math.Pow(10, (double)workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.Current.ValueType != JsonValueType.String)
                            throw reader.CreateException("Unexpected character");
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.Current.ValueType == JsonValueType.String)
                        {
                            if (state.ErrorOnTypeMismatch)
                                throw reader.CreateException($"Cannot convert to {typeDetail.Type.GetNiceName()} (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                            else
                                state.NumberParseFailed = true;
                        }
                        else
                        {
                            throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }
        }
    }
}