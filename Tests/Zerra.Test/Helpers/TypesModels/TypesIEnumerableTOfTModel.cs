// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.SourceGeneration.SourceGenerationTypeDetail]
    public class TypesIEnumerableTOfTModel
    {
        public sealed class CustomIEnumerable<T> : IEnumerable<T>
        {
            private readonly List<T> list;
            public CustomIEnumerable()
            {
                list = new();
            }
            public CustomIEnumerable(int capacity)
            {
                list = new(capacity);
            }

            public void Add(T item) => list.Add(item);

            public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
        }

        public CustomIEnumerable<bool> BooleanIEnumerableT { get; set; }
        public CustomIEnumerable<byte> ByteIEnumerableT { get; set; }
        public CustomIEnumerable<sbyte> SByteIEnumerableT { get; set; }
        public CustomIEnumerable<short> Int16IEnumerableT { get; set; }
        public CustomIEnumerable<ushort> UInt16IEnumerableT { get; set; }
        public CustomIEnumerable<int> Int32IEnumerableT { get; set; }
        public CustomIEnumerable<uint> UInt32IEnumerableT { get; set; }
        public CustomIEnumerable<long> Int64IEnumerableT { get; set; }
        public CustomIEnumerable<ulong> UInt64IEnumerableT { get; set; }
        public CustomIEnumerable<float> SingleIEnumerableT { get; set; }
        public CustomIEnumerable<double> DoubleIEnumerableT { get; set; }
        public CustomIEnumerable<decimal> DecimalIEnumerableT { get; set; }
        public CustomIEnumerable<char> CharIEnumerableT { get; set; }
        public CustomIEnumerable<DateTime> DateTimeIEnumerableT { get; set; }
        public CustomIEnumerable<DateTimeOffset> DateTimeOffsetIEnumerableT { get; set; }
        public CustomIEnumerable<TimeSpan> TimeSpanIEnumerableT { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable<DateOnly> DateOnlyIEnumerableT { get; set; }
        public CustomIEnumerable<TimeOnly> TimeOnlyIEnumerableT { get; set; }
#endif
        public CustomIEnumerable<Guid> GuidIEnumerableT { get; set; }

        public CustomIEnumerable<bool> BooleanIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<byte> ByteIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<sbyte> SByteIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<short> Int16IEnumerableTEmpty { get; set; }
        public CustomIEnumerable<ushort> UInt16IEnumerableTEmpty { get; set; }
        public CustomIEnumerable<int> Int32IEnumerableTEmpty { get; set; }
        public CustomIEnumerable<uint> UInt32IEnumerableTEmpty { get; set; }
        public CustomIEnumerable<long> Int64IEnumerableTEmpty { get; set; }
        public CustomIEnumerable<ulong> UInt64IEnumerableTEmpty { get; set; }
        public CustomIEnumerable<float> SingleIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<double> DoubleIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<decimal> DecimalIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<char> CharIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<DateTime> DateTimeIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<DateTimeOffset> DateTimeOffsetIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<TimeSpan> TimeSpanIEnumerableTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable<DateOnly> DateOnlyIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<TimeOnly> TimeOnlyIEnumerableTEmpty { get; set; }
#endif
        public CustomIEnumerable<Guid> GuidIEnumerableTEmpty { get; set; }

        public CustomIEnumerable<bool> BooleanIEnumerableTNull { get; set; }
        public CustomIEnumerable<byte> ByteIEnumerableTNull { get; set; }
        public CustomIEnumerable<sbyte> SByteIEnumerableTNull { get; set; }
        public CustomIEnumerable<short> Int16IEnumerableTNull { get; set; }
        public CustomIEnumerable<ushort> UInt16IEnumerableTNull { get; set; }
        public CustomIEnumerable<int> Int32IEnumerableTNull { get; set; }
        public CustomIEnumerable<uint> UInt32IEnumerableTNull { get; set; }
        public CustomIEnumerable<long> Int64IEnumerableTNull { get; set; }
        public CustomIEnumerable<ulong> UInt64IEnumerableTNull { get; set; }
        public CustomIEnumerable<float> SingleIEnumerableTNull { get; set; }
        public CustomIEnumerable<double> DoubleIEnumerableTNull { get; set; }
        public CustomIEnumerable<decimal> DecimalIEnumerableTNull { get; set; }
        public CustomIEnumerable<char> CharIEnumerableTNull { get; set; }
        public CustomIEnumerable<DateTime> DateTimeIEnumerableTNull { get; set; }
        public CustomIEnumerable<DateTimeOffset> DateTimeOffsetIEnumerableTNull { get; set; }
        public CustomIEnumerable<TimeSpan> TimeSpanIEnumerableTNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable<DateOnly> DateOnlyIEnumerableTNull { get; set; }
        public CustomIEnumerable<TimeOnly> TimeOnlyIEnumerableTNull { get; set; }
#endif
        public CustomIEnumerable<Guid> GuidIEnumerableTNull { get; set; }

        public CustomIEnumerable<bool?> BooleanIEnumerableTNullable { get; set; }
        public CustomIEnumerable<byte?> ByteIEnumerableTNullable { get; set; }
        public CustomIEnumerable<sbyte?> SByteIEnumerableTNullable { get; set; }
        public CustomIEnumerable<short?> Int16IEnumerableTNullable { get; set; }
        public CustomIEnumerable<ushort?> UInt16IEnumerableTNullable { get; set; }
        public CustomIEnumerable<int?> Int32IEnumerableTNullable { get; set; }
        public CustomIEnumerable<uint?> UInt32IEnumerableTNullable { get; set; }
        public CustomIEnumerable<long?> Int64IEnumerableTNullable { get; set; }
        public CustomIEnumerable<ulong?> UInt64IEnumerableTNullable { get; set; }
        public CustomIEnumerable<float?> SingleIEnumerableTNullable { get; set; }
        public CustomIEnumerable<double?> DoubleIEnumerableTNullable { get; set; }
        public CustomIEnumerable<decimal?> DecimalIEnumerableTNullable { get; set; }
        public CustomIEnumerable<char?> CharIEnumerableTNullable { get; set; }
        public CustomIEnumerable<DateTime?> DateTimeIEnumerableTNullable { get; set; }
        public CustomIEnumerable<DateTimeOffset?> DateTimeOffsetIEnumerableTNullable { get; set; }
        public CustomIEnumerable<TimeSpan?> TimeSpanIEnumerableTNullable { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable<DateOnly?> DateOnlyIEnumerableTNullable { get; set; }
        public CustomIEnumerable<TimeOnly?> TimeOnlyIEnumerableTNullable { get; set; }
#endif
        public CustomIEnumerable<Guid?> GuidIEnumerableTNullable { get; set; }

        public CustomIEnumerable<bool?> BooleanIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<byte?> ByteIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<sbyte?> SByteIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<short?> Int16IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<ushort?> UInt16IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<int?> Int32IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<uint?> UInt32IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<long?> Int64IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<ulong?> UInt64IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<float?> SingleIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<double?> DoubleIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<decimal?> DecimalIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<char?> CharIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<DateTime?> DateTimeIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<DateTimeOffset?> DateTimeOffsetIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<TimeSpan?> TimeSpanIEnumerableTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable<DateOnly?> DateOnlyIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<TimeOnly?> TimeOnlyIEnumerableTNullableEmpty { get; set; }
#endif
        public CustomIEnumerable<Guid?> GuidIEnumerableTNullableEmpty { get; set; }

        public CustomIEnumerable<bool?> BooleanIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<byte?> ByteIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<sbyte?> SByteIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<short?> Int16IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<ushort?> UInt16IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<int?> Int32IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<uint?> UInt32IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<long?> Int64IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<ulong?> UInt64IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<float?> SingleIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<double?> DoubleIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<decimal?> DecimalIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<char?> CharIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<DateTime?> DateTimeIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<DateTimeOffset?> DateTimeOffsetIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<TimeSpan?> TimeSpanIEnumerableTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable<DateOnly?> DateOnlyIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable<TimeOnly?> TimeOnlyIEnumerableTNullableNull { get; set; }
#endif
        public CustomIEnumerable<Guid?> GuidIEnumerableTNullableNull { get; set; }

        public CustomIEnumerable<string> StringIEnumerableT { get; set; }
        public CustomIEnumerable<string> StringIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<string> StringIEnumerableTNull { get; set; }

        public CustomIEnumerable<EnumModel> EnumIEnumerableT { get; set; }
        public CustomIEnumerable<EnumModel> EnumIEnumerableTEmpty { get; set; }
        public CustomIEnumerable<EnumModel> EnumIEnumerableTNull { get; set; }

        public CustomIEnumerable<EnumModel?> EnumIEnumerableTNullable { get; set; }
        public CustomIEnumerable<EnumModel?> EnumIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable<EnumModel?> EnumIEnumerableTNullableNull { get; set; }

        public CustomIEnumerable<SimpleModel> ClassIEnumerable { get; set; }
        public CustomIEnumerable<SimpleModel> ClassIEnumerableEmpty { get; set; }
        public CustomIEnumerable<SimpleModel> ClassIEnumerableNull { get; set; }

        public static TypesIEnumerableTOfTModel Create()
        {
            var model = new TypesIEnumerableTOfTModel()
            {
                BooleanIEnumerableT = new CustomIEnumerable<bool>() { true, false, true },
                ByteIEnumerableT = new CustomIEnumerable<byte>() { 1, 2, 3 },
                SByteIEnumerableT = new CustomIEnumerable<sbyte>() { 4, 5, 6 },
                Int16IEnumerableT = new CustomIEnumerable<short>() { 7, 8, 9 },
                UInt16IEnumerableT = new CustomIEnumerable<ushort>() { 10, 11, 12 },
                Int32IEnumerableT = new CustomIEnumerable<int>() { 13, 14, 15 },
                UInt32IEnumerableT = new CustomIEnumerable<uint>() { 16, 17, 18 },
                Int64IEnumerableT = new CustomIEnumerable<long>() { 19, 20, 21 },
                UInt64IEnumerableT = new CustomIEnumerable<ulong>() { 22, 23, 24 },
                SingleIEnumerableT = new CustomIEnumerable<float>() { 25, 26, 27 },
                DoubleIEnumerableT = new CustomIEnumerable<double>() { 28, 29, 30 },
                DecimalIEnumerableT = new CustomIEnumerable<decimal>() { 31, 32, 33 },
                CharIEnumerableT = new CustomIEnumerable<char>() { 'A', 'B', 'C' },
                DateTimeIEnumerableT = new CustomIEnumerable<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIEnumerableT = new CustomIEnumerable<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIEnumerableT = new CustomIEnumerable<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableT = new CustomIEnumerable<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIEnumerableT = new CustomIEnumerable<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIEnumerableT = new CustomIEnumerable<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanIEnumerableTEmpty = new CustomIEnumerable<bool>(0),
                ByteIEnumerableTEmpty = new CustomIEnumerable<byte>(0),
                SByteIEnumerableTEmpty = new CustomIEnumerable<sbyte>(0),
                Int16IEnumerableTEmpty = new CustomIEnumerable<short>(0),
                UInt16IEnumerableTEmpty = new CustomIEnumerable<ushort>(0),
                Int32IEnumerableTEmpty = new CustomIEnumerable<int>(0),
                UInt32IEnumerableTEmpty = new CustomIEnumerable<uint>(0),
                Int64IEnumerableTEmpty = new CustomIEnumerable<long>(0),
                UInt64IEnumerableTEmpty = new CustomIEnumerable<ulong>(0),
                SingleIEnumerableTEmpty = new CustomIEnumerable<float>(0),
                DoubleIEnumerableTEmpty = new CustomIEnumerable<double>(0),
                DecimalIEnumerableTEmpty = new CustomIEnumerable<decimal>(0),
                CharIEnumerableTEmpty = new CustomIEnumerable<char>(0),
                DateTimeIEnumerableTEmpty = new CustomIEnumerable<DateTime>(0),
                DateTimeOffsetIEnumerableTEmpty = new CustomIEnumerable<DateTimeOffset>(0),
                TimeSpanIEnumerableTEmpty = new CustomIEnumerable<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableTEmpty = new CustomIEnumerable<DateOnly>(0),
                TimeOnlyIEnumerableTEmpty = new CustomIEnumerable<TimeOnly>(0),
#endif
                GuidIEnumerableTEmpty = new CustomIEnumerable<Guid>(0),

                BooleanIEnumerableTNull = null,
                ByteIEnumerableTNull = null,
                SByteIEnumerableTNull = null,
                Int16IEnumerableTNull = null,
                UInt16IEnumerableTNull = null,
                Int32IEnumerableTNull = null,
                UInt32IEnumerableTNull = null,
                Int64IEnumerableTNull = null,
                UInt64IEnumerableTNull = null,
                SingleIEnumerableTNull = null,
                DoubleIEnumerableTNull = null,
                DecimalIEnumerableTNull = null,
                CharIEnumerableTNull = null,
                DateTimeIEnumerableTNull = null,
                DateTimeOffsetIEnumerableTNull = null,
                TimeSpanIEnumerableTNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableTNull = null,
                TimeOnlyIEnumerableTNull = null,
#endif
                GuidIEnumerableTNull = null,

                BooleanIEnumerableTNullable = new CustomIEnumerable<bool?>() { true, null, true },
                ByteIEnumerableTNullable = new CustomIEnumerable<byte?>() { 1, null, 3 },
                SByteIEnumerableTNullable = new CustomIEnumerable<sbyte?>() { 4, null, 6 },
                Int16IEnumerableTNullable = new CustomIEnumerable<short?>() { 7, null, 9 },
                UInt16IEnumerableTNullable = new CustomIEnumerable<ushort?>() { 10, null, 12 },
                Int32IEnumerableTNullable = new CustomIEnumerable<int?>() { 13, null, 15 },
                UInt32IEnumerableTNullable = new CustomIEnumerable<uint?>() { 16, null, 18 },
                Int64IEnumerableTNullable = new CustomIEnumerable<long?>() { 19, null, 21 },
                UInt64IEnumerableTNullable = new CustomIEnumerable<ulong?>() { 22, null, 24 },
                SingleIEnumerableTNullable = new CustomIEnumerable<float?>() { 25, null, 27 },
                DoubleIEnumerableTNullable = new CustomIEnumerable<double?>() { 28, null, 30 },
                DecimalIEnumerableTNullable = new CustomIEnumerable<decimal?>() { 31, null, 33 },
                CharIEnumerableTNullable = new CustomIEnumerable<char?>() { 'A', null, 'C' },
                DateTimeIEnumerableTNullable = new CustomIEnumerable<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIEnumerableTNullable = new CustomIEnumerable<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIEnumerableTNullable = new CustomIEnumerable<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableTNullable = new CustomIEnumerable<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIEnumerableTNullable = new CustomIEnumerable<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIEnumerableTNullable = new CustomIEnumerable<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanIEnumerableTNullableEmpty = new CustomIEnumerable<bool?>(0),
                ByteIEnumerableTNullableEmpty = new CustomIEnumerable<byte?>(0),
                SByteIEnumerableTNullableEmpty = new CustomIEnumerable<sbyte?>(0),
                Int16IEnumerableTNullableEmpty = new CustomIEnumerable<short?>(0),
                UInt16IEnumerableTNullableEmpty = new CustomIEnumerable<ushort?>(0),
                Int32IEnumerableTNullableEmpty = new CustomIEnumerable<int?>(0),
                UInt32IEnumerableTNullableEmpty = new CustomIEnumerable<uint?>(0),
                Int64IEnumerableTNullableEmpty = new CustomIEnumerable<long?>(0),
                UInt64IEnumerableTNullableEmpty = new CustomIEnumerable<ulong?>(0),
                SingleIEnumerableTNullableEmpty = new CustomIEnumerable<float?>(0),
                DoubleIEnumerableTNullableEmpty = new CustomIEnumerable<double?>(0),
                DecimalIEnumerableTNullableEmpty = new CustomIEnumerable<decimal?>(0),
                CharIEnumerableTNullableEmpty = new CustomIEnumerable<char?>(0),
                DateTimeIEnumerableTNullableEmpty = new CustomIEnumerable<DateTime?>(0),
                DateTimeOffsetIEnumerableTNullableEmpty = new CustomIEnumerable<DateTimeOffset?>(0),
                TimeSpanIEnumerableTNullableEmpty = new CustomIEnumerable<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableTNullableEmpty = new CustomIEnumerable<DateOnly?>(0),
                TimeOnlyIEnumerableTNullableEmpty = new CustomIEnumerable<TimeOnly?>(0),
#endif
                GuidIEnumerableTNullableEmpty = new CustomIEnumerable<Guid?>(0),

                BooleanIEnumerableTNullableNull = null,
                ByteIEnumerableTNullableNull = null,
                SByteIEnumerableTNullableNull = null,
                Int16IEnumerableTNullableNull = null,
                UInt16IEnumerableTNullableNull = null,
                Int32IEnumerableTNullableNull = null,
                UInt32IEnumerableTNullableNull = null,
                Int64IEnumerableTNullableNull = null,
                UInt64IEnumerableTNullableNull = null,
                SingleIEnumerableTNullableNull = null,
                DoubleIEnumerableTNullableNull = null,
                DecimalIEnumerableTNullableNull = null,
                CharIEnumerableTNullableNull = null,
                DateTimeIEnumerableTNullableNull = null,
                DateTimeOffsetIEnumerableTNullableNull = null,
                TimeSpanIEnumerableTNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableTNullableNull = null,
                TimeOnlyIEnumerableTNullableNull = null,
#endif
                GuidIEnumerableTNullableNull = null,

                StringIEnumerableT = new CustomIEnumerable<string>() { "Hello", "World", null, "", "People" },
                StringIEnumerableTEmpty = new CustomIEnumerable<string>(0),
                StringIEnumerableTNull = null,

                EnumIEnumerableT = new CustomIEnumerable<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumIEnumerableTEmpty = new CustomIEnumerable<EnumModel>(0),
                EnumIEnumerableTNull = null,

                EnumIEnumerableTNullable = new CustomIEnumerable<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumIEnumerableTNullableEmpty = new CustomIEnumerable<EnumModel?>(0),
                EnumIEnumerableTNullableNull = null,

                ClassIEnumerable = new CustomIEnumerable<SimpleModel>() { new SimpleModel { Value1 = 10, Value2 = "S-10" }, new SimpleModel { Value1 = 11, Value2 = "S-11" } },
                ClassIEnumerableEmpty = new CustomIEnumerable<SimpleModel>(0),
                ClassIEnumerableNull = null,
            };
            return model;
        }
    }
}