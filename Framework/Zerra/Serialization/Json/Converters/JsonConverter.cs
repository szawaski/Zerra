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
                    state.Current.ValueType = JsonValueType.String;
                    break;

                case '{':
                    state.Current.ValueType = JsonValueType.Object;
                    break;

                case '[':
                    state.Current.ValueType = JsonValueType.Array;
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
                    state.Current.ValueType = JsonValueType.Number;
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
                    state.PushFrame();
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
                        state.StringPosition = 0;
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
    }
}
