// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Serialization.Bytes.Converters;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Test.Helpers.Models
{
    public class CustomTypeByteConverter : ByteConverter<CustomType>
    {
        protected override bool StackRequired => false;

        protected override bool TryReadValue(ref ByteReader reader, ref ReadState state, out CustomType value)
        {
            if (!reader.TryRead(out string str, out state.SizeNeeded))
            {
                value = null;
                return false;
            }
            var values = str.Split(" - ");
            value = new CustomType()
            {
                Things1 = values[0],
                Things2 = values.Length > 0 ? values[1] : null
            };
            return true;
        }

        protected override bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in CustomType value)
        {
            var str = $"{value.Things1} - {value.Things2}";
            return writer.TryWrite(str, out state.BytesNeeded);
        }
    }
}
