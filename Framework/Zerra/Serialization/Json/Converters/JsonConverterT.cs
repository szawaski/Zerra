// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.State;
using Zerra.Serialization.Json.IO;

namespace Zerra.Serialization.Json.Converters
{
    public abstract class JsonConverter<TParent> : JsonConverter
    {
        public abstract bool TryReadFromParent(ref JsonReader reader, ref ReadState state, TParent? parent);
        public abstract bool TryWriteFromParent(ref JsonWriter writer, ref WriteState state, TParent parent, string? propertyName = null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadValueBoxed(ref JsonReader reader, ref ReadState state, out object? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWriteValueBoxed(ref JsonWriter writer, ref WriteState state, object? value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void CollectedValuesSetter(TParent? parent, object? value);
    }
}