// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Test
{
    public class TypesHashSetModel
    {
        public HashSet<bool> BooleanHashSet { get; set; }
        public HashSet<byte> ByteHashSet { get; set; }
        public HashSet<sbyte> SByteHashSet { get; set; }
        public HashSet<short> Int16HashSet { get; set; }
        public HashSet<ushort> UInt16HashSet { get; set; }
        public HashSet<int> Int32HashSet { get; set; }
        public HashSet<uint> UInt32HashSet { get; set; }
        public HashSet<long> Int64HashSet { get; set; }
        public HashSet<ulong> UInt64HashSet { get; set; }
        public HashSet<float> SingleHashSet { get; set; }
        public HashSet<double> DoubleHashSet { get; set; }
        public HashSet<decimal> DecimalHashSet { get; set; }
        public HashSet<char> CharHashSet { get; set; }
        public HashSet<DateTime> DateTimeHashSet { get; set; }
        public HashSet<DateTimeOffset> DateTimeOffsetHashSet { get; set; }
        public HashSet<TimeSpan> TimeSpanHashSet { get; set; }
#if NET6_0_OR_GREATER
        public HashSet<DateOnly> DateOnlyHashSet { get; set; }
        public HashSet<TimeOnly> TimeOnlyHashSet { get; set; }
#endif
        public HashSet<Guid> GuidHashSet { get; set; }

        public HashSet<bool> BooleanHashSetEmpty { get; set; }
        public HashSet<byte> ByteHashSetEmpty { get; set; }
        public HashSet<sbyte> SByteHashSetEmpty { get; set; }
        public HashSet<short> Int16HashSetEmpty { get; set; }
        public HashSet<ushort> UInt16HashSetEmpty { get; set; }
        public HashSet<int> Int32HashSetEmpty { get; set; }
        public HashSet<uint> UInt32HashSetEmpty { get; set; }
        public HashSet<long> Int64HashSetEmpty { get; set; }
        public HashSet<ulong> UInt64HashSetEmpty { get; set; }
        public HashSet<float> SingleHashSetEmpty { get; set; }
        public HashSet<double> DoubleHashSetEmpty { get; set; }
        public HashSet<decimal> DecimalHashSetEmpty { get; set; }
        public HashSet<char> CharHashSetEmpty { get; set; }
        public HashSet<DateTime> DateTimeHashSetEmpty { get; set; }
        public HashSet<DateTimeOffset> DateTimeOffsetHashSetEmpty { get; set; }
        public HashSet<TimeSpan> TimeSpanHashSetEmpty { get; set; }
#if NET6_0_OR_GREATER
        public HashSet<DateOnly> DateOnlyHashSetEmpty { get; set; }
        public HashSet<TimeOnly> TimeOnlyHashSetEmpty { get; set; }
#endif
        public HashSet<Guid> GuidHashSetEmpty { get; set; }

        public HashSet<bool> BooleanHashSetNull { get; set; }
        public HashSet<byte> ByteHashSetNull { get; set; }
        public HashSet<sbyte> SByteHashSetNull { get; set; }
        public HashSet<short> Int16HashSetNull { get; set; }
        public HashSet<ushort> UInt16HashSetNull { get; set; }
        public HashSet<int> Int32HashSetNull { get; set; }
        public HashSet<uint> UInt32HashSetNull { get; set; }
        public HashSet<long> Int64HashSetNull { get; set; }
        public HashSet<ulong> UInt64HashSetNull { get; set; }
        public HashSet<float> SingleHashSetNull { get; set; }
        public HashSet<double> DoubleHashSetNull { get; set; }
        public HashSet<decimal> DecimalHashSetNull { get; set; }
        public HashSet<char> CharHashSetNull { get; set; }
        public HashSet<DateTime> DateTimeHashSetNull { get; set; }
        public HashSet<DateTimeOffset> DateTimeOffsetHashSetNull { get; set; }
        public HashSet<TimeSpan> TimeSpanHashSetNull { get; set; }
#if NET6_0_OR_GREATER
        public HashSet<DateOnly> DateOnlyHashSetNull { get; set; }
        public HashSet<TimeOnly> TimeOnlyHashSetNull { get; set; }
#endif
        public HashSet<Guid> GuidHashSetNull { get; set; }

        public HashSet<bool?> BooleanHashSetNullable { get; set; }
        public HashSet<byte?> ByteHashSetNullable { get; set; }
        public HashSet<sbyte?> SByteHashSetNullable { get; set; }
        public HashSet<short?> Int16HashSetNullable { get; set; }
        public HashSet<ushort?> UInt16HashSetNullable { get; set; }
        public HashSet<int?> Int32HashSetNullable { get; set; }
        public HashSet<uint?> UInt32HashSetNullable { get; set; }
        public HashSet<long?> Int64HashSetNullable { get; set; }
        public HashSet<ulong?> UInt64HashSetNullable { get; set; }
        public HashSet<float?> SingleHashSetNullable { get; set; }
        public HashSet<double?> DoubleHashSetNullable { get; set; }
        public HashSet<decimal?> DecimalHashSetNullable { get; set; }
        public HashSet<char?> CharHashSetNullable { get; set; }
        public HashSet<DateTime?> DateTimeHashSetNullable { get; set; }
        public HashSet<DateTimeOffset?> DateTimeOffsetHashSetNullable { get; set; }
        public HashSet<TimeSpan?> TimeSpanHashSetNullable { get; set; }
#if NET6_0_OR_GREATER
        public HashSet<DateOnly?> DateOnlyHashSetNullable { get; set; }
        public HashSet<TimeOnly?> TimeOnlyHashSetNullable { get; set; }
#endif
        public HashSet<Guid?> GuidHashSetNullable { get; set; }

        public HashSet<bool?> BooleanHashSetNullableEmpty { get; set; }
        public HashSet<byte?> ByteHashSetNullableEmpty { get; set; }
        public HashSet<sbyte?> SByteHashSetNullableEmpty { get; set; }
        public HashSet<short?> Int16HashSetNullableEmpty { get; set; }
        public HashSet<ushort?> UInt16HashSetNullableEmpty { get; set; }
        public HashSet<int?> Int32HashSetNullableEmpty { get; set; }
        public HashSet<uint?> UInt32HashSetNullableEmpty { get; set; }
        public HashSet<long?> Int64HashSetNullableEmpty { get; set; }
        public HashSet<ulong?> UInt64HashSetNullableEmpty { get; set; }
        public HashSet<float?> SingleHashSetNullableEmpty { get; set; }
        public HashSet<double?> DoubleHashSetNullableEmpty { get; set; }
        public HashSet<decimal?> DecimalHashSetNullableEmpty { get; set; }
        public HashSet<char?> CharHashSetNullableEmpty { get; set; }
        public HashSet<DateTime?> DateTimeHashSetNullableEmpty { get; set; }
        public HashSet<DateTimeOffset?> DateTimeOffsetHashSetNullableEmpty { get; set; }
        public HashSet<TimeSpan?> TimeSpanHashSetNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public HashSet<DateOnly?> DateOnlyHashSetNullableEmpty { get; set; }
        public HashSet<TimeOnly?> TimeOnlyHashSetNullableEmpty { get; set; }
#endif
        public HashSet<Guid?> GuidHashSetNullableEmpty { get; set; }

        public HashSet<bool?> BooleanHashSetNullableNull { get; set; }
        public HashSet<byte?> ByteHashSetNullableNull { get; set; }
        public HashSet<sbyte?> SByteHashSetNullableNull { get; set; }
        public HashSet<short?> Int16HashSetNullableNull { get; set; }
        public HashSet<ushort?> UInt16HashSetNullableNull { get; set; }
        public HashSet<int?> Int32HashSetNullableNull { get; set; }
        public HashSet<uint?> UInt32HashSetNullableNull { get; set; }
        public HashSet<long?> Int64HashSetNullableNull { get; set; }
        public HashSet<ulong?> UInt64HashSetNullableNull { get; set; }
        public HashSet<float?> SingleHashSetNullableNull { get; set; }
        public HashSet<double?> DoubleHashSetNullableNull { get; set; }
        public HashSet<decimal?> DecimalHashSetNullableNull { get; set; }
        public HashSet<char?> CharHashSetNullableNull { get; set; }
        public HashSet<DateTime?> DateTimeHashSetNullableNull { get; set; }
        public HashSet<DateTimeOffset?> DateTimeOffsetHashSetNullableNull { get; set; }
        public HashSet<TimeSpan?> TimeSpanHashSetNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public HashSet<DateOnly?> DateOnlyHashSetNullableNull { get; set; }
        public HashSet<TimeOnly?> TimeOnlyHashSetNullableNull { get; set; }
#endif
        public HashSet<Guid?> GuidHashSetNullableNull { get; set; }

        public HashSet<string> StringHashSet { get; set; }
        public HashSet<string> StringHashSetEmpty { get; set; }
        public HashSet<string> StringHashSetNull { get; set; }

        public HashSet<EnumModel> EnumHashSet { get; set; }
        public HashSet<EnumModel> EnumHashSetEmpty { get; set; }
        public HashSet<EnumModel> EnumHashSetNull { get; set; }

        public HashSet<EnumModel?> EnumHashSetNullable { get; set; }
        public HashSet<EnumModel?> EnumHashSetNullableEmpty { get; set; }
        public HashSet<EnumModel?> EnumHashSetNullableNull { get; set; }

        public HashSet<SimpleModel> ClassHashSet { get; set; }
        public HashSet<SimpleModel> ClassHashSetEmpty { get; set; }
        public HashSet<SimpleModel> ClassHashSetNull { get; set; }

        public static TypesHashSetModel Create()
        {
            var model = new TypesHashSetModel()
            {
                BooleanHashSet = new HashSet<bool>() { true, false, true },
                ByteHashSet = new HashSet<byte>() { 1, 2, 3 },
                SByteHashSet = new HashSet<sbyte>() { 4, 5, 6 },
                Int16HashSet = new HashSet<short>() { 7, 8, 9 },
                UInt16HashSet = new HashSet<ushort>() { 10, 11, 12 },
                Int32HashSet = new HashSet<int>() { 13, 14, 15 },
                UInt32HashSet = new HashSet<uint>() { 16, 17, 18 },
                Int64HashSet = new HashSet<long>() { 19, 20, 21 },
                UInt64HashSet = new HashSet<ulong>() { 22, 23, 24 },
                SingleHashSet = new HashSet<float>() { 25, 26, 27 },
                DoubleHashSet = new HashSet<double>() { 28, 29, 30 },
                DecimalHashSet = new HashSet<decimal>() { 31, 32, 33 },
                CharHashSet = new HashSet<char>() { 'A', 'B', 'C' },
                DateTimeHashSet = new HashSet<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetHashSet = new HashSet<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanHashSet = new HashSet<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyHashSet = new HashSet<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyHashSet = new HashSet<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidHashSet = new HashSet<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanHashSetEmpty = new HashSet<bool>(0),
                ByteHashSetEmpty = new HashSet<byte>(0),
                SByteHashSetEmpty = new HashSet<sbyte>(0),
                Int16HashSetEmpty = new HashSet<short>(0),
                UInt16HashSetEmpty = new HashSet<ushort>(0),
                Int32HashSetEmpty = new HashSet<int>(0),
                UInt32HashSetEmpty = new HashSet<uint>(0),
                Int64HashSetEmpty = new HashSet<long>(0),
                UInt64HashSetEmpty = new HashSet<ulong>(0),
                SingleHashSetEmpty = new HashSet<float>(0),
                DoubleHashSetEmpty = new HashSet<double>(0),
                DecimalHashSetEmpty = new HashSet<decimal>(0),
                CharHashSetEmpty = new HashSet<char>(0),
                DateTimeHashSetEmpty = new HashSet<DateTime>(0),
                DateTimeOffsetHashSetEmpty = new HashSet<DateTimeOffset>(0),
                TimeSpanHashSetEmpty = new HashSet<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyHashSetEmpty = new HashSet<DateOnly>(0),
                TimeOnlyHashSetEmpty = new HashSet<TimeOnly>(0),
#endif
                GuidHashSetEmpty = new HashSet<Guid>(0),

                BooleanHashSetNull = null,
                ByteHashSetNull = null,
                SByteHashSetNull = null,
                Int16HashSetNull = null,
                UInt16HashSetNull = null,
                Int32HashSetNull = null,
                UInt32HashSetNull = null,
                Int64HashSetNull = null,
                UInt64HashSetNull = null,
                SingleHashSetNull = null,
                DoubleHashSetNull = null,
                DecimalHashSetNull = null,
                CharHashSetNull = null,
                DateTimeHashSetNull = null,
                DateTimeOffsetHashSetNull = null,
                TimeSpanHashSetNull = null,
#if NET6_0_OR_GREATER
                DateOnlyHashSetNull = null,
                TimeOnlyHashSetNull = null,
#endif
                GuidHashSetNull = null,

                BooleanHashSetNullable = new HashSet<bool?>() { true, null, true },
                ByteHashSetNullable = new HashSet<byte?>() { 1, null, 3 },
                SByteHashSetNullable = new HashSet<sbyte?>() { 4, null, 6 },
                Int16HashSetNullable = new HashSet<short?>() { 7, null, 9 },
                UInt16HashSetNullable = new HashSet<ushort?>() { 10, null, 12 },
                Int32HashSetNullable = new HashSet<int?>() { 13, null, 15 },
                UInt32HashSetNullable = new HashSet<uint?>() { 16, null, 18 },
                Int64HashSetNullable = new HashSet<long?>() { 19, null, 21 },
                UInt64HashSetNullable = new HashSet<ulong?>() { 22, null, 24 },
                SingleHashSetNullable = new HashSet<float?>() { 25, null, 27 },
                DoubleHashSetNullable = new HashSet<double?>() { 28, null, 30 },
                DecimalHashSetNullable = new HashSet<decimal?>() { 31, null, 33 },
                CharHashSetNullable = new HashSet<char?>() { 'A', null, 'C' },
                DateTimeHashSetNullable = new HashSet<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetHashSetNullable = new HashSet<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanHashSetNullable = new HashSet<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyHashSetNullable = new HashSet<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyHashSetNullable = new HashSet<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidHashSetNullable = new HashSet<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanHashSetNullableEmpty = new HashSet<bool?>(0),
                ByteHashSetNullableEmpty = new HashSet<byte?>(0),
                SByteHashSetNullableEmpty = new HashSet<sbyte?>(0),
                Int16HashSetNullableEmpty = new HashSet<short?>(0),
                UInt16HashSetNullableEmpty = new HashSet<ushort?>(0),
                Int32HashSetNullableEmpty = new HashSet<int?>(0),
                UInt32HashSetNullableEmpty = new HashSet<uint?>(0),
                Int64HashSetNullableEmpty = new HashSet<long?>(0),
                UInt64HashSetNullableEmpty = new HashSet<ulong?>(0),
                SingleHashSetNullableEmpty = new HashSet<float?>(0),
                DoubleHashSetNullableEmpty = new HashSet<double?>(0),
                DecimalHashSetNullableEmpty = new HashSet<decimal?>(0),
                CharHashSetNullableEmpty = new HashSet<char?>(0),
                DateTimeHashSetNullableEmpty = new HashSet<DateTime?>(0),
                DateTimeOffsetHashSetNullableEmpty = new HashSet<DateTimeOffset?>(0),
                TimeSpanHashSetNullableEmpty = new HashSet<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyHashSetNullableEmpty = new HashSet<DateOnly?>(0),
                TimeOnlyHashSetNullableEmpty = new HashSet<TimeOnly?>(0),
#endif
                GuidHashSetNullableEmpty = new HashSet<Guid?>(0),

                BooleanHashSetNullableNull = null,
                ByteHashSetNullableNull = null,
                SByteHashSetNullableNull = null,
                Int16HashSetNullableNull = null,
                UInt16HashSetNullableNull = null,
                Int32HashSetNullableNull = null,
                UInt32HashSetNullableNull = null,
                Int64HashSetNullableNull = null,
                UInt64HashSetNullableNull = null,
                SingleHashSetNullableNull = null,
                DoubleHashSetNullableNull = null,
                DecimalHashSetNullableNull = null,
                CharHashSetNullableNull = null,
                DateTimeHashSetNullableNull = null,
                DateTimeOffsetHashSetNullableNull = null,
                TimeSpanHashSetNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyHashSetNullableNull = null,
                TimeOnlyHashSetNullableNull = null,
#endif
                GuidHashSetNullableNull = null,

                StringHashSet = new HashSet<string>() { "Hello", "World", null, "", "People" },
                StringHashSetEmpty = new HashSet<string>(0),
                StringHashSetNull = null,

                EnumHashSet = new HashSet<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumHashSetEmpty = new(0),
                EnumHashSetNull = null,

                EnumHashSetNullable = new HashSet<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumHashSetNullableEmpty = new(0),
                EnumHashSetNullableNull = null,

                ClassHashSet = new HashSet<SimpleModel>() { new SimpleModel { Value1 = 10, Value2 = "S-10" }, new SimpleModel { Value1 = 11, Value2 = "S-11" } },
                ClassHashSetEmpty = new HashSet<SimpleModel>(0),
                ClassHashSetNull = null,
            };
            return model;
        }
    }
}
