using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static class QueryStringSerializer
    {
        public static T Deserialize<T>(IEnumerable<KeyValuePair<string, string>> query)
        {
            var model = Instantiator.Create<T>();
            var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(T));

            foreach (var item in query)
            {
                SetValue<T>(typeDetail, model, item.Key, item.Value);
            }

            return model;
        }
        public static T Deserialize<T>(IEnumerable<KeyValuePair<string, StringValues>> query)
        {
            var model = Instantiator.Create<T>();
            var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(T));

            foreach (var item in query)
            {
                SetValue<T>(typeDetail, model, item.Key, item.Value);
            }

            return model;
        }

        public static unsafe T Deserialize<T>(string queryString)
        {
            var model = Instantiator.Create<T>();
            var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(T));

            var bufferOwner = BufferArrayPool<char>.Rent(256);
            var buffer = bufferOwner.AsSpan();
            var bufferLength = 0;
            try
            {
                fixed (char* pFixed = queryString)
                {
                    var p = pFixed;
                    string name = null;
                    for (var i = 0; i < queryString.Length; i++)
                    {
                        var c = *p;
                        switch (c)
                        {
                            case '=':
                                if (name != null)
                                    throw new Exception($"Invalid query string at position {i}");
#if NETSTANDARD2_0
                                name = new string(bufferOwner, 0, bufferLength);
#else
                                name = new string(buffer.Slice(0, bufferLength));
#endif
                                bufferLength = 0;
                                break;
                            case '&':
                                if (name == null)
                                    throw new Exception($"Invalid query string at position {i}");
#if NETSTANDARD2_0
                                var value = new string(bufferOwner, 0, bufferLength);
#else
                                var value = new string(buffer.Slice(0, bufferLength));
#endif
                                SetValue(typeDetail, model, WebUtility.UrlDecode(name), WebUtility.UrlDecode(value));
                                name = null;
                                bufferLength = 0;
                                break;
                            default:
                                if (bufferLength == buffer.Length)
                                {
                                    BufferArrayPool<char>.Grow(ref bufferOwner, bufferOwner.Length * 2);
                                    buffer = bufferOwner.AsSpan();
                                }
                                buffer[bufferLength++] = c;
                                break;
                        }
                        p++;
                    }

                    if (name != null)
                    {
#if NETSTANDARD2_0
                        var value = new string(bufferOwner, 0, bufferLength);
#else
                        var value = new string(buffer.Slice(0, bufferLength));
#endif
                        SetValue(typeDetail, model, WebUtility.UrlDecode(name), WebUtility.UrlDecode(value));
                        bufferLength = 0;
                    }
                }
            }
            finally
            {
                BufferArrayPool<char>.Return(bufferOwner);
            }

            return model;
        }

        public static string Serialize<T>(T model)
        {
            var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(T));

            var writer = new CharWriter(256);
            try
            {
                foreach (var member in typeDetail.MemberDetails)
                {
                    var value = member.Getter(model);
                    if (value == null)
                        continue;

                    if (writer.Length > 0)
                        writer.Write('&');
                    writer.Write(WebUtility.UrlEncode(member.Name));
                    writer.Write('=');
                    if (!member.TypeDetail.CoreType.HasValue)
                        throw new Exception($"{nameof(QueryStringSerializer)} does not support serializing type {member.Type.Name}");

                    switch (member.TypeDetail.CoreType.Value)
                    {
                        case CoreType.String:
                            writer.Write(WebUtility.UrlEncode((string)value));
                            break;
                        case CoreType.Boolean:
                        case CoreType.BooleanNullable:
                            writer.Write((bool)value == false ? "false" : "true");
                            break;
                        case CoreType.Byte:
                        case CoreType.ByteNullable:
                            writer.Write((byte)value);
                            break;
                        case CoreType.SByte:
                        case CoreType.SByteNullable:
                            writer.Write((sbyte)value);
                            break;
                        case CoreType.Int16:
                        case CoreType.Int16Nullable:
                            writer.Write((short)value);
                            break;
                        case CoreType.UInt16:
                        case CoreType.UInt16Nullable:
                            writer.Write((ushort)value);
                            break;
                        case CoreType.Int32:
                        case CoreType.Int32Nullable:
                            writer.Write((int)value);
                            break;
                        case CoreType.UInt32:
                        case CoreType.UInt32Nullable:
                            writer.Write((uint)value);
                            break;
                        case CoreType.Int64:
                        case CoreType.Int64Nullable:
                            writer.Write((long)value);
                            break;
                        case CoreType.UInt64:
                        case CoreType.UInt64Nullable:
                            writer.Write((ulong)value);
                            break;
                        case CoreType.Single:
                        case CoreType.SingleNullable:
                            writer.Write((float)value);
                            break;
                        case CoreType.Double:
                        case CoreType.DoubleNullable:
                            writer.Write((double)value);
                            break;
                        case CoreType.Decimal:
                        case CoreType.DecimalNullable:
                            writer.Write((decimal)value);
                            break;
                        case CoreType.Char:
                        case CoreType.CharNullable:
                            writer.Write((char)value);
                            break;
                        case CoreType.DateTime:
                        case CoreType.DateTimeNullable:
                            writer.Write((DateTime)value, DateTimeFormat.ISO8601);
                            break;
                        case CoreType.DateTimeOffset:
                        case CoreType.DateTimeOffsetNullable:
                            writer.Write((DateTimeOffset)value, DateTimeFormat.ISO8601);
                            break;
                        case CoreType.TimeSpan:
                        case CoreType.TimeSpanNullable:
                            writer.Write((TimeSpan)value, TimeFormat.ISO8601);
                            break;
                        case CoreType.Guid:
                        case CoreType.GuidNullable:
                            writer.Write((Guid)value);
                            break;
                        default:
                            throw new Exception($"{nameof(QueryStringSerializer)} does not support serializing type {member.Type.Name}");
                    }
                }
                return writer.ToString();
            }
            finally
            {
                writer.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetValue<T>(TypeDetail typeDetail, T model, string name, string value)
        {
            if (!typeDetail.TryGetSerializableMemberCaseInsensitive(name, out var member))
                return;

            if (!TypeLookup.CoreTypeLookup(member.Type, out var coreType))
                throw new NotImplementedException($"Type convert not available for {member.Type.Name}");

            object parsed = coreType switch
            {
                CoreType.Boolean => Boolean.Parse(value),
                CoreType.Byte => Byte.Parse(value),
                CoreType.SByte => SByte.Parse(value),
                CoreType.UInt16 => UInt16.Parse(value),
                CoreType.Int16 => Int16.Parse(value),
                CoreType.UInt32 => UInt32.Parse(value),
                CoreType.Int32 => Int32.Parse(value),
                CoreType.UInt64 => UInt64.Parse(value),
                CoreType.Int64 => Int64.Parse(value),
                CoreType.Single => Single.Parse(value),
                CoreType.Double => Double.Parse(value),
                CoreType.Decimal => Decimal.Parse(value),
                CoreType.Char => Char.Parse(value),
                CoreType.DateTime => DateTime.Parse(value),
                CoreType.DateTimeOffset => DateTimeOffset.Parse(value),
                CoreType.TimeSpan => TimeSpan.Parse(value),
                CoreType.Guid => Guid.Parse(value),
                CoreType.String => value,
                CoreType.BooleanNullable => value == null ? null : Boolean.Parse(value),
                CoreType.ByteNullable => value == null ? null : Byte.Parse(value),
                CoreType.SByteNullable => value == null ? null : SByte.Parse(value),
                CoreType.UInt16Nullable => value == null ? null : UInt16.Parse(value),
                CoreType.Int16Nullable => value == null ? null : Int16.Parse(value),
                CoreType.UInt32Nullable => value == null ? null : UInt32.Parse(value),
                CoreType.Int32Nullable => value == null ? null : Int32.Parse(value),
                CoreType.UInt64Nullable => value == null ? null : UInt64.Parse(value),
                CoreType.Int64Nullable => value == null ? null : Int64.Parse(value),
                CoreType.SingleNullable => value == null ? null : Single.Parse(value),
                CoreType.DoubleNullable => value == null ? null : Double.Parse(value),
                CoreType.DecimalNullable => value == null ? null : Decimal.Parse(value),
                CoreType.CharNullable => value == null ? null : Char.Parse(value),
                CoreType.DateTimeNullable => value == null ? null : DateTime.Parse(value),
                CoreType.DateTimeOffsetNullable => value == null ? null : DateTimeOffset.Parse(value),
                CoreType.TimeSpanNullable => value == null ? null : TimeSpan.Parse(value),
                CoreType.GuidNullable => value == null ? null : Guid.Parse(value),
                _ => throw new NotImplementedException($"Type conversion not available for {member.Type.Name}"),
            };

            member.Setter(model, parsed);
        }
    }
}
