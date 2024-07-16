// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zerra.Test
{
    public class TypesBoxedCollectionsModel
    {
        #region ILists

        public IList BooleanIList { get; set; }
        public IList ByteIList { get; set; }
        public IList SByteIList { get; set; }
        public IList Int16IList { get; set; }
        public IList UInt16IList { get; set; }
        public IList Int32IList { get; set; }
        public IList UInt32IList { get; set; }
        public IList Int64IList { get; set; }
        public IList UInt64IList { get; set; }
        public IList SingleIList { get; set; }
        public IList DoubleIList { get; set; }
        public IList DecimalIList { get; set; }
        public IList CharIList { get; set; }
        public IList DateTimeIList { get; set; }
        public IList DateTimeOffsetIList { get; set; }
        public IList TimeSpanIList { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIList { get; set; }
        public IList TimeOnlyIList { get; set; }
#endif
        public IList GuidIList { get; set; }

        public IList BooleanIListEmpty { get; set; }
        public IList ByteIListEmpty { get; set; }
        public IList SByteIListEmpty { get; set; }
        public IList Int16IListEmpty { get; set; }
        public IList UInt16IListEmpty { get; set; }
        public IList Int32IListEmpty { get; set; }
        public IList UInt32IListEmpty { get; set; }
        public IList Int64IListEmpty { get; set; }
        public IList UInt64IListEmpty { get; set; }
        public IList SingleIListEmpty { get; set; }
        public IList DoubleIListEmpty { get; set; }
        public IList DecimalIListEmpty { get; set; }
        public IList CharIListEmpty { get; set; }
        public IList DateTimeIListEmpty { get; set; }
        public IList DateTimeOffsetIListEmpty { get; set; }
        public IList TimeSpanIListEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIListEmpty { get; set; }
        public IList TimeOnlyIListEmpty { get; set; }
#endif
        public IList GuidIListEmpty { get; set; }

        public IList BooleanIListNull { get; set; }
        public IList ByteIListNull { get; set; }
        public IList SByteIListNull { get; set; }
        public IList Int16IListNull { get; set; }
        public IList UInt16IListNull { get; set; }
        public IList Int32IListNull { get; set; }
        public IList UInt32IListNull { get; set; }
        public IList Int64IListNull { get; set; }
        public IList UInt64IListNull { get; set; }
        public IList SingleIListNull { get; set; }
        public IList DoubleIListNull { get; set; }
        public IList DecimalIListNull { get; set; }
        public IList CharIListNull { get; set; }
        public IList DateTimeIListNull { get; set; }
        public IList DateTimeOffsetIListNull { get; set; }
        public IList TimeSpanIListNull { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIListNull { get; set; }
        public IList TimeOnlyIListNull { get; set; }
#endif
        public IList GuidIListNull { get; set; }

        public IList BooleanIListNullable { get; set; }
        public IList ByteIListNullable { get; set; }
        public IList SByteIListNullable { get; set; }
        public IList Int16IListNullable { get; set; }
        public IList UInt16IListNullable { get; set; }
        public IList Int32IListNullable { get; set; }
        public IList UInt32IListNullable { get; set; }
        public IList Int64IListNullable { get; set; }
        public IList UInt64IListNullable { get; set; }
        public IList SingleIListNullable { get; set; }
        public IList DoubleIListNullable { get; set; }
        public IList DecimalIListNullable { get; set; }
        public IList CharIListNullable { get; set; }
        public IList DateTimeIListNullable { get; set; }
        public IList DateTimeOffsetIListNullable { get; set; }
        public IList TimeSpanIListNullable { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIListNullable { get; set; }
        public IList TimeOnlyIListNullable { get; set; }
#endif
        public IList GuidIListNullable { get; set; }

        public IList BooleanIListNullableEmpty { get; set; }
        public IList ByteIListNullableEmpty { get; set; }
        public IList SByteIListNullableEmpty { get; set; }
        public IList Int16IListNullableEmpty { get; set; }
        public IList UInt16IListNullableEmpty { get; set; }
        public IList Int32IListNullableEmpty { get; set; }
        public IList UInt32IListNullableEmpty { get; set; }
        public IList Int64IListNullableEmpty { get; set; }
        public IList UInt64IListNullableEmpty { get; set; }
        public IList SingleIListNullableEmpty { get; set; }
        public IList DoubleIListNullableEmpty { get; set; }
        public IList DecimalIListNullableEmpty { get; set; }
        public IList CharIListNullableEmpty { get; set; }
        public IList DateTimeIListNullableEmpty { get; set; }
        public IList DateTimeOffsetIListNullableEmpty { get; set; }
        public IList TimeSpanIListNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIListNullableEmpty { get; set; }
        public IList TimeOnlyIListNullableEmpty { get; set; }
#endif
        public IList GuidIListNullableEmpty { get; set; }

        public IList BooleanIListNullableNull { get; set; }
        public IList ByteIListNullableNull { get; set; }
        public IList SByteIListNullableNull { get; set; }
        public IList Int16IListNullableNull { get; set; }
        public IList UInt16IListNullableNull { get; set; }
        public IList Int32IListNullableNull { get; set; }
        public IList UInt32IListNullableNull { get; set; }
        public IList Int64IListNullableNull { get; set; }
        public IList UInt64IListNullableNull { get; set; }
        public IList SingleIListNullableNull { get; set; }
        public IList DoubleIListNullableNull { get; set; }
        public IList DecimalIListNullableNull { get; set; }
        public IList CharIListNullableNull { get; set; }
        public IList DateTimeIListNullableNull { get; set; }
        public IList DateTimeOffsetIListNullableNull { get; set; }
        public IList TimeSpanIListNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public IList DateOnlyIListNullableNull { get; set; }
        public IList TimeOnlyIListNullableNull { get; set; }
#endif
        public IList GuidIListNullableNull { get; set; }

        public IList StringIList { get; set; }
        public IList StringIListEmpty { get; set; }
        public IList StringIListNull { get; set; }

        public IList EnumIList { get; set; }
        public IList EnumIListEmpty { get; set; }
        public IList EnumIListNull { get; set; }

        public IList EnumIListNullable { get; set; }
        public IList EnumIListNullableEmpty { get; set; }
        public IList EnumIListNullableNull { get; set; }

        #endregion

        #region ICollections

        public ICollection BooleanICollection { get; set; }
        public ICollection ByteICollection { get; set; }
        public ICollection SByteICollection { get; set; }
        public ICollection Int16ICollection { get; set; }
        public ICollection UInt16ICollection { get; set; }
        public ICollection Int32ICollection { get; set; }
        public ICollection UInt32ICollection { get; set; }
        public ICollection Int64ICollection { get; set; }
        public ICollection UInt64ICollection { get; set; }
        public ICollection SingleICollection { get; set; }
        public ICollection DoubleICollection { get; set; }
        public ICollection DecimalICollection { get; set; }
        public ICollection CharICollection { get; set; }
        public ICollection DateTimeICollection { get; set; }
        public ICollection DateTimeOffsetICollection { get; set; }
        public ICollection TimeSpanICollection { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollection { get; set; }
        public ICollection TimeOnlyICollection { get; set; }
#endif
        public ICollection GuidICollection { get; set; }

        public ICollection BooleanICollectionEmpty { get; set; }
        public ICollection ByteICollectionEmpty { get; set; }
        public ICollection SByteICollectionEmpty { get; set; }
        public ICollection Int16ICollectionEmpty { get; set; }
        public ICollection UInt16ICollectionEmpty { get; set; }
        public ICollection Int32ICollectionEmpty { get; set; }
        public ICollection UInt32ICollectionEmpty { get; set; }
        public ICollection Int64ICollectionEmpty { get; set; }
        public ICollection UInt64ICollectionEmpty { get; set; }
        public ICollection SingleICollectionEmpty { get; set; }
        public ICollection DoubleICollectionEmpty { get; set; }
        public ICollection DecimalICollectionEmpty { get; set; }
        public ICollection CharICollectionEmpty { get; set; }
        public ICollection DateTimeICollectionEmpty { get; set; }
        public ICollection DateTimeOffsetICollectionEmpty { get; set; }
        public ICollection TimeSpanICollectionEmpty { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollectionEmpty { get; set; }
        public ICollection TimeOnlyICollectionEmpty { get; set; }
#endif
        public ICollection GuidICollectionEmpty { get; set; }

        public ICollection BooleanICollectionNull { get; set; }
        public ICollection ByteICollectionNull { get; set; }
        public ICollection SByteICollectionNull { get; set; }
        public ICollection Int16ICollectionNull { get; set; }
        public ICollection UInt16ICollectionNull { get; set; }
        public ICollection Int32ICollectionNull { get; set; }
        public ICollection UInt32ICollectionNull { get; set; }
        public ICollection Int64ICollectionNull { get; set; }
        public ICollection UInt64ICollectionNull { get; set; }
        public ICollection SingleICollectionNull { get; set; }
        public ICollection DoubleICollectionNull { get; set; }
        public ICollection DecimalICollectionNull { get; set; }
        public ICollection CharICollectionNull { get; set; }
        public ICollection DateTimeICollectionNull { get; set; }
        public ICollection DateTimeOffsetICollectionNull { get; set; }
        public ICollection TimeSpanICollectionNull { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollectionNull { get; set; }
        public ICollection TimeOnlyICollectionNull { get; set; }
#endif
        public ICollection GuidICollectionNull { get; set; }

        public ICollection BooleanICollectionNullable { get; set; }
        public ICollection ByteICollectionNullable { get; set; }
        public ICollection SByteICollectionNullable { get; set; }
        public ICollection Int16ICollectionNullable { get; set; }
        public ICollection UInt16ICollectionNullable { get; set; }
        public ICollection Int32ICollectionNullable { get; set; }
        public ICollection UInt32ICollectionNullable { get; set; }
        public ICollection Int64ICollectionNullable { get; set; }
        public ICollection UInt64ICollectionNullable { get; set; }
        public ICollection SingleICollectionNullable { get; set; }
        public ICollection DoubleICollectionNullable { get; set; }
        public ICollection DecimalICollectionNullable { get; set; }
        public ICollection CharICollectionNullable { get; set; }
        public ICollection DateTimeICollectionNullable { get; set; }
        public ICollection DateTimeOffsetICollectionNullable { get; set; }
        public ICollection TimeSpanICollectionNullable { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollectionNullable { get; set; }
        public ICollection TimeOnlyICollectionNullable { get; set; }
#endif
        public ICollection GuidICollectionNullable { get; set; }

        public ICollection BooleanICollectionNullableEmpty { get; set; }
        public ICollection ByteICollectionNullableEmpty { get; set; }
        public ICollection SByteICollectionNullableEmpty { get; set; }
        public ICollection Int16ICollectionNullableEmpty { get; set; }
        public ICollection UInt16ICollectionNullableEmpty { get; set; }
        public ICollection Int32ICollectionNullableEmpty { get; set; }
        public ICollection UInt32ICollectionNullableEmpty { get; set; }
        public ICollection Int64ICollectionNullableEmpty { get; set; }
        public ICollection UInt64ICollectionNullableEmpty { get; set; }
        public ICollection SingleICollectionNullableEmpty { get; set; }
        public ICollection DoubleICollectionNullableEmpty { get; set; }
        public ICollection DecimalICollectionNullableEmpty { get; set; }
        public ICollection CharICollectionNullableEmpty { get; set; }
        public ICollection DateTimeICollectionNullableEmpty { get; set; }
        public ICollection DateTimeOffsetICollectionNullableEmpty { get; set; }
        public ICollection TimeSpanICollectionNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollectionNullableEmpty { get; set; }
        public ICollection TimeOnlyICollectionNullableEmpty { get; set; }
#endif
        public ICollection GuidICollectionNullableEmpty { get; set; }

        public ICollection BooleanICollectionNullableNull { get; set; }
        public ICollection ByteICollectionNullableNull { get; set; }
        public ICollection SByteICollectionNullableNull { get; set; }
        public ICollection Int16ICollectionNullableNull { get; set; }
        public ICollection UInt16ICollectionNullableNull { get; set; }
        public ICollection Int32ICollectionNullableNull { get; set; }
        public ICollection UInt32ICollectionNullableNull { get; set; }
        public ICollection Int64ICollectionNullableNull { get; set; }
        public ICollection UInt64ICollectionNullableNull { get; set; }
        public ICollection SingleICollectionNullableNull { get; set; }
        public ICollection DoubleICollectionNullableNull { get; set; }
        public ICollection DecimalICollectionNullableNull { get; set; }
        public ICollection CharICollectionNullableNull { get; set; }
        public ICollection DateTimeICollectionNullableNull { get; set; }
        public ICollection DateTimeOffsetICollectionNullableNull { get; set; }
        public ICollection TimeSpanICollectionNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public ICollection DateOnlyICollectionNullableNull { get; set; }
        public ICollection TimeOnlyICollectionNullableNull { get; set; }
#endif
        public ICollection GuidICollectionNullableNull { get; set; }

        public ICollection StringICollection { get; set; }
        public ICollection StringICollectionEmpty { get; set; }
        public ICollection StringICollectionNull { get; set; }

        public ICollection EnumICollection { get; set; }
        public ICollection EnumICollectionEmpty { get; set; }
        public ICollection EnumICollectionNull { get; set; }

        public ICollection EnumICollectionNullable { get; set; }
        public ICollection EnumICollectionNullableEmpty { get; set; }
        public ICollection EnumICollectionNullableNull { get; set; }

        #endregion

        #region IEnumerables

        public IEnumerable BooleanIEnumerable { get; set; }
        public IEnumerable ByteIEnumerable { get; set; }
        public IEnumerable SByteIEnumerable { get; set; }
        public IEnumerable Int16IEnumerable { get; set; }
        public IEnumerable UInt16IEnumerable { get; set; }
        public IEnumerable Int32IEnumerable { get; set; }
        public IEnumerable UInt32IEnumerable { get; set; }
        public IEnumerable Int64IEnumerable { get; set; }
        public IEnumerable UInt64IEnumerable { get; set; }
        public IEnumerable SingleIEnumerable { get; set; }
        public IEnumerable DoubleIEnumerable { get; set; }
        public IEnumerable DecimalIEnumerable { get; set; }
        public IEnumerable CharIEnumerable { get; set; }
        public IEnumerable DateTimeIEnumerable { get; set; }
        public IEnumerable DateTimeOffsetIEnumerable { get; set; }
        public IEnumerable TimeSpanIEnumerable { get; set; }
#if NET6_0_OR_GREATER
        public IEnumerable DateOnlyIEnumerable { get; set; }
        public IEnumerable TimeOnlyIEnumerable { get; set; }
#endif
        public IEnumerable GuidIEnumerable { get; set; }

        public IEnumerable BooleanIEnumerableEmpty { get; set; }
        public IEnumerable ByteIEnumerableEmpty { get; set; }
        public IEnumerable SByteIEnumerableEmpty { get; set; }
        public IEnumerable Int16IEnumerableEmpty { get; set; }
        public IEnumerable UInt16IEnumerableEmpty { get; set; }
        public IEnumerable Int32IEnumerableEmpty { get; set; }
        public IEnumerable UInt32IEnumerableEmpty { get; set; }
        public IEnumerable Int64IEnumerableEmpty { get; set; }
        public IEnumerable UInt64IEnumerableEmpty { get; set; }
        public IEnumerable SingleIEnumerableEmpty { get; set; }
        public IEnumerable DoubleIEnumerableEmpty { get; set; }
        public IEnumerable DecimalIEnumerableEmpty { get; set; }
        public IEnumerable CharIEnumerableEmpty { get; set; }
        public IEnumerable DateTimeIEnumerableEmpty { get; set; }
        public IEnumerable DateTimeOffsetIEnumerableEmpty { get; set; }
        public IEnumerable TimeSpanIEnumerableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IEnumerable DateOnlyIEnumerableEmpty { get; set; }
        public IEnumerable TimeOnlyIEnumerableEmpty { get; set; }
#endif
        public IEnumerable GuidIEnumerableEmpty { get; set; }

        public IEnumerable BooleanIEnumerableNull { get; set; }
        public IEnumerable ByteIEnumerableNull { get; set; }
        public IEnumerable SByteIEnumerableNull { get; set; }
        public IEnumerable Int16IEnumerableNull { get; set; }
        public IEnumerable UInt16IEnumerableNull { get; set; }
        public IEnumerable Int32IEnumerableNull { get; set; }
        public IEnumerable UInt32IEnumerableNull { get; set; }
        public IEnumerable Int64IEnumerableNull { get; set; }
        public IEnumerable UInt64IEnumerableNull { get; set; }
        public IEnumerable SingleIEnumerableNull { get; set; }
        public IEnumerable DoubleIEnumerableNull { get; set; }
        public IEnumerable DecimalIEnumerableNull { get; set; }
        public IEnumerable CharIEnumerableNull { get; set; }
        public IEnumerable DateTimeIEnumerableNull { get; set; }
        public IEnumerable DateTimeOffsetIEnumerableNull { get; set; }
        public IEnumerable TimeSpanIEnumerableNull { get; set; }
#if NET6_0_OR_GREATER
        public IEnumerable DateOnlyIEnumerableNull { get; set; }
        public IEnumerable TimeOnlyIEnumerableNull { get; set; }
#endif
        public IEnumerable GuidIEnumerableNull { get; set; }

        public IEnumerable BooleanIEnumerableNullable { get; set; }
        public IEnumerable ByteIEnumerableNullable { get; set; }
        public IEnumerable SByteIEnumerableNullable { get; set; }
        public IEnumerable Int16IEnumerableNullable { get; set; }
        public IEnumerable UInt16IEnumerableNullable { get; set; }
        public IEnumerable Int32IEnumerableNullable { get; set; }
        public IEnumerable UInt32IEnumerableNullable { get; set; }
        public IEnumerable Int64IEnumerableNullable { get; set; }
        public IEnumerable UInt64IEnumerableNullable { get; set; }
        public IEnumerable SingleIEnumerableNullable { get; set; }
        public IEnumerable DoubleIEnumerableNullable { get; set; }
        public IEnumerable DecimalIEnumerableNullable { get; set; }
        public IEnumerable CharIEnumerableNullable { get; set; }
        public IEnumerable DateTimeIEnumerableNullable { get; set; }
        public IEnumerable DateTimeOffsetIEnumerableNullable { get; set; }
        public IEnumerable TimeSpanIEnumerableNullable { get; set; }
#if NET6_0_OR_GREATER
        public IEnumerable DateOnlyIEnumerableNullable { get; set; }
        public IEnumerable TimeOnlyIEnumerableNullable { get; set; }
#endif
        public IEnumerable GuidIEnumerableNullable { get; set; }

        public IEnumerable BooleanIEnumerableNullableEmpty { get; set; }
        public IEnumerable ByteIEnumerableNullableEmpty { get; set; }
        public IEnumerable SByteIEnumerableNullableEmpty { get; set; }
        public IEnumerable Int16IEnumerableNullableEmpty { get; set; }
        public IEnumerable UInt16IEnumerableNullableEmpty { get; set; }
        public IEnumerable Int32IEnumerableNullableEmpty { get; set; }
        public IEnumerable UInt32IEnumerableNullableEmpty { get; set; }
        public IEnumerable Int64IEnumerableNullableEmpty { get; set; }
        public IEnumerable UInt64IEnumerableNullableEmpty { get; set; }
        public IEnumerable SingleIEnumerableNullableEmpty { get; set; }
        public IEnumerable DoubleIEnumerableNullableEmpty { get; set; }
        public IEnumerable DecimalIEnumerableNullableEmpty { get; set; }
        public IEnumerable CharIEnumerableNullableEmpty { get; set; }
        public IEnumerable DateTimeIEnumerableNullableEmpty { get; set; }
        public IEnumerable DateTimeOffsetIEnumerableNullableEmpty { get; set; }
        public IEnumerable TimeSpanIEnumerableNullableEmpty { get; set; }
#if NET6_0_OR_GREATER
        public IEnumerable DateOnlyIEnumerableNullableEmpty { get; set; }
        public IEnumerable TimeOnlyIEnumerableNullableEmpty { get; set; }
#endif
        public IEnumerable GuidIEnumerableNullableEmpty { get; set; }

        public IEnumerable BooleanIEnumerableNullableNull { get; set; }
        public IEnumerable ByteIEnumerableNullableNull { get; set; }
        public IEnumerable SByteIEnumerableNullableNull { get; set; }
        public IEnumerable Int16IEnumerableNullableNull { get; set; }
        public IEnumerable UInt16IEnumerableNullableNull { get; set; }
        public IEnumerable Int32IEnumerableNullableNull { get; set; }
        public IEnumerable UInt32IEnumerableNullableNull { get; set; }
        public IEnumerable Int64IEnumerableNullableNull { get; set; }
        public IEnumerable UInt64IEnumerableNullableNull { get; set; }
        public IEnumerable SingleIEnumerableNullableNull { get; set; }
        public IEnumerable DoubleIEnumerableNullableNull { get; set; }
        public IEnumerable DecimalIEnumerableNullableNull { get; set; }
        public IEnumerable CharIEnumerableNullableNull { get; set; }
        public IEnumerable DateTimeIEnumerableNullableNull { get; set; }
        public IEnumerable DateTimeOffsetIEnumerableNullableNull { get; set; }
        public IEnumerable TimeSpanIEnumerableNullableNull { get; set; }
#if NET6_0_OR_GREATER
        public IEnumerable DateOnlyIEnumerableNullableNull { get; set; }
        public IEnumerable TimeOnlyIEnumerableNullableNull { get; set; }
#endif
        public IEnumerable GuidIEnumerableNullableNull { get; set; }

        public IEnumerable StringIEnumerable { get; set; }
        public IEnumerable StringIEnumerableEmpty { get; set; }
        public IEnumerable StringIEnumerableNull { get; set; }

        public IEnumerable EnumIEnumerable { get; set; }
        public IEnumerable EnumIEnumerableEmpty { get; set; }
        public IEnumerable EnumIEnumerableNull { get; set; }

        public IEnumerable EnumIEnumerableNullable { get; set; }
        public IEnumerable EnumIEnumerableNullableEmpty { get; set; }
        public IEnumerable EnumIEnumerableNullableNull { get; set; }

        #endregion

        #region Dictionaries

        public IDictionary DictionaryThing1 { get; set; }
        public IDictionary DictionaryThing2 { get; set; }

        #endregion

        public static TypesBoxedCollectionsModel Create()
        {
            var model = new TypesBoxedCollectionsModel()
            {
                #region ILists

                BooleanIList = new List<bool>() { true, false, true },
                ByteIList = new List<byte>() { 1, 2, 3 },
                SByteIList = new List<sbyte>() { 4, 5, 6 },
                Int16IList = new List<short>() { 7, 8, 9 },
                UInt16IList = new List<ushort>() { 10, 11, 12 },
                Int32IList = new List<int>() { 13, 14, 15 },
                UInt32IList = new List<uint>() { 16, 17, 18 },
                Int64IList = new List<long>() { 19, 20, 21 },
                UInt64IList = new List<ulong>() { 22, 23, 24 },
                SingleIList = new List<float>() { 25, 26, 27 },
                DoubleIList = new List<double>() { 28, 29, 30 },
                DecimalIList = new List<decimal>() { 31, 32, 33 },
                CharIList = new List<char>() { 'A', 'B', 'C' },
                DateTimeIList = new List<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIList = new List<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIList = new List<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIList = new List<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIList = new List<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIList = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanIListEmpty = new List<bool>(0),
                ByteIListEmpty = new List<byte>(0),
                SByteIListEmpty = new List<sbyte>(0),
                Int16IListEmpty = new List<short>(0),
                UInt16IListEmpty = new List<ushort>(0),
                Int32IListEmpty = new List<int>(0),
                UInt32IListEmpty = new List<uint>(0),
                Int64IListEmpty = new List<long>(0),
                UInt64IListEmpty = new List<ulong>(0),
                SingleIListEmpty = new List<float>(0),
                DoubleIListEmpty = new List<double>(0),
                DecimalIListEmpty = new List<decimal>(0),
                CharIListEmpty = new List<char>(0),
                DateTimeIListEmpty = new List<DateTime>(0),
                DateTimeOffsetIListEmpty = new List<DateTimeOffset>(0),
                TimeSpanIListEmpty = new List<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyIListEmpty = new List<DateOnly>(0),
                TimeOnlyIListEmpty = new List<TimeOnly>(0),
#endif
                GuidIListEmpty = new List<Guid>(0),

                BooleanIListNull = null,
                ByteIListNull = null,
                SByteIListNull = null,
                Int16IListNull = null,
                UInt16IListNull = null,
                Int32IListNull = null,
                UInt32IListNull = null,
                Int64IListNull = null,
                UInt64IListNull = null,
                SingleIListNull = null,
                DoubleIListNull = null,
                DecimalIListNull = null,
                CharIListNull = null,
                DateTimeIListNull = null,
                DateTimeOffsetIListNull = null,
                TimeSpanIListNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIListNull = null,
                TimeOnlyIListNull = null,
#endif
                GuidIListNull = null,

                BooleanIListNullable = new List<bool?>() { true, null, true },
                ByteIListNullable = new List<byte?>() { 1, null, 3 },
                SByteIListNullable = new List<sbyte?>() { 4, null, 6 },
                Int16IListNullable = new List<short?>() { 7, null, 9 },
                UInt16IListNullable = new List<ushort?>() { 10, null, 12 },
                Int32IListNullable = new List<int?>() { 13, null, 15 },
                UInt32IListNullable = new List<uint?>() { 16, null, 18 },
                Int64IListNullable = new List<long?>() { 19, null, 21 },
                UInt64IListNullable = new List<ulong?>() { 22, null, 24 },
                SingleIListNullable = new List<float?>() { 25, null, 27 },
                DoubleIListNullable = new List<double?>() { 28, null, 30 },
                DecimalIListNullable = new List<decimal?>() { 31, null, 33 },
                CharIListNullable = new List<char?>() { 'A', null, 'C' },
                DateTimeIListNullable = new List<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIListNullable = new List<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIListNullable = new List<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIListNullable = new List<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIListNullable = new List<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIListNullable = new List<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanIListNullableEmpty = new List<bool?>(0),
                ByteIListNullableEmpty = new List<byte?>(0),
                SByteIListNullableEmpty = new List<sbyte?>(0),
                Int16IListNullableEmpty = new List<short?>(0),
                UInt16IListNullableEmpty = new List<ushort?>(0),
                Int32IListNullableEmpty = new List<int?>(0),
                UInt32IListNullableEmpty = new List<uint?>(0),
                Int64IListNullableEmpty = new List<long?>(0),
                UInt64IListNullableEmpty = new List<ulong?>(0),
                SingleIListNullableEmpty = new List<float?>(0),
                DoubleIListNullableEmpty = new List<double?>(0),
                DecimalIListNullableEmpty = new List<decimal?>(0),
                CharIListNullableEmpty = new List<char?>(0),
                DateTimeIListNullableEmpty = new List<DateTime?>(0),
                DateTimeOffsetIListNullableEmpty = new List<DateTimeOffset?>(0),
                TimeSpanIListNullableEmpty = new List<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyIListNullableEmpty = new List<DateOnly?>(0),
                TimeOnlyIListNullableEmpty = new List<TimeOnly?>(0),
#endif
                GuidIListNullableEmpty = new List<Guid?>(0),

                BooleanIListNullableNull = null,
                ByteIListNullableNull = null,
                SByteIListNullableNull = null,
                Int16IListNullableNull = null,
                UInt16IListNullableNull = null,
                Int32IListNullableNull = null,
                UInt32IListNullableNull = null,
                Int64IListNullableNull = null,
                UInt64IListNullableNull = null,
                SingleIListNullableNull = null,
                DoubleIListNullableNull = null,
                DecimalIListNullableNull = null,
                CharIListNullableNull = null,
                DateTimeIListNullableNull = null,
                DateTimeOffsetIListNullableNull = null,
                TimeSpanIListNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIListNullableNull = null,
                TimeOnlyIListNullableNull = null,
#endif
                GuidIListNullableNull = null,

                StringIList = new List<string>() { "Hello", "World", null, "", "People" },
                StringIListEmpty = new List<string>(0),
                StringIListNull = null,

                EnumIList = new List<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumIListEmpty = new List<EnumModel>(0),
                EnumIListNull = null,

                EnumIListNullable = new List<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumIListNullableEmpty = new List<EnumModel?>(0),
                EnumIListNullableNull = null,

                #endregion

                #region ICollections

                BooleanICollection = new List<bool>() { true, false, true },
                ByteICollection = new List<byte>() { 1, 2, 3 },
                SByteICollection = new List<sbyte>() { 4, 5, 6 },
                Int16ICollection = new List<short>() { 7, 8, 9 },
                UInt16ICollection = new List<ushort>() { 10, 11, 12 },
                Int32ICollection = new List<int>() { 13, 14, 15 },
                UInt32ICollection = new List<uint>() { 16, 17, 18 },
                Int64ICollection = new List<long>() { 19, 20, 21 },
                UInt64ICollection = new List<ulong>() { 22, 23, 24 },
                SingleICollection = new List<float>() { 25, 26, 27 },
                DoubleICollection = new List<double>() { 28, 29, 30 },
                DecimalICollection = new List<decimal>() { 31, 32, 33 },
                CharICollection = new List<char>() { 'A', 'B', 'C' },
                DateTimeICollection = new List<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetICollection = new List<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanICollection = new List<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyICollection = new List<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyICollection = new List<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidICollection = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanICollectionEmpty = new List<bool>(0),
                ByteICollectionEmpty = new List<byte>(0),
                SByteICollectionEmpty = new List<sbyte>(0),
                Int16ICollectionEmpty = new List<short>(0),
                UInt16ICollectionEmpty = new List<ushort>(0),
                Int32ICollectionEmpty = new List<int>(0),
                UInt32ICollectionEmpty = new List<uint>(0),
                Int64ICollectionEmpty = new List<long>(0),
                UInt64ICollectionEmpty = new List<ulong>(0),
                SingleICollectionEmpty = new List<float>(0),
                DoubleICollectionEmpty = new List<double>(0),
                DecimalICollectionEmpty = new List<decimal>(0),
                CharICollectionEmpty = new List<char>(0),
                DateTimeICollectionEmpty = new List<DateTime>(0),
                DateTimeOffsetICollectionEmpty = new List<DateTimeOffset>(0),
                TimeSpanICollectionEmpty = new List<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyICollectionEmpty = new List<DateOnly>(0),
                TimeOnlyICollectionEmpty = new List<TimeOnly>(0),
#endif
                GuidICollectionEmpty = new List<Guid>(0),

                BooleanICollectionNull = null,
                ByteICollectionNull = null,
                SByteICollectionNull = null,
                Int16ICollectionNull = null,
                UInt16ICollectionNull = null,
                Int32ICollectionNull = null,
                UInt32ICollectionNull = null,
                Int64ICollectionNull = null,
                UInt64ICollectionNull = null,
                SingleICollectionNull = null,
                DoubleICollectionNull = null,
                DecimalICollectionNull = null,
                CharICollectionNull = null,
                DateTimeICollectionNull = null,
                DateTimeOffsetICollectionNull = null,
                TimeSpanICollectionNull = null,
#if NET6_0_OR_GREATER
                DateOnlyICollectionNull = null,
                TimeOnlyICollectionNull = null,
#endif
                GuidICollectionNull = null,

                BooleanICollectionNullable = new List<bool?>() { true, null, true },
                ByteICollectionNullable = new List<byte?>() { 1, null, 3 },
                SByteICollectionNullable = new List<sbyte?>() { 4, null, 6 },
                Int16ICollectionNullable = new List<short?>() { 7, null, 9 },
                UInt16ICollectionNullable = new List<ushort?>() { 10, null, 12 },
                Int32ICollectionNullable = new List<int?>() { 13, null, 15 },
                UInt32ICollectionNullable = new List<uint?>() { 16, null, 18 },
                Int64ICollectionNullable = new List<long?>() { 19, null, 21 },
                UInt64ICollectionNullable = new List<ulong?>() { 22, null, 24 },
                SingleICollectionNullable = new List<float?>() { 25, null, 27 },
                DoubleICollectionNullable = new List<double?>() { 28, null, 30 },
                DecimalICollectionNullable = new List<decimal?>() { 31, null, 33 },
                CharICollectionNullable = new List<char?>() { 'A', null, 'C' },
                DateTimeICollectionNullable = new List<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetICollectionNullable = new List<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanICollectionNullable = new List<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyICollectionNullable = new List<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyICollectionNullable = new List<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidICollectionNullable = new List<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanICollectionNullableEmpty = new List<bool?>(0),
                ByteICollectionNullableEmpty = new List<byte?>(0),
                SByteICollectionNullableEmpty = new List<sbyte?>(0),
                Int16ICollectionNullableEmpty = new List<short?>(0),
                UInt16ICollectionNullableEmpty = new List<ushort?>(0),
                Int32ICollectionNullableEmpty = new List<int?>(0),
                UInt32ICollectionNullableEmpty = new List<uint?>(0),
                Int64ICollectionNullableEmpty = new List<long?>(0),
                UInt64ICollectionNullableEmpty = new List<ulong?>(0),
                SingleICollectionNullableEmpty = new List<float?>(0),
                DoubleICollectionNullableEmpty = new List<double?>(0),
                DecimalICollectionNullableEmpty = new List<decimal?>(0),
                CharICollectionNullableEmpty = new List<char?>(0),
                DateTimeICollectionNullableEmpty = new List<DateTime?>(0),
                DateTimeOffsetICollectionNullableEmpty = new List<DateTimeOffset?>(0),
                TimeSpanICollectionNullableEmpty = new List<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyICollectionNullableEmpty = new List<DateOnly?>(0),
                TimeOnlyICollectionNullableEmpty = new List<TimeOnly?>(0),
#endif
                GuidICollectionNullableEmpty = new List<Guid?>(0),

                BooleanICollectionNullableNull = null,
                ByteICollectionNullableNull = null,
                SByteICollectionNullableNull = null,
                Int16ICollectionNullableNull = null,
                UInt16ICollectionNullableNull = null,
                Int32ICollectionNullableNull = null,
                UInt32ICollectionNullableNull = null,
                Int64ICollectionNullableNull = null,
                UInt64ICollectionNullableNull = null,
                SingleICollectionNullableNull = null,
                DoubleICollectionNullableNull = null,
                DecimalICollectionNullableNull = null,
                CharICollectionNullableNull = null,
                DateTimeICollectionNullableNull = null,
                DateTimeOffsetICollectionNullableNull = null,
                TimeSpanICollectionNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyICollectionNullableNull = null,
                TimeOnlyICollectionNullableNull = null,
#endif
                GuidICollectionNullableNull = null,

                StringICollection = new List<string>() { "Hello", "World", null, "", "People" },
                StringICollectionEmpty = new List<string>(0),
                StringICollectionNull = null,

                EnumICollection = new List<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumICollectionEmpty = new List<EnumModel>(0),
                EnumICollectionNull = null,

                EnumICollectionNullable = new List<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumICollectionNullableEmpty = new List<EnumModel?>(0),
                EnumICollectionNullableNull = null,

                #endregion

                #region IEnumerables

                BooleanIEnumerable = new List<bool>() { true, false, true },
                ByteIEnumerable = new List<byte>() { 1, 2, 3 },
                SByteIEnumerable = new List<sbyte>() { 4, 5, 6 },
                Int16IEnumerable = new List<short>() { 7, 8, 9 },
                UInt16IEnumerable = new List<ushort>() { 10, 11, 12 },
                Int32IEnumerable = new List<int>() { 13, 14, 15 },
                UInt32IEnumerable = new List<uint>() { 16, 17, 18 },
                Int64IEnumerable = new List<long>() { 19, 20, 21 },
                UInt64IEnumerable = new List<ulong>() { 22, 23, 24 },
                SingleIEnumerable = new List<float>() { 25, 26, 27 },
                DoubleIEnumerable = new List<double>() { 28, 29, 30 },
                DecimalIEnumerable = new List<decimal>() { 31, 32, 33 },
                CharIEnumerable = new List<char>() { 'A', 'B', 'C' },
                DateTimeIEnumerable = new List<DateTime>() { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIEnumerable = new List<DateTimeOffset>() { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIEnumerable = new List<TimeSpan>() { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIEnumerable = new List<DateOnly>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIEnumerable = new List<TimeOnly>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIEnumerable = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanIEnumerableEmpty = new List<bool>(0),
                ByteIEnumerableEmpty = new List<byte>(0),
                SByteIEnumerableEmpty = new List<sbyte>(0),
                Int16IEnumerableEmpty = new List<short>(0),
                UInt16IEnumerableEmpty = new List<ushort>(0),
                Int32IEnumerableEmpty = new List<int>(0),
                UInt32IEnumerableEmpty = new List<uint>(0),
                Int64IEnumerableEmpty = new List<long>(0),
                UInt64IEnumerableEmpty = new List<ulong>(0),
                SingleIEnumerableEmpty = new List<float>(0),
                DoubleIEnumerableEmpty = new List<double>(0),
                DecimalIEnumerableEmpty = new List<decimal>(0),
                CharIEnumerableEmpty = new List<char>(0),
                DateTimeIEnumerableEmpty = new List<DateTime>(0),
                DateTimeOffsetIEnumerableEmpty = new List<DateTimeOffset>(0),
                TimeSpanIEnumerableEmpty = new List<TimeSpan>(0),
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableEmpty = new List<DateOnly>(0),
                TimeOnlyIEnumerableEmpty = new List<TimeOnly>(0),
#endif
                GuidIEnumerableEmpty = new List<Guid>(0),

                BooleanIEnumerableNull = null,
                ByteIEnumerableNull = null,
                SByteIEnumerableNull = null,
                Int16IEnumerableNull = null,
                UInt16IEnumerableNull = null,
                Int32IEnumerableNull = null,
                UInt32IEnumerableNull = null,
                Int64IEnumerableNull = null,
                UInt64IEnumerableNull = null,
                SingleIEnumerableNull = null,
                DoubleIEnumerableNull = null,
                DecimalIEnumerableNull = null,
                CharIEnumerableNull = null,
                DateTimeIEnumerableNull = null,
                DateTimeOffsetIEnumerableNull = null,
                TimeSpanIEnumerableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableNull = null,
                TimeOnlyIEnumerableNull = null,
#endif
                GuidIEnumerableNull = null,

                BooleanIEnumerableNullable = new List<bool?>() { true, null, true },
                ByteIEnumerableNullable = new List<byte?>() { 1, null, 3 },
                SByteIEnumerableNullable = new List<sbyte?>() { 4, null, 6 },
                Int16IEnumerableNullable = new List<short?>() { 7, null, 9 },
                UInt16IEnumerableNullable = new List<ushort?>() { 10, null, 12 },
                Int32IEnumerableNullable = new List<int?>() { 13, null, 15 },
                UInt32IEnumerableNullable = new List<uint?>() { 16, null, 18 },
                Int64IEnumerableNullable = new List<long?>() { 19, null, 21 },
                UInt64IEnumerableNullable = new List<ulong?>() { 22, null, 24 },
                SingleIEnumerableNullable = new List<float?>() { 25, null, 27 },
                DoubleIEnumerableNullable = new List<double?>() { 28, null, 30 },
                DecimalIEnumerableNullable = new List<decimal?>() { 31, null, 33 },
                CharIEnumerableNullable = new List<char?>() { 'A', null, 'C' },
                DateTimeIEnumerableNullable = new List<DateTime?>() { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIEnumerableNullable = new List<DateTimeOffset?>() { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIEnumerableNullable = new List<TimeSpan?>() { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableNullable = new List<DateOnly?>() { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIEnumerableNullable = new List<TimeOnly?>() { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIEnumerableNullable = new List<Guid?>() { Guid.NewGuid(), null, Guid.NewGuid() },

                BooleanIEnumerableNullableEmpty = new List<bool?>(0),
                ByteIEnumerableNullableEmpty = new List<byte?>(0),
                SByteIEnumerableNullableEmpty = new List<sbyte?>(0),
                Int16IEnumerableNullableEmpty = new List<short?>(0),
                UInt16IEnumerableNullableEmpty = new List<ushort?>(0),
                Int32IEnumerableNullableEmpty = new List<int?>(0),
                UInt32IEnumerableNullableEmpty = new List<uint?>(0),
                Int64IEnumerableNullableEmpty = new List<long?>(0),
                UInt64IEnumerableNullableEmpty = new List<ulong?>(0),
                SingleIEnumerableNullableEmpty = new List<float?>(0),
                DoubleIEnumerableNullableEmpty = new List<double?>(0),
                DecimalIEnumerableNullableEmpty = new List<decimal?>(0),
                CharIEnumerableNullableEmpty = new List<char?>(0),
                DateTimeIEnumerableNullableEmpty = new List<DateTime?>(0),
                DateTimeOffsetIEnumerableNullableEmpty = new List<DateTimeOffset?>(0),
                TimeSpanIEnumerableNullableEmpty = new List<TimeSpan?>(0),
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableNullableEmpty = new List<DateOnly?>(0),
                TimeOnlyIEnumerableNullableEmpty = new List<TimeOnly?>(0),
#endif
                GuidIEnumerableNullableEmpty = new List<Guid?>(0),

                BooleanIEnumerableNullableNull = null,
                ByteIEnumerableNullableNull = null,
                SByteIEnumerableNullableNull = null,
                Int16IEnumerableNullableNull = null,
                UInt16IEnumerableNullableNull = null,
                Int32IEnumerableNullableNull = null,
                UInt32IEnumerableNullableNull = null,
                Int64IEnumerableNullableNull = null,
                UInt64IEnumerableNullableNull = null,
                SingleIEnumerableNullableNull = null,
                DoubleIEnumerableNullableNull = null,
                DecimalIEnumerableNullableNull = null,
                CharIEnumerableNullableNull = null,
                DateTimeIEnumerableNullableNull = null,
                DateTimeOffsetIEnumerableNullableNull = null,
                TimeSpanIEnumerableNullableNull = null,
#if NET6_0_OR_GREATER
                DateOnlyIEnumerableNullableNull = null,
                TimeOnlyIEnumerableNullableNull = null,
#endif
                GuidIEnumerableNullableNull = null,

                StringIEnumerable = new List<string>() { "Hello", "World", null, "", "People" },
                StringIEnumerableEmpty = new List<string>(0),
                StringIEnumerableNull = null,

                EnumIEnumerable = new List<EnumModel>() { EnumModel.EnumItem1, EnumModel.EnumItem2, EnumModel.EnumItem3 },
                EnumIEnumerableEmpty = new List<EnumModel>(0),
                EnumIEnumerableNull = null,

                EnumIEnumerableNullable = new List<EnumModel?>() { EnumModel.EnumItem1, null, EnumModel.EnumItem3 },
                EnumIEnumerableNullableEmpty = new List<EnumModel?>(0),
                EnumIEnumerableNullableNull = null,

                #endregion

                #region Dictionaries

                DictionaryThing1 = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing2 = new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } },

                #endregion
            };
            return model;
        }
    }
}