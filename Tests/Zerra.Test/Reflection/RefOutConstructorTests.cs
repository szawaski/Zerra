// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection
{
    //ref and out arguments are written back into the arguments array, by reflection and by generated code alike,
    //Zerra.Test.NetStandard generates these models so the same tests run against generated details there
    public class RefOutConstructorTests
    {
        [GenerateTypeDetail]
        public class RefOutConstructorModel
        {
            public int Value { get; }
            public string? Text { get; }

            public RefOutConstructorModel() { }
            public RefOutConstructorModel(ref int value, out string text, in int add)
            {
                Value = value + add;
                value *= 2;
                text = "out";
                Text = text;
            }
        }

        [GenerateTypeDetail]
        public class RefOutRequiredModel
        {
            public required int Required { get; set; }
            public int Out { get; }

            public RefOutRequiredModel() { }
            public RefOutRequiredModel(out int value)
            {
                value = 3;
                Out = value;
            }
        }

        [Fact]
        public void RefOutAndIn()
        {
            var detail = TypeAnalyzer.GetTypeDetail(typeof(RefOutConstructorModel));
            var constructor = Assert.Single(detail.Constructors, x => x.Parameters.Count == 3);
            Assert.Equal(typeof(int).MakeByRefType(), constructor.Parameters[0].Type);
            Assert.Equal(typeof(string).MakeByRefType(), constructor.Parameters[1].Type);
            Assert.Equal(typeof(int).MakeByRefType(), constructor.Parameters[2].Type);

            var args = new object?[] { 5, null, 1 };
            var model = (RefOutConstructorModel)constructor.CreatorBoxed(args);
            Assert.Equal(6, model.Value);
            Assert.Equal("out", model.Text);
            Assert.Equal(10, args[0]);
            Assert.Equal("out", args[1]);
            Assert.Equal(1, args[2]);

            //a null ref argument is the default
            args = [null, null, 2];
            model = ((ConstructorDetail<RefOutConstructorModel>)constructor).Creator(args);
            Assert.Equal(2, model.Value);
            Assert.Equal(0, args[0]);
        }

        [Fact]
        public void RequiredMembers()
        {
            var detail = TypeAnalyzer.GetTypeDetail(typeof(RefOutRequiredModel));
            var constructor = Assert.Single(detail.Constructors, x => x.Parameters.Count == 2);
            Assert.Equal(typeof(int).MakeByRefType(), constructor.Parameters[0].Type);
            Assert.Equal(nameof(RefOutRequiredModel.Required), constructor.Parameters[1].Name);

            var args = new object?[] { null, 4 };
            var model = (RefOutRequiredModel)constructor.CreatorBoxed(args);
            Assert.Equal(3, model.Out);
            Assert.Equal(4, model.Required);
            Assert.Equal(3, args[0]);
        }
    }
}
