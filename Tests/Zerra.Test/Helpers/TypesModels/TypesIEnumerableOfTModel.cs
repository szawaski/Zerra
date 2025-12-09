// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.Reflection.GenerateTypeDetail]
    public class TypesIEnumerableOfTModel
    {
        public sealed class CustomIEnumerable : IEnumerable
        {
            private readonly List<object> list;
            public CustomIEnumerable()
            {
                list = new();
            }
            public CustomIEnumerable(int capacity)
            {
                list = new(capacity);
            }

            public void Add(object item) => list.Add(item);

            public IEnumerator GetEnumerator() => list.GetEnumerator();
        }

        public CustomIEnumerable BooleanIEnumerableT { get; set; }
        public CustomIEnumerable ByteIEnumerableT { get; set; }
        public CustomIEnumerable SByteIEnumerableT { get; set; }
        public CustomIEnumerable Int16IEnumerableT { get; set; }
        public CustomIEnumerable UInt16IEnumerableT { get; set; }
        public CustomIEnumerable Int32IEnumerableT { get; set; }
        public CustomIEnumerable UInt32IEnumerableT { get; set; }
        public CustomIEnumerable Int64IEnumerableT { get; set; }
        public CustomIEnumerable UInt64IEnumerableT { get; set; }
        public CustomIEnumerable SingleIEnumerableT { get; set; }
        public CustomIEnumerable DoubleIEnumerableT { get; set; }
        public CustomIEnumerable DecimalIEnumerableT { get; set; }
        public CustomIEnumerable CharIEnumerableT { get; set; }
        public CustomIEnumerable DateTimeIEnumerableT { get; set; }
        public CustomIEnumerable DateTimeOffsetIEnumerableT { get; set; }
        public CustomIEnumerable TimeSpanIEnumerableT { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable DateOnlyIEnumerableT { get; set; }
        public CustomIEnumerable TimeOnlyIEnumerableT { get; set; }
#endif
        public CustomIEnumerable GuidIEnumerableT { get; set; }

        public CustomIEnumerable BooleanIEnumerableTEmpty { get; set; }
        public CustomIEnumerable ByteIEnumerableTEmpty { get; set; }
        public CustomIEnumerable SByteIEnumerableTEmpty { get; set; }
        public CustomIEnumerable Int16IEnumerableTEmpty { get; set; }
        public CustomIEnumerable UInt16IEnumerableTEmpty { get; set; }
        public CustomIEnumerable Int32IEnumerableTEmpty { get; set; }
        public CustomIEnumerable UInt32IEnumerableTEmpty { get; set; }
        public CustomIEnumerable Int64IEnumerableTEmpty { get; set; }
        public CustomIEnumerable UInt64IEnumerableTEmpty { get; set; }
        public CustomIEnumerable SingleIEnumerableTEmpty { get; set; }
        public CustomIEnumerable DoubleIEnumerableTEmpty { get; set; }
        public CustomIEnumerable DecimalIEnumerableTEmpty { get; set; }
        public CustomIEnumerable CharIEnumerableTEmpty { get; set; }
        public CustomIEnumerable DateTimeIEnumerableTEmpty { get; set; }
        public CustomIEnumerable DateTimeOffsetIEnumerableTEmpty { get; set; }
        public CustomIEnumerable TimeSpanIEnumerableTEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable DateOnlyIEnumerableTEmpty { get; set; }
        public CustomIEnumerable TimeOnlyIEnumerableTEmpty { get; set; }
#endif
        public CustomIEnumerable GuidIEnumerableTEmpty { get; set; }

        public CustomIEnumerable BooleanIEnumerableTNull { get; set; }
        public CustomIEnumerable ByteIEnumerableTNull { get; set; }
        public CustomIEnumerable SByteIEnumerableTNull { get; set; }
        public CustomIEnumerable Int16IEnumerableTNull { get; set; }
        public CustomIEnumerable UInt16IEnumerableTNull { get; set; }
        public CustomIEnumerable Int32IEnumerableTNull { get; set; }
        public CustomIEnumerable UInt32IEnumerableTNull { get; set; }
        public CustomIEnumerable Int64IEnumerableTNull { get; set; }
        public CustomIEnumerable UInt64IEnumerableTNull { get; set; }
        public CustomIEnumerable SingleIEnumerableTNull { get; set; }
        public CustomIEnumerable DoubleIEnumerableTNull { get; set; }
        public CustomIEnumerable DecimalIEnumerableTNull { get; set; }
        public CustomIEnumerable CharIEnumerableTNull { get; set; }
        public CustomIEnumerable DateTimeIEnumerableTNull { get; set; }
        public CustomIEnumerable DateTimeOffsetIEnumerableTNull { get; set; }
        public CustomIEnumerable TimeSpanIEnumerableTNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable DateOnlyIEnumerableTNull { get; set; }
        public CustomIEnumerable TimeOnlyIEnumerableTNull { get; set; }
#endif
        public CustomIEnumerable GuidIEnumerableTNull { get; set; }

        public CustomIEnumerable BooleanIEnumerableTNullable { get; set; }
        public CustomIEnumerable ByteIEnumerableTNullable { get; set; }
        public CustomIEnumerable SByteIEnumerableTNullable { get; set; }
        public CustomIEnumerable Int16IEnumerableTNullable { get; set; }
        public CustomIEnumerable UInt16IEnumerableTNullable { get; set; }
        public CustomIEnumerable Int32IEnumerableTNullable { get; set; }
        public CustomIEnumerable UInt32IEnumerableTNullable { get; set; }
        public CustomIEnumerable Int64IEnumerableTNullable { get; set; }
        public CustomIEnumerable UInt64IEnumerableTNullable { get; set; }
        public CustomIEnumerable SingleIEnumerableTNullable { get; set; }
        public CustomIEnumerable DoubleIEnumerableTNullable { get; set; }
        public CustomIEnumerable DecimalIEnumerableTNullable { get; set; }
        public CustomIEnumerable CharIEnumerableTNullable { get; set; }
        public CustomIEnumerable DateTimeIEnumerableTNullable { get; set; }
        public CustomIEnumerable DateTimeOffsetIEnumerableTNullable { get; set; }
        public CustomIEnumerable TimeSpanIEnumerableTNullable { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable DateOnlyIEnumerableTNullable { get; set; }
        public CustomIEnumerable TimeOnlyIEnumerableTNullable { get; set; }
#endif
        public CustomIEnumerable GuidIEnumerableTNullable { get; set; }

        public CustomIEnumerable BooleanIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable ByteIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable SByteIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable Int16IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable UInt16IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable Int32IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable UInt32IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable Int64IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable UInt64IEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable SingleIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable DoubleIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable DecimalIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable CharIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable DateTimeIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable DateTimeOffsetIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable TimeSpanIEnumerableTNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable DateOnlyIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable TimeOnlyIEnumerableTNullableEmpty { get; set; }
#endif
        public CustomIEnumerable GuidIEnumerableTNullableEmpty { get; set; }

        public CustomIEnumerable BooleanIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable ByteIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable SByteIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable Int16IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable UInt16IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable Int32IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable UInt32IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable Int64IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable UInt64IEnumerableTNullableNull { get; set; }
        public CustomIEnumerable SingleIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable DoubleIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable DecimalIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable CharIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable DateTimeIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable DateTimeOffsetIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable TimeSpanIEnumerableTNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public CustomIEnumerable DateOnlyIEnumerableTNullableNull { get; set; }
        public CustomIEnumerable TimeOnlyIEnumerableTNullableNull { get; set; }
#endif
        public CustomIEnumerable GuidIEnumerableTNullableNull { get; set; }

        public CustomIEnumerable StringIEnumerableT { get; set; }
        public CustomIEnumerable StringIEnumerableTEmpty { get; set; }
        public CustomIEnumerable StringIEnumerableTNull { get; set; }

        public CustomIEnumerable EnumIEnumerableT { get; set; }
        public CustomIEnumerable EnumIEnumerableTEmpty { get; set; }
        public CustomIEnumerable EnumIEnumerableTNull { get; set; }

        public CustomIEnumerable EnumIEnumerableTNullable { get; set; }
        public CustomIEnumerable EnumIEnumerableTNullableEmpty { get; set; }
        public CustomIEnumerable EnumIEnumerableTNullableNull { get; set; }

        public CustomIEnumerable ClassIEnumerable { get; set; }
        public CustomIEnumerable ClassIEnumerableEmpty { get; set; }
        public CustomIEnumerable ClassIEnumerableNull { get; set; }

        public static TypesIEnumerableOfTModel Create()
        {
            var model = new TypesIEnumerableOfTModel()
            {
                BooleanIEnumerableT = new CustomIEnumerable() { true, false, true },
                ByteIEnumerableT = new CustomIEnumerable() { 1, 2, 3 },
                SByteIEnumerableT = new CustomIEnumerable() { 4, 5, 6 },
                Int16IEnumerableT = new CustomIEnumerable() { 7, 8, 9 },
                UInt16IEnumerableT = new CustomIEnumerable() { 10, 11, 12 },
                Int32IEnumerableT = new CustomIEnumerable() { 13, 14, 15 },
                UInt32IEnumerableT = new CustomIEnumerable() { 16, 17, 18 },
                Int64IEnumerableT = new CustomIEnumerable() { 19, 20, 21 },
                UInt64IEnumerableT = new CustomIEnumerable() { 22, 23, 24 },
                SingleIEnumerableT = new CustomIEnumerable() { 25, 26, 27 },
                DoubleIEnumerableT = new CustomIEnumerable() { 28, 29, 30 },
                DecimalIEnumerableT = new CustomIEnumerable() { 31, 32, 33 },
                CharIEnumerableT = new CustomIEnumerable() { 'A', 'B', 'C' },
                DateTimeIEnumerableT = new CustomIEnumerable() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIEnumerableT = new CustomIEnumerable() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIEnumerableT = new CustomIEnumerable() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableT = new CustomIEnumerable() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIEnumerableT = new CustomIEnumerable() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIEnumerableT = new CustomIEnumerable() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanIEnumerableTEmpty = new CustomIEnumerable(0),
                ByteIEnumerableTEmpty = new CustomIEnumerable(0),
                SByteIEnumerableTEmpty = new CustomIEnumerable(0),
                Int16IEnumerableTEmpty = new CustomIEnumerable(0),
                UInt16IEnumerableTEmpty = new CustomIEnumerable(0),
                Int32IEnumerableTEmpty = new CustomIEnumerable(0),
                UInt32IEnumerableTEmpty = new CustomIEnumerable(0),
                Int64IEnumerableTEmpty = new CustomIEnumerable(0),
                UInt64IEnumerableTEmpty = new CustomIEnumerable(0),
                SingleIEnumerableTEmpty = new CustomIEnumerable(0),
                DoubleIEnumerableTEmpty = new CustomIEnumerable(0),
                DecimalIEnumerableTEmpty = new CustomIEnumerable(0),
                CharIEnumerableTEmpty = new CustomIEnumerable(0),
                DateTimeIEnumerableTEmpty = new CustomIEnumerable(0),
                DateTimeOffsetIEnumerableTEmpty = new CustomIEnumerable(0),
                TimeSpanIEnumerableTEmpty = new CustomIEnumerable(0),
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableTEmpty = new CustomIEnumerable(0),
                TimeOnlyIEnumerableTEmpty = new CustomIEnumerable(0),
#endif
                GuidIEnumerableTEmpty = new CustomIEnumerable(0),

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

                BooleanIEnumerableTNullable = new CustomIEnumerable() { true, null, true },
                ByteIEnumerableTNullable = new CustomIEnumerable() { 1, null, 3 },
                SByteIEnumerableTNullable = new CustomIEnumerable() { 4, null, 6 },
                Int16IEnumerableTNullable = new CustomIEnumerable() { 7, null, 9 },
                UInt16IEnumerableTNullable = new CustomIEnumerable() { 10, null, 12 },
                Int32IEnumerableTNullable = new CustomIEnumerable() { 13, null, 15 },
                UInt32IEnumerableTNullable = new CustomIEnumerable() { 16, null, 18 },
                Int64IEnumerableTNullable = new CustomIEnumerable() { 19, null, 21 },
                UInt64IEnumerableTNullable = new CustomIEnumerable() { 22, null, 24 },
                SingleIEnumerableTNullable = new CustomIEnumerable() { 25, null, 27 },
                DoubleIEnumerableTNullable = new CustomIEnumerable() { 28, null, 30 },
                DecimalIEnumerableTNullable = new CustomIEnumerable() { 31, null, 33 },
                CharIEnumerableTNullable = new CustomIEnumerable() { 'A', null, 'C' },
                DateTimeIEnumerableTNullable = new CustomIEnumerable() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIEnumerableTNullable = new CustomIEnumerable() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIEnumerableTNullable = new CustomIEnumerable() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableTNullable = new CustomIEnumerable() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIEnumerableTNullable = new CustomIEnumerable() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIEnumerableTNullable = new CustomIEnumerable() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanIEnumerableTNullableEmpty = new CustomIEnumerable(0),
                ByteIEnumerableTNullableEmpty = new CustomIEnumerable(0),
                SByteIEnumerableTNullableEmpty = new CustomIEnumerable(0),
                Int16IEnumerableTNullableEmpty = new CustomIEnumerable(0),
                UInt16IEnumerableTNullableEmpty = new CustomIEnumerable(0),
                Int32IEnumerableTNullableEmpty = new CustomIEnumerable(0),
                UInt32IEnumerableTNullableEmpty = new CustomIEnumerable(0),
                Int64IEnumerableTNullableEmpty = new CustomIEnumerable(0),
                UInt64IEnumerableTNullableEmpty = new CustomIEnumerable(0),
                SingleIEnumerableTNullableEmpty = new CustomIEnumerable(0),
                DoubleIEnumerableTNullableEmpty = new CustomIEnumerable(0),
                DecimalIEnumerableTNullableEmpty = new CustomIEnumerable(0),
                CharIEnumerableTNullableEmpty = new CustomIEnumerable(0),
                DateTimeIEnumerableTNullableEmpty = new CustomIEnumerable(0),
                DateTimeOffsetIEnumerableTNullableEmpty = new CustomIEnumerable(0),
                TimeSpanIEnumerableTNullableEmpty = new CustomIEnumerable(0),
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableTNullableEmpty = new CustomIEnumerable(0),
                TimeOnlyIEnumerableTNullableEmpty = new CustomIEnumerable(0),
#endif
                GuidIEnumerableTNullableEmpty = new CustomIEnumerable(0),

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

                StringIEnumerableT = new CustomIEnumerable() { "Hello", "World", null, "", "People" },
                StringIEnumerableTEmpty = new CustomIEnumerable(0),
                StringIEnumerableTNull = null,

                EnumIEnumerableT = new CustomIEnumerable() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumIEnumerableTEmpty = new CustomIEnumerable(0),
                EnumIEnumerableTNull = null,

                EnumIEnumerableTNullable = new CustomIEnumerable() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumIEnumerableTNullableEmpty = new CustomIEnumerable(0),
                EnumIEnumerableTNullableNull = null,

                ClassIEnumerable = new CustomIEnumerable() { new SimpleModel { Value1 = 10, Value2 = "S-10" }, new SimpleModel { Value1 = 11, Value2 = "S-11" } },
                ClassIEnumerableEmpty = new CustomIEnumerable(0),
                ClassIEnumerableNull = null,
            };
            return model;
        }
    }
}