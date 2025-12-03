// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.SourceGeneration.GenerateTypeDetail]
    public class TypesIListOfTModel
    {
        public sealed class CustomIList : IList
        {
            private readonly List<object> list;
            public CustomIList()
            {
                list = new();
            }
            public CustomIList(int capacity)
            {
                list = new(capacity);
            }

            public object this[int index] { get => list[index]; set => list[index] = value; }
            public bool IsFixedSize => ((IList)list).IsFixedSize;
            public bool IsReadOnly => false;
            public int Count => list.Count;
            public bool IsSynchronized => ((IList)list).IsSynchronized;
            public object SyncRoot => ((IList)list).SyncRoot;
            public int Add(object value) => ((IList)list).Add(value);
            public void Clear() => list.Clear();
            public bool Contains(object value) => list.Contains(value);
            public void CopyTo(Array array, int index) => ((IList)list).CopyTo(array, index);
            public IEnumerator GetEnumerator() => list.GetEnumerator();
            public int IndexOf(object value) => list.IndexOf(value);
            public void Insert(int index, object value) => list.Insert(index, value);
            public void Remove(object value) => list.Remove(value);
            public void RemoveAt(int index) => list.RemoveAt(index);
        }

        public CustomIList BooleanIListT { get; set; }
        public CustomIList ByteIListT { get; set; }
        public CustomIList SByteIListT { get; set; }
        public CustomIList Int16IListT { get; set; }
        public CustomIList UInt16IListT { get; set; }
        public CustomIList Int32IListT { get; set; }
        public CustomIList UInt32IListT { get; set; }
        public CustomIList Int64IListT { get; set; }
        public CustomIList UInt64IListT { get; set; }
        public CustomIList SingleIListT { get; set; }
        public CustomIList DoubleIListT { get; set; }
        public CustomIList DecimalIListT { get; set; }
        public CustomIList CharIListT { get; set; }
        public CustomIList DateTimeIListT { get; set; }
        public CustomIList DateTimeOffsetIListT { get; set; }
        public CustomIList TimeSpanIListT { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList DateOnlyIListT { get; set; }
        public CustomIList TimeOnlyIListT { get; set; }
#endif
        public CustomIList GuidIListT { get; set; }

        public CustomIList BooleanIListTEmpty { get; set; }
        public CustomIList ByteIListTEmpty { get; set; }
        public CustomIList SByteIListTEmpty { get; set; }
        public CustomIList Int16IListTEmpty { get; set; }
        public CustomIList UInt16IListTEmpty { get; set; }
        public CustomIList Int32IListTEmpty { get; set; }
        public CustomIList UInt32IListTEmpty { get; set; }
        public CustomIList Int64IListTEmpty { get; set; }
        public CustomIList UInt64IListTEmpty { get; set; }
        public CustomIList SingleIListTEmpty { get; set; }
        public CustomIList DoubleIListTEmpty { get; set; }
        public CustomIList DecimalIListTEmpty { get; set; }
        public CustomIList CharIListTEmpty { get; set; }
        public CustomIList DateTimeIListTEmpty { get; set; }
        public CustomIList DateTimeOffsetIListTEmpty { get; set; }
        public CustomIList TimeSpanIListTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList DateOnlyIListTEmpty { get; set; }
        public CustomIList TimeOnlyIListTEmpty { get; set; }
#endif
        public CustomIList GuidIListTEmpty { get; set; }

        public CustomIList BooleanIListTNull { get; set; }
        public CustomIList ByteIListTNull { get; set; }
        public CustomIList SByteIListTNull { get; set; }
        public CustomIList Int16IListTNull { get; set; }
        public CustomIList UInt16IListTNull { get; set; }
        public CustomIList Int32IListTNull { get; set; }
        public CustomIList UInt32IListTNull { get; set; }
        public CustomIList Int64IListTNull { get; set; }
        public CustomIList UInt64IListTNull { get; set; }
        public CustomIList SingleIListTNull { get; set; }
        public CustomIList DoubleIListTNull { get; set; }
        public CustomIList DecimalIListTNull { get; set; }
        public CustomIList CharIListTNull { get; set; }
        public CustomIList DateTimeIListTNull { get; set; }
        public CustomIList DateTimeOffsetIListTNull { get; set; }
        public CustomIList TimeSpanIListTNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList DateOnlyIListTNull { get; set; }
        public CustomIList TimeOnlyIListTNull { get; set; }
#endif
        public CustomIList GuidIListTNull { get; set; }

        public CustomIList BooleanIListTNullable { get; set; }
        public CustomIList ByteIListTNullable { get; set; }
        public CustomIList SByteIListTNullable { get; set; }
        public CustomIList Int16IListTNullable { get; set; }
        public CustomIList UInt16IListTNullable { get; set; }
        public CustomIList Int32IListTNullable { get; set; }
        public CustomIList UInt32IListTNullable { get; set; }
        public CustomIList Int64IListTNullable { get; set; }
        public CustomIList UInt64IListTNullable { get; set; }
        public CustomIList SingleIListTNullable { get; set; }
        public CustomIList DoubleIListTNullable { get; set; }
        public CustomIList DecimalIListTNullable { get; set; }
        public CustomIList CharIListTNullable { get; set; }
        public CustomIList DateTimeIListTNullable { get; set; }
        public CustomIList DateTimeOffsetIListTNullable { get; set; }
        public CustomIList TimeSpanIListTNullable { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList DateOnlyIListTNullable { get; set; }
        public CustomIList TimeOnlyIListTNullable { get; set; }
#endif
        public CustomIList GuidIListTNullable { get; set; }

        public CustomIList BooleanIListTNullableEmpty { get; set; }
        public CustomIList ByteIListTNullableEmpty { get; set; }
        public CustomIList SByteIListTNullableEmpty { get; set; }
        public CustomIList Int16IListTNullableEmpty { get; set; }
        public CustomIList UInt16IListTNullableEmpty { get; set; }
        public CustomIList Int32IListTNullableEmpty { get; set; }
        public CustomIList UInt32IListTNullableEmpty { get; set; }
        public CustomIList Int64IListTNullableEmpty { get; set; }
        public CustomIList UInt64IListTNullableEmpty { get; set; }
        public CustomIList SingleIListTNullableEmpty { get; set; }
        public CustomIList DoubleIListTNullableEmpty { get; set; }
        public CustomIList DecimalIListTNullableEmpty { get; set; }
        public CustomIList CharIListTNullableEmpty { get; set; }
        public CustomIList DateTimeIListTNullableEmpty { get; set; }
        public CustomIList DateTimeOffsetIListTNullableEmpty { get; set; }
        public CustomIList TimeSpanIListTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList DateOnlyIListTNullableEmpty { get; set; }
        public CustomIList TimeOnlyIListTNullableEmpty { get; set; }
#endif
        public CustomIList GuidIListTNullableEmpty { get; set; }

        public CustomIList BooleanIListTNullableNull { get; set; }
        public CustomIList ByteIListTNullableNull { get; set; }
        public CustomIList SByteIListTNullableNull { get; set; }
        public CustomIList Int16IListTNullableNull { get; set; }
        public CustomIList UInt16IListTNullableNull { get; set; }
        public CustomIList Int32IListTNullableNull { get; set; }
        public CustomIList UInt32IListTNullableNull { get; set; }
        public CustomIList Int64IListTNullableNull { get; set; }
        public CustomIList UInt64IListTNullableNull { get; set; }
        public CustomIList SingleIListTNullableNull { get; set; }
        public CustomIList DoubleIListTNullableNull { get; set; }
        public CustomIList DecimalIListTNullableNull { get; set; }
        public CustomIList CharIListTNullableNull { get; set; }
        public CustomIList DateTimeIListTNullableNull { get; set; }
        public CustomIList DateTimeOffsetIListTNullableNull { get; set; }
        public CustomIList TimeSpanIListTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomIList DateOnlyIListTNullableNull { get; set; }
        public CustomIList TimeOnlyIListTNullableNull { get; set; }
#endif
        public CustomIList GuidIListTNullableNull { get; set; }

        public CustomIList StringIListT { get; set; }
        public CustomIList StringIListTEmpty { get; set; }
        public CustomIList StringIListTNull { get; set; }

        public CustomIList EnumIListT { get; set; }
        public CustomIList EnumIListTEmpty { get; set; }
        public CustomIList EnumIListTNull { get; set; }

        public CustomIList EnumIListTNullable { get; set; }
        public CustomIList EnumIListTNullableEmpty { get; set; }
        public CustomIList EnumIListTNullableNull { get; set; }

        public CustomIList ClassIList { get; set; }
        public CustomIList ClassIListEmpty { get; set; }
        public CustomIList ClassIListNull { get; set; }

        public static TypesIListOfTModel Create()
        {
            var model = new TypesIListOfTModel()
            {
                BooleanIListT = new CustomIList() { true, false, true },
                ByteIListT = new CustomIList() { 1, 2, 3 },
                SByteIListT = new CustomIList() { 4, 5, 6 },
                Int16IListT = new CustomIList() { 7, 8, 9 },
                UInt16IListT = new CustomIList() { 10, 11, 12 },
                Int32IListT = new CustomIList() { 13, 14, 15 },
                UInt32IListT = new CustomIList() { 16, 17, 18 },
                Int64IListT = new CustomIList() { 19, 20, 21 },
                UInt64IListT = new CustomIList() { 22, 23, 24 },
                SingleIListT = new CustomIList() { 25, 26, 27 },
                DoubleIListT = new CustomIList() { 28, 29, 30 },
                DecimalIListT = new CustomIList() { 31, 32, 33 },
                CharIListT = new CustomIList() { 'A', 'B', 'C' },
                DateTimeIListT = new CustomIList() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIListT = new CustomIList() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIListT = new CustomIList() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIListT = new CustomIList() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIListT = new CustomIList() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIListT = new CustomIList() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanIListTEmpty = new CustomIList(0),
                ByteIListTEmpty = new CustomIList(0),
                SByteIListTEmpty = new CustomIList(0),
                Int16IListTEmpty = new CustomIList(0),
                UInt16IListTEmpty = new CustomIList(0),
                Int32IListTEmpty = new CustomIList(0),
                UInt32IListTEmpty = new CustomIList(0),
                Int64IListTEmpty = new CustomIList(0),
                UInt64IListTEmpty = new CustomIList(0),
                SingleIListTEmpty = new CustomIList(0),
                DoubleIListTEmpty = new CustomIList(0),
                DecimalIListTEmpty = new CustomIList(0),
                CharIListTEmpty = new CustomIList(0),
                DateTimeIListTEmpty = new CustomIList(0),
                DateTimeOffsetIListTEmpty = new CustomIList(0),
                TimeSpanIListTEmpty = new CustomIList(0),
#if NET6_0_OR_GREATER
                DateOnlyIListTEmpty = new CustomIList(0),
                TimeOnlyIListTEmpty = new CustomIList(0),
#endif
                GuidIListTEmpty = new CustomIList(0),

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

                BooleanIListTNullable = new CustomIList() { true, null, true },
                ByteIListTNullable = new CustomIList() { 1, null, 3 },
                SByteIListTNullable = new CustomIList() { 4, null, 6 },
                Int16IListTNullable = new CustomIList() { 7, null, 9 },
                UInt16IListTNullable = new CustomIList() { 10, null, 12 },
                Int32IListTNullable = new CustomIList() { 13, null, 15 },
                UInt32IListTNullable = new CustomIList() { 16, null, 18 },
                Int64IListTNullable = new CustomIList() { 19, null, 21 },
                UInt64IListTNullable = new CustomIList() { 22, null, 24 },
                SingleIListTNullable = new CustomIList() { 25, null, 27 },
                DoubleIListTNullable = new CustomIList() { 28, null, 30 },
                DecimalIListTNullable = new CustomIList() { 31, null, 33 },
                CharIListTNullable = new CustomIList() { 'A', null, 'C' },
                DateTimeIListTNullable = new CustomIList() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIListTNullable = new CustomIList() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIListTNullable = new CustomIList() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIListTNullable = new CustomIList() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIListTNullable = new CustomIList() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIListTNullable = new CustomIList() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanIListTNullableEmpty = new CustomIList(0),
                ByteIListTNullableEmpty = new CustomIList(0),
                SByteIListTNullableEmpty = new CustomIList(0),
                Int16IListTNullableEmpty = new CustomIList(0),
                UInt16IListTNullableEmpty = new CustomIList(0),
                Int32IListTNullableEmpty = new CustomIList(0),
                UInt32IListTNullableEmpty = new CustomIList(0),
                Int64IListTNullableEmpty = new CustomIList(0),
                UInt64IListTNullableEmpty = new CustomIList(0),
                SingleIListTNullableEmpty = new CustomIList(0),
                DoubleIListTNullableEmpty = new CustomIList(0),
                DecimalIListTNullableEmpty = new CustomIList(0),
                CharIListTNullableEmpty = new CustomIList(0),
                DateTimeIListTNullableEmpty = new CustomIList(0),
                DateTimeOffsetIListTNullableEmpty = new CustomIList(0),
                TimeSpanIListTNullableEmpty = new CustomIList(0),
#if NET6_0_OR_GREATER
                DateOnlyIListTNullableEmpty = new CustomIList(0),
                TimeOnlyIListTNullableEmpty = new CustomIList(0),
#endif
                GuidIListTNullableEmpty = new CustomIList(0),

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

                StringIListT = new CustomIList() { "Hello", "World", null, "", "People" },
                StringIListTEmpty = new CustomIList(0),
                StringIListTNull = null,

                EnumIListT = new CustomIList() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumIListTEmpty = new CustomIList(0),
                EnumIListTNull = null,

                EnumIListTNullable = new CustomIList() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumIListTNullableEmpty = new CustomIList(0),
                EnumIListTNullableNull = null,

                ClassIList = new CustomIList() { new SimpleModel { Value1 = 10, Value2 = "S-10" }, new SimpleModel { Value1 = 11, Value2 = "S-11" } },
                ClassIListEmpty = new CustomIList(0),
                ClassIListNull = null,
            };
            return model;
        }
    }
}