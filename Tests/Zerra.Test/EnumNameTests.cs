// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using System;

namespace Zerra.Test
{
    public class EnumNameTests
    {
        private enum TestEnum
        {
            None = 0,
            Thing1 = 1,
            [EnumName("Thing 2")]
            Thing2 = 2,
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
        }

        [Flags]
        private enum TestFlagsEnum : int
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
    }
}
