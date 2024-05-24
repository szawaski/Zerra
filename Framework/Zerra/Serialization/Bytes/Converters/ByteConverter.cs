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

        protected ByteConverterOptions options { get; private set; }
        protected TypeDetail? typeDetail { get; private set; }
        protected Delegate? getter { get; private set; }
        protected Delegate? setter { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Setup(ByteConverterOptions options, TypeDetail? typeDetail, Delegate? getter, Delegate? setter)
        {
            this.options = options;
            this.typeDetail = typeDetail;
            this.getter = getter;
            this.setter = setter;
            SetupRoot();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void SetupRoot() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void Read(ref ByteReader reader, ref ReadState state);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract void Write(ref ByteWriter writer, ref WriteState state);
    }
}