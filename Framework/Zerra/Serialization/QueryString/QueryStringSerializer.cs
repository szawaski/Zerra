using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Buffers;
using Zerra.Reflection;

namespace Zerra.Serialization.QueryString
{
    public static class QueryStringSerializer
    {
        public static T Deserialize<T>(IEnumerable<KeyValuePair<string, string>> query)
        {
            var model = Instantiator.Create<T>();
            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();

            foreach (var item in query)
            {
                SetValue(typeDetail, model, item.Key, item.Value);
            }

            return model;
        }
        public static T Deserialize<T>(IEnumerable<KeyValuePair<string, StringValues>> query)
        {
            var model = Instantiator.Create<T>();
            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();

            foreach (var item in query)
            {
                var value = (string?)item.Value;
                if (value is not null)
                    SetValue(typeDetail, model, item.Key, value);
            }

            return model;
        }

        public static unsafe T Deserialize<T>(string queryString)
        {
            var model = Instantiator.Create<T>();
            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();

            var bufferOwner = ArrayPoolHelper<char>.Rent(256);
            var buffer = bufferOwner.AsSpan();
            var bufferLength = 0;
            try
            {
                fixed (char* pFixed = queryString)
                {
                    var p = pFixed;
                    string? name = null;
                    for (var i = 0; i < queryString.Length; i++)
                    {
                        var c = *p;
                        switch (c)
                        {
                            case '=':
                                if (name is not null)
                                    throw new Exception($"Invalid query string at position {i}");
#if NETSTANDARD2_0
                                name = new string(bufferOwner, 0, bufferLength);
#else
                                name = new string(buffer.Slice(0, bufferLength));
#endif
                                bufferLength = 0;
                                break;
                            case '&':
                                if (name is null)
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
                                    ArrayPoolHelper<char>.Grow(ref bufferOwner, bufferOwner.Length * 2);
                                    buffer = bufferOwner.AsSpan();
                                }
                                buffer[bufferLength++] = c;
                                break;
                        }
                        p++;
                    }

                    if (name is not null)
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
                ArrayPoolHelper<char>.Return(bufferOwner);
            }

            return model;
        }

        public static string Serialize<T>(T model)
        {
            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();

            var sb = new StringBuilder(256);

            foreach (var member in typeDetail.MemberDetails)
            {
                var value = member.GetterBoxed(model!);
                if (value is null)
                    continue;

                if (sb.Length > 0)
                    _ = sb.Append('&');
                _ = sb.Append(WebUtility.UrlEncode(member.Name));
                _ = sb.Append('=');
                if (!member.TypeDetailBoxed.CoreType.HasValue)
                    throw new Exception($"{nameof(QueryStringSerializer)} does not support serializing type {member.Type.Name}");

                switch (member.TypeDetailBoxed.CoreType.Value)
                {
                    case CoreType.String:
                        _ = sb.Append(WebUtility.UrlEncode((string)value));
                        break;
                    case CoreType.Boolean:
                    case CoreType.BooleanNullable:
                        _ = sb.Append((bool)value == false ? "false" : "true");
                        break;
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        _ = sb.Append((byte)value);
                        break;
                    case CoreType.SByte:
                    case CoreType.SByteNullable:
                        _ = sb.Append((sbyte)value);
                        break;
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                        _ = sb.Append((short)value);
                        break;
                    case CoreType.UInt16:
                    case CoreType.UInt16Nullable:
                        _ = sb.Append((ushort)value);
                        break;
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                        _ = sb.Append((int)value);
                        break;
                    case CoreType.UInt32:
                    case CoreType.UInt32Nullable:
                        _ = sb.Append((uint)value);
                        break;
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        _ = sb.Append((long)value);
                        break;
                    case CoreType.UInt64:
                    case CoreType.UInt64Nullable:
                        _ = sb.Append((ulong)value);
                        break;
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                        _ = sb.Append((float)value);
                        break;
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        _ = sb.Append((double)value);
                        break;
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        _ = sb.Append((decimal)value);
                        break;
                    case CoreType.Char:
                    case CoreType.CharNullable:
                        _ = sb.Append((char)value);
                        break;
                    case CoreType.DateTime:
                    case CoreType.DateTimeNullable:
                        _ = sb.Append(WebUtility.UrlEncode(((DateTime)value).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")));
                        break;
                    case CoreType.DateTimeOffset:
                    case CoreType.DateTimeOffsetNullable:
                        _ = sb.Append(WebUtility.UrlEncode(((DateTimeOffset)value).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")));
                        break;
                    case CoreType.TimeSpan:
                    case CoreType.TimeSpanNullable:
                        _ = sb.Append(WebUtility.UrlEncode(((TimeSpan)value).ToString("c")));
                        break;
#if NET6_0_OR_GREATER
                    case CoreType.DateOnly:
                    case CoreType.DateOnlyNullable:
                        _ = sb.Append(WebUtility.UrlEncode(((DateOnly)value).ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-dd")));
                        break;
                    case CoreType.TimeOnly:
                    case CoreType.TimeOnlyNullable:
                        _ = sb.Append(WebUtility.UrlEncode(((TimeOnly)value).ToTimeSpan().ToString("c")));
                        break;
#endif
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        _ = sb.Append((Guid)value);
                        break;
                    default:
                        throw new Exception($"{nameof(QueryStringSerializer)} does not support serializing type {member.Type.Name}");
                }
            }
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetValue<T>(TypeDetail<T> typeDetail, T model, string name, string value)
        {
            if (!typeDetail.TryGetSerializableMemberCaseInsensitive(name, out var member))
                return;

            if (!TypeLookup.CoreTypeLookup(member.Type, out var coreType))
                throw new NotImplementedException($"Type convert not available for {member.Type.Name}");

            object? parsed = coreType switch
            {
                CoreType.Boolean => bool.Parse(value),
                CoreType.Byte => byte.Parse(value),
                CoreType.SByte => sbyte.Parse(value),
                CoreType.UInt16 => ushort.Parse(value),
                CoreType.Int16 => short.Parse(value),
                CoreType.UInt32 => uint.Parse(value),
                CoreType.Int32 => int.Parse(value),
                CoreType.UInt64 => ulong.Parse(value),
                CoreType.Int64 => long.Parse(value),
                CoreType.Single => float.Parse(value),
                CoreType.Double => double.Parse(value),
                CoreType.Decimal => decimal.Parse(value),
                CoreType.Char => char.Parse(value),
                CoreType.DateTime => DateTime.Parse(value, null, DateTimeStyles.RoundtripKind),
                CoreType.DateTimeOffset => DateTimeOffset.Parse(value, null, DateTimeStyles.RoundtripKind),
                CoreType.TimeSpan => TimeSpan.Parse(value),
#if NET6_0_OR_GREATER
                CoreType.DateOnly => DateOnly.Parse(value),
                CoreType.TimeOnly => TimeOnly.Parse(value),
#endif
                CoreType.Guid => Guid.Parse(value),
                CoreType.String => value,
                CoreType.BooleanNullable => value is null ? null : bool.Parse(value),
                CoreType.ByteNullable => value is null ? null : byte.Parse(value),
                CoreType.SByteNullable => value is null ? null : sbyte.Parse(value),
                CoreType.UInt16Nullable => value is null ? null : ushort.Parse(value),
                CoreType.Int16Nullable => value is null ? null : short.Parse(value),
                CoreType.UInt32Nullable => value is null ? null : uint.Parse(value),
                CoreType.Int32Nullable => value is null ? null : int.Parse(value),
                CoreType.UInt64Nullable => value is null ? null : ulong.Parse(value),
                CoreType.Int64Nullable => value is null ? null : long.Parse(value),
                CoreType.SingleNullable => value is null ? null : float.Parse(value),
                CoreType.DoubleNullable => value is null ? null : double.Parse(value),
                CoreType.DecimalNullable => value is null ? null : decimal.Parse(value),
                CoreType.CharNullable => value is null ? null : char.Parse(value),
                CoreType.DateTimeNullable => value is null ? null : DateTime.Parse(value, null, DateTimeStyles.RoundtripKind),
                CoreType.DateTimeOffsetNullable => value is null ? null : DateTimeOffset.Parse(value, null, DateTimeStyles.RoundtripKind),
                CoreType.TimeSpanNullable => value is null ? null : TimeSpan.Parse(value),
#if NET6_0_OR_GREATER
                CoreType.DateOnlyNullable => value is null ? null : DateOnly.Parse(value),
                CoreType.TimeOnlyNullable => value is null ? null : TimeOnly.Parse(value),
#endif
                CoreType.GuidNullable => value is null ? null : Guid.Parse(value),
                _ => throw new NotImplementedException($"Type conversion not available for {member.Type.Name}"),
            };

            member.SetterBoxed(model!, parsed);
        }
    }
}
