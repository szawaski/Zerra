// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Test
{
    public class HashSetModel
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

        public HashSet<string> StringHashSet { get; set; }

        public HashSet<SimpleModel> ClassHashSet { get; set; }

        public ISet<bool> BooleanISet { get; set; }
        public ISet<byte> ByteISet { get; set; }
        public ISet<sbyte> SByteISet { get; set; }
        public ISet<short> Int16ISet { get; set; }
        public ISet<ushort> UInt16ISet { get; set; }
        public ISet<int> Int32ISet { get; set; }
        public ISet<uint> UInt32ISet { get; set; }
        public ISet<long> Int64ISet { get; set; }
        public ISet<ulong> UInt64ISet { get; set; }
        public ISet<float> SingleISet { get; set; }
        public ISet<double> DoubleISet { get; set; }
        public ISet<decimal> DecimalISet { get; set; }
        public ISet<char> CharISet { get; set; }
        public ISet<DateTime> DateTimeISet { get; set; }
        public ISet<DateTimeOffset> DateTimeOffsetISet { get; set; }
        public ISet<TimeSpan> TimeSpanISet { get; set; }
#if NET6_0_OR_GREATER
        public ISet<DateOnly> DateOnlyISet { get; set; }
        public ISet<TimeOnly> TimeOnlyISet { get; set; }
#endif
        public ISet<Guid> GuidISet { get; set; }

        public ISet<bool?> BooleanISetNullable { get; set; }
        public ISet<byte?> ByteISetNullable { get; set; }
        public ISet<sbyte?> SByteISetNullable { get; set; }
        public ISet<short?> Int16ISetNullable { get; set; }
        public ISet<ushort?> UInt16ISetNullable { get; set; }
        public ISet<int?> Int32ISetNullable { get; set; }
        public ISet<uint?> UInt32ISetNullable { get; set; }
        public ISet<long?> Int64ISetNullable { get; set; }
        public ISet<ulong?> UInt64ISetNullable { get; set; }
        public ISet<float?> SingleISetNullable { get; set; }
        public ISet<double?> DoubleISetNullable { get; set; }
        public ISet<decimal?> DecimalISetNullable { get; set; }
        public ISet<char?> CharISetNullable { get; set; }
        public ISet<DateTime?> DateTimeISetNullable { get; set; }
        public ISet<DateTimeOffset?> DateTimeOffsetISetNullable { get; set; }
        public ISet<TimeSpan?> TimeSpanISetNullable { get; set; }
#if NET6_0_OR_GREATER
        public ISet<DateOnly?> DateOnlyISetNullable { get; set; }
        public ISet<TimeOnly?> TimeOnlyISetNullable { get; set; }
#endif
        public ISet<Guid?> GuidISetNullable { get; set; }

        public ISet<string> StringISet { get; set; }

        public ISet<SimpleModel> ClassISet { get; set; }

#if NET5_0_OR_GREATER
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

        public IReadOnlySet<string> StringIReadOnlySet { get; set; }

        public IReadOnlySet<SimpleModel> ClassIReadOnlySet { get; set; }
#endif

        public static HashSetModel Create()
        {
            var model = new HashSetModel()
            {
                BooleanHashSet = new HashSet<bool> { true, false, true },
                ByteHashSet = new HashSet<byte> { 1, 2, 3 },
                SByteHashSet = new HashSet<sbyte> { 4, 5, 6 },
                Int16HashSet = new HashSet<short> { 7, 8, 9 },
                UInt16HashSet = new HashSet<ushort> { 10, 11, 12 },
                Int32HashSet = new HashSet<int> { 13, 14, 15 },
                UInt32HashSet = new HashSet<uint> { 16, 17, 18 },
                Int64HashSet = new HashSet<long> { 19, 20, 21 },
                UInt64HashSet = new HashSet<ulong> { 22, 23, 24 },
                SingleHashSet = new HashSet<float> { 25, 26, 27 },
                DoubleHashSet = new HashSet<double> { 28, 29, 30 },
                DecimalHashSet = new HashSet<decimal> { 31, 32, 33 },
                CharHashSet = new HashSet<char> { 'A', 'B', 'C' },
                DateTimeHashSet = new HashSet<DateTime> { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetHashSet = new HashSet<DateTimeOffset> { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanHashSet = new HashSet<TimeSpan> { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyHashSet = new HashSet<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyHashSet = new HashSet<TimeOnly> { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidHashSet = new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanHashSetNullable = new HashSet<bool?> { true, null, true },
                ByteHashSetNullable = new HashSet<byte?> { 1, null, 3 },
                SByteHashSetNullable = new HashSet<sbyte?> { 4, null, 6 },
                Int16HashSetNullable = new HashSet<short?> { 7, null, 9 },
                UInt16HashSetNullable = new HashSet<ushort?> { 10, null, 12 },
                Int32HashSetNullable = new HashSet<int?> { 13, null, 15 },
                UInt32HashSetNullable = new HashSet<uint?> { 16, null, 18 },
                Int64HashSetNullable = new HashSet<long?> { 19, null, 21 },
                UInt64HashSetNullable = new HashSet<ulong?> { 22, null, 24 },
                SingleHashSetNullable = new HashSet<float?> { 25, null, 27 },
                DoubleHashSetNullable = new HashSet<double?> { 28, null, 30 },
                DecimalHashSetNullable = new HashSet<decimal?> { 31, null, 33 },
                CharHashSetNullable = new HashSet<char?> { 'A', null, 'C' },
                DateTimeHashSetNullable = new HashSet<DateTime?> { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetHashSetNullable = new HashSet<DateTimeOffset?> { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanHashSetNullable = new HashSet<TimeSpan?> { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyHashSetNullable = new HashSet<DateOnly?> { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyHashSetNullable = new HashSet<TimeOnly?> { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidHashSetNullable = new HashSet<Guid?> { Guid.NewGuid(), null, Guid.NewGuid() },

                StringHashSet = new HashSet<string> { "Hello", "World", "People" },

                ClassHashSet = new HashSet<SimpleModel> { new() { Value1 = 1, Value2 = "1" }, new() { Value1 = 2, Value2 = "2" }, null, new() { Value1 = 3, Value2 = "3" } },

                BooleanISet = new HashSet<bool> { true, false, true },
                ByteISet = new HashSet<byte> { 1, 2, 3 },
                SByteISet = new HashSet<sbyte> { 4, 5, 6 },
                Int16ISet = new HashSet<short> { 7, 8, 9 },
                UInt16ISet = new HashSet<ushort> { 10, 11, 12 },
                Int32ISet = new HashSet<int> { 13, 14, 15 },
                UInt32ISet = new HashSet<uint> { 16, 17, 18 },
                Int64ISet = new HashSet<long> { 19, 20, 21 },
                UInt64ISet = new HashSet<ulong> { 22, 23, 24 },
                SingleISet = new HashSet<float> { 25, 26, 27 },
                DoubleISet = new HashSet<double> { 28, 29, 30 },
                DecimalISet = new HashSet<decimal> { 31, 32, 33 },
                CharISet = new HashSet<char> { 'A', 'B', 'C' },
                DateTimeISet = new HashSet<DateTime> { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetISet = new HashSet<DateTimeOffset> { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanISet = new HashSet<TimeSpan> { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyISet = new HashSet<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyISet = new HashSet<TimeOnly> { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidISet = new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanISetNullable = new HashSet<bool?> { true, null, true },
                ByteISetNullable = new HashSet<byte?> { 1, null, 3 },
                SByteISetNullable = new HashSet<sbyte?> { 4, null, 6 },
                Int16ISetNullable = new HashSet<short?> { 7, null, 9 },
                UInt16ISetNullable = new HashSet<ushort?> { 10, null, 12 },
                Int32ISetNullable = new HashSet<int?> { 13, null, 15 },
                UInt32ISetNullable = new HashSet<uint?> { 16, null, 18 },
                Int64ISetNullable = new HashSet<long?> { 19, null, 21 },
                UInt64ISetNullable = new HashSet<ulong?> { 22, null, 24 },
                SingleISetNullable = new HashSet<float?> { 25, null, 27 },
                DoubleISetNullable = new HashSet<double?> { 28, null, 30 },
                DecimalISetNullable = new HashSet<decimal?> { 31, null, 33 },
                CharISetNullable = new HashSet<char?> { 'A', null, 'C' },
                DateTimeISetNullable = new HashSet<DateTime?> { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetISetNullable = new HashSet<DateTimeOffset?> { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanISetNullable = new HashSet<TimeSpan?> { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyISetNullable = new HashSet<DateOnly?> { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyISetNullable = new HashSet<TimeOnly?> { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidISetNullable = new HashSet<Guid?> { Guid.NewGuid(), null, Guid.NewGuid() },

                StringISet = new HashSet<string> { "Hello", "World", "People" },

                ClassISet = new HashSet<SimpleModel> { new() { Value1 = 1, Value2 = "1" }, new() { Value1 = 2, Value2 = "2" }, null, new() { Value1 = 3, Value2 = "3" } },

#if NET5_0_OR_GREATER
                BooleanIReadOnlySet = new HashSet<bool> { true, false, true },
                ByteIReadOnlySet = new HashSet<byte> { 1, 2, 3 },
                SByteIReadOnlySet = new HashSet<sbyte> { 4, 5, 6 },
                Int16IReadOnlySet = new HashSet<short> { 7, 8, 9 },
                UInt16IReadOnlySet = new HashSet<ushort> { 10, 11, 12 },
                Int32IReadOnlySet = new HashSet<int> { 13, 14, 15 },
                UInt32IReadOnlySet = new HashSet<uint> { 16, 17, 18 },
                Int64IReadOnlySet = new HashSet<long> { 19, 20, 21 },
                UInt64IReadOnlySet = new HashSet<ulong> { 22, 23, 24 },
                SingleIReadOnlySet = new HashSet<float> { 25, 26, 27 },
                DoubleIReadOnlySet = new HashSet<double> { 28, 29, 30 },
                DecimalIReadOnlySet = new HashSet<decimal> { 31, 32, 33 },
                CharIReadOnlySet = new HashSet<char> { 'A', 'B', 'C' },
                DateTimeIReadOnlySet = new HashSet<DateTime> { DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(2), DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIReadOnlySet = new HashSet<DateTimeOffset> { DateTimeOffset.UtcNow.AddMonths(4), DateTimeOffset.UtcNow.AddMonths(5), DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIReadOnlySet = new HashSet<TimeSpan> { DateTime.UtcNow.AddHours(1).TimeOfDay, DateTime.UtcNow.AddHours(2).TimeOfDay, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlySet = new HashSet<DateOnly> { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIReadOnlySet = new HashSet<TimeOnly> { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)), TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIReadOnlySet = new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },

                BooleanIReadOnlySetNullable = new HashSet<bool?> { true, null, true },
                ByteIReadOnlySetNullable = new HashSet<byte?> { 1, null, 3 },
                SByteIReadOnlySetNullable = new HashSet<sbyte?> { 4, null, 6 },
                Int16IReadOnlySetNullable = new HashSet<short?> { 7, null, 9 },
                UInt16IReadOnlySetNullable = new HashSet<ushort?> { 10, null, 12 },
                Int32IReadOnlySetNullable = new HashSet<int?> { 13, null, 15 },
                UInt32IReadOnlySetNullable = new HashSet<uint?> { 16, null, 18 },
                Int64IReadOnlySetNullable = new HashSet<long?> { 19, null, 21 },
                UInt64IReadOnlySetNullable = new HashSet<ulong?> { 22, null, 24 },
                SingleIReadOnlySetNullable = new HashSet<float?> { 25, null, 27 },
                DoubleIReadOnlySetNullable = new HashSet<double?> { 28, null, 30 },
                DecimalIReadOnlySetNullable = new HashSet<decimal?> { 31, null, 33 },
                CharIReadOnlySetNullable = new HashSet<char?> { 'A', null, 'C' },
                DateTimeIReadOnlySetNullable = new HashSet<DateTime?> { DateTime.UtcNow.AddMonths(1), null, DateTime.UtcNow.AddMonths(3) },
                DateTimeOffsetIReadOnlySetNullable = new HashSet<DateTimeOffset?> { DateTimeOffset.UtcNow.AddMonths(4), null, DateTimeOffset.UtcNow.AddMonths(6) },
                TimeSpanIReadOnlySetNullable = new HashSet<TimeSpan?> { DateTime.UtcNow.AddHours(1).TimeOfDay, null, DateTime.UtcNow.AddHours(3).TimeOfDay },
#if NET6_0_OR_GREATER
                DateOnlyIReadOnlySetNullable = new HashSet<DateOnly?> { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)) },
                TimeOnlyIReadOnlySetNullable = new HashSet<TimeOnly?> { TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1)), null, TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) },
#endif
                GuidIReadOnlySetNullable = new HashSet<Guid?> { Guid.NewGuid(), null, Guid.NewGuid() },

                StringIReadOnlySet = new HashSet<string> { "Hello", "World", "People" },

                ClassIReadOnlySet = new HashSet<SimpleModel> { new() { Value1 = 1, Value2 = "1" }, new() { Value1 = 2, Value2 = "2" }, null, new() { Value1 = 3, Value2 = "3" } }
#endif
            };

            return model;
        }
    }
}
