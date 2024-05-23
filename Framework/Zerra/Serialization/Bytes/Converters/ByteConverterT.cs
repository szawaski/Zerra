// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.IO;

namespace Zerra.Serialization
{
    internal abstract class ByteConverter<TParent> : ByteConverter, IByteConverterDiscoverable<TParent>
    {
        public override sealed void Read(ref ByteReader reader, ref ReadState state)
        {
            Read(ref reader, ref state, (TParent?)state.CurrentFrame.ResultObject);
        }
        public override sealed void Write(ref ByteWriter writer, ref WriteState state)
        {
            Write(ref writer, ref state, (TParent?)state.CurrentFrame.Object);
        }

        public abstract void Read(ref ByteReader reader, ref ReadState state, TParent? parent);
        public abstract void Write(ref ByteWriter writer, ref WriteState state, TParent? parent);
    }
}