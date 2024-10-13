// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using System;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.State;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Zerra.Serialization.Json.Converters.Collections;
using Zerra.Buffers;

namespace Zerra.Serialization.Json.Converters
{
    public abstract partial class JsonConverter
    {
        //The max converter stack before we unwind
        protected const int maxStackDepth = 31;

        public abstract void Setup(TypeDetail typeDetail, string memberKey, Delegate? getterDelegate, Delegate? setterDelegate);

        public abstract bool TryReadBoxed(ref JsonReader reader, ref ReadState state, out object? value);
        public abstract bool TryWriteBoxed(ref JsonWriter writer, ref WriteState state, in object? value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadNumberAsString(ref JsonReader reader, ref ReadState state,
#if !NETSTANDARD2_0
         [MaybeNullWhen(false)]
#endif
        out string value)
        {
            scoped Span<char> buffer;

            if (state.StringBuffer is null)
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
                    state.StringBuffer.AsSpan().Slice(0, state.StringPosition).CopyTo(buffer);
                    ArrayPoolHelper<char>.Return(state.StringBuffer);
                    state.StringBuffer = null;
                }
            }

            char c;
            if (state.NumberStage != ReadNumberStage.Setup)
            {
                switch (state.NumberStage)
                {
                    case ReadNumberStage.Value:
                        goto startValue;
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

        startValue:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    value = buffer.Slice(0, state.StringPosition).ToString();
                    if (state.StringBuffer is not null)
                    {
                        ArrayPoolHelper<char>.Return(state.StringBuffer);
                        state.StringBuffer = null;
                    }
                    state.StringPosition = 0;
                    state.NumberStage = ReadNumberStage.Setup;
                    return true;
                }
                state.CharsNeeded = 1;
                if (state.StringBuffer is null)
                {
                    state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length);
                    buffer.Slice(0, state.StringPosition).CopyTo(state.StringBuffer);
                }
                value = default;
                state.NumberStage = ReadNumberStage.Value;
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
                case '-': break;
                default: throw reader.CreateException("Unexpected character");
            }
            if (state.StringPosition + 1 > buffer.Length)
            {
                var oldRented = state.StringBuffer;
                state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length * 2);
                buffer.CopyTo(state.StringBuffer);
                buffer = state.StringBuffer;
                if (oldRented is not null)
                    ArrayPoolHelper<char>.Return(oldRented);
            }
            buffer[state.StringPosition++] = c;

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer is not null)
                        {
                            ArrayPoolHelper<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    if (state.StringBuffer is null)
                    {
                        state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length);
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
                        if (state.StringPosition + 1 > buffer.Length)
                        {
                            var oldRented = state.StringBuffer;
                            state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length * 2);
                            buffer.CopyTo(state.StringBuffer);
                            buffer = state.StringBuffer;
                            if (oldRented is not null)
                                ArrayPoolHelper<char>.Return(oldRented);
                        }
                        buffer[state.StringPosition++] = c;
                        goto startDecimal;
                    case 'e':
                    case 'E':
                        if (state.StringPosition + 1 > buffer.Length)
                        {
                            var oldRented = state.StringBuffer;
                            state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length * 2);
                            buffer.CopyTo(state.StringBuffer);
                            buffer = state.StringBuffer;
                            if (oldRented is not null)
                                ArrayPoolHelper<char>.Return(oldRented);
                        }
                        buffer[state.StringPosition++] = c;
                        goto startExponent;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer is not null)
                        {
                            ArrayPoolHelper<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer is not null)
                        {
                            ArrayPoolHelper<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default: throw reader.CreateException("Unexpected character");
                }
                if (state.StringPosition + 1 > buffer.Length)
                {
                    var oldRented = state.StringBuffer;
                    state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length * 2);
                    buffer.CopyTo(state.StringBuffer);
                    buffer = state.StringBuffer;
                    if (oldRented is not null)
                        ArrayPoolHelper<char>.Return(oldRented);
                }
                buffer[state.StringPosition++] = c;
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer is not null)
                        {
                            ArrayPoolHelper<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    if (state.StringBuffer is null)
                    {
                        state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length);
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
                        if (state.StringPosition + 1 > buffer.Length)
                        {
                            var oldRented = state.StringBuffer;
                            state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length * 2);
                            buffer.CopyTo(state.StringBuffer);
                            buffer = state.StringBuffer;
                            if (oldRented is not null)
                                ArrayPoolHelper<char>.Return(oldRented);
                        }
                        buffer[state.StringPosition++] = c;
                        goto startExponent;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer is not null)
                        {
                            ArrayPoolHelper<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer is not null)
                        {
                            ArrayPoolHelper<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default: throw reader.CreateException("Unexpected character");
                }
                if (state.StringPosition + 1 > buffer.Length)
                {
                    var oldRented = state.StringBuffer;
                    state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length * 2);
                    buffer.CopyTo(state.StringBuffer);
                    buffer = state.StringBuffer;
                    if (oldRented is not null)
                        ArrayPoolHelper<char>.Return(oldRented);
                }
                buffer[state.StringPosition++] = c;
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    value = buffer.Slice(0, state.StringPosition).ToString();
                    if (state.StringBuffer is not null)
                    {
                        ArrayPoolHelper<char>.Return(state.StringBuffer);
                        state.StringBuffer = null;
                    }
                    state.StringPosition = 0;
                    state.NumberStage = ReadNumberStage.Setup;
                    return true;
                }
                state.CharsNeeded = 1;
                if (state.StringBuffer is null)
                {
                    state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length);
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
                state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length * 2);
                buffer.CopyTo(state.StringBuffer);
                buffer = state.StringBuffer;
                if (oldRented is not null)
                    ArrayPoolHelper<char>.Return(oldRented);
            }
            buffer[state.StringPosition++] = c;

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer is not null)
                        {
                            ArrayPoolHelper<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    }
                    state.CharsNeeded = 1;
                    if (state.StringBuffer is null)
                    {
                        state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length);
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
                        if (state.StringBuffer is not null)
                        {
                            ArrayPoolHelper<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer is not null)
                        {
                            ArrayPoolHelper<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default: throw reader.CreateException("Unexpected character");
                }
                if (state.StringPosition + 1 > buffer.Length)
                {
                    var oldRented = state.StringBuffer;
                    state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length * 2);
                    buffer.CopyTo(state.StringBuffer);
                    buffer = state.StringBuffer;
                    if (oldRented is not null)
                        ArrayPoolHelper<char>.Return(oldRented);
                }
                buffer[state.StringPosition++] = c;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadNumberAsInt64(ref JsonReader reader, ref ReadState state, out long value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Setup)
            {
                state.NumberInt64 = 0;
                state.NumberWorkingDouble = 0;
                state.NumberIsNegative = false;
                state.NumberWorkingIsNegative = false;
                value = 0;
                workingNumber = 0;
            }
            else
            {
                value = state.NumberInt64;
                workingNumber = state.NumberWorkingDouble;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.Value:
                        goto startValue;
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

        startValue:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Setup;
                    return true;
                }
                state.CharsNeeded = 1;
                state.NumberInt64 = value;
                state.NumberStage = ReadNumberStage.Value;
                return false;
            }
            switch (c)
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
                    throw reader.CreateException("Unexpected character");
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Setup;
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
                    throw reader.CreateException("Unexpected character");
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (long)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (long)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadNumberAsUInt64(ref JsonReader reader, ref ReadState state, out ulong value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Setup)
            {
                state.NumberUInt64 = 0;
                state.NumberWorkingDouble = 0;
                state.NumberIsNegative = false;
                state.NumberWorkingIsNegative = false;
                value = 0;
                workingNumber = 0;
            }
            else
            {
                value = state.NumberUInt64;
                workingNumber = state.NumberWorkingDouble;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.Value:
                        goto startValue;
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

        startValue:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    state.NumberStage = ReadNumberStage.Setup;
                    return true;
                }
                state.CharsNeeded = 1;
                state.NumberUInt64 = value;
                state.NumberStage = ReadNumberStage.Value;
                return false;
            }
            switch (c)
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
                    throw reader.CreateException("Unexpected character");
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        state.NumberStage = ReadNumberStage.Setup;
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
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        state.NumberStage = ReadNumberStage.Setup;
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
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    state.NumberStage = ReadNumberStage.Setup;
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
                    throw reader.CreateException("Unexpected character");
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (ulong)Math.Pow(10, workingNumber);
                        state.NumberStage = ReadNumberStage.Setup;
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
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (ulong)Math.Pow(10, workingNumber);
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (ulong)Math.Pow(10, workingNumber);
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadNumberAsDouble(ref JsonReader reader, ref ReadState state, out double value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Setup)
            {
                state.NumberDouble = 0;
                state.NumberWorkingDouble = 0;
                state.NumberIsNegative = false;
                state.NumberWorkingIsNegative = false;
                value = 0;
                workingNumber = 0;
            }
            else
            {
                value = state.NumberDouble;
                workingNumber = state.NumberWorkingDouble;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.Value:
                        goto startValue;
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

        startValue:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Setup;
                    return true;
                }
                state.CharsNeeded = 1;
                state.NumberDouble = value;
                state.NumberStage = ReadNumberStage.Value;
                return false;
            }
            switch (c)
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
                    throw reader.CreateException("Unexpected character");
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
                workingNumber *= 10;
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Setup;
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
                    throw reader.CreateException("Unexpected character");
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (double)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (double)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadNumberAsDecimal(ref JsonReader reader, ref ReadState state, out decimal value)
        {
            decimal workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Setup)
            {
                state.NumberDecimal = 0;
                state.NumberWorkingDecimal = 0;
                state.NumberIsNegative = false;
                state.NumberWorkingIsNegative = false;
                value = 0;
                workingNumber = 0m;
            }
            else
            {
                value = state.NumberDecimal;
                workingNumber = state.NumberWorkingDecimal;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.Value:
                        goto startValue;
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

        startValue:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Setup;
                    return true;
                }
                state.CharsNeeded = 1;
                state.NumberDecimal = value;
                state.NumberStage = ReadNumberStage.Value;
                return false;
            }
            switch (c)
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
                    throw reader.CreateException("Unexpected character");
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
                workingNumber *= 10;
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Setup;
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
                    throw reader.CreateException("Unexpected character");
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (decimal)Math.Pow(10, (double)workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (decimal)Math.Pow(10, (double)workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
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
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadStringAsInt64(ref JsonReader reader, ref ReadState state, out long? value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Setup)
            {
                state.NumberInt64 = 0;
                state.NumberWorkingDouble = 0;
                state.NumberIsNegative = false;
                state.NumberWorkingIsNegative = false;
                state.NumberParseFailed = false;
                value = 0;
                workingNumber = 0;
            }
            else
            {
                value = state.NumberInt64;
                workingNumber = state.NumberWorkingDouble;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.Value:
                        goto startValue;
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

        startValue:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                    throw reader.CreateException("Unexpected character");
                state.CharsNeeded = 1;
                state.NumberInt64 = value.Value;
                state.NumberStage = ReadNumberStage.Value;
                return false;
            }
            switch (c)
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
                case '"':
                    value = null;
                    return true;
                default:
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    state.NumberParseFailed = true;
                    break;
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberInt64 = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberInt64 = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                    throw reader.CreateException("Unexpected character");
                state.CharsNeeded = 1;
                state.NumberInt64 = value.Value;
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
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    state.NumberParseFailed = true;
                    break;
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberInt64 = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (long)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadStringAsUInt64(ref JsonReader reader, ref ReadState state, out ulong? value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Setup)
            {
                state.NumberUInt64 = 0;
                state.NumberWorkingDouble = 0;
                state.NumberIsNegative = false;
                state.NumberWorkingIsNegative = false;
                state.NumberParseFailed = false;
                value = 0;
                workingNumber = 0;
            }
            else
            {
                value = state.NumberUInt64;
                workingNumber = state.NumberWorkingDouble;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.Value:
                        goto startValue;
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

        startValue:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                    throw reader.CreateException("Unexpected character");
                state.CharsNeeded = 1;
                state.NumberUInt64 = value.Value;
                state.NumberStage = ReadNumberStage.Value;
                return false;
            }
            switch (c)
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
                case '"':
                    value = null;
                    return true;
                default:
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    state.NumberParseFailed = true;
                    break;
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberUInt64 = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberUInt64 = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                    throw reader.CreateException("Unexpected character");
                state.CharsNeeded = 1;
                state.NumberUInt64 = value.Value;
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
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    state.NumberParseFailed = true;
                    break;
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberUInt64 = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (ulong)Math.Pow(10, workingNumber);
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadStringAsDouble(ref JsonReader reader, ref ReadState state, out double? value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Setup)
            {
                state.NumberDouble = 0;
                state.NumberWorkingDouble = 0;
                state.NumberIsNegative = false;
                state.NumberWorkingIsNegative = false;
                state.NumberParseFailed = false;
                value = 0;
                workingNumber = 0;
            }
            else
            {
                value = state.NumberDouble;
                workingNumber = state.NumberWorkingDouble;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.Value:
                        goto startValue;
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

        startValue:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                    throw reader.CreateException("Unexpected character");
                state.CharsNeeded = 1;
                state.NumberDouble = value.Value;
                state.NumberStage = ReadNumberStage.Value;
                return false;
            }
            switch (c)
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
                case '"':
                    value = null;
                    return true;
                default:
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    state.NumberParseFailed = true;
                    break;
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberDouble = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberDouble = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
                workingNumber *= 10;
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                    throw reader.CreateException("Unexpected character");
                state.CharsNeeded = 1;
                state.NumberDouble = value.Value;
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
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    state.NumberParseFailed = true;
                    break;
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberDouble = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (double)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadStringAsDecimal(ref JsonReader reader, ref ReadState state, out decimal? value)
        {
            decimal workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Setup)
            {
                state.NumberDecimal = 0;
                state.NumberWorkingDecimal = 0;
                state.NumberIsNegative = false;
                state.NumberParseFailed = false;
                state.NumberWorkingIsNegative = false;
                value = 0;
                workingNumber = 0m;
            }
            else
            {
                value = state.NumberDecimal;
                workingNumber = state.NumberWorkingDecimal;

                switch (state.NumberStage)
                {
                    case ReadNumberStage.Value:
                        goto startValue;
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

        startValue:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                    throw reader.CreateException("Unexpected character");
                state.CharsNeeded = 1;
                state.NumberDecimal = value.Value;
                state.NumberStage = ReadNumberStage.Value;
                return false;
            }
            switch (c)
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
                case '"':
                    value = null;
                    return true;
                default:
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    state.NumberParseFailed = true;
                    break;
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberDecimal = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberDecimal = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
                workingNumber *= 10;
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                    throw reader.CreateException("Unexpected character");
                state.CharsNeeded = 1;
                state.NumberDecimal = value.Value;
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
                    if (state.ErrorOnTypeMismatch)
                        throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                    state.NumberParseFailed = true;
                    break;
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                        throw reader.CreateException("Unexpected character");
                    state.CharsNeeded = 1;
                    state.NumberDecimal = value.Value;
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
                        throw reader.CreateException("Unexpected character");
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (decimal)Math.Pow(10, (double)workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    case '"':
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Setup;
                        if (state.NumberParseFailed)
                            value = default;
                        return true;
                    default:
                        if (state.ErrorOnTypeMismatch)
                            throw reader.CreateException($"Cannot convert to number (disable {nameof(state.ErrorOnTypeMismatch)} to prevent this exception)");
                        state.NumberParseFailed = true;
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainFromParent(ref JsonReader reader, ref ReadState state)
        {
            if (state.Current.ChildValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.Current.ChildValueType, out state.CharsNeeded))
                    return false;
            }

            switch (state.Current.ChildValueType)
            {
                case JsonValueType.Object:
                    state.PushFrame(null);
                    if (!DrainObject(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Array:
                    state.PushFrame(null);
                    if (!DrainArray(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.String:
                    state.PushFrame(null);
                    if (!DrainString(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Null_Completed:
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.False_Completed:
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.True_Completed:
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Number:
                    state.PushFrame(null);
                    if (!DrainNumber(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool Drain(ref JsonReader reader, ref ReadState state, JsonValueType valueType)
        {
            switch (valueType)
            {
                case JsonValueType.Object:
                    return DrainObject(ref reader, ref state);
                case JsonValueType.Array:
                    return DrainArray(ref reader, ref state);
                case JsonValueType.String:
                    return DrainString(ref reader, ref state);
                case JsonValueType.Null_Completed:
                    return true;
                case JsonValueType.False_Completed:
                    return true;
                case JsonValueType.True_Completed:
                    return true;
                case JsonValueType.Number:
                    return DrainNumber(ref reader, ref state);
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainObject(ref JsonReader reader, ref ReadState state)
        {
            char c;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    return false;
                }

                if (c == '}')
                    return true;

                reader.BackOne();

                state.Current.HasCreated = true;
            }

            for (; ; )
            {
                if (!state.Current.HasReadProperty)
                {
                    if (!ReadString(ref reader, ref state, false, out var name))
                        return false;

                    if (String.IsNullOrWhiteSpace(name))
                        throw reader.CreateException("Unexpected character");
                }

                if (!state.Current.HasReadSeperator)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.Current.HasReadProperty = true;
                        state.CharsNeeded = 1;
                        return false;
                    }
                    if (c != ':')
                        throw reader.CreateException("Unexpected character");
                }

                if (!state.Current.HasReadValue)
                {
                    if (!DrainFromParent(ref reader, ref state))
                    {
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        return false;
                    }
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.HasReadProperty = true;
                    state.Current.HasReadSeperator = true;
                    state.Current.HasReadValue = true;
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

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainArray(ref JsonReader reader, ref ReadState state)
        {
            char c;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    return false;
                }

                if (c == ']')
                {
                    return true;
                }

                reader.BackOne();
            }

            for (; ; )
            {
                if (!state.Current.HasReadValue)
                {
                    if (!DrainFromParent(ref reader, ref state))
                    {
                        state.Current.HasCreated = true;
                        return false;
                    }
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.HasCreated = true;
                    state.Current.HasReadValue = true;
                    return false;
                }

                if (c == ']')
                    return true;

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadValue = false;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainString(ref JsonReader reader, ref ReadState state)
        {
            char c;
            for (; ; )
            {
                //reading segment
                if (!state.ReadStringEscape)
                {
                    if (!reader.TryReadSpanUntilQuoteOrEscape(out var s, out state.CharsNeeded))
                    {
                        return false;
                    }
                    c = s[s.Length - 1];
                    if (c == '\"')
                    {
                        return true;
                    }
                    state.ReadStringEscape = true;
                }

                //reading escape
                if (!state.ReadStringEscapeUnicode)
                {
                    if (!reader.TryReadNext(out c))
                    {
                        state.CharsNeeded = 1;
                        return false;
                    }

                    if (c == 'u')
                    {
                        state.ReadStringEscapeUnicode = true;
                    }
                    else
                    {
                        state.ReadStringEscape = false;
                        continue;
                    }
                }

                //reading escape unicode
                if (!reader.TryReadEscapeHex(out var unicodeSpan, out state.CharsNeeded))
                {
                    return false;
                }
                if (!lowUnicodeHexToChar.TryGetValue(unicodeSpan.ToString(), out var unicodeChar))
                    throw reader.CreateException("Incomplete escape sequence");

                state.ReadStringEscape = false;
                state.ReadStringEscapeUnicode = false;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainNumber(ref JsonReader reader, ref ReadState state)
        {
            char c;
            if (state.NumberStage != ReadNumberStage.Setup)
            {
                switch (state.NumberStage)
                {
                    case ReadNumberStage.Value:
                        goto startValue;
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

        startValue:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    state.NumberStage = ReadNumberStage.Setup;
                    return true;
                }
                state.CharsNeeded = 1;
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
                case '-':
                    break;
                default: throw reader.CreateException("Unexpected character");
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    }
                    state.CharsNeeded = 1;
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
                        goto startDecimal;
                    case 'e':
                    case 'E':
                        goto startExponent;
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    }
                    state.CharsNeeded = 1;
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
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                }
            }

        startExponent:
            if (!reader.TryReadNext(out c))
            {
                if (state.IsFinalBlock && reader.Position == reader.Length)
                {
                    state.NumberStage = ReadNumberStage.Setup;
                    return true;
                }
                state.CharsNeeded = 1;
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
                case '-':
                    break;
                default: throw reader.CreateException("Unexpected character");
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryReadNext(out c))
                {
                    if (state.IsFinalBlock && reader.Position == reader.Length)
                    {
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    }
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
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Setup;
                        return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadString(ref JsonReader reader, ref ReadState state, bool hasStarted,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out string value)
        {
            scoped Span<char> buffer;

            if (!hasStarted && !state.ReadStringStart)
            {
                if (!reader.TryReadNextQuote())
                {
                    state.CharsNeeded = 1;
                    value = default;
                    return false;
                }
                state.ReadStringStart = true;
            }

            if (state.StringBuffer is null)
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
                    ArrayPoolHelper<char>.Return(state.StringBuffer);
                    state.StringBuffer = null;
                }
            }

            char c;
            for (; ; )
            {
                //reading segment
                if (!state.ReadStringEscape)
                {
                    if (!reader.TryReadSpanUntilQuoteOrEscape(out var s, out state.CharsNeeded))
                    {
                        if (state.StringBuffer is null)
                        {
                            state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length);
                            buffer.Slice(0, state.StringPosition).CopyTo(state.StringBuffer);
                        }
                        value = default;
                        return false;
                    }
                    if (state.StringPosition + s.Length - 1 > buffer.Length)
                    {
                        var oldRented = state.StringBuffer;
                        state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length * 2);
                        buffer.CopyTo(state.StringBuffer);
                        buffer = state.StringBuffer;
                        if (oldRented is not null)
                            ArrayPoolHelper<char>.Return(oldRented);
                    }
                    s.Slice(0, s.Length - 1).CopyTo(buffer.Slice(state.StringPosition));
                    state.StringPosition += s.Length - 1;
                    c = s[s.Length - 1];
                    if (c == '\"')
                    {
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer is not null)
                        {
                            ArrayPoolHelper<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        state.StringPosition = 0;
                        if (!hasStarted)
                            state.ReadStringStart = false;
                        return true;
                    }
                    state.ReadStringEscape = true;
                }

                //reading escape
                if (!state.ReadStringEscapeUnicode)
                {
                    if (!reader.TryReadNext(out c))
                    {
                        state.CharsNeeded = 1;
                        if (state.StringBuffer is null)
                        {
                            state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length);
                            buffer.Slice(0, state.StringPosition).CopyTo(state.StringBuffer);
                        }
                        value = default;
                        return false;
                    }

                    if (state.StringPosition + 1 > buffer.Length)
                    {
                        var oldRented = state.StringBuffer;
                        state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length * 2);
                        buffer.CopyTo(state.StringBuffer);
                        buffer = state.StringBuffer;
                        if (oldRented is not null)
                            ArrayPoolHelper<char>.Return(oldRented);
                    }

                    switch (c)
                    {
                        case 'b':
                            buffer[state.StringPosition++] = '\b';
                            state.ReadStringEscape = false;
                            continue;
                        case 't':
                            buffer[state.StringPosition++] = '\t';
                            state.ReadStringEscape = false;
                            continue;
                        case 'n':
                            buffer[state.StringPosition++] = '\n';
                            state.ReadStringEscape = false;
                            continue;
                        case 'f':
                            buffer[state.StringPosition++] = '\f';
                            state.ReadStringEscape = false;
                            continue;
                        case 'r':
                            buffer[state.StringPosition++] = '\r';
                            state.ReadStringEscape = false;
                            continue;
                        case 'u':
                            state.ReadStringEscapeUnicode = true;
                            break;
                        default:
                            buffer[state.StringPosition++] = c;
                            state.ReadStringEscape = false;
                            continue;
                    }
                }

                //reading escape unicode
                if (!reader.TryReadEscapeHex(out var unicodeSpan, out state.CharsNeeded))
                {
                    if (state.StringBuffer is null)
                    {
                        state.StringBuffer = ArrayPoolHelper<char>.Rent(buffer.Length);
                        buffer.Slice(0, state.StringPosition).CopyTo(state.StringBuffer);
                    }
                    value = default;
                    return false;
                }
                if (!lowUnicodeHexToChar.TryGetValue(unicodeSpan.ToString(), out var unicodeChar))
                    throw reader.CreateException("Incomplete escape sequence");

                //length checked in earlier state
                buffer[state.StringPosition++] = unicodeChar;

                state.ReadStringEscape = false;
                state.ReadStringEscapeUnicode = false;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool WriteString(ref JsonWriter writer, ref WriteState state, string? value)
        {
            if (value is null)
            {
                if (!writer.TryWriteNull(out state.CharsNeeded))
                    return false;
                return true;
            }

            if (state.WorkingStringStage == 0)
            {
                if (!writer.TryWriteQuote(out state.CharsNeeded))
                    return false;
                state.WorkingStringStage = 1;
            }

            if (state.WorkingStringStage == 1)
            {
                if (value.Length == 0)
                {
                    if (!writer.TryWriteQuote(out state.CharsNeeded))
                        return false;
                    state.WorkingStringStage = 0;
                    return true;
                }
                state.WorkingString = value.AsMemory();
                state.WorkingStringStage = 2;
            }

            var chars = state.WorkingString.Span;
            for (; state.WorkingStringIndex < chars.Length; state.WorkingStringIndex++)
            {
                var c = chars[state.WorkingStringIndex];
                char escapedChar;
                switch (c)
                {
                    case '"':
                        escapedChar = '"';
                        break;
                    case '\\':
                        escapedChar = '\\';
                        break;
                    case '\b':
                        escapedChar = 'b';
                        break;
                    case '\f':
                        escapedChar = 'f';
                        break;
                    case '\n':
                        escapedChar = 'n';
                        break;
                    case '\r':
                        escapedChar = 'r';
                        break;
                    case '\t':
                        escapedChar = 't';
                        break;
                    default:
                        if (c >= ' ') //32
                            continue;

                        if (state.WorkingStringStage == 2)
                        {
                            var slice = chars.Slice(state.WorkingStringStart, state.WorkingStringIndex - state.WorkingStringStart);
                            if (!writer.TryWrite(slice, out state.CharsNeeded))
                                return false;
                            state.WorkingStringStage = 3;
                        }

                        var code = lowUnicodeIntToEncodedHex[c];
                        if (!writer.TryWriteRaw(code, out state.CharsNeeded))
                            return false;
                        state.WorkingStringStage = 2;
                        state.WorkingStringStart = state.WorkingStringIndex + 1;
                        continue;
                }

                if (state.WorkingStringStage == 2)
                {
                    var slice = chars.Slice(state.WorkingStringStart, state.WorkingStringIndex - state.WorkingStringStart);
                    if (!writer.TryWrite(slice, out state.CharsNeeded))
                        return false;
                    state.WorkingStringStage = 3;
                }
                if (state.WorkingStringStage == 3)
                {
                    if (!writer.TryWriteEscape(out state.CharsNeeded))
                        return false;
                    state.WorkingStringStage = 4;
                }
                if (!writer.TryWriteRaw(escapedChar, out state.CharsNeeded))
                    return false;
                state.WorkingStringStage = 2;
                state.WorkingStringStart = state.WorkingStringIndex + 1;
            }

            if (state.WorkingStringStage == 2)
            {
                if (chars.Length < state.WorkingStringStart)
                {
                    state.CharsNeeded = state.WorkingStringStart;
                    return false;
                }
                else if (chars.Length > state.WorkingStringStart)
                {
                    var slice = chars.Slice(state.WorkingStringStart, chars.Length - state.WorkingStringStart);
                    if (!writer.TryWrite(slice, out state.CharsNeeded))
                        return false;
                }
                state.WorkingStringStage = 3;
            }

            if (!writer.TryWriteQuote(out state.CharsNeeded))
                return false;

            state.WorkingStringStage = 0;
            state.WorkingStringIndex = 0;
            state.WorkingStringStart = 0;
            state.WorkingString = null;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool WriteChar(ref JsonWriter writer, ref WriteState state, char? value)
        {
            if (value is null)
            {
                if (!writer.TryWriteNull(out state.CharsNeeded))
                    return false;
                return true;
            }

            switch (value.Value)
            {
                case '\\':
                    if (!writer.TryWriteQuoted("\\\\", out state.CharsNeeded))
                        return false;
                    state.WorkingStringStage = 0;
                    return true;
                case '"':
                    if (!writer.TryWriteQuoted("\\\"", out state.CharsNeeded))
                        return false;
                    state.WorkingStringStage = 0;
                    return true;
                case '/':
                    if (!writer.TryWriteQuoted("\\/", out state.CharsNeeded))
                        return false;
                    state.WorkingStringStage = 0;
                    return true;
                case '\b':
                    if (!writer.TryWriteQuoted("\\b", out state.CharsNeeded))
                        return false;
                    state.WorkingStringStage = 0;
                    return true;
                case '\t':
                    if (!writer.TryWriteQuoted("\\t", out state.CharsNeeded))
                        return false;
                    state.WorkingStringStage = 0;
                    return true;
                case '\n':
                    if (!writer.TryWriteQuoted("\\n", out state.CharsNeeded))
                        return false;
                    state.WorkingStringStage = 0;
                    return true;
                case '\f':
                    if (!writer.TryWriteQuoted("\\f", out state.CharsNeeded))
                        return false;
                    state.WorkingStringStage = 0;
                    return true;
                case '\r':
                    if (!writer.TryWriteQuoted("\\r", out state.CharsNeeded))
                        return false;
                    state.WorkingStringStage = 0;
                    return true;
            }

            if (value < ' ') //32
            {
                var code = lowUnicodeIntToEncodedHex[value.Value];
                if (!writer.TryWriteQuoted(code, out state.CharsNeeded))
                    return false;
                return true;
            }

            if (!writer.TryWriteQuoted(value.Value, out state.CharsNeeded))
                return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadJsonObject(ref JsonReader reader, ref ReadState state, out JsonObject? value)
        {
            if (state.EntryValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.EntryValueType, out state.CharsNeeded))
                {
                    value = default;
                    return false;
                }
            }

            switch (state.EntryValueType)
            {
                case JsonValueType.Object:
                    state.PushFrame(null);
                    if (!ReadJsonObjectObject(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        value = default;
                        return false;
                    }
                    state.EndFrame();
                    return true;
                case JsonValueType.Array:
                    state.PushFrame(null);
                    if (!ReadJsonObjectArray(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        value = default;
                        return false;
                    }
                    state.EndFrame();
                    return true;
                case JsonValueType.String:
                    if (!ReadString(ref reader, ref state, true, out var str))
                    {
                        value = default;
                        return false;
                    }
                    value = new JsonObject(str);
                    return true;
                case JsonValueType.Null_Completed:
                    value = new JsonObject();
                    return true;
                case JsonValueType.False_Completed:
                    value = new JsonObject(false);
                    return true;
                case JsonValueType.True_Completed:
                    value = new JsonObject(true);
                    return true;
                case JsonValueType.Number:
                    if (!ReadNumberAsDecimal(ref reader, ref state, out var number))
                    {
                        value = default!;
                        return false;
                    }
                    value = new JsonObject(number);
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObjectFromParent(ref JsonReader reader, ref ReadState state, in JsonObject parent, string property)
        {
            if (state.Current.ChildValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.Current.ChildValueType, out state.CharsNeeded))
                    return false;
            }

            JsonObject? value;

            switch (state.Current.ChildValueType)
            {
                case JsonValueType.Object:
                    state.PushFrame(null);
                    if (!ReadJsonObjectObject(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    parent[property] = value!;
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Array:
                    state.PushFrame(null);
                    if (!ReadJsonObjectArray(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    parent[property] = value!;
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.String:
                    if (!ReadString(ref reader, ref state, true, out var str))
                    {
                        value = default;
                        return false;
                    }
                    parent[property] = new JsonObject(str!);
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Null_Completed:
                    parent[property] = new JsonObject();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.False_Completed:
                    parent[property] = new JsonObject(false);
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.True_Completed:
                    parent[property] = new JsonObject(true);
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Number:
                    if (!ReadNumberAsDecimal(ref reader, ref state, out var number))
                    {
                        value = default!;
                        return false;
                    }
                    parent[property] = new JsonObject(number!);
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObjectFromParent(ref JsonReader reader, ref ReadState state, in Action<JsonObject> addMethod)
        {
            if (state.Current.ChildValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.Current.ChildValueType, out state.CharsNeeded))
                    return false;
            }

            JsonObject? value;

            switch (state.Current.ChildValueType)
            {
                case JsonValueType.Object:
                    state.PushFrame(null);
                    if (!ReadJsonObjectObject(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    addMethod(value!);
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Array:
                    state.PushFrame(null);
                    if (!ReadJsonObjectArray(ref reader, ref state, out value))
                    {
                        state.StashFrame();
                        return false;
                    }
                    addMethod(value!);
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.String:
                    if (!ReadString(ref reader, ref state, true, out var str))
                    {
                        value = default;
                        return false;
                    }
                    addMethod(new JsonObject(str!));
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Null_Completed:
                    addMethod(new JsonObject());
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.False_Completed:
                    addMethod(new JsonObject(false));
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.True_Completed:
                    addMethod(new JsonObject(true));
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Number:
                    if (!ReadNumberAsDecimal(ref reader, ref state, out var number))
                    {
                        value = default!;
                        return false;
                    }
                    addMethod(new JsonObject(number));
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObject(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out JsonObject? value)
        {
            switch (valueType)
            {
                case JsonValueType.Object:
                    return ReadJsonObjectObject(ref reader, ref state, out value);
                case JsonValueType.Array:
                    return ReadJsonObjectArray(ref reader, ref state, out value);
                case JsonValueType.String:
                    if (!ReadString(ref reader, ref state, true, out var str))
                    {
                        value = default;
                        return false;
                    }
                    value = new JsonObject(str);
                    return true;
                case JsonValueType.Null_Completed:
                    value = new JsonObject();
                    return true;
                case JsonValueType.False_Completed:
                    value = new JsonObject(false);
                    return true;
                case JsonValueType.True_Completed:
                    value = new JsonObject(true);
                    return true;
                case JsonValueType.Number:
                    if (!ReadNumberAsDecimal(ref reader, ref state, out var number))
                    {
                        value = default!;
                        return false;
                    }
                    value = new JsonObject(number);
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObjectObject(ref JsonReader reader, ref ReadState state, out JsonObject? value)
        {
            char c;

            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    value = default;
                    return false;
                }

                value = new JsonObject(new Dictionary<string, JsonObject>());

                if (c == '}')
                    return true;

                reader.BackOne();

                state.Current.HasCreated = true;
            }
            else
            {
                value = (JsonObject)state.Current.Object!;
            }

            for (; ; )
            {
                string? property;
                if (!state.Current.HasReadProperty)
                {
                    if (!ReadString(ref reader, ref state, false, out property))
                    {
                        state.Current.Object = value;
                        return false;
                    }

                    if (String.IsNullOrWhiteSpace(property))
                        throw reader.CreateException("Unexpected character");
                }
                else
                {
                    property = (string)state.Current.Property!;
                }

                if (!state.Current.HasReadSeperator)
                {
                    if (!reader.TryReadNextSkipWhiteSpace(out c))
                    {
                        state.Current.Object = value;
                        state.Current.Property = property;
                        state.Current.HasReadProperty = true;
                        state.CharsNeeded = 1;
                        return false;
                    }
                    if (c != ':')
                        throw reader.CreateException("Unexpected character");
                }

                if (!state.Current.HasReadValue)
                {
                    if (!ReadJsonObjectFromParent(ref reader, ref state, value, property))
                    {
                        state.Current.Object = value;
                        state.Current.Property = property;
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadSeperator = true;
                        return false;
                    }
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.Object = value;
                    state.Current.Property = property;
                    state.Current.HasReadProperty = true;
                    state.Current.HasReadSeperator = true;
                    state.Current.HasReadValue = true;
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

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadJsonObjectArray(ref JsonReader reader, ref ReadState state, out JsonObject? value)
        {
            char c;

            ArrayOrListAccessor<JsonObject> accessor;
            if (!state.Current.HasCreated)
            {
                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    value = default;
                    return false;
                }

                if (c == ']')
                {
                    value = new JsonObject(new List<JsonObject>(0));
                    return true;
                }

                reader.BackOne();

                if (reader.TryPeakArrayLength(out var length))
                    accessor = new ArrayOrListAccessor<JsonObject>(new JsonObject[length]);
                else
                    accessor = new ArrayOrListAccessor<JsonObject>();
            }
            else
            {
                accessor = (ArrayOrListAccessor<JsonObject>)state.Current.Object!;
            }

            for (; ; )
            {
                if (!state.Current.HasReadValue)
                {
                    if (!ReadJsonObjectFromParent(ref reader, ref state, accessor.Add))
                    {
                        state.Current.Object = accessor;
                        state.Current.HasCreated = true;
                        value = default;
                        return false;
                    }
                }

                if (!reader.TryReadNextSkipWhiteSpace(out c))
                {
                    state.CharsNeeded = 1;
                    state.Current.Object = accessor;
                    state.Current.HasCreated = true;
                    state.Current.HasReadValue = true;
                    value = default;
                    return false;
                }

                if (c == ']')
                {
                    value = new JsonObject(accessor.ToList());
                    return true;
                }

                if (c != ',')
                    throw reader.CreateException("Unexpected character");

                state.Current.HasReadValue = false;
            }
        }
    }
}
