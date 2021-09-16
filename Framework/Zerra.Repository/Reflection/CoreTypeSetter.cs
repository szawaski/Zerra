// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Reflection;

namespace Zerra.Repository.Reflection
{
    public class CoreTypeSetter<T>
    {
        public CoreType? CoreType { get; private set; }
        public bool IsByteArray { get; set; }

        private readonly Action<T, bool> setterBool;
        private readonly Action<T, byte> setterByte;
        private readonly Action<T, sbyte> setterSByte;
        private readonly Action<T, short> setterInt16;
        private readonly Action<T, ushort> setterUInt16;
        private readonly Action<T, int> setterInt32;
        private readonly Action<T, uint> setterUInt32;
        private readonly Action<T, long> setterInt64;
        private readonly Action<T, ulong> setterUInt64;
        private readonly Action<T, float> setterSingle;
        private readonly Action<T, double> setterDouble;
        private readonly Action<T, decimal> setterDecimal;
        private readonly Action<T, char> setterChar;
        private readonly Action<T, DateTime> setterDateTime;
        private readonly Action<T, DateTimeOffset> setterDateTimeOffset;
        private readonly Action<T, TimeSpan> setterTimeSpan;
        private readonly Action<T, Guid> setterGuid;

        private readonly Action<T, string> setterString;

        private readonly Action<T, bool?> setterBoolNullable;
        private readonly Action<T, byte?> setterByteNullable;
        private readonly Action<T, sbyte?> setterSByteNullable;
        private readonly Action<T, short?> setterInt16Nullable;
        private readonly Action<T, ushort?> setterUInt16Nullable;
        private readonly Action<T, int?> setterInt32Nullable;
        private readonly Action<T, uint?> setterUInt32Nullable;
        private readonly Action<T, long?> setterInt64Nullable;
        private readonly Action<T, ulong?> setterUInt64Nullable;
        private readonly Action<T, float?> setterSingleNullable;
        private readonly Action<T, double?> setterDoubleNullable;
        private readonly Action<T, decimal?> setterDecimalNullable;
        private readonly Action<T, char?> setterCharNullable;
        private readonly Action<T, DateTime?> setterDateTimeNullable;
        private readonly Action<T, DateTimeOffset?> setterDateTimeOffsetNullable;
        private readonly Action<T, TimeSpan?> setterTimeSpanNullable;
        private readonly Action<T, Guid?> setterGuidNullable;

        private readonly Action<T, byte[]> setterBytes;

        public CoreTypeSetter(CoreType? coreType, bool isByteArray, object setter)
        {
            this.CoreType = coreType;
            this.IsByteArray = isByteArray;

            if (coreType.HasValue)
            {
                switch (coreType.Value)
                {
                    case Zerra.Reflection.CoreType.Boolean:
                        setterBool = (Action<T, bool>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Byte:
                        setterByte = (Action<T, byte>)setter;
                        break;
                    case Zerra.Reflection.CoreType.SByte:
                        setterSByte = (Action<T, sbyte>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Int16:
                        setterInt16 = (Action<T, short>)setter;
                        break;
                    case Zerra.Reflection.CoreType.UInt16:
                        setterUInt16 = (Action<T, ushort>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Int32:
                        setterInt32 = (Action<T, int>)setter;
                        break;
                    case Zerra.Reflection.CoreType.UInt32:
                        setterUInt32 = (Action<T, uint>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Int64:
                        setterInt64 = (Action<T, long>)setter;
                        break;
                    case Zerra.Reflection.CoreType.UInt64:
                        setterUInt64 = (Action<T, ulong>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Single:
                        setterSingle = (Action<T, float>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Double:
                        setterDouble = (Action<T, double>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Decimal:
                        setterDecimal = (Action<T, decimal>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Char:
                        setterChar = (Action<T, char>)setter;
                        break;
                    case Zerra.Reflection.CoreType.DateTime:
                        setterDateTime = (Action<T, DateTime>)setter;
                        break;
                    case Zerra.Reflection.CoreType.DateTimeOffset:
                        setterDateTimeOffset = (Action<T, DateTimeOffset>)setter;
                        break;
                    case Zerra.Reflection.CoreType.TimeSpan:
                        setterTimeSpan = (Action<T, TimeSpan>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Guid:
                        setterGuid = (Action<T, Guid>)setter;
                        break;

                    case Zerra.Reflection.CoreType.String:
                        setterString = (Action<T, string>)setter;
                        break;

                    case Zerra.Reflection.CoreType.BooleanNullable:
                        setterBoolNullable = (Action<T, bool?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.ByteNullable:
                        setterByteNullable = (Action<T, byte?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.SByteNullable:
                        setterSByteNullable = (Action<T, sbyte?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Int16Nullable:
                        setterInt16Nullable = (Action<T, short?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.UInt16Nullable:
                        setterUInt16Nullable = (Action<T, ushort?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Int32Nullable:
                        setterInt32Nullable = (Action<T, int?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.UInt32Nullable:
                        setterUInt32Nullable = (Action<T, uint?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.Int64Nullable:
                        setterInt64Nullable = (Action<T, long?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.UInt64Nullable:
                        setterUInt64Nullable = (Action<T, ulong?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.SingleNullable:
                        setterSingleNullable = (Action<T, float?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.DoubleNullable:
                        setterDoubleNullable = (Action<T, double?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.DecimalNullable:
                        setterDecimalNullable = (Action<T, decimal?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.CharNullable:
                        setterCharNullable = (Action<T, char?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.DateTimeNullable:
                        setterDateTimeNullable = (Action<T, DateTime?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.DateTimeOffsetNullable:
                        setterDateTimeOffsetNullable = (Action<T, DateTimeOffset?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.TimeSpanNullable:
                        setterTimeSpanNullable = (Action<T, TimeSpan?>)setter;
                        break;
                    case Zerra.Reflection.CoreType.GuidNullable:
                        setterGuidNullable = (Action<T, Guid?>)setter;
                        break;
                }
            }
            else if (isByteArray)
            {
                setterBytes = (Action<T, byte[]>)setter;
            }
        }

        public void Setter(T model, bool value) => setterBool(model, value);
        public void Setter(T model, byte value) => setterByte(model, value);
        public void Setter(T model, sbyte value) => setterSByte(model, value);
        public void Setter(T model, short value) => setterInt16(model, value);
        public void Setter(T model, ushort value) => setterUInt16(model, value);
        public void Setter(T model, int value) => setterInt32(model, value);
        public void Setter(T model, uint value) => setterUInt32(model, value);
        public void Setter(T model, long value) => setterInt64(model, value);
        public void Setter(T model, ulong value) => setterUInt64(model, value);
        public void Setter(T model, float value) => setterSingle(model, value);
        public void Setter(T model, double value) => setterDouble(model, value);
        public void Setter(T model, decimal value) => setterDecimal(model, value);
        public void Setter(T model, char value) => setterChar(model, value);
        public void Setter(T model, DateTime value) => setterDateTime(model, value);
        public void Setter(T model, DateTimeOffset value) => setterDateTimeOffset(model, value);
        public void Setter(T model, TimeSpan value) => setterTimeSpan(model, value);
        public void Setter(T model, Guid value) => setterGuid(model, value);

        public void Setter(T model, string value) => setterString(model, value);

        public void Setter(T model, bool? value) => setterBoolNullable(model, value);
        public void Setter(T model, byte? value) => setterByteNullable(model, value);
        public void Setter(T model, sbyte? value) => setterSByteNullable(model, value);
        public void Setter(T model, short? value) => setterInt16Nullable(model, value);
        public void Setter(T model, ushort? value) => setterUInt16Nullable(model, value);
        public void Setter(T model, int? value) => setterInt32Nullable(model, value);
        public void Setter(T model, uint? value) => setterUInt32Nullable(model, value);
        public void Setter(T model, long? value) => setterInt64Nullable(model, value);
        public void Setter(T model, ulong? value) => setterUInt64Nullable(model, value);
        public void Setter(T model, float? value) => setterSingleNullable(model, value);
        public void Setter(T model, double? value) => setterDoubleNullable(model, value);
        public void Setter(T model, decimal? value) => setterDecimalNullable(model, value);
        public void Setter(T model, char? value) => setterCharNullable(model, value);
        public void Setter(T model, DateTime? value) => setterDateTimeNullable(model, value);
        public void Setter(T model, DateTimeOffset? value) => setterDateTimeOffsetNullable(model, value);
        public void Setter(T model, TimeSpan? value) => setterTimeSpanNullable(model, value);
        public void Setter(T model, Guid? value) => setterGuidNullable(model, value);

        public void Setter(T model, byte[] value) => setterBytes(model, value);
    }
}
