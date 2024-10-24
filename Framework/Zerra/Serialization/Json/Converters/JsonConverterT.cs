// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.State;
using Zerra.Serialization.Json.IO;
using System;

namespace Zerra.Serialization.Json.Converters
{
    public abstract class JsonConverter<TParent> : JsonConverter
    {
        public abstract bool TryReadFromParent(ref JsonReader reader, ref ReadState state, TParent? parent, string? propertyName = null);
        public abstract bool TryWriteFromParent(ref JsonWriter writer, ref WriteState state, TParent parent, string? propertyName = null, ReadOnlySpan<char> jsonNameSegmentChars = default, ReadOnlySpan<byte> jsonNameSegmentBytes = default, bool ignoreDoNotWriteNullProperties = false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadValueBoxed(ref JsonReader reader, ref ReadState state, JsonValueType valueType, out object? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWriteValueBoxed(ref JsonWriter writer, ref WriteState state, in object value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void CollectedValuesSetter(TParent? parent, in object? value);
    }
}