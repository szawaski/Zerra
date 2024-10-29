// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;
using Zerra.Serialization.Bytes;

namespace Zerra.Repository
{
    internal static class EventStoreCommon
    {
        private static readonly ByteSerializerOptions byteSerializerOptions = new()
        {
            IndexType = ByteSerializerIndexType.PropertyNames,
            UseTypes = true,
            IgnoreIndexAttribute = true
        };

        public static byte[] Serialize(object obj)
        {
            return ByteSerializer.Serialize(obj, byteSerializerOptions);
        }

        public static T? Deserialize<T>(ReadOnlySpan<byte> bytes)
        {
            return ByteSerializer.Deserialize<T>(bytes, byteSerializerOptions);
        }

        public static string GetStreamName<TModel>(object id)
        {
            var typeName = typeof(TModel).GetNiceName();

            var sbStreamName = new StringBuilder();
            _ = sbStreamName.Append(typeName).Append('_');

            if (id is object[] identityArray)
            {
                for (var i = 0; i < identityArray.Length; i++)
                {
                    if (i > 0)
                        _ = sbStreamName.Append('_');
                    _ = sbStreamName.Append(identityArray[i].ToString());
                }
            }
            else
            {
                _ = sbStreamName.Append(id.ToString());
            }
            return sbStreamName.ToString();
        }

        public static string GetStateStreamName<TModel>(object id)
        {
            var typeName = typeof(TModel).GetNiceName();

            var sbStreamName = new StringBuilder();
            _ = sbStreamName.Append(typeName).Append("_State_");

            if (id is object[] identityArray)
            {
                for (var i = 0; i < identityArray.Length; i++)
                {
                    if (i > 0)
                        _ = sbStreamName.Append('_');
                    _ = sbStreamName.Append(identityArray[i].ToString());
                }
            }
            else
            {
                _ = sbStreamName.Append(id.ToString());
            }
            return sbStreamName.ToString();
        }
    }
}
