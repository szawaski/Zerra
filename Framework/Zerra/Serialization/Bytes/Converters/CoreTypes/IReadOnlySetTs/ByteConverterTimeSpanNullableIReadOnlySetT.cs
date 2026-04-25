// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET5_0_OR_GREATER

using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.CoreTypes.IReadOnlySetTs
{
    internal sealed class ByteConverterTimeSpanNullableIReadOnlySet : ByteConverter<IReadOnlySet<TimeSpan?>>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out IReadOnlySet<TimeSpan?>? value)
        {
            if (!reader.TryRead(out HashSet<TimeSpan?>? valueTyped, out state.SizeNeeded))
            {
                value = default;
                return false;
            }

            value = valueTyped;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in IReadOnlySet<TimeSpan?> value)
            => writer.TryWrite(value, value.Count, out state.SizeNeeded);
    }
}

#endif