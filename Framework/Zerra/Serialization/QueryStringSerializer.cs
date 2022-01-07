using System;
using System.Net;
using System.Runtime.CompilerServices;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static class QueryStringSerializer
    {
        public static unsafe T Deserialize<T>(string queryString)
        {
            var model = Instantiator.CreateInstance<T>();
            var typeDetail = TypeAnalyzer.GetTypeDetail(typeof(T));

            var bufferOwner = BufferArrayPool<char>.Rent(256);
            var buffer = bufferOwner.AsSpan();
            var bufferLength = 0;
            try
            {
                fixed (char* pFixed = queryString)
                {
                    char* p = pFixed;
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
                                name = new String(bufferOwner, 0, bufferLength);
#else
                                name = new String(buffer.Slice(0, bufferLength));
#endif
                                bufferLength = 0;
                                break;
                            case '&':
                                if (name == null)
                                    throw new Exception($"Invalid query string at position {i}");
#if NETSTANDARD2_0
                                var value = new String(bufferOwner, 0, bufferLength);
#else
                                var value = new String(buffer.Slice(0, bufferLength));
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
                        var value = new String(bufferOwner, 0, bufferLength);
#else
                        var value = new String(buffer.Slice(0, bufferLength));
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

            var writer = new CharWriteBuffer(256);
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
            if (!typeDetail.TryGetMemberCaseInsensitive(name, out MemberDetail member))
                return;

            var convertedValue = TypeAnalyzer.Convert(value, member.Type);
            member.Setter(model, convertedValue);
        }
    }
}
