// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.Reflection.GenerateTypeDetail]
    public class TypesICollectionTOfTModel
    {
        public sealed class CustomICollection<T> : ICollection<T>
        {
            private readonly List<T> list;
            public CustomICollection()
            {
                list = new();
            }
            public CustomICollection(int capacity)
            {
                list = new(capacity);
            }

            public int Count => list.Count;
            public bool IsReadOnly => false;
            public void Add(T item) => list.Add(item);
            public void Clear() => list.Clear();
            public bool Contains(T item) => list.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
            public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
            public bool Remove(T item) => list.Remove(item);
            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
        }

        public CustomICollection<bool> BooleanICollectionT { get; set; }
        public CustomICollection<byte> ByteICollectionT { get; set; }
        public CustomICollection<sbyte> SByteICollectionT { get; set; }
        public CustomICollection<short> Int16ICollectionT { get; set; }
        public CustomICollection<ushort> UInt16ICollectionT { get; set; }
        public CustomICollection<int> Int32ICollectionT { get; set; }
        public CustomICollection<uint> UInt32ICollectionT { get; set; }
        public CustomICollection<long> Int64ICollectionT { get; set; }
        public CustomICollection<ulong> UInt64ICollectionT { get; set; }
        public CustomICollection<float> SingleICollectionT { get; set; }
        public CustomICollection<double> DoubleICollectionT { get; set; }
        public CustomICollection<decimal> DecimalICollectionT { get; set; }
        public CustomICollection<char> CharICollectionT { get; set; }
        public CustomICollection<DateTime> DateTimeICollectionT { get; set; }
        public CustomICollection<DateTimeOffset> DateTimeOffsetICollectionT { get; set; }
        public CustomICollection<TimeSpan> TimeSpanICollectionT { get; set; }
#if NET6_0_OR_GREATER
        public CustomICollection<DateOnly> DateOnlyICollectionT { get; set; }
        public CustomICollection<TimeOnly> TimeOnlyICollectionT { get; set; }
#endif
        public CustomICollection<Guid> GuidICollectionT { get; set; }

        public CustomICollection<bool> BooleanICollectionTEmpty { get; set; }
        public CustomICollection<byte> ByteICollectionTEmpty { get; set; }
        public CustomICollection<sbyte> SByteICollectionTEmpty { get; set; }
        public CustomICollection<short> Int16ICollectionTEmpty { get; set; }
        public CustomICollection<ushort> UInt16ICollectionTEmpty { get; set; }
        public CustomICollection<int> Int32ICollectionTEmpty { get; set; }
        public CustomICollection<uint> UInt32ICollectionTEmpty { get; set; }
        public CustomICollection<long> Int64ICollectionTEmpty { get; set; }
        public CustomICollection<ulong> UInt64ICollectionTEmpty { get; set; }
        public CustomICollection<float> SingleICollectionTEmpty { get; set; }
        public CustomICollection<double> DoubleICollectionTEmpty { get; set; }
        public CustomICollection<decimal> DecimalICollectionTEmpty { get; set; }
        public CustomICollection<char> CharICollectionTEmpty { get; set; }
        public CustomICollection<DateTime> DateTimeICollectionTEmpty { get; set; }
        public CustomICollection<DateTimeOffset> DateTimeOffsetICollectionTEmpty { get; set; }
        public CustomICollection<TimeSpan> TimeSpanICollectionTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomICollection<DateOnly> DateOnlyICollectionTEmpty { get; set; }
        public CustomICollection<TimeOnly> TimeOnlyICollectionTEmpty { get; set; }
#endif
        public CustomICollection<Guid> GuidICollectionTEmpty { get; set; }

        public CustomICollection<bool> BooleanICollectionTNull { get; set; }
        public CustomICollection<byte> ByteICollectionTNull { get; set; }
        public CustomICollection<sbyte> SByteICollectionTNull { get; set; }
        public CustomICollection<short> Int16ICollectionTNull { get; set; }
        public CustomICollection<ushort> UInt16ICollectionTNull { get; set; }
        public CustomICollection<int> Int32ICollectionTNull { get; set; }
        public CustomICollection<uint> UInt32ICollectionTNull { get; set; }
        public CustomICollection<long> Int64ICollectionTNull { get; set; }
        public CustomICollection<ulong> UInt64ICollectionTNull { get; set; }
        public CustomICollection<float> SingleICollectionTNull { get; set; }
        public CustomICollection<double> DoubleICollectionTNull { get; set; }
        public CustomICollection<decimal> DecimalICollectionTNull { get; set; }
        public CustomICollection<char> CharICollectionTNull { get; set; }
        public CustomICollection<DateTime> DateTimeICollectionTNull { get; set; }
        public CustomICollection<DateTimeOffset> DateTimeOffsetICollectionTNull { get; set; }
        public CustomICollection<TimeSpan> TimeSpanICollectionTNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomICollection<DateOnly> DateOnlyICollectionTNull { get; set; }
        public CustomICollection<TimeOnly> TimeOnlyICollectionTNull { get; set; }
#endif
        public CustomICollection<Guid> GuidICollectionTNull { get; set; }

        public CustomICollection<bool?> BooleanICollectionTNullable { get; set; }
        public CustomICollection<byte?> ByteICollectionTNullable { get; set; }
        public CustomICollection<sbyte?> SByteICollectionTNullable { get; set; }
        public CustomICollection<short?> Int16ICollectionTNullable { get; set; }
        public CustomICollection<ushort?> UInt16ICollectionTNullable { get; set; }
        public CustomICollection<int?> Int32ICollectionTNullable { get; set; }
        public CustomICollection<uint?> UInt32ICollectionTNullable { get; set; }
        public CustomICollection<long?> Int64ICollectionTNullable { get; set; }
        public CustomICollection<ulong?> UInt64ICollectionTNullable { get; set; }
        public CustomICollection<float?> SingleICollectionTNullable { get; set; }
        public CustomICollection<double?> DoubleICollectionTNullable { get; set; }
        public CustomICollection<decimal?> DecimalICollectionTNullable { get; set; }
        public CustomICollection<char?> CharICollectionTNullable { get; set; }
        public CustomICollection<DateTime?> DateTimeICollectionTNullable { get; set; }
        public CustomICollection<DateTimeOffset?> DateTimeOffsetICollectionTNullable { get; set; }
        public CustomICollection<TimeSpan?> TimeSpanICollectionTNullable { get; set; }
#if NET6_0_OR_GREATER
        public CustomICollection<DateOnly?> DateOnlyICollectionTNullable { get; set; }
        public CustomICollection<TimeOnly?> TimeOnlyICollectionTNullable { get; set; }
#endif
        public CustomICollection<Guid?> GuidICollectionTNullable { get; set; }

        public CustomICollection<bool?> BooleanICollectionTNullableEmpty { get; set; }
        public CustomICollection<byte?> ByteICollectionTNullableEmpty { get; set; }
        public CustomICollection<sbyte?> SByteICollectionTNullableEmpty { get; set; }
        public CustomICollection<short?> Int16ICollectionTNullableEmpty { get; set; }
        public CustomICollection<ushort?> UInt16ICollectionTNullableEmpty { get; set; }
        public CustomICollection<int?> Int32ICollectionTNullableEmpty { get; set; }
        public CustomICollection<uint?> UInt32ICollectionTNullableEmpty { get; set; }
        public CustomICollection<long?> Int64ICollectionTNullableEmpty { get; set; }
        public CustomICollection<ulong?> UInt64ICollectionTNullableEmpty { get; set; }
        public CustomICollection<float?> SingleICollectionTNullableEmpty { get; set; }
        public CustomICollection<double?> DoubleICollectionTNullableEmpty { get; set; }
        public CustomICollection<decimal?> DecimalICollectionTNullableEmpty { get; set; }
        public CustomICollection<char?> CharICollectionTNullableEmpty { get; set; }
        public CustomICollection<DateTime?> DateTimeICollectionTNullableEmpty { get; set; }
        public CustomICollection<DateTimeOffset?> DateTimeOffsetICollectionTNullableEmpty { get; set; }
        public CustomICollection<TimeSpan?> TimeSpanICollectionTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomICollection<DateOnly?> DateOnlyICollectionTNullableEmpty { get; set; }
        public CustomICollection<TimeOnly?> TimeOnlyICollectionTNullableEmpty { get; set; }
#endif
        public CustomICollection<Guid?> GuidICollectionTNullableEmpty { get; set; }

        public CustomICollection<bool?> BooleanICollectionTNullableNull { get; set; }
        public CustomICollection<byte?> ByteICollectionTNullableNull { get; set; }
        public CustomICollection<sbyte?> SByteICollectionTNullableNull { get; set; }
        public CustomICollection<short?> Int16ICollectionTNullableNull { get; set; }
        public CustomICollection<ushort?> UInt16ICollectionTNullableNull { get; set; }
        public CustomICollection<int?> Int32ICollectionTNullableNull { get; set; }
        public CustomICollection<uint?> UInt32ICollectionTNullableNull { get; set; }
        public CustomICollection<long?> Int64ICollectionTNullableNull { get; set; }
        public CustomICollection<ulong?> UInt64ICollectionTNullableNull { get; set; }
        public CustomICollection<float?> SingleICollectionTNullableNull { get; set; }
        public CustomICollection<double?> DoubleICollectionTNullableNull { get; set; }
        public CustomICollection<decimal?> DecimalICollectionTNullableNull { get; set; }
        public CustomICollection<char?> CharICollectionTNullableNull { get; set; }
        public CustomICollection<DateTime?> DateTimeICollectionTNullableNull { get; set; }
        public CustomICollection<DateTimeOffset?> DateTimeOffsetICollectionTNullableNull { get; set; }
        public CustomICollection<TimeSpan?> TimeSpanICollectionTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomICollection<DateOnly?> DateOnlyICollectionTNullableNull { get; set; }
        public CustomICollection<TimeOnly?> TimeOnlyICollectionTNullableNull { get; set; }
#endif
        public CustomICollection<Guid?> GuidICollectionTNullableNull { get; set; }

        public CustomICollection<string> StringICollectionT { get; set; }
        public CustomICollection<string> StringICollectionTEmpty { get; set; }
        public CustomICollection<string> StringICollectionTNull { get; set; }

        public CustomICollection<EnumModel> EnumICollectionT { get; set; }
        public CustomICollection<EnumModel> EnumICollectionTEmpty { get; set; }
        public CustomICollection<EnumModel> EnumICollectionTNull { get; set; }

        public CustomICollection<EnumModel?> EnumICollectionTNullable { get; set; }
        public CustomICollection<EnumModel?> EnumICollectionTNullableEmpty { get; set; }
        public CustomICollection<EnumModel?> EnumICollectionTNullableNull { get; set; }

        public CustomICollection<SimpleModel> ClassICollection { get; set; }
        public CustomICollection<SimpleModel> ClassICollectionEmpty { get; set; }
        public CustomICollection<SimpleModel> ClassICollectionNull { get; set; }

        public static TypesICollectionTOfTModel Create()
        {
            var model = new TypesICollectionTOfTModel()
            {
                BooleanICollectionT = new CustomICollection<bool>() { true, false, true },
                ByteICollectionT = new CustomICollection<byte>() { 1, 2, 3 },
                SByteICollectionT = new CustomICollection<sbyte>() { 4, 5, 6 },
                Int16ICollectionT = new CustomICollection<short>() { 7, 8, 9 },
                UInt16ICollectionT = new CustomICollection<ushort>() { 10, 11, 12 },
                Int32ICollectionT = new CustomICollection<int>() { 13, 14, 15 },
                UInt32ICollectionT = new CustomICollection<uint>() { 16, 17, 18 },
                Int64ICollectionT = new CustomICollection<long>() { 19, 20, 21 },
                UInt64ICollectionT = new CustomICollection<ulong>() { 22, 23, 24 },
                SingleICollectionT = new CustomICollection<float>() { 25, 26, 27 },
                DoubleICollectionT = new CustomICollection<double>() { 28, 29, 30 },
                DecimalICollectionT = new CustomICollection<decimal>() { 31, 32, 33 },
                CharICollectionT = new CustomICollection<char>() { 'A', 'B', 'C' },
                DateTimeICollectionT = new CustomICollection<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetICollectionT = new CustomICollection<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanICollectionT = new CustomICollection<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyICollectionT = new CustomICollection<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyICollectionT = new CustomICollection<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidICollectionT = new CustomICollection<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanICollectionTEmpty = new CustomICollection<bool>(0),
                ByteICollectionTEmpty = new CustomICollection<byte>(0),
                SByteICollectionTEmpty = new CustomICollection<sbyte>(0),
                Int16ICollectionTEmpty = new CustomICollection<short>(0),
                UInt16ICollectionTEmpty = new CustomICollection<ushort>(0),
                Int32ICollectionTEmpty = new CustomICollection<int>(0),
                UInt32ICollectionTEmpty = new CustomICollection<uint>(0),
                Int64ICollectionTEmpty = new CustomICollection<long>(0),
                UInt64ICollectionTEmpty = new CustomICollection<ulong>(0),
                SingleICollectionTEmpty = new CustomICollection<float>(0),
                DoubleICollectionTEmpty = new CustomICollection<double>(0),
                DecimalICollectionTEmpty = new CustomICollection<decimal>(0),
                CharICollectionTEmpty = new CustomICollection<char>(0),
                DateTimeICollectionTEmpty = new CustomICollection<DateTime>(0),
                DateTimeOffsetICollectionTEmpty = new CustomICollection<DateTimeOffset>(0),
                TimeSpanICollectionTEmpty = new CustomICollection<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyICollectionTEmpty = new CustomICollection<DateOnly>(0),
                TimeOnlyICollectionTEmpty = new CustomICollection<TimeOnly>(0),
#endif
                GuidICollectionTEmpty = new CustomICollection<Guid>(0),

                BooleanICollectionTNull = null,
                ByteICollectionTNull = null,
                SByteICollectionTNull = null,
                Int16ICollectionTNull = null,
                UInt16ICollectionTNull = null,
                Int32ICollectionTNull = null,
                UInt32ICollectionTNull = null,
                Int64ICollectionTNull = null,
                UInt64ICollectionTNull = null,
                SingleICollectionTNull = null,
                DoubleICollectionTNull = null,
                DecimalICollectionTNull = null,
                CharICollectionTNull = null,
                DateTimeICollectionTNull = null,
                DateTimeOffsetICollectionTNull = null,
                TimeSpanICollectionTNull = null,
#if NET6_0_OR_GREATER
                DateOnlyICollectionTNull = null,
                TimeOnlyICollectionTNull = null,
#endif
                GuidICollectionTNull = null,

                BooleanICollectionTNullable = new CustomICollection<bool?>() { true, null, true },
                ByteICollectionTNullable = new CustomICollection<byte?>() { 1, null, 3 },
                SByteICollectionTNullable = new CustomICollection<sbyte?>() { 4, null, 6 },
                Int16ICollectionTNullable = new CustomICollection<short?>() { 7, null, 9 },
                UInt16ICollectionTNullable = new CustomICollection<ushort?>() { 10, null, 12 },
                Int32ICollectionTNullable = new CustomICollection<int?>() { 13, null, 15 },
                UInt32ICollectionTNullable = new CustomICollection<uint?>() { 16, null, 18 },
                Int64ICollectionTNullable = new CustomICollection<long?>() { 19, null, 21 },
                UInt64ICollectionTNullable = new CustomICollection<ulong?>() { 22, null, 24 },
                SingleICollectionTNullable = new CustomICollection<float?>() { 25, null, 27 },
                DoubleICollectionTNullable = new CustomICollection<double?>() { 28, null, 30 },
                DecimalICollectionTNullable = new CustomICollection<decimal?>() { 31, null, 33 },
                CharICollectionTNullable = new CustomICollection<char?>() { 'A', null, 'C' },
                DateTimeICollectionTNullable = new CustomICollection<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetICollectionTNullable = new CustomICollection<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanICollectionTNullable = new CustomICollection<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyICollectionTNullable = new CustomICollection<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyICollectionTNullable = new CustomICollection<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidICollectionTNullable = new CustomICollection<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanICollectionTNullableEmpty = new CustomICollection<bool?>(0),
                ByteICollectionTNullableEmpty = new CustomICollection<byte?>(0),
                SByteICollectionTNullableEmpty = new CustomICollection<sbyte?>(0),
                Int16ICollectionTNullableEmpty = new CustomICollection<short?>(0),
                UInt16ICollectionTNullableEmpty = new CustomICollection<ushort?>(0),
                Int32ICollectionTNullableEmpty = new CustomICollection<int?>(0),
                UInt32ICollectionTNullableEmpty = new CustomICollection<uint?>(0),
                Int64ICollectionTNullableEmpty = new CustomICollection<long?>(0),
                UInt64ICollectionTNullableEmpty = new CustomICollection<ulong?>(0),
                SingleICollectionTNullableEmpty = new CustomICollection<float?>(0),
                DoubleICollectionTNullableEmpty = new CustomICollection<double?>(0),
                DecimalICollectionTNullableEmpty = new CustomICollection<decimal?>(0),
                CharICollectionTNullableEmpty = new CustomICollection<char?>(0),
                DateTimeICollectionTNullableEmpty = new CustomICollection<DateTime?>(0),
                DateTimeOffsetICollectionTNullableEmpty = new CustomICollection<DateTimeOffset?>(0),
                TimeSpanICollectionTNullableEmpty = new CustomICollection<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyICollectionTNullableEmpty = new CustomICollection<DateOnly?>(0),
                TimeOnlyICollectionTNullableEmpty = new CustomICollection<TimeOnly?>(0),
#endif
                GuidICollectionTNullableEmpty = new CustomICollection<Guid?>(0),

                BooleanICollectionTNullableNull = null,
                ByteICollectionTNullableNull = null,
                SByteICollectionTNullableNull = null,
                Int16ICollectionTNullableNull = null,
                UInt16ICollectionTNullableNull = null,
                Int32ICollectionTNullableNull = null,
                UInt32ICollectionTNullableNull = null,
                Int64ICollectionTNullableNull = null,
                UInt64ICollectionTNullableNull = null,
                SingleICollectionTNullableNull = null,
                DoubleICollectionTNullableNull = null,
                DecimalICollectionTNullableNull = null,
                CharICollectionTNullableNull = null,
                DateTimeICollectionTNullableNull = null,
                DateTimeOffsetICollectionTNullableNull = null,
                TimeSpanICollectionTNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyICollectionTNullableNull = null,
                TimeOnlyICollectionTNullableNull = null,
#endif
                GuidICollectionTNullableNull = null,

                StringICollectionT = new CustomICollection<string>() { "Hello", "World", null, "", "People" },
                StringICollectionTEmpty = new CustomICollection<string>(0),
                StringICollectionTNull = null,

                EnumICollectionT = new CustomICollection<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumICollectionTEmpty = new CustomICollection<EnumModel>(0),
                EnumICollectionTNull = null,

                EnumICollectionTNullable = new CustomICollection<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumICollectionTNullableEmpty = new CustomICollection<EnumModel?>(0),
                EnumICollectionTNullableNull = null,

                ClassICollection = new CustomICollection<SimpleModel>() { new SimpleModel { Value1 = 10, Value2 = "S-10" }, new SimpleModel { Value1 = 11, Value2 = "S-11" } },
                ClassICollectionEmpty = new CustomICollection<SimpleModel>(0),
                ClassICollectionNull = null,
            };
            return model;
        }
    }
}