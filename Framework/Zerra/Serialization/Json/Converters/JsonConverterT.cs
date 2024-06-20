// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.State;
using Zerra.IO;

namespace Zerra.Serialization.Json.Converters
{
    public abstract class JsonConverter<TParent> : JsonConverter
    {
        public abstract bool TryReadFromParent(ref CharReader reader, ref ReadState state, TParent? parent);
        public abstract bool TryWriteFromParent(ref CharWriter writer, ref WriteState state, TParent parent);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadValueBoxed(ref CharReader reader, ref ReadState state, out object? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWriteValueBoxed(ref CharWriter writer, ref WriteState state, object? value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void CollectedValuesSetter(TParent? parent, object? value);
    }
}