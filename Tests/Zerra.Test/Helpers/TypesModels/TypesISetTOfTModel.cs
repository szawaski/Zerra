// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.Reflection.GenerateTypeDetail]
    public class TypesISetTOfTModel
    {
        public sealed class CustomISet<T> : ISet<T>
        {
            private readonly HashSet<T> set;
            public CustomISet()
            {
                set = new();
            }
            public CustomISet(int capacity)
            {
                set = new(capacity);
            }

            public int Count => set.Count;
            public bool IsReadOnly => false;
            public bool Add(T item) => set.Add(item);
            public void Clear() => set.Clear();
            public bool Contains(T item) => set.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => set.CopyTo(array, arrayIndex);
            public void ExceptWith(IEnumerable<T> other) => set.ExceptWith(other);
            public IEnumerator<T> GetEnumerator() => set.GetEnumerator();
            public void IntersectWith(IEnumerable<T> other) => set.IntersectWith(other);
            public bool IsProperSubsetOf(IEnumerable<T> other) => set.IsProperSubsetOf(other);
            public bool IsProperSupersetOf(IEnumerable<T> other) => set.IsProperSupersetOf(other);
            public bool IsSubsetOf(IEnumerable<T> other) => set.IsSubsetOf(other);
            public bool IsSupersetOf(IEnumerable<T> other) => set.IsSupersetOf(other);
            public bool Overlaps(IEnumerable<T> other) => set.Overlaps(other);
            public bool Remove(T item) => set.Remove(item);
            public bool SetEquals(IEnumerable<T> other) => set.SetEquals(other);
            public void SymmetricExceptWith(IEnumerable<T> other) => set.SymmetricExceptWith(other);
            public void UnionWith(IEnumerable<T> other) => set.UnionWith(other);
            void ICollection<T>.Add(T item) => set.Add(item);
            IEnumerator IEnumerable.GetEnumerator() => set.GetEnumerator();
        }

        public CustomISet<bool> BooleanISet { get; set; }
        public CustomISet<byte> ByteISet { get; set; }
        public CustomISet<sbyte> SByteISet { get; set; }
        public CustomISet<short> Int16ISet { get; set; }
        public CustomISet<ushort> UInt16ISet { get; set; }
        public CustomISet<int> Int32ISet { get; set; }
        public CustomISet<uint> UInt32ISet { get; set; }
        public CustomISet<long> Int64ISet { get; set; }
        public CustomISet<ulong> UInt64ISet { get; set; }
        public CustomISet<float> SingleISet { get; set; }
        public CustomISet<double> DoubleISet { get; set; }
        public CustomISet<decimal> DecimalISet { get; set; }
        public CustomISet<char> CharISet { get; set; }
        public CustomISet<DateTime> DateTimeISet { get; set; }
        public CustomISet<DateTimeOffset> DateTimeOffsetISet { get; set; }
        public CustomISet<TimeSpan> TimeSpanISet { get; set; }
#if NET6_0_OR_GREATER
        public CustomISet<DateOnly> DateOnlyISet { get; set; }
        public CustomISet<TimeOnly> TimeOnlyISet { get; set; }
#endif
        public CustomISet<Guid> GuidISet { get; set; }

        public CustomISet<bool> BooleanISetEmpty { get; set; }
        public CustomISet<byte> ByteISetEmpty { get; set; }
        public CustomISet<sbyte> SByteISetEmpty { get; set; }
        public CustomISet<short> Int16ISetEmpty { get; set; }
        public CustomISet<ushort> UInt16ISetEmpty { get; set; }
        public CustomISet<int> Int32ISetEmpty { get; set; }
        public CustomISet<uint> UInt32ISetEmpty { get; set; }
        public CustomISet<long> Int64ISetEmpty { get; set; }
        public CustomISet<ulong> UInt64ISetEmpty { get; set; }
        public CustomISet<float> SingleISetEmpty { get; set; }
        public CustomISet<double> DoubleISetEmpty { get; set; }
        public CustomISet<decimal> DecimalISetEmpty { get; set; }
        public CustomISet<char> CharISetEmpty { get; set; }
        public CustomISet<DateTime> DateTimeISetEmpty { get; set; }
        public CustomISet<DateTimeOffset> DateTimeOffsetISetEmpty { get; set; }
        public CustomISet<TimeSpan> TimeSpanISetEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomISet<DateOnly> DateOnlyISetEmpty { get; set; }
        public CustomISet<TimeOnly> TimeOnlyISetEmpty { get; set; }
#endif
        public CustomISet<Guid> GuidISetEmpty { get; set; }

        public CustomISet<bool> BooleanISetNull { get; set; }
        public CustomISet<byte> ByteISetNull { get; set; }
        public CustomISet<sbyte> SByteISetNull { get; set; }
        public CustomISet<short> Int16ISetNull { get; set; }
        public CustomISet<ushort> UInt16ISetNull { get; set; }
        public CustomISet<int> Int32ISetNull { get; set; }
        public CustomISet<uint> UInt32ISetNull { get; set; }
        public CustomISet<long> Int64ISetNull { get; set; }
        public CustomISet<ulong> UInt64ISetNull { get; set; }
        public CustomISet<float> SingleISetNull { get; set; }
        public CustomISet<double> DoubleISetNull { get; set; }
        public CustomISet<decimal> DecimalISetNull { get; set; }
        public CustomISet<char> CharISetNull { get; set; }
        public CustomISet<DateTime> DateTimeISetNull { get; set; }
        public CustomISet<DateTimeOffset> DateTimeOffsetISetNull { get; set; }
        public CustomISet<TimeSpan> TimeSpanISetNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomISet<DateOnly> DateOnlyISetNull { get; set; }
        public CustomISet<TimeOnly> TimeOnlyISetNull { get; set; }
#endif
        public CustomISet<Guid> GuidISetNull { get; set; }

        public CustomISet<bool?> BooleanISetNullable { get; set; }
        public CustomISet<byte?> ByteISetNullable { get; set; }
        public CustomISet<sbyte?> SByteISetNullable { get; set; }
        public CustomISet<short?> Int16ISetNullable { get; set; }
        public CustomISet<ushort?> UInt16ISetNullable { get; set; }
        public CustomISet<int?> Int32ISetNullable { get; set; }
        public CustomISet<uint?> UInt32ISetNullable { get; set; }
        public CustomISet<long?> Int64ISetNullable { get; set; }
        public CustomISet<ulong?> UInt64ISetNullable { get; set; }
        public CustomISet<float?> SingleISetNullable { get; set; }
        public CustomISet<double?> DoubleISetNullable { get; set; }
        public CustomISet<decimal?> DecimalISetNullable { get; set; }
        public CustomISet<char?> CharISetNullable { get; set; }
        public CustomISet<DateTime?> DateTimeISetNullable { get; set; }
        public CustomISet<DateTimeOffset?> DateTimeOffsetISetNullable { get; set; }
        public CustomISet<TimeSpan?> TimeSpanISetNullable { get; set; }
#if NET6_0_OR_GREATER
        public CustomISet<DateOnly?> DateOnlyISetNullable { get; set; }
        public CustomISet<TimeOnly?> TimeOnlyISetNullable { get; set; }
#endif
        public CustomISet<Guid?> GuidISetNullable { get; set; }

        public CustomISet<bool?> BooleanISetNullableEmpty { get; set; }
        public CustomISet<byte?> ByteISetNullableEmpty { get; set; }
        public CustomISet<sbyte?> SByteISetNullableEmpty { get; set; }
        public CustomISet<short?> Int16ISetNullableEmpty { get; set; }
        public CustomISet<ushort?> UInt16ISetNullableEmpty { get; set; }
        public CustomISet<int?> Int32ISetNullableEmpty { get; set; }
        public CustomISet<uint?> UInt32ISetNullableEmpty { get; set; }
        public CustomISet<long?> Int64ISetNullableEmpty { get; set; }
        public CustomISet<ulong?> UInt64ISetNullableEmpty { get; set; }
        public CustomISet<float?> SingleISetNullableEmpty { get; set; }
        public CustomISet<double?> DoubleISetNullableEmpty { get; set; }
        public CustomISet<decimal?> DecimalISetNullableEmpty { get; set; }
        public CustomISet<char?> CharISetNullableEmpty { get; set; }
        public CustomISet<DateTime?> DateTimeISetNullableEmpty { get; set; }
        public CustomISet<DateTimeOffset?> DateTimeOffsetISetNullableEmpty { get; set; }
        public CustomISet<TimeSpan?> TimeSpanISetNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomISet<DateOnly?> DateOnlyISetNullableEmpty { get; set; }
        public CustomISet<TimeOnly?> TimeOnlyISetNullableEmpty { get; set; }
#endif
        public CustomISet<Guid?> GuidISetNullableEmpty { get; set; }

        public CustomISet<bool?> BooleanISetNullableNull { get; set; }
        public CustomISet<byte?> ByteISetNullableNull { get; set; }
        public CustomISet<sbyte?> SByteISetNullableNull { get; set; }
        public CustomISet<short?> Int16ISetNullableNull { get; set; }
        public CustomISet<ushort?> UInt16ISetNullableNull { get; set; }
        public CustomISet<int?> Int32ISetNullableNull { get; set; }
        public CustomISet<uint?> UInt32ISetNullableNull { get; set; }
        public CustomISet<long?> Int64ISetNullableNull { get; set; }
        public CustomISet<ulong?> UInt64ISetNullableNull { get; set; }
        public CustomISet<float?> SingleISetNullableNull { get; set; }
        public CustomISet<double?> DoubleISetNullableNull { get; set; }
        public CustomISet<decimal?> DecimalISetNullableNull { get; set; }
        public CustomISet<char?> CharISetNullableNull { get; set; }
        public CustomISet<DateTime?> DateTimeISetNullableNull { get; set; }
        public CustomISet<DateTimeOffset?> DateTimeOffsetISetNullableNull { get; set; }
        public CustomISet<TimeSpan?> TimeSpanISetNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomISet<DateOnly?> DateOnlyISetNullableNull { get; set; }
        public CustomISet<TimeOnly?> TimeOnlyISetNullableNull { get; set; }
#endif
        public CustomISet<Guid?> GuidISetNullableNull { get; set; }

        public CustomISet<string> StringISet { get; set; }
        public CustomISet<string> StringISetEmpty { get; set; }
        public CustomISet<string> StringISetNull { get; set; }

        public CustomISet<EnumModel> EnumISet { get; set; }
        public CustomISet<EnumModel> EnumISetEmpty { get; set; }
        public CustomISet<EnumModel> EnumISetNull { get; set; }

        public CustomISet<EnumModel?> EnumISetNullable { get; set; }
        public CustomISet<EnumModel?> EnumISetNullableEmpty { get; set; }
        public CustomISet<EnumModel?> EnumISetNullableNull { get; set; }

        public CustomISet<SimpleModel> ClassISet { get; set; }
        public CustomISet<SimpleModel> ClassISetEmpty { get; set; }
        public CustomISet<SimpleModel> ClassISetNull { get; set; }

        public static TypesISetTOfTModel Create()
        {
            var model = new TypesISetTOfTModel()
            {
                BooleanISet = new CustomISet<bool>() { true, false, true },
                ByteISet = new CustomISet<byte>() { 1, 2, 3 },
                SByteISet = new CustomISet<sbyte>() { 4, 5, 6 },
                Int16ISet = new CustomISet<short>() { 7, 8, 9 },
                UInt16ISet = new CustomISet<ushort>() { 10, 11, 12 },
                Int32ISet = new CustomISet<int>() { 13, 14, 15 },
                UInt32ISet = new CustomISet<uint>() { 16, 17, 18 },
                Int64ISet = new CustomISet<long>() { 19, 20, 21 },
                UInt64ISet = new CustomISet<ulong>() { 22, 23, 24 },
                SingleISet = new CustomISet<float>() { 25, 26, 27 },
                DoubleISet = new CustomISet<double>() { 28, 29, 30 },
                DecimalISet = new CustomISet<decimal>() { 31, 32, 33 },
                CharISet = new CustomISet<char>() { 'A', 'B', 'C' },
                DateTimeISet = new CustomISet<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetISet = new CustomISet<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanISet = new CustomISet<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyISet = new CustomISet<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyISet = new CustomISet<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidISet = new CustomISet<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanISetEmpty = new CustomISet<bool>(0),
                ByteISetEmpty = new CustomISet<byte>(0),
                SByteISetEmpty = new CustomISet<sbyte>(0),
                Int16ISetEmpty = new CustomISet<short>(0),
                UInt16ISetEmpty = new CustomISet<ushort>(0),
                Int32ISetEmpty = new CustomISet<int>(0),
                UInt32ISetEmpty = new CustomISet<uint>(0),
                Int64ISetEmpty = new CustomISet<long>(0),
                UInt64ISetEmpty = new CustomISet<ulong>(0),
                SingleISetEmpty = new CustomISet<float>(0),
                DoubleISetEmpty = new CustomISet<double>(0),
                DecimalISetEmpty = new CustomISet<decimal>(0),
                CharISetEmpty = new CustomISet<char>(0),
                DateTimeISetEmpty = new CustomISet<DateTime>(0),
                DateTimeOffsetISetEmpty = new CustomISet<DateTimeOffset>(0),
                TimeSpanISetEmpty = new CustomISet<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyISetEmpty = new CustomISet<DateOnly>(0),
                TimeOnlyISetEmpty = new CustomISet<TimeOnly>(0),
#endif
                GuidISetEmpty = new CustomISet<Guid>(0),

                BooleanISetNull = null,
                ByteISetNull = null,
                SByteISetNull = null,
                Int16ISetNull = null,
                UInt16ISetNull = null,
                Int32ISetNull = null,
                UInt32ISetNull = null,
                Int64ISetNull = null,
                UInt64ISetNull = null,
                SingleISetNull = null,
                DoubleISetNull = null,
                DecimalISetNull = null,
                CharISetNull = null,
                DateTimeISetNull = null,
                DateTimeOffsetISetNull = null,
                TimeSpanISetNull = null,
#if NET6_0_OR_GREATER
                DateOnlyISetNull = null,
                TimeOnlyISetNull = null,
#endif
                GuidISetNull = null,

                BooleanISetNullable = new CustomISet<bool?>() { true, null, true },
                ByteISetNullable = new CustomISet<byte?>() { 1, null, 3 },
                SByteISetNullable = new CustomISet<sbyte?>() { 4, null, 6 },
                Int16ISetNullable = new CustomISet<short?>() { 7, null, 9 },
                UInt16ISetNullable = new CustomISet<ushort?>() { 10, null, 12 },
                Int32ISetNullable = new CustomISet<int?>() { 13, null, 15 },
                UInt32ISetNullable = new CustomISet<uint?>() { 16, null, 18 },
                Int64ISetNullable = new CustomISet<long?>() { 19, null, 21 },
                UInt64ISetNullable = new CustomISet<ulong?>() { 22, null, 24 },
                SingleISetNullable = new CustomISet<float?>() { 25, null, 27 },
                DoubleISetNullable = new CustomISet<double?>() { 28, null, 30 },
                DecimalISetNullable = new CustomISet<decimal?>() { 31, null, 33 },
                CharISetNullable = new CustomISet<char?>() { 'A', null, 'C' },
                DateTimeISetNullable = new CustomISet<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetISetNullable = new CustomISet<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanISetNullable = new CustomISet<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyISetNullable = new CustomISet<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyISetNullable = new CustomISet<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidISetNullable = new CustomISet<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanISetNullableEmpty = new CustomISet<bool?>(0),
                ByteISetNullableEmpty = new CustomISet<byte?>(0),
                SByteISetNullableEmpty = new CustomISet<sbyte?>(0),
                Int16ISetNullableEmpty = new CustomISet<short?>(0),
                UInt16ISetNullableEmpty = new CustomISet<ushort?>(0),
                Int32ISetNullableEmpty = new CustomISet<int?>(0),
                UInt32ISetNullableEmpty = new CustomISet<uint?>(0),
                Int64ISetNullableEmpty = new CustomISet<long?>(0),
                UInt64ISetNullableEmpty = new CustomISet<ulong?>(0),
                SingleISetNullableEmpty = new CustomISet<float?>(0),
                DoubleISetNullableEmpty = new CustomISet<double?>(0),
                DecimalISetNullableEmpty = new CustomISet<decimal?>(0),
                CharISetNullableEmpty = new CustomISet<char?>(0),
                DateTimeISetNullableEmpty = new CustomISet<DateTime?>(0),
                DateTimeOffsetISetNullableEmpty = new CustomISet<DateTimeOffset?>(0),
                TimeSpanISetNullableEmpty = new CustomISet<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyISetNullableEmpty = new CustomISet<DateOnly?>(0),
                TimeOnlyISetNullableEmpty = new CustomISet<TimeOnly?>(0),
#endif
                GuidISetNullableEmpty = new CustomISet<Guid?>(0),

                BooleanISetNullableNull = null,
                ByteISetNullableNull = null,
                SByteISetNullableNull = null,
                Int16ISetNullableNull = null,
                UInt16ISetNullableNull = null,
                Int32ISetNullableNull = null,
                UInt32ISetNullableNull = null,
                Int64ISetNullableNull = null,
                UInt64ISetNullableNull = null,
                SingleISetNullableNull = null,
                DoubleISetNullableNull = null,
                DecimalISetNullableNull = null,
                CharISetNullableNull = null,
                DateTimeISetNullableNull = null,
                DateTimeOffsetISetNullableNull = null,
                TimeSpanISetNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyISetNullableNull = null,
                TimeOnlyISetNullableNull = null,
#endif
                GuidISetNullableNull = null,

                StringISet = new CustomISet<string>() { "Hello", "World", null, "", "People" },
                StringISetEmpty = new CustomISet<string>(0),
                StringISetNull = null,

                EnumISet = new CustomISet<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumISetEmpty = new CustomISet<EnumModel>(0),
                EnumISetNull = null,

                EnumISetNullable = new CustomISet<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumISetNullableEmpty = new CustomISet<EnumModel?>(0),
                EnumISetNullableNull = null,

                ClassISet = new CustomISet<SimpleModel>() { new SimpleModel { Value1 = 10, Value2 = "S-10" }, new SimpleModel { Value1 = 11, Value2 = "S-11" } },
                ClassISetEmpty = new CustomISet<SimpleModel>(0),
                ClassISetNull = null,
            };
            return model;
        }
    }
}
