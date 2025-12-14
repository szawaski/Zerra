// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.General
{
    internal sealed class ByteConverterType : ByteConverter<Type?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out Type? value)
        {
            if (!reader.TryRead(out string? typeName, out state.SizeNeeded))
            {
                value = default;
                return false;
            }
            if (typeName is null)
            {
                value = default;
                return true;
            }

            value = TypeFinder.GetTypeFromName(typeName);
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in Type? value)
            => writer.TryWrite(value!.FullName ?? throw new InvalidOperationException($"Type {value} does not have a {nameof(value.FullName)}"), out state.SizeNeeded);
    }
}