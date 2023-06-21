// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        private const int defaultBufferSize = 8 * 1024;
        private const int defaultDecodeBufferSize = 1024;
        private static readonly Encoding encoding = Encoding.UTF8;
#if DEBUG
        public static bool Testing { get; set; }
#endif

        private static readonly MethodInfo dictionaryToArrayMethod = typeof(System.Linq.Enumerable).GetMethod("ToArray");
        private static readonly Type genericListType = typeof(List<>);
        private static readonly Type genericHashSetType = typeof(HashSet<>);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ConvertStringToType(string s, TypeDetail typeDetail)
        {
            if (typeDetail == null)
                return null;

            if (typeDetail.CoreType.HasValue)
            {
                switch (typeDetail.CoreType.Value)
                {
                    case CoreType.String:
                        {
                            return s;
                        }
                    case CoreType.Boolean:
                    case CoreType.BooleanNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Boolean.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Byte.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.SByte:
                    case CoreType.SByteNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (SByte.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Int16.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.UInt16:
                    case CoreType.UInt16Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (UInt16.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Int32.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.UInt32:
                    case CoreType.UInt32Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (UInt32.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Int64.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.UInt64:
                    case CoreType.UInt64Nullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (UInt64.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Single.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Double.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Decimal.TryParse(s, out var value))
                                return value;
                            return null;
                        }

                    case CoreType.Char:
                    case CoreType.CharNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            return s[0];
                        }
                    case CoreType.DateTime:
                    case CoreType.DateTimeNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (DateTime.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.DateTimeOffset:
                    case CoreType.DateTimeOffsetNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (DateTimeOffset.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.TimeSpan:
                    case CoreType.TimeSpanNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (TimeSpan.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                    case CoreType.Guid:
                    case CoreType.GuidNullable:
                        {
                            if (s.Length == 0)
                                return null;
                            if (Guid.TryParse(s, out var value))
                                return value;
                            return null;
                        }
                }
            }

            if (typeDetail.Type.IsEnum)
            {
                if (EnumName.TryParse(s, typeDetail.Type, out var value))
                    return value;
                return null;
            }

            if (typeDetail.IsNullable && typeDetail.InnerTypeDetails[0].Type.IsEnum)
            {
                if (EnumName.TryParse(s, typeDetail.InnerTypeDetails[0].Type, out var value))
                    return value;
                return null;
            }

            if (typeDetail.Type.IsArray && typeDetail.InnerTypeDetails[0].CoreType == CoreType.Byte)
            {
                //special case
                return Convert.FromBase64String(s);
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ConvertNullToType(CoreType coreType)
        {
            return coreType switch
            {
                CoreType.String => null,
                CoreType.Boolean => default(bool),
                CoreType.Byte => default(byte),
                CoreType.SByte => default(sbyte),
                CoreType.Int16 => default(short),
                CoreType.UInt16 => default(ushort),
                CoreType.Int32 => default(int),
                CoreType.UInt32 => default(uint),
                CoreType.Int64 => default(long),
                CoreType.UInt64 => default(ulong),
                CoreType.Single => default(float),
                CoreType.Double => default(double),
                CoreType.Decimal => default(decimal),
                CoreType.Char => default(char),
                CoreType.DateTime => default(DateTime),
                CoreType.DateTimeOffset => default(DateTimeOffset),
                CoreType.TimeSpan => default(TimeSpan),
                CoreType.Guid => default(Guid),
                CoreType.BooleanNullable => null,
                CoreType.ByteNullable => null,
                CoreType.SByteNullable => null,
                CoreType.Int16Nullable => null,
                CoreType.UInt16Nullable => null,
                CoreType.Int32Nullable => null,
                CoreType.UInt32Nullable => null,
                CoreType.Int64Nullable => null,
                CoreType.UInt64Nullable => null,
                CoreType.SingleNullable => null,
                CoreType.DoubleNullable => null,
                CoreType.DecimalNullable => null,
                CoreType.CharNullable => null,
                CoreType.DateTimeNullable => null,
                CoreType.DateTimeOffsetNullable => null,
                CoreType.TimeSpanNullable => null,
                CoreType.GuidNullable => null,
                _ => throw new NotImplementedException(),
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ConvertTrueToType(CoreType coreType)
        {
            return coreType switch
            {
                CoreType.String => "true",
                CoreType.Boolean or CoreType.BooleanNullable => true,
                CoreType.Byte or CoreType.ByteNullable => (byte)1,
                CoreType.SByte or CoreType.SByteNullable => (sbyte)1,
                CoreType.Int16 or CoreType.Int16Nullable => (short)1,
                CoreType.UInt16 or CoreType.UInt16Nullable => (ushort)1,
                CoreType.Int32 or CoreType.Int32Nullable => (int)1,
                CoreType.UInt32 or CoreType.UInt32Nullable => (uint)1,
                CoreType.Int64 or CoreType.Int64Nullable => (long)1,
                CoreType.UInt64 or CoreType.UInt64Nullable => (ulong)1,
                CoreType.Single or CoreType.SingleNullable => (float)1,
                CoreType.Double or CoreType.DoubleNullable => (double)1,
                CoreType.Decimal or CoreType.DecimalNullable => (decimal)1,
                CoreType.Char => default(char),
                CoreType.DateTime => default(DateTime),
                CoreType.DateTimeOffset => default(DateTimeOffset),
                CoreType.TimeSpan => default(TimeSpan),
                CoreType.Guid => default(Guid),
                CoreType.CharNullable => null,
                CoreType.DateTimeNullable => null,
                CoreType.DateTimeOffsetNullable => null,
                CoreType.TimeSpanNullable => null,
                CoreType.GuidNullable => null,
                _ => throw new NotImplementedException(),
            };
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ConvertFalseToType(CoreType coreType)
        {
            return coreType switch
            {
                CoreType.String => "false",
                CoreType.Boolean or CoreType.BooleanNullable => false,
                CoreType.Byte or CoreType.ByteNullable => (byte)0,
                CoreType.SByte or CoreType.SByteNullable => (sbyte)0,
                CoreType.Int16 or CoreType.Int16Nullable => (short)0,
                CoreType.UInt16 or CoreType.UInt16Nullable => (ushort)0,
                CoreType.Int32 or CoreType.Int32Nullable => (int)0,
                CoreType.UInt32 or CoreType.UInt32Nullable => (uint)0,
                CoreType.Int64 or CoreType.Int64Nullable => (long)0,
                CoreType.UInt64 or CoreType.UInt64Nullable => (ulong)0,
                CoreType.Single or CoreType.SingleNullable => (float)0,
                CoreType.Double or CoreType.DoubleNullable => (double)0,
                CoreType.Decimal or CoreType.DecimalNullable => (decimal)0,
                CoreType.Char => default(char),
                CoreType.DateTime => default(DateTime),
                CoreType.DateTimeOffset => default(DateTimeOffset),
                CoreType.TimeSpan => default(TimeSpan),
                CoreType.Guid => default(Guid),
                CoreType.CharNullable => null,
                CoreType.DateTimeNullable => null,
                CoreType.DateTimeOffsetNullable => null,
                CoreType.TimeSpanNullable => null,
                CoreType.GuidNullable => null,
                _ => throw new NotImplementedException(),
            };
        }
    }
}