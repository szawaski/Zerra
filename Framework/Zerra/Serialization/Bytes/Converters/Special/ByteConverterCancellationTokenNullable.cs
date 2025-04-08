// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Special
{
    internal sealed class ByteConverterCancellationTokenNullable<TParent> : ByteConverter<TParent, CancellationToken?>
    {
        protected override bool StackRequired => false;

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out CancellationToken? value)
        {
            if (!reader.TryReadIsNull(out var isNull, out state.SizeNeeded))
            {
                value = default;
                return false;
            }
            value = isNull ? null : CancellationToken.None;
            return true;
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in CancellationToken? value)
            => value is null ? writer.TryWriteNull(out state.BytesNeeded) : writer.TryWriteNotNull(out state.BytesNeeded);
    }
}