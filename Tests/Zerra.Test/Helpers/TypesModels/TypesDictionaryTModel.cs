// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Helpers.TypesModels
{
    [Zerra.Reflection.GenerateTypeDetail]
    public class TypesDictionaryTModel
    {
        public Dictionary<int, string> DictionaryThing1 { get; set; }
        public Dictionary<int, SimpleModel> DictionaryThing2 { get; set; }
        public Dictionary<SimpleModel, int?> DictionaryThing3 { get; set; }
        public Dictionary<string, string> DictionaryThing4 { get; set; }
        public Dictionary<string, string> DictionaryThingEmpty { get; set; }
        public Dictionary<string, string> DictionaryThingNull { get; set; }
        public Dictionary<DateTime, int> DictionaryDateTimeKey { get; set; }
        public Dictionary<DateTimeOffset, int> DictionaryDateTimeOffsetKey { get; set; }
#if !NETSTANDARD2_0
        public Dictionary<DateOnly, int> DictionaryDateOnlyKey { get; set; }
        public Dictionary<TimeOnly, int> DictionaryTimeOnlyKey { get; set; }
#endif
        public Dictionary<TimeSpan, int> DictionaryTimeSpanKey { get; set; }
        public Dictionary<Guid, int> DictionaryGuidKey { get; set; }
        public Dictionary<double, int> DictionaryDoubleKey { get; set; }
        public Dictionary<decimal, int> DictionaryDecimalKey { get; set; }
        public Dictionary<long, int> DictionaryLongKey { get; set; }
        public Dictionary<ulong, int> DictionaryULongKey { get; set; }
        public Dictionary<bool, int> DictionaryBoolKey { get; set; }
        public Dictionary<char, int> DictionaryCharKey { get; set; }
        public Dictionary<EnumSignedModel, int> DictionaryEnumKey { get; set; }

        public static TypesDictionaryTModel Create()
        {
            var model = new TypesDictionaryTModel()
            {
                DictionaryThing1 = new Dictionary<int, string>() { { 1, "A" }, { 2, "B" }, { 3, "C" }, { 4, null } },
                DictionaryThing2 = new Dictionary<int, SimpleModel>() { { 1, new() { Value1 = 1, Value2 = "A" } }, { 2, new() { Value1 = 2, Value2 = "B" } }, { 3, new() { Value1 = 3, Value2 = "C" } }, { 4, null } },
                DictionaryThing3 = new Dictionary<SimpleModel, int?>() { { new() { Value1 = 1, Value2 = "A" }, 1 }, { new() { Value1 = 2, Value2 = "B" }, 2 }, { new() { Value1 = 3, Value2 = "C" }, 3 }, { new() { Value1 = 4, Value2 = "D" }, null } },
                DictionaryThing4 = new Dictionary<string, string>() { { "A", "1" }, { "B", "2" }, { "C", "3" }, { "D", null } },
                DictionaryThingEmpty = new Dictionary<string, string>(),
                DictionaryThingNull = null,
                DictionaryDateTimeKey = new() { { new DateTime(2026, 10, 6, 13, 45, 30, DateTimeKind.Utc).AddTicks(1234567), 1 }, { new DateTime(2026, 10, 6, 13, 45, 30, DateTimeKind.Unspecified), 2 } },
                DictionaryDateTimeOffsetKey = new() { { new DateTimeOffset(2026, 10, 6, 13, 45, 30, TimeSpan.FromHours(-5)), 1 }, { new DateTimeOffset(2026, 10, 6, 13, 45, 30, 123, TimeSpan.FromMinutes(330)), 2 } },
#if !NETSTANDARD2_0
                DictionaryDateOnlyKey = new() { { new DateOnly(2026, 10, 6), 1 }, { DateOnly.MinValue, 2 } },
                DictionaryTimeOnlyKey = new() { { new TimeOnly(13, 45, 30, 500), 1 }, { new TimeOnly(0, 0), 2 } },
#endif
                DictionaryTimeSpanKey = new() { { new TimeSpan(-1, 2, 3, 4, 500), 1 }, { TimeSpan.Zero, 2 } },
                DictionaryGuidKey = new() { { Guid.Parse("b755b826-d5cb-4d53-9879-2dca1b24dfa0"), 1 }, { Guid.Empty, 2 } },
                DictionaryDoubleKey = new() { { -1.5, 1 }, { 1234567.125, 2 } },
                DictionaryDecimalKey = new() { { -1.5m, 1 }, { 79228162514264337593543950335m, 2 } },
                DictionaryLongKey = new() { { long.MinValue, 1 }, { long.MaxValue, 2 } },
                DictionaryULongKey = new() { { 0, 1 }, { ulong.MaxValue, 2 } },
                DictionaryBoolKey = new() { { true, 1 }, { false, 2 } },
                DictionaryCharKey = new() { { 'Z', 1 }, { '"', 2 } },
                DictionaryEnumKey = new() { { EnumSignedModel.Negative, 1 }, { EnumSignedModel.Positive, 2 } },
            };
            return model;
        }
    }
}