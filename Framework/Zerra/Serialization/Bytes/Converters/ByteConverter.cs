// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.IO;

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

        protected OptionsStruct options { get; private set; }
        public TypeDetail? TypeDetail { get; private set; }
        protected MemberDetail? memberDetail { get; private set; }

        public void Setup(OptionsStruct options, TypeDetail? typeDetail, MemberDetail? memberDetail)
        {
            this.options = options;
            this.TypeDetail = typeDetail;
            this.memberDetail = memberDetail;
            Setup();
        }

        public virtual void Setup() { }

        public abstract void Read(ref ByteReader reader, ref ReadState state);
        public abstract void Write(ref ByteWriter writer, ref WriteState state);
    }
}