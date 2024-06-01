// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.General
{
    internal sealed class ByteConverterType<TParent> : ByteConverter<TParent, Type?>
    {
        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out Type? value)
        {
            if (!reader.TryRead(state.Current.NullFlags, out string? typeName, out state.BytesNeeded))
            {
                value = default;
                return false;
            }
            if (typeName == null)
            {
                value = default;
                return true;
            }

            value = Discovery.GetTypeFromName(typeName);
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, Type? value)
            => writer.TryWrite(value?.FullName, state.Current.NullFlags, out state.BytesNeeded);
    }
}