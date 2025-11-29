// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.State;
using Zerra.Serialization.Bytes.IO;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization.Bytes.Converters
{
    public abstract class ByteConverter
    {
        //In byte array, object properties start with index values from SerializerIndexAttribute or property order
        protected const ushort indexOffset = 1; //offset index values to reserve for Flag: 0

        //Flag: 0 indicating the end of an object
        protected const ushort endObjectFlagUShort = 0;
        protected const byte endObjectFlagByte = 0;
        protected static readonly byte[] endObjectFlagUInt16 = [0, 0];

        //The max converter stack before we unwind
        protected const int maxStackDepth = 31;

        public abstract void Setup(string memberKey, Delegate? getterDelegate, Delegate? setterDelegate);

        public abstract bool TryReadBoxed(ref ByteReader reader, ref ReadState state, out object? value);
        public abstract bool TryWriteBoxed(ref ByteWriter writer, ref WriteState state, in object? value);

        public abstract bool TryReadFromParent(ref ByteReader reader, ref ReadState state, object? parent, bool nullFlags, bool drainBytes = false);
        public abstract bool TryWriteFromParent(ref ByteWriter writer, ref WriteState state, object parent, bool nullFlags, ushort indexProperty = default, ReadOnlySpan<byte> indexPropertyName = default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryReadValueBoxed(ref ByteReader reader, ref ReadState state, out object? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWriteValueBoxed(ref ByteWriter writer, ref WriteState state, in object value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void CollectedValuesSetter(object? parent, in object? value);
    }
}