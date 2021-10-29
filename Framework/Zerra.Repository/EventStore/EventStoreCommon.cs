// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;
using Zerra.Serialization;

namespace Zerra.Repository
{
    internal static class EventStoreCommon
    {
        public static byte[] Serialize(object obj)
        {
            var serializer = new ByteSerializer(true, true);
            return serializer.Serialize(obj);
        }

        public static T Deserialize<T>(ReadOnlySpan<byte> bytes)
        {
            var serializer = new ByteSerializer(true, true);
            return serializer.Deserialize<T>(bytes);
        }

        public static string GetStreamName<TModel>(object id)
        {
            var typeName = typeof(TModel).GetNiceName();

            var sbStreamName = new StringBuilder();
            sbStreamName.Append(typeName).Append('_');

            if (id is object[] identityArray)
            {
                for (var i = 0; i < identityArray.Length; i++)
                {
                    if (i > 0)
                        sbStreamName.Append('_');
                    sbStreamName.Append(identityArray[i].ToString());
                }
            }
            else
            {
                sbStreamName.Append(id.ToString());
            }
            return sbStreamName.ToString();
        }

        public static string GetStateStreamName<TModel>(object id)
        {
            var typeName = typeof(TModel).GetNiceName();

            var sbStreamName = new StringBuilder();
            sbStreamName.Append(typeName).Append("_State_");

            if (id is object[] identityArray)
            {
                for (var i = 0; i < identityArray.Length; i++)
                {
                    if (i > 0)
                        sbStreamName.Append('_');
                    sbStreamName.Append(identityArray[i].ToString());
                }
            }
            else
            {
                sbStreamName.Append(id.ToString());
            }
            return sbStreamName.ToString();
        }
    }
}
