// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.IO;
using System;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization
{
    internal abstract class ByteConverter
    {       
        //In byte array, object properties start with index values from SerializerIndexAttribute or property order
        protected const ushort indexOffset = 1; //offset index values to reseve for Flag: 0

        //Flag: 0 indicating the end of an object
        protected const ushort endObjectFlagUShort = 0;
        protected const byte endObjectFlagByte = 0;
        protected static readonly byte[] endObjectFlagUInt16 = new byte[2] { 0, 0 };

        //The max converter stack before we unwind
        protected const int maxStackDepth = 32;

        protected ByteConverterOptions options { get; private set; }
        protected string? memberKey { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Setup(ByteConverterOptions options, TypeDetail? typeDetail, string? memberKey, Delegate? getterBoxed, Delegate? setterBoxed)
        {
            this.options = options;
            this.memberKey = memberKey;
            Setup(typeDetail, getterBoxed, setterBoxed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void Setup(TypeDetail? typeDetail, Delegate? getterBoxed, Delegate? setterBoxed);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryRead(ref ByteReader reader, ref ReadState state, object? parent);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract bool TryWrite(ref ByteWriter writer, ref WriteState state, object? parent);
    }
}