// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.SourceGeneration.GenerateTypeDetail]
    public class TypesIListTOfTModel
    {
        public sealed class CustomIList<T> : IList<T>
        {
            private readonly List<T> list;
            public CustomIList()
            {
                list = new();
            }
            public CustomIList(int capacity)
            {
                list = new(capacity);
            }

            public T this[int index] { get => list[index]; set => list[index] = value; }
            public int Count => list.Count;
            public bool IsReadOnly => false;
            public void Add(T item) => list.Add(item);
            public void Clear() => list.Clear();
            public bool Contains(T item) => list.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
            public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
            public int IndexOf(T item) => list.IndexOf(item);
            public void Insert(int index, T item) => list.Insert(index, item);
            public bool Remove(T item) => list.Remove(item);
            public void RemoveAt(int index) => list.RemoveAt(index);
            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
        }

        public CustomIList<bool> BooleanIListT { get; set; }
        public CustomIList<byte> ByteIListT { get; set; }
        public CustomIList<sbyte> SByteIListT { get; set; }
        public CustomIList<short> Int16IListT { get; set; }
        public CustomIList<ushort> UInt16IListT { get; set; }
        public CustomIList<int> Int32IListT { get; set; }
        public CustomIList<uint> UInt32IListT { get; set; }
        public CustomIList<long> Int64IListT { get; set; }
        public CustomIList<ulong> UInt64IListT { get; set; }
        public CustomIList<float> SingleIListT { get; set; }
        public CustomIList<double> DoubleIListT { get; set; }
        public CustomIList<decimal> DecimalIListT { get; set; }
        public CustomIList<char> CharIListT { get; set; }
        public CustomIList<DateTime> DateTimeIListT { get; set; }
        public CustomIList<DateTimeOffset> DateTimeOffsetIListT { get; set; }
        public CustomIList<TimeSpan> TimeSpanIListT { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList<DateOnly> DateOnlyIListT { get; set; }
        public CustomIList<TimeOnly> TimeOnlyIListT { get; set; }
#endif
        public CustomIList<Guid> GuidIListT { get; set; }

        public CustomIList<bool> BooleanIListTEmpty { get; set; }
        public CustomIList<byte> ByteIListTEmpty { get; set; }
        public CustomIList<sbyte> SByteIListTEmpty { get; set; }
        public CustomIList<short> Int16IListTEmpty { get; set; }
        public CustomIList<ushort> UInt16IListTEmpty { get; set; }
        public CustomIList<int> Int32IListTEmpty { get; set; }
        public CustomIList<uint> UInt32IListTEmpty { get; set; }
        public CustomIList<long> Int64IListTEmpty { get; set; }
        public CustomIList<ulong> UInt64IListTEmpty { get; set; }
        public CustomIList<float> SingleIListTEmpty { get; set; }
        public CustomIList<double> DoubleIListTEmpty { get; set; }
        public CustomIList<decimal> DecimalIListTEmpty { get; set; }
        public CustomIList<char> CharIListTEmpty { get; set; }
        public CustomIList<DateTime> DateTimeIListTEmpty { get; set; }
        public CustomIList<DateTimeOffset> DateTimeOffsetIListTEmpty { get; set; }
        public CustomIList<TimeSpan> TimeSpanIListTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList<DateOnly> DateOnlyIListTEmpty { get; set; }
        public CustomIList<TimeOnly> TimeOnlyIListTEmpty { get; set; }
#endif
        public CustomIList<Guid> GuidIListTEmpty { get; set; }

        public CustomIList<bool> BooleanIListTNull { get; set; }
        public CustomIList<byte> ByteIListTNull { get; set; }
        public CustomIList<sbyte> SByteIListTNull { get; set; }
        public CustomIList<short> Int16IListTNull { get; set; }
        public CustomIList<ushort> UInt16IListTNull { get; set; }
        public CustomIList<int> Int32IListTNull { get; set; }
        public CustomIList<uint> UInt32IListTNull { get; set; }
        public CustomIList<long> Int64IListTNull { get; set; }
        public CustomIList<ulong> UInt64IListTNull { get; set; }
        public CustomIList<float> SingleIListTNull { get; set; }
        public CustomIList<double> DoubleIListTNull { get; set; }
        public CustomIList<decimal> DecimalIListTNull { get; set; }
        public CustomIList<char> CharIListTNull { get; set; }
        public CustomIList<DateTime> DateTimeIListTNull { get; set; }
        public CustomIList<DateTimeOffset> DateTimeOffsetIListTNull { get; set; }
        public CustomIList<TimeSpan> TimeSpanIListTNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList<DateOnly> DateOnlyIListTNull { get; set; }
        public CustomIList<TimeOnly> TimeOnlyIListTNull { get; set; }
#endif
        public CustomIList<Guid> GuidIListTNull { get; set; }

        public CustomIList<bool?> BooleanIListTNullable { get; set; }
        public CustomIList<byte?> ByteIListTNullable { get; set; }
        public CustomIList<sbyte?> SByteIListTNullable { get; set; }
        public CustomIList<short?> Int16IListTNullable { get; set; }
        public CustomIList<ushort?> UInt16IListTNullable { get; set; }
        public CustomIList<int?> Int32IListTNullable { get; set; }
        public CustomIList<uint?> UInt32IListTNullable { get; set; }
        public CustomIList<long?> Int64IListTNullable { get; set; }
        public CustomIList<ulong?> UInt64IListTNullable { get; set; }
        public CustomIList<float?> SingleIListTNullable { get; set; }
        public CustomIList<double?> DoubleIListTNullable { get; set; }
        public CustomIList<decimal?> DecimalIListTNullable { get; set; }
        public CustomIList<char?> CharIListTNullable { get; set; }
        public CustomIList<DateTime?> DateTimeIListTNullable { get; set; }
        public CustomIList<DateTimeOffset?> DateTimeOffsetIListTNullable { get; set; }
        public CustomIList<TimeSpan?> TimeSpanIListTNullable { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList<DateOnly?> DateOnlyIListTNullable { get; set; }
        public CustomIList<TimeOnly?> TimeOnlyIListTNullable { get; set; }
#endif
        public CustomIList<Guid?> GuidIListTNullable { get; set; }

        public CustomIList<bool?> BooleanIListTNullableEmpty { get; set; }
        public CustomIList<byte?> ByteIListTNullableEmpty { get; set; }
        public CustomIList<sbyte?> SByteIListTNullableEmpty { get; set; }
        public CustomIList<short?> Int16IListTNullableEmpty { get; set; }
        public CustomIList<ushort?> UInt16IListTNullableEmpty { get; set; }
        public CustomIList<int?> Int32IListTNullableEmpty { get; set; }
        public CustomIList<uint?> UInt32IListTNullableEmpty { get; set; }
        public CustomIList<long?> Int64IListTNullableEmpty { get; set; }
        public CustomIList<ulong?> UInt64IListTNullableEmpty { get; set; }
        public CustomIList<float?> SingleIListTNullableEmpty { get; set; }
        public CustomIList<double?> DoubleIListTNullableEmpty { get; set; }
        public CustomIList<decimal?> DecimalIListTNullableEmpty { get; set; }
        public CustomIList<char?> CharIListTNullableEmpty { get; set; }
        public CustomIList<DateTime?> DateTimeIListTNullableEmpty { get; set; }
        public CustomIList<DateTimeOffset?> DateTimeOffsetIListTNullableEmpty { get; set; }
        public CustomIList<TimeSpan?> TimeSpanIListTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList<DateOnly?> DateOnlyIListTNullableEmpty { get; set; }
        public CustomIList<TimeOnly?> TimeOnlyIListTNullableEmpty { get; set; }
#endif
        public CustomIList<Guid?> GuidIListTNullableEmpty { get; set; }

        public CustomIList<bool?> BooleanIListTNullableNull { get; set; }
        public CustomIList<byte?> ByteIListTNullableNull { get; set; }
        public CustomIList<sbyte?> SByteIListTNullableNull { get; set; }
        public CustomIList<short?> Int16IListTNullableNull { get; set; }
        public CustomIList<ushort?> UInt16IListTNullableNull { get; set; }
        public CustomIList<int?> Int32IListTNullableNull { get; set; }
        public CustomIList<uint?> UInt32IListTNullableNull { get; set; }
        public CustomIList<long?> Int64IListTNullableNull { get; set; }
        public CustomIList<ulong?> UInt64IListTNullableNull { get; set; }
        public CustomIList<float?> SingleIListTNullableNull { get; set; }
        public CustomIList<double?> DoubleIListTNullableNull { get; set; }
        public CustomIList<decimal?> DecimalIListTNullableNull { get; set; }
        public CustomIList<char?> CharIListTNullableNull { get; set; }
        public CustomIList<DateTime?> DateTimeIListTNullableNull { get; set; }
        public CustomIList<DateTimeOffset?> DateTimeOffsetIListTNullableNull { get; set; }
        public CustomIList<TimeSpan?> TimeSpanIListTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList<DateOnly?> DateOnlyIListTNullableNull { get; set; }
        public CustomIList<TimeOnly?> TimeOnlyIListTNullableNull { get; set; }
#endif
        public CustomIList<Guid?> GuidIListTNullableNull { get; set; }

        public CustomIList<string> StringIListT { get; set; }
        public CustomIList<string> StringIListTEmpty { get; set; }
        public CustomIList<string> StringIListTNull { get; set; }

        public CustomIList<EnumModel> EnumIListT { get; set; }
        public CustomIList<EnumModel> EnumIListTEmpty { get; set; }
        public CustomIList<EnumModel> EnumIListTNull { get; set; }

        public CustomIList<EnumModel?> EnumIListTNullable { get; set; }
        public CustomIList<EnumModel?> EnumIListTNullableEmpty { get; set; }
        public CustomIList<EnumModel?> EnumIListTNullableNull { get; set; }

        public CustomIList<SimpleModel> ClassIList { get; set; }
        public CustomIList<SimpleModel> ClassIListEmpty { get; set; }
        public CustomIList<SimpleModel> ClassIListNull { get; set; }

        public static TypesIListTOfTModel Create()
        {
            var model = new TypesIListTOfTModel()
            {
                BooleanIListT = new CustomIList<bool>() { true, false, true },
                ByteIListT = new CustomIList<byte>() { 1, 2, 3 },
                SByteIListT = new CustomIList<sbyte>() { 4, 5, 6 },
                Int16IListT = new CustomIList<short>() { 7, 8, 9 },
                UInt16IListT = new CustomIList<ushort>() { 10, 11, 12 },
                Int32IListT = new CustomIList<int>() { 13, 14, 15 },
                UInt32IListT = new CustomIList<uint>() { 16, 17, 18 },
                Int64IListT = new CustomIList<long>() { 19, 20, 21 },
                UInt64IListT = new CustomIList<ulong>() { 22, 23, 24 },
                SingleIListT = new CustomIList<float>() { 25, 26, 27 },
                DoubleIListT = new CustomIList<double>() { 28, 29, 30 },
                DecimalIListT = new CustomIList<decimal>() { 31, 32, 33 },
                CharIListT = new CustomIList<char>() { 'A', 'B', 'C' },
                DateTimeIListT = new CustomIList<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIListT = new CustomIList<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIListT = new CustomIList<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIListT = new CustomIList<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIListT = new CustomIList<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIListT = new CustomIList<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanIListTEmpty = new CustomIList<bool>(0),
                ByteIListTEmpty = new CustomIList<byte>(0),
                SByteIListTEmpty = new CustomIList<sbyte>(0),
                Int16IListTEmpty = new CustomIList<short>(0),
                UInt16IListTEmpty = new CustomIList<ushort>(0),
                Int32IListTEmpty = new CustomIList<int>(0),
                UInt32IListTEmpty = new CustomIList<uint>(0),
                Int64IListTEmpty = new CustomIList<long>(0),
                UInt64IListTEmpty = new CustomIList<ulong>(0),
                SingleIListTEmpty = new CustomIList<float>(0),
                DoubleIListTEmpty = new CustomIList<double>(0),
                DecimalIListTEmpty = new CustomIList<decimal>(0),
                CharIListTEmpty = new CustomIList<char>(0),
                DateTimeIListTEmpty = new CustomIList<DateTime>(0),
                DateTimeOffsetIListTEmpty = new CustomIList<DateTimeOffset>(0),
                TimeSpanIListTEmpty = new CustomIList<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyIListTEmpty = new CustomIList<DateOnly>(0),
                TimeOnlyIListTEmpty = new CustomIList<TimeOnly>(0),
#endif
                GuidIListTEmpty = new CustomIList<Guid>(0),

                BooleanIListTNull = null,
                ByteIListTNull = null,
                SByteIListTNull = null,
                Int16IListTNull = null,
                UInt16IListTNull = null,
                Int32IListTNull = null,
                UInt32IListTNull = null,
                Int64IListTNull = null,
                UInt64IListTNull = null,
                SingleIListTNull = null,
                DoubleIListTNull = null,
                DecimalIListTNull = null,
                CharIListTNull = null,
                DateTimeIListTNull = null,
                DateTimeOffsetIListTNull = null,
                TimeSpanIListTNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIListTNull = null,
                TimeOnlyIListTNull = null,
#endif
                GuidIListTNull = null,

                BooleanIListTNullable = new CustomIList<bool?>() { true, null, true },
                ByteIListTNullable = new CustomIList<byte?>() { 1, null, 3 },
                SByteIListTNullable = new CustomIList<sbyte?>() { 4, null, 6 },
                Int16IListTNullable = new CustomIList<short?>() { 7, null, 9 },
                UInt16IListTNullable = new CustomIList<ushort?>() { 10, null, 12 },
                Int32IListTNullable = new CustomIList<int?>() { 13, null, 15 },
                UInt32IListTNullable = new CustomIList<uint?>() { 16, null, 18 },
                Int64IListTNullable = new CustomIList<long?>() { 19, null, 21 },
                UInt64IListTNullable = new CustomIList<ulong?>() { 22, null, 24 },
                SingleIListTNullable = new CustomIList<float?>() { 25, null, 27 },
                DoubleIListTNullable = new CustomIList<double?>() { 28, null, 30 },
                DecimalIListTNullable = new CustomIList<decimal?>() { 31, null, 33 },
                CharIListTNullable = new CustomIList<char?>() { 'A', null, 'C' },
                DateTimeIListTNullable = new CustomIList<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIListTNullable = new CustomIList<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIListTNullable = new CustomIList<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIListTNullable = new CustomIList<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIListTNullable = new CustomIList<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIListTNullable = new CustomIList<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanIListTNullableEmpty = new CustomIList<bool?>(0),
                ByteIListTNullableEmpty = new CustomIList<byte?>(0),
                SByteIListTNullableEmpty = new CustomIList<sbyte?>(0),
                Int16IListTNullableEmpty = new CustomIList<short?>(0),
                UInt16IListTNullableEmpty = new CustomIList<ushort?>(0),
                Int32IListTNullableEmpty = new CustomIList<int?>(0),
                UInt32IListTNullableEmpty = new CustomIList<uint?>(0),
                Int64IListTNullableEmpty = new CustomIList<long?>(0),
                UInt64IListTNullableEmpty = new CustomIList<ulong?>(0),
                SingleIListTNullableEmpty = new CustomIList<float?>(0),
                DoubleIListTNullableEmpty = new CustomIList<double?>(0),
                DecimalIListTNullableEmpty = new CustomIList<decimal?>(0),
                CharIListTNullableEmpty = new CustomIList<char?>(0),
                DateTimeIListTNullableEmpty = new CustomIList<DateTime?>(0),
                DateTimeOffsetIListTNullableEmpty = new CustomIList<DateTimeOffset?>(0),
                TimeSpanIListTNullableEmpty = new CustomIList<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyIListTNullableEmpty = new CustomIList<DateOnly?>(0),
                TimeOnlyIListTNullableEmpty = new CustomIList<TimeOnly?>(0),
#endif
                GuidIListTNullableEmpty = new CustomIList<Guid?>(0),

                BooleanIListTNullableNull = null,
                ByteIListTNullableNull = null,
                SByteIListTNullableNull = null,
                Int16IListTNullableNull = null,
                UInt16IListTNullableNull = null,
                Int32IListTNullableNull = null,
                UInt32IListTNullableNull = null,
                Int64IListTNullableNull = null,
                UInt64IListTNullableNull = null,
                SingleIListTNullableNull = null,
                DoubleIListTNullableNull = null,
                DecimalIListTNullableNull = null,
                CharIListTNullableNull = null,
                DateTimeIListTNullableNull = null,
                DateTimeOffsetIListTNullableNull = null,
                TimeSpanIListTNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIListTNullableNull = null,
                TimeOnlyIListTNullableNull = null,
#endif
                GuidIListTNullableNull = null,

                StringIListT = new CustomIList<string>() { "Hello", "World", null, "", "People" },
                StringIListTEmpty = new CustomIList<string>(0),
                StringIListTNull = null,

                EnumIListT = new CustomIList<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumIListTEmpty = new CustomIList<EnumModel>(0),
                EnumIListTNull = null,

                EnumIListTNullable = new CustomIList<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumIListTNullableEmpty = new CustomIList<EnumModel?>(0),
                EnumIListTNullableNull = null,

                ClassIList = new CustomIList<SimpleModel>() { new SimpleModel { Value1 = 10, Value2 = "S-10" }, new SimpleModel { Value1 = 11, Value2 = "S-11" } },
                ClassIListEmpty = new CustomIList<SimpleModel>(0),
                ClassIListNull = null,
            };
            return model;
        }
    }
}