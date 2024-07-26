// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using System;
using Zerra.IO;
using Zerra.Serialization.Json.IO;
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
        protected const int maxStackDepth = 31;

        public abstract void Setup(TypeDetail typeDetail, string memberKey, Delegate? getterDelegate, Delegate? setterDelegate);

        public abstract bool TryReadBoxed(ref JsonReader reader, ref ReadState state, out object? value);
        public abstract bool TryWriteBoxed(ref JsonWriter writer, ref WriteState state, object? value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool DrainFromParent(ref JsonReader reader, ref ReadState state)
        {
            if (state.Current.ChildValueType == JsonValueType.NotDetermined)
            {
                if (!reader.TryReadValueType(out state.Current.ChildValueType))
                    return false;
            }

            switch (state.Current.ChildValueType)
            {
                case JsonValueType.Object:
                    state.PushFrame();
                    if (!DrainObject(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.Array:
                    state.PushFrame();
                    if (!DrainArray(ref reader, ref state))
                    {
                        state.StashFrame();
                        return false;
                    }
                    state.EndFrame();
                    state.Current.ChildValueType = JsonValueType.NotDetermined;
                    return true;
                case JsonValueType.String:
                    state.PushFrame();
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
                    state.PushFrame();
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
                    if (!reader.TryReadSpanUntilQuoteOrEscape(out var s))
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
                if (!reader.TryReadEscapeHex(out var unicodeSpan))
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
                    if (!reader.TryReadSpanUntilQuoteOrEscape(out var s))
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
                if (!reader.TryReadEscapeHex(out var unicodeSpan))
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
        protected static bool WriteString(ref JsonWriter writer, ref WriteState state, string? value)
        {
            if (value == null)
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
            if (value == null)
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
    }
}
