// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Test
{
    public class EnumNameTests
    {
        public enum TestEnum
        {
            None = 0,
            Thing1 = 1,
            [EnumName("Thing 2")]
            Thing2 = 2,
            Thing3 = 3,
            thing3 = 4,
        }

        [Fact]
        public void GetName()
        {
            var test0 = EnumName.GetName(TestEnum.None);
            Assert.Equal("None", test0);

            var test1 = EnumName.GetName(TestEnum.Thing1);
            Assert.Equal("Thing1", test1);

            var test2 = EnumName.GetName(TestEnum.Thing2);
            Assert.Equal("Thing 2", test2);

            var test3 = EnumName.GetName(TestEnum.Thing3);
            Assert.Equal("Thing3", test3);

            var test4 = EnumName.GetName(TestEnum.thing3);
            Assert.Equal("thing3", test4);
        }

        [Fact]
        public void Parse()
        {
            var test0 = EnumName.Parse<TestEnum>("None");
            Assert.Equal(TestEnum.None, test0);

            var test1 = EnumName.Parse<TestEnum>("Thing1");
            Assert.Equal(TestEnum.Thing1, test1);

            var test2a = EnumName.Parse<TestEnum>("Thing 2");
            Assert.Equal(TestEnum.Thing2, test2a);

            var test2b = EnumName.Parse<TestEnum>("Thing2");
            Assert.Equal(TestEnum.Thing2, test2b);

            var test3 = EnumName.Parse<TestEnum>("Thing3");
            Assert.Equal(TestEnum.Thing3, test3);

            var test4 = EnumName.Parse<TestEnum>("thing3");
            Assert.Equal(TestEnum.thing3, test4);
        }

        [Flags]
        public enum TestFlagsEnum : int
        {
            None = 0,
            Thing1 = 65536,
            [EnumName("Thing 2")]
            Thing2 = 131072,
            [EnumName("Thing 3")]
            Thing3 = 262144,
            [EnumName("Thing 4")]
            Thing4 = 524288,
            [EnumName("Thing Duplicate")]
            ThingDuplicate = 1048576,
            [EnumName("Thing 5")]
            Thing5 = 1048576,
        }

        [Fact]
        public void GetNameFlags()
        {
            var test0 = EnumName.GetName(TestFlagsEnum.None);
            Assert.Equal("None", test0);

            var test01 = EnumName.GetName(TestFlagsEnum.None | TestFlagsEnum.Thing1);
            Assert.Equal("Thing1", test01);

            var test12 = EnumName.GetName(TestFlagsEnum.Thing1 | TestFlagsEnum.Thing2);
            Assert.Equal("Thing1|Thing 2", test12);

            var test123 = EnumName.GetName(TestFlagsEnum.Thing1 | TestFlagsEnum.Thing2 | TestFlagsEnum.Thing3);
            Assert.Equal("Thing1|Thing 2|Thing 3", test123);

            var test1235 = EnumName.GetName(TestFlagsEnum.Thing1 | TestFlagsEnum.Thing2 | TestFlagsEnum.Thing3 | TestFlagsEnum.Thing5);
            Assert.Equal("Thing1|Thing 2|Thing 3|Thing Duplicate|Thing 5", test1235);

            Assert.Equal("Thing5", TestFlagsEnum.ThingDuplicate.ToString()); //Last assigned takes priority
            var testDuplicate = EnumName.GetName(TestFlagsEnum.ThingDuplicate);
            Assert.Equal("Thing 5", testDuplicate);
        }

        [Fact]
        public void ParseFlags()
        {
            var test0 = EnumName.Parse<TestFlagsEnum>("None");
            Assert.True(test0.HasFlag(TestFlagsEnum.None));

            var test01 = EnumName.Parse<TestFlagsEnum>("None|Thing1");
            Assert.True(test01.HasFlag(TestFlagsEnum.Thing1));

            var test12 = EnumName.Parse<TestFlagsEnum>("Thing1|Thing 2");
            Assert.True(test12.HasFlag(TestFlagsEnum.Thing1));
            Assert.True(test12.HasFlag(TestFlagsEnum.Thing2));

            var test123 = EnumName.Parse<TestFlagsEnum>("Thing1|Thing 2|Thing 3");
            Assert.True(test123.HasFlag(TestFlagsEnum.Thing1));
            Assert.True(test123.HasFlag(TestFlagsEnum.Thing2));
            Assert.True(test123.HasFlag(TestFlagsEnum.Thing3));

            var test1235 = EnumName.Parse<TestFlagsEnum>("Thing1|Thing 2|Thing 3|Thing 5");
            Assert.True(test1235.HasFlag(TestFlagsEnum.Thing1));
            Assert.True(test1235.HasFlag(TestFlagsEnum.Thing2));
            Assert.True(test1235.HasFlag(TestFlagsEnum.Thing3));
            Assert.True(test1235.HasFlag(TestFlagsEnum.Thing5));

            var testDuplicate = EnumName.Parse<TestFlagsEnum>("Thing Duplicate");
            Assert.True(testDuplicate.HasFlag(TestFlagsEnum.Thing5));
            Assert.True(testDuplicate.HasFlag(TestFlagsEnum.ThingDuplicate));
        }

        [Flags]
        public enum ByteFlagsEnum : byte
        {
            None = 0,
            A = 1,
            [EnumName("B b")]
            B = 2,
            C = 128,
        }

        [Flags]
        public enum ULongFlagsEnum : ulong
        {
            None = 0,
            A = 1,
            Top = 1UL << 63,
        }

        [Flags]
        public enum SByteFlagsEnum : sbyte
        {
            None = 0,
            A = 1,
            Low = -128,
        }

        [Fact]
        public void FlagsOtherUnderlyingTypes()
        {
            Assert.Equal("A|B b|C", EnumName.GetName(ByteFlagsEnum.A | ByteFlagsEnum.B | ByteFlagsEnum.C));
            Assert.Equal(ByteFlagsEnum.A | ByteFlagsEnum.C, EnumName.Parse<ByteFlagsEnum>("A|C"));
            Assert.Equal("A|Top", EnumName.GetName(ULongFlagsEnum.A | ULongFlagsEnum.Top));
            Assert.Equal(ULongFlagsEnum.A | ULongFlagsEnum.Top, EnumName.Parse<ULongFlagsEnum>("A|Top"));
            Assert.Equal("A|Low", EnumName.GetName(SByteFlagsEnum.A | SByteFlagsEnum.Low));
            Assert.Equal(SByteFlagsEnum.A | SByteFlagsEnum.Low, EnumName.Parse<SByteFlagsEnum>("A|Low"));
        }

        [Fact]
        public void NonGenericAndFailures()
        {
            Assert.Equal("Thing 2", EnumName.GetName(typeof(TestEnum), TestEnum.Thing2));
            Assert.Equal(TestEnum.Thing2, EnumName.Parse("Thing 2", typeof(TestEnum)));
            Assert.True(EnumName.TryParse("Thing 2", typeof(TestEnum), out var parsed));
            Assert.Equal(TestEnum.Thing2, parsed);

            Assert.False(EnumName.TryParse<TestEnum>("Missing", out _));
            Assert.False(EnumName.TryParse<TestEnum>(null, out _));
            Assert.False(EnumName.TryParse<TestEnum>("thing 2", out _));
            Assert.False(EnumName.TryParse("Missing", typeof(TestEnum), out _));
            _ = Assert.ThrowsAny<Exception>(() => EnumName.Parse<TestEnum>("Missing"));
            _ = Assert.ThrowsAny<Exception>(() => EnumName.Parse<TestEnum>(null));
            _ = Assert.ThrowsAny<Exception>(() => EnumName.Parse("Missing", typeof(TestEnum)));
        }

        [Fact]
        public void Extensions()
        {
            Assert.Equal("Thing 2", TestEnum.Thing2.EnumName());
            Assert.Equal("Thing 2", ((TestEnum?)TestEnum.Thing2).EnumName());
            Assert.Null(((TestEnum?)null).EnumName());

            Assert.Equal(TestEnum.Thing2, "Thing 2".ToEnum<TestEnum>());
            _ = Assert.ThrowsAny<Exception>(() => "Missing".ToEnum<TestEnum>());

            Assert.Equal(TestEnum.Thing2, "Thing 2".ToEnumNullable<TestEnum>());
            Assert.Equal(TestEnum.None, "None".ToEnumNullable<TestEnum>());
            Assert.Null("Missing".ToEnumNullable<TestEnum>());
            Assert.Null(((string?)null).ToEnumNullable<TestEnum>());
        }

        [Flags]
        public enum ShortFlagsEnum : short { None = 0, A = 1, B = 2, High = 0x4000 }
        [Flags]
        public enum UShortFlagsEnum : ushort { None = 0, A = 1, Top = 0x8000 }
        [Flags]
        public enum UIntFlagsEnum : uint { None = 0, A = 1, Top = 0x80000000 }
        [Flags]
        public enum LongFlagsEnum : long { None = 0, A = 1, High = 1L << 40 }
        [Flags]
        public enum ManyFlagsEnum
        {
            None = 0, F0 = 1 << 0, F1 = 1 << 1, F2 = 1 << 2, F3 = 1 << 3, F4 = 1 << 4, F5 = 1 << 5, F6 = 1 << 6, F7 = 1 << 7,
            F8 = 1 << 8, F9 = 1 << 9, F10 = 1 << 10, F11 = 1 << 11, F12 = 1 << 12, F13 = 1 << 13,
        }

        [Fact]
        public void Flags_EveryUnderlyingType()
        {
            Assert.Equal("A|High", EnumName.GetName(ShortFlagsEnum.A | ShortFlagsEnum.High));
            Assert.Equal(ShortFlagsEnum.A | ShortFlagsEnum.High, EnumName.Parse<ShortFlagsEnum>("A|High"));
            Assert.Equal("A|Top", EnumName.GetName(UShortFlagsEnum.A | UShortFlagsEnum.Top));
            Assert.Equal("A|Top", EnumName.GetName(UIntFlagsEnum.A | UIntFlagsEnum.Top));
            Assert.Equal("A|High", EnumName.GetName(LongFlagsEnum.A | LongFlagsEnum.High));
            Assert.Equal(LongFlagsEnum.A | LongFlagsEnum.High, EnumName.Parse<LongFlagsEnum>("A|High"));

            //a combination is cached after the first time
            Assert.Equal("A|B", EnumName.GetName(ShortFlagsEnum.A | ShortFlagsEnum.B));
            Assert.Equal("A|B", EnumName.GetName(ShortFlagsEnum.A | ShortFlagsEnum.B));

            //a part that isn't a name is skipped
            Assert.Equal(ShortFlagsEnum.A | ShortFlagsEnum.B, EnumName.Parse<ShortFlagsEnum>("A|Missing|B"));
        }

        [Fact]
        public void Flags_ConcurrentCombinations()
        {
            //new combinations are added while other threads read the names
            var errors = 0;
            _ = Parallel.For(0, 8, thread =>
            {
                for (var i = 0; i < 1 << 14; i++)
                {
                    var value = (ManyFlagsEnum)((i * 7919 + thread * 104729) & ((1 << 14) - 1));
                    var expected = value == ManyFlagsEnum.None ? "None" : string.Join("|", Enum.GetValues<ManyFlagsEnum>().Where(x => x != ManyFlagsEnum.None && value.HasFlag(x)).Select(x => x.ToString()));
                    if (EnumName.GetName(value) != expected)
                        _ = Interlocked.Increment(ref errors);
                }
            });
            Assert.Equal(0, errors);
        }

        [Fact]
        public void GetName_Invalid()
        {
            _ = Assert.Throws<ArgumentException>(() => EnumName.GetName(typeof(int), 1));
            _ = Assert.Throws<InvalidOperationException>(() => EnumName.GetName((TestEnum)99));
            Assert.Equal("Thing 2", EnumName.GetName<Enum>(TestEnum.Thing2));
        }
    }
}
