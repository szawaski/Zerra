// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

#if NET5_0_OR_GREATER

using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.SourceGeneration.GenerateTypeDetail]
    public class TypesIReadOnlySetTModel
    {
        public IReadOnlySet<bool> BooleanIReadOnlySet { get; set; }
        public IReadOnlySet<byte> ByteIReadOnlySet { get; set; }
        public IReadOnlySet<sbyte> SByteIReadOnlySet { get; set; }
        public IReadOnlySet<short> Int16IReadOnlySet { get; set; }
        public IReadOnlySet<ushort> UInt16IReadOnlySet { get; set; }
        public IReadOnlySet<int> Int32IReadOnlySet { get; set; }
        public IReadOnlySet<uint> UInt32IReadOnlySet { get; set; }
        public IReadOnlySet<long> Int64IReadOnlySet { get; set; }
        public IReadOnlySet<ulong> UInt64IReadOnlySet { get; set; }
        public IReadOnlySet<float> SingleIReadOnlySet { get; set; }
        public IReadOnlySet<double> DoubleIReadOnlySet { get; set; }
        public IReadOnlySet<decimal> DecimalIReadOnlySet { get; set; }
        public IReadOnlySet<char> CharIReadOnlySet { get; set; }
        public IReadOnlySet<DateTime> DateTimeIReadOnlySet { get; set; }
        public IReadOnlySet<DateTimeOffset> DateTimeOffsetIReadOnlySet { get; set; }
        public IReadOnlySet<TimeSpan> TimeSpanIReadOnlySet { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlySet<DateOnly> DateOnlyIReadOnlySet { get; set; }
        public IReadOnlySet<TimeOnly> TimeOnlyIReadOnlySet { get; set; }
#endif
        public IReadOnlySet<Guid> GuidIReadOnlySet { get; set; }

        public IReadOnlySet<bool> BooleanIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<byte> ByteIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<sbyte> SByteIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<short> Int16IReadOnlySetEmpty { get; set; }
        public IReadOnlySet<ushort> UInt16IReadOnlySetEmpty { get; set; }
        public IReadOnlySet<int> Int32IReadOnlySetEmpty { get; set; }
        public IReadOnlySet<uint> UInt32IReadOnlySetEmpty { get; set; }
        public IReadOnlySet<long> Int64IReadOnlySetEmpty { get; set; }
        public IReadOnlySet<ulong> UInt64IReadOnlySetEmpty { get; set; }
        public IReadOnlySet<float> SingleIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<double> DoubleIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<decimal> DecimalIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<char> CharIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<DateTime> DateTimeIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<DateTimeOffset> DateTimeOffsetIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<TimeSpan> TimeSpanIReadOnlySetEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlySet<DateOnly> DateOnlyIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<TimeOnly> TimeOnlyIReadOnlySetEmpty { get; set; }
#endif
        public IReadOnlySet<Guid> GuidIReadOnlySetEmpty { get; set; }

        public IReadOnlySet<bool> BooleanIReadOnlySetNull { get; set; }
        public IReadOnlySet<byte> ByteIReadOnlySetNull { get; set; }
        public IReadOnlySet<sbyte> SByteIReadOnlySetNull { get; set; }
        public IReadOnlySet<short> Int16IReadOnlySetNull { get; set; }
        public IReadOnlySet<ushort> UInt16IReadOnlySetNull { get; set; }
        public IReadOnlySet<int> Int32IReadOnlySetNull { get; set; }
        public IReadOnlySet<uint> UInt32IReadOnlySetNull { get; set; }
        public IReadOnlySet<long> Int64IReadOnlySetNull { get; set; }
        public IReadOnlySet<ulong> UInt64IReadOnlySetNull { get; set; }
        public IReadOnlySet<float> SingleIReadOnlySetNull { get; set; }
        public IReadOnlySet<double> DoubleIReadOnlySetNull { get; set; }
        public IReadOnlySet<decimal> DecimalIReadOnlySetNull { get; set; }
        public IReadOnlySet<char> CharIReadOnlySetNull { get; set; }
        public IReadOnlySet<DateTime> DateTimeIReadOnlySetNull { get; set; }
        public IReadOnlySet<DateTimeOffset> DateTimeOffsetIReadOnlySetNull { get; set; }
        public IReadOnlySet<TimeSpan> TimeSpanIReadOnlySetNull { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlySet<DateOnly> DateOnlyIReadOnlySetNull { get; set; }
        public IReadOnlySet<TimeOnly> TimeOnlyIReadOnlySetNull { get; set; }
#endif
        public IReadOnlySet<Guid> GuidIReadOnlySetNull { get; set; }

        public IReadOnlySet<bool?> BooleanIReadOnlySetNullable { get; set; }
        public IReadOnlySet<byte?> ByteIReadOnlySetNullable { get; set; }
        public IReadOnlySet<sbyte?> SByteIReadOnlySetNullable { get; set; }
        public IReadOnlySet<short?> Int16IReadOnlySetNullable { get; set; }
        public IReadOnlySet<ushort?> UInt16IReadOnlySetNullable { get; set; }
        public IReadOnlySet<int?> Int32IReadOnlySetNullable { get; set; }
        public IReadOnlySet<uint?> UInt32IReadOnlySetNullable { get; set; }
        public IReadOnlySet<long?> Int64IReadOnlySetNullable { get; set; }
        public IReadOnlySet<ulong?> UInt64IReadOnlySetNullable { get; set; }
        public IReadOnlySet<float?> SingleIReadOnlySetNullable { get; set; }
        public IReadOnlySet<double?> DoubleIReadOnlySetNullable { get; set; }
        public IReadOnlySet<decimal?> DecimalIReadOnlySetNullable { get; set; }
        public IReadOnlySet<char?> CharIReadOnlySetNullable { get; set; }
        public IReadOnlySet<DateTime?> DateTimeIReadOnlySetNullable { get; set; }
        public IReadOnlySet<DateTimeOffset?> DateTimeOffsetIReadOnlySetNullable { get; set; }
        public IReadOnlySet<TimeSpan?> TimeSpanIReadOnlySetNullable { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlySet<DateOnly?> DateOnlyIReadOnlySetNullable { get; set; }
        public IReadOnlySet<TimeOnly?> TimeOnlyIReadOnlySetNullable { get; set; }
#endif
        public IReadOnlySet<Guid?> GuidIReadOnlySetNullable { get; set; }

        public IReadOnlySet<bool?> BooleanIReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<byte?> ByteIReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<sbyte?> SByteIReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<short?> Int16IReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<ushort?> UInt16IReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<int?> Int32IReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<uint?> UInt32IReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<long?> Int64IReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<ulong?> UInt64IReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<float?> SingleIReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<double?> DoubleIReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<decimal?> DecimalIReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<char?> CharIReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<DateTime?> DateTimeIReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<DateTimeOffset?> DateTimeOffsetIReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<TimeSpan?> TimeSpanIReadOnlySetNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlySet<DateOnly?> DateOnlyIReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<TimeOnly?> TimeOnlyIReadOnlySetNullableEmpty { get; set; }
#endif
        public IReadOnlySet<Guid?> GuidIReadOnlySetNullableEmpty { get; set; }

        public IReadOnlySet<bool?> BooleanIReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<byte?> ByteIReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<sbyte?> SByteIReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<short?> Int16IReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<ushort?> UInt16IReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<int?> Int32IReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<uint?> UInt32IReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<long?> Int64IReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<ulong?> UInt64IReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<float?> SingleIReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<double?> DoubleIReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<decimal?> DecimalIReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<char?> CharIReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<DateTime?> DateTimeIReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<DateTimeOffset?> DateTimeOffsetIReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<TimeSpan?> TimeSpanIReadOnlySetNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public IReadOnlySet<DateOnly?> DateOnlyIReadOnlySetNullableNull { get; set; }
        public IReadOnlySet<TimeOnly?> TimeOnlyIReadOnlySetNullableNull { get; set; }
#endif
        public IReadOnlySet<Guid?> GuidIReadOnlySetNullableNull { get; set; }

        public IReadOnlySet<string> StringIReadOnlySet { get; set; }
        public IReadOnlySet<string> StringIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<string> StringIReadOnlySetNull { get; set; }

        public IReadOnlySet<EnumModel> EnumIReadOnlySet { get; set; }
        public IReadOnlySet<EnumModel> EnumIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<EnumModel> EnumIReadOnlySetNull { get; set; }

        public IReadOnlySet<EnumModel?> EnumIReadOnlySetNullable { get; set; }
        public IReadOnlySet<EnumModel?> EnumIReadOnlySetNullableEmpty { get; set; }
        public IReadOnlySet<EnumModel?> EnumIReadOnlySetNullableNull { get; set; }

        public IReadOnlySet<SimpleModel> ClassIReadOnlySet { get; set; }
        public IReadOnlySet<SimpleModel> ClassIReadOnlySetEmpty { get; set; }
        public IReadOnlySet<SimpleModel> ClassIReadOnlySetNull { get; set; }

        public static TypesIReadOnlySetTModel Create()
        {
            var model = new TypesIReadOnlySetTModel()
            {
                BooleanIReadOnlySet = new HashSet<bool>() { true, false, true },
                ByteIReadOnlySet = new HashSet<byte>() { 1, 2, 3 },
                SByteIReadOnlySet = new HashSet<sbyte>() { 4, 5, 6 },
                Int16IReadOnlySet = new HashSet<short>() { 7, 8, 9 },
                UInt16IReadOnlySet = new HashSet<ushort>() { 10, 11, 12 },
                Int32IReadOnlySet = new HashSet<int>() { 13, 14, 15 },
                UInt32IReadOnlySet = new HashSet<uint>() { 16, 17, 18 },
                Int64IReadOnlySet = new HashSet<long>() { 19, 20, 21 },
                UInt64IReadOnlySet = new HashSet<ulong>() { 22, 23, 24 },
                SingleIReadOnlySet = new HashSet<float>() { 25, 26, 27 },
                DoubleIReadOnlySet = new HashSet<double>() { 28, 29, 30 },
                DecimalIReadOnlySet = new HashSet<decimal>() { 31, 32, 33 },
                CharIReadOnlySet = new HashSet<char>() { 'A', 'B', 'C' },
                DateTimeIReadOnlySet = new HashSet<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIReadOnlySet = new HashSet<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIReadOnlySet = new HashSet<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlySet = new HashSet<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIReadOnlySet = new HashSet<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIReadOnlySet = new HashSet<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanIReadOnlySetEmpty = new HashSet<bool>(0),
                ByteIReadOnlySetEmpty = new HashSet<byte>(0),
                SByteIReadOnlySetEmpty = new HashSet<sbyte>(0),
                Int16IReadOnlySetEmpty = new HashSet<short>(0),
                UInt16IReadOnlySetEmpty = new HashSet<ushort>(0),
                Int32IReadOnlySetEmpty = new HashSet<int>(0),
                UInt32IReadOnlySetEmpty = new HashSet<uint>(0),
                Int64IReadOnlySetEmpty = new HashSet<long>(0),
                UInt64IReadOnlySetEmpty = new HashSet<ulong>(0),
                SingleIReadOnlySetEmpty = new HashSet<float>(0),
                DoubleIReadOnlySetEmpty = new HashSet<double>(0),
                DecimalIReadOnlySetEmpty = new HashSet<decimal>(0),
                CharIReadOnlySetEmpty = new HashSet<char>(0),
                DateTimeIReadOnlySetEmpty = new HashSet<DateTime>(0),
                DateTimeOffsetIReadOnlySetEmpty = new HashSet<DateTimeOffset>(0),
                TimeSpanIReadOnlySetEmpty = new HashSet<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlySetEmpty = new HashSet<DateOnly>(0),
                TimeOnlyIReadOnlySetEmpty = new HashSet<TimeOnly>(0),
#endif
                GuidIReadOnlySetEmpty = new HashSet<Guid>(0),

                BooleanIReadOnlySetNull = null,
                ByteIReadOnlySetNull = null,
                SByteIReadOnlySetNull = null,
                Int16IReadOnlySetNull = null,
                UInt16IReadOnlySetNull = null,
                Int32IReadOnlySetNull = null,
                UInt32IReadOnlySetNull = null,
                Int64IReadOnlySetNull = null,
                UInt64IReadOnlySetNull = null,
                SingleIReadOnlySetNull = null,
                DoubleIReadOnlySetNull = null,
                DecimalIReadOnlySetNull = null,
                CharIReadOnlySetNull = null,
                DateTimeIReadOnlySetNull = null,
                DateTimeOffsetIReadOnlySetNull = null,
                TimeSpanIReadOnlySetNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlySetNull = null,
                TimeOnlyIReadOnlySetNull = null,
#endif
                GuidIReadOnlySetNull = null,

                BooleanIReadOnlySetNullable = new HashSet<bool?>() { true, null, true },
                ByteIReadOnlySetNullable = new HashSet<byte?>() { 1, null, 3 },
                SByteIReadOnlySetNullable = new HashSet<sbyte?>() { 4, null, 6 },
                Int16IReadOnlySetNullable = new HashSet<short?>() { 7, null, 9 },
                UInt16IReadOnlySetNullable = new HashSet<ushort?>() { 10, null, 12 },
                Int32IReadOnlySetNullable = new HashSet<int?>() { 13, null, 15 },
                UInt32IReadOnlySetNullable = new HashSet<uint?>() { 16, null, 18 },
                Int64IReadOnlySetNullable = new HashSet<long?>() { 19, null, 21 },
                UInt64IReadOnlySetNullable = new HashSet<ulong?>() { 22, null, 24 },
                SingleIReadOnlySetNullable = new HashSet<float?>() { 25, null, 27 },
                DoubleIReadOnlySetNullable = new HashSet<double?>() { 28, null, 30 },
                DecimalIReadOnlySetNullable = new HashSet<decimal?>() { 31, null, 33 },
                CharIReadOnlySetNullable = new HashSet<char?>() { 'A', null, 'C' },
                DateTimeIReadOnlySetNullable = new HashSet<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIReadOnlySetNullable = new HashSet<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIReadOnlySetNullable = new HashSet<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlySetNullable = new HashSet<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIReadOnlySetNullable = new HashSet<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIReadOnlySetNullable = new HashSet<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanIReadOnlySetNullableEmpty = new HashSet<bool?>(0),
                ByteIReadOnlySetNullableEmpty = new HashSet<byte?>(0),
                SByteIReadOnlySetNullableEmpty = new HashSet<sbyte?>(0),
                Int16IReadOnlySetNullableEmpty = new HashSet<short?>(0),
                UInt16IReadOnlySetNullableEmpty = new HashSet<ushort?>(0),
                Int32IReadOnlySetNullableEmpty = new HashSet<int?>(0),
                UInt32IReadOnlySetNullableEmpty = new HashSet<uint?>(0),
                Int64IReadOnlySetNullableEmpty = new HashSet<long?>(0),
                UInt64IReadOnlySetNullableEmpty = new HashSet<ulong?>(0),
                SingleIReadOnlySetNullableEmpty = new HashSet<float?>(0),
                DoubleIReadOnlySetNullableEmpty = new HashSet<double?>(0),
                DecimalIReadOnlySetNullableEmpty = new HashSet<decimal?>(0),
                CharIReadOnlySetNullableEmpty = new HashSet<char?>(0),
                DateTimeIReadOnlySetNullableEmpty = new HashSet<DateTime?>(0),
                DateTimeOffsetIReadOnlySetNullableEmpty = new HashSet<DateTimeOffset?>(0),
                TimeSpanIReadOnlySetNullableEmpty = new HashSet<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlySetNullableEmpty = new HashSet<DateOnly?>(0),
                TimeOnlyIReadOnlySetNullableEmpty = new HashSet<TimeOnly?>(0),
#endif
                GuidIReadOnlySetNullableEmpty = new HashSet<Guid?>(0),

                BooleanIReadOnlySetNullableNull = null,
                ByteIReadOnlySetNullableNull = null,
                SByteIReadOnlySetNullableNull = null,
                Int16IReadOnlySetNullableNull = null,
                UInt16IReadOnlySetNullableNull = null,
                Int32IReadOnlySetNullableNull = null,
                UInt32IReadOnlySetNullableNull = null,
                Int64IReadOnlySetNullableNull = null,
                UInt64IReadOnlySetNullableNull = null,
                SingleIReadOnlySetNullableNull = null,
                DoubleIReadOnlySetNullableNull = null,
                DecimalIReadOnlySetNullableNull = null,
                CharIReadOnlySetNullableNull = null,
                DateTimeIReadOnlySetNullableNull = null,
                DateTimeOffsetIReadOnlySetNullableNull = null,
                TimeSpanIReadOnlySetNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlySetNullableNull = null,
                TimeOnlyIReadOnlySetNullableNull = null,
#endif
                GuidIReadOnlySetNullableNull = null,

                StringIReadOnlySet = new HashSet<string>() { "Hello", "World", null, "", "People" },
                StringIReadOnlySetEmpty = new HashSet<string>(0),
                StringIReadOnlySetNull = null,

                EnumIReadOnlySet = new HashSet<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumIReadOnlySetEmpty = new HashSet<EnumModel>(0),
                EnumIReadOnlySetNull = null,

                EnumIReadOnlySetNullable = new HashSet<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumIReadOnlySetNullableEmpty = new HashSet<EnumModel?>(0),
                EnumIReadOnlySetNullableNull = null,

                ClassIReadOnlySet = new HashSet<SimpleModel>() { new SimpleModel { Value1 = 10, Value2 = "S-10" }, new SimpleModel { Value1 = 11, Value2 = "S-11" } },
                ClassIReadOnlySetEmpty = new HashSet<SimpleModel>(0),
                ClassIReadOnlySetNull = null,
            };
            return model;
        }
    }
}

#endif