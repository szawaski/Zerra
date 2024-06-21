// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using System;
using Zerra.IO;
using Zerra.Serialization.Json.State;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Collections;

namespace Zerra.Serialization.Json.Converters
{
    public abstract partial class JsonConverter
    {
        //The max converter stack before we unwind
        protected const int maxStackDepth = 32;

        public abstract void Setup(TypeDetail typeDetail, string? memberKey, Delegate? getterDelegate, Delegate? setterDelegate);

        public abstract bool TryReadBoxed(ref CharReader reader, ref ReadState state, out object? value);
        public abstract bool TryWriteBoxed(ref CharWriter writer, ref WriteState state, object? value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadValueType(ref CharReader reader, ref ReadState state)
        {
            if (state.Current.ValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadSkipWhiteSpace(out state.Current.FirstChar))
                {
                    state.CharsNeeded = 1;
                    return false;
                }
            }
            else
            {
                if (state.Current.ValueType != JsonValueType.ReadingInProgress)
                    return true;
            }

            switch (state.Current.FirstChar)
            {
                case '"':
                    state.Current.ValueType = JsonValueType.String_Started;
                    break;

                case '{':
                    state.Current.ValueType = JsonValueType.Object_Started;
                    break;

                case '[':
                    state.Current.ValueType = JsonValueType.Array_Started;
                    break;

                case 'n':
                    if (!reader.TryReadSpan(out var s, 3))
                    {
                        state.Current.ValueType = JsonValueType.ReadingInProgress;
                        state.CharsNeeded = 3;
                        return false;
                    }
                    if (s[0] != 'u' || s[1] != 'l' || s[2] != 'l')
                        throw reader.CreateException("Invalid number/true/false/null");
                    state.Current.ValueType = JsonValueType.Null_Completed;
                    break;
                case 't':
                    if (!reader.TryReadSpan(out s, 3))
                    {
                        state.Current.ValueType = JsonValueType.ReadingInProgress;
                        state.CharsNeeded = 3;
                        return false;
                    }
                    if (s[0] != 'r' || s[1] != 'u' || s[2] != 'e')
                        throw reader.CreateException("Invalid number/true/false/null");
                    state.Current.ValueType = JsonValueType.True_Completed;
                    break;
                case 'f':
                    if (!reader.TryReadSpan(out s, 4))
                    {
                        state.Current.ValueType = JsonValueType.ReadingInProgress;
                        state.CharsNeeded = 4;
                        return false;
                    }
                    if (s[0] != 'a' || s[1] != 'l' || s[2] != 's' || s[3] != 'e')
                        throw reader.CreateException("Invalid number/true/false/null");
                    state.Current.ValueType = JsonValueType.False_Completed;
                    break;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    state.Current.ValueType = JsonValueType.Number_Started;
                    break;
                default:
                    throw reader.CreateException("Invalid number/true/false/null");
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool Drain(ref CharReader reader, ref ReadState state)
        {
            if (state.Current.ValueType <= JsonValueType.ReadingInProgress)
            {
                if (!ReadValueType(ref reader, ref state))
                    return false;
            }
            switch (state.Current.ValueType)
            {
                case JsonValueType.Object_Started:
                    return DrainObject(ref reader, ref state);
                case JsonValueType.Array_Started:
                    return DrainArray(ref reader, ref state);
                case JsonValueType.String_Started:
                    return DrainString(ref reader, ref state);
                case JsonValueType.Null_Completed:
                    return true;
                case JsonValueType.False_Completed:
                    return true;
                case JsonValueType.True_Completed:
                    return true;
                case JsonValueType.Number_Started:
                    return DrainNumber(ref reader, ref state);
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainObject(ref CharReader reader, ref ReadState state)
        {
            char c;
            for (; ; )
            {
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
                    state.PushFrame(null);
                    if (!Drain(ref reader, ref state))
                    {
                        state.Current.HasReadProperty = true;
                        state.Current.HasReadPropertySeperator = true;
                        return false;
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

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainArray(ref CharReader reader, ref ReadState state)
        {
            for (; ; )
            {
                switch (state.Current.State)
                {
                    case 0: //array value or end
                        if (!reader.TryReadSkipWhiteSpace(out var c))
                        {
                            state.CharsNeeded = 1;
                            return false;
                        }

                        if (c == ']')
                        {
                            state.EndFrame();
                            return true;
                        }

                        reader.BackOne();

                        state.Current.State = 1;
                        goto case 1;

                    case 1: //array value
                        state.PushFrame();
                        if (!Drain(ref reader, ref state))
                            return false;

                        state.Current.State = 2;
                        goto case 2;

                    case 2: //next array value or end
                        if (!reader.TryReadSkipWhiteSpace(out c))
                        {
                            state.CharsNeeded = 1;
                            return false;
                        }
                        switch (c)
                        {
                            case ',':
                                state.Current.State = 1;
                                break;
                            case ']':
                                state.EndFrame();
                                return true;
                            default:
                                throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainString(ref CharReader reader, ref ReadState state)
        {
            char c;
            for (; ; )
            {
                //reading segment
                if (!state.ReadStringEscape)
                {
                    if (!reader.TryReadSpanUntil(out var s, '\"', '\\'))
                    {
                        state.CharsNeeded = 1;
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
                    if (!reader.TryRead(out c))
                    {
                        state.CharsNeeded = 1;
                        return false;
                    }

                    if (c == 'u')
                        state.ReadStringEscapeUnicode = true;
                    else
                        continue;
                }

                //reading escape unicode
                if (!reader.TryReadSpan(out var unicodeSpan, 4))
                {
                    state.CharsNeeded = 4;
                    return false;
                }
                if (!lowUnicodeHexToChar.TryGetValue(unicodeSpan.ToString(), out var unicodeChar))
                    throw reader.CreateException("Incomplete escape sequence");

                state.ReadStringEscape = false;
                state.ReadStringEscapeUnicode = false;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainNumber(ref CharReader reader, ref ReadState state)
        {
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
                case '-':
                    break;
                default: throw reader.CreateException("Unexpected character");
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
            }

        startExponent:
            if (!reader.TryRead(out c))
            {
                if (state.IsFinalBlock)
                {
                    state.NumberStage = ReadNumberStage.Value;
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
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadString(ref CharReader reader, ref ReadState state,
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
                    state.StringBuffer = null;
                }
            }

            char c;
            for (; ; )
            {
                //reading segment
                if (!state.ReadStringEscape)
                {
                    if (!reader.TryReadSpanUntil(out var s, '\"', '\\'))
                    {
                        state.CharsNeeded = 1;
                        if (state.StringBuffer == null)
                        {
                            state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length);
                            buffer.Slice(0, state.StringPosition).CopyTo(state.StringBuffer);
                        }
                        value = default;
                        return false;
                    }
                    if (state.StringPosition + s.Length - 1 > buffer.Length)
                    {
                        var oldRented = state.StringBuffer;
                        state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length * 2);
                        buffer.CopyTo(state.StringBuffer);
                        buffer = state.StringBuffer;
                        if (oldRented != null)
                            BufferArrayPool<char>.Return(oldRented);
                    }
                    s.Slice(0, s.Length - 1).CopyTo(buffer.Slice(state.StringPosition));
                    state.StringPosition += s.Length - 1;
                    c = s[s.Length - 1];
                    if (c == '\"')
                    {
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
                        return true;
                    }
                    state.ReadStringEscape = true;
                }

                //reading escape
                if (!state.ReadStringEscapeUnicode)
                {
                    if (!reader.TryRead(out c))
                    {
                        state.CharsNeeded = 1;
                        if (state.StringBuffer == null)
                        {
                            state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length);
                            buffer.Slice(0, state.StringPosition).CopyTo(state.StringBuffer);
                        }
                        value = default;
                        return false;
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
                if (!reader.TryReadSpan(out var unicodeSpan, 4))
                {
                    state.CharsNeeded = 4;
                    if (state.StringBuffer == null)
                    {
                        state.StringBuffer = BufferArrayPool<char>.Rent(buffer.Length);
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
        protected static bool ReadNumberAsString(ref CharReader reader, ref ReadState state,
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
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
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
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
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
            if (!reader.TryRead(out c))
            {
                if (state.IsFinalBlock)
                {
                    value = buffer.Slice(0, state.StringPosition).ToString();
                    if (state.StringBuffer != null)
                    {
                        BufferArrayPool<char>.Return(state.StringBuffer);
                        state.StringBuffer = null;
                    }
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

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        value = buffer.Slice(0, state.StringPosition).ToString();
                        if (state.StringBuffer != null)
                        {
                            BufferArrayPool<char>.Return(state.StringBuffer);
                            state.StringBuffer = null;
                        }
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
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
        protected static bool ReadNumberAsInt64(ref CharReader reader, ref ReadState state, out long value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Value)
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
                default: throw reader.CreateException("Unexpected character");
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
            }

        startExponent:
            if (!reader.TryRead(out c))
            {
                if (state.IsFinalBlock)
                {
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Value;
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
                default: throw reader.CreateException("Unexpected character");
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (long)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
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
                        return true;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadNumberAsUInt64(ref CharReader reader, ref ReadState state, out ulong value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Value)
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
                default: throw reader.CreateException("Unexpected character");
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
            }

        startExponent:
            if (!reader.TryRead(out c))
            {
                if (state.IsFinalBlock)
                {
                    state.NumberStage = ReadNumberStage.Value;
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
                default: throw reader.CreateException("Unexpected character");
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (ulong)Math.Pow(10, workingNumber);
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (ulong)Math.Pow(10, workingNumber);
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadNumberAsDouble(ref CharReader reader, ref ReadState state, out double value)
        {
            double workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Value)
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
                default: throw reader.CreateException("Unexpected character");
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
                workingNumber *= 10;
            }

        startExponent:
            if (!reader.TryRead(out c))
            {
                if (state.IsFinalBlock)
                {
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Value;
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
                default: throw reader.CreateException("Unexpected character");
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (double)Math.Pow(10, workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
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
                        return true;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ReadNumberAsDecimal(ref CharReader reader, ref ReadState state, out decimal value)
        {
            decimal workingNumber;
            char c;
            if (state.NumberStage == ReadNumberStage.Value)
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
                default: throw reader.CreateException("Unexpected character");
            }

        startValueContinue:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
            }

        startDecimal:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
                        return true;
                }
                workingNumber *= 10;
            }

        startExponent:
            if (!reader.TryRead(out c))
            {
                if (state.IsFinalBlock)
                {
                    if (state.NumberIsNegative)
                        value *= -1;
                    state.NumberStage = ReadNumberStage.Value;
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
                default: throw reader.CreateException("Unexpected character");
            }

        startExponentContinue:
            for (; ; )
            {
                if (!reader.TryRead(out c))
                {
                    if (state.IsFinalBlock)
                    {
                        if (state.NumberWorkingIsNegative)
                            workingNumber *= -1;
                        value *= (decimal)Math.Pow(10, (double)workingNumber);
                        if (state.NumberIsNegative)
                            value *= -1;
                        state.NumberStage = ReadNumberStage.Value;
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
                        state.NumberStage = ReadNumberStage.Value;
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
                        return true;
                }
            }
        }
    }
}
