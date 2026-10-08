// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Globalization;
using System.Linq.Expressions;
using Xunit;
using Zerra.Linq;

namespace Zerra.Repository.Test.Linq
{
    public sealed class LinqStringConverterTests
    {
        public enum Color { Red, Blue }

        public sealed class Model
        {
            public int Value { get; set; }
            public string? Name { get; set; }
            public int? Maybe { get; set; }
            public Model? Child { get; set; }
            public int[]? Items { get; set; }
            public int Field;
            public static int Static { get; set; }
        }

        private static class Statics
        {
            public static Holder Holder { get; } = new();
        }


        private sealed class NamedBinder : System.Runtime.CompilerServices.CallSiteBinder
        {
            public override Expression Bind(object[] args, System.Collections.ObjectModel.ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel) => throw new NotSupportedException();
        }

        public sealed class Holder
        {
            public int Captured = 5;
            public Model? Model = new() { Value = 9 };
        }

        private static readonly ParameterExpression i = Expression.Parameter(typeof(int), "i");
        private static readonly LabelTarget end = Expression.Label("end");
        private static readonly Guid guid = new("11111111-2222-3333-4444-555555555555");

        public static TheoryData<string, Expression> Expressions()
        {
            var holder = new Holder();
            var list = new List<int> { 1, 2 };
            var nullModel = new Holder { Model = null };
            var data = new TheoryData<string, Expression>
            {
                //nested operations are grouped so the text reads in the order they're evaluated
                { "(Model x)=>((Model.Value==1)&&(Model.Name!=\"a\"\"b\"))||!(Model.Value>2)", (Expression<Func<Model, bool>>)(x => x.Value == 1 && x.Name != "a\"b" || !(x.Value > 2)) },
                { "(Model x)=>(((Model.Value&1)|(Model.Value<<1))|~Model.Value)|-Model.Value", (Expression<Func<Model, int>>)(x => (x.Value & 1) | (x.Value << 1) | ~x.Value | -x.Value) },
                { "(Model x)=>Model.Maybe??3", (Expression<Func<Model, int>>)(x => x.Maybe ?? 3) },
                { "(Model x)=>Model.Value>0?1:2", (Expression<Func<Model, int>>)(x => x.Value > 0 ? 1 : 2) },
                { "(Model x)=>(Object)Model.Value", (Expression<Func<Model, object>>)(x => (object)x.Value) },
                //captured values are shown as their values
                { "(Model x)=>(Model.Name.StartsWith(\"a\")&&(Model.Child.Child.Value==5))&&(Model.Value==9)", (Expression<Func<Model, bool>>)(x => x.Name!.StartsWith("a") && x.Child!.Child!.Value == holder.Captured && x.Value == holder.Model.Value) },
                { "(Model x)=>[1,2].Contains(Model.Value)", (Expression<Func<Model, bool>>)(x => list.Contains(x.Value)) },
                { "(Model x)=>(Model.Items.Length>0)&&(Model.Items[0]==1)", (Expression<Func<Model, bool>>)(x => x.Items!.Length > 0 && x.Items[0] == 1) },
                { "(Model x)=>Model.Name is String&&(Model.Child as Model!=null)", (Expression<Func<Model, bool>>)(x => x.Name is string && x.Child as Model != null) },
                { "(Model x)=>new Model(){Int32 Value, String Name}", (Expression<Func<Model, Model>>)(x => new Model { Value = 1, Name = "a" }) },
                { "(Model x)=>new List`1(){new (1), new (2)}", (Expression<Func<Model, List<int>>>)(x => new List<int> { 1, 2 }) },
                { "(Model x)=>new Int32[3]", (Expression<Func<Model, int[]>>)(x => new int[3]) },
                { "new Int32[] {1, 2}", Expression.NewArrayInit(typeof(int), Expression.Constant(1), Expression.Constant(2)) },
                { "(Model x)=>(Model.Static==1)&&(Model.Field==2)", (Expression<Func<Model, bool>>)(x => Model.Static == 1 && x.Field == 2) },
                { "(Model x)=>((Int32)(Color)Model.Value==1)&&(Model.Maybe==null)", (Expression<Func<Model, bool>>)(x => (Color)x.Value == Color.Blue && x.Maybe == null) },

                { "DateTime.Parse(\"2024-01-02T03:04:05.0000000\")", Expression.Constant(new DateTime(2024, 1, 2, 3, 4, 5)) },
                { "DateTimeOffset.Parse(\"2024-01-02T03:04:05.0000000+01:00\")", Expression.Constant(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(1))) },
                { "TimeSpan.Parse(\"01:30:00\")", Expression.Constant(TimeSpan.FromMinutes(90)) },
                { "DateOnly.Parse(\"2024-01-02\")", Expression.Constant(new DateOnly(2024, 1, 2)) },
                { "TimeOnly.Parse(\"03:04:05.0000000\")", Expression.Constant(new TimeOnly(3, 4, 5)) },
                { "\"11111111-2222-3333-4444-555555555555\"", Expression.Constant(guid) },
                { "\"c\"", Expression.Constant('c') },
                { "true", Expression.Constant(true) },
                { "1", Expression.Constant((byte)1) },
                { "-1", Expression.Constant((sbyte)-1) },
                { "-2", Expression.Constant((short)-2) },
                { "2", Expression.Constant((ushort)2) },
                { "3", Expression.Constant(3u) },
                { "-4", Expression.Constant(-4L) },
                { "4", Expression.Constant(4UL) },
                { "1.5", Expression.Constant(1.5f) },
                { "2.5", Expression.Constant(2.5d) },
                { "3.5", Expression.Constant(3.5m) },
                { "\"System.Object\"", Expression.Constant(new object(), typeof(object)) },
                { "null", Expression.Constant(null, typeof(string)) },
                { "[1,null]", Expression.Constant(new int?[] { 1, null }) },
                { "Color.Red", Expression.Constant(Color.Red) },

                { "{Int32=1;Int32+=2;}", Expression.Block(Expression.Assign(i, Expression.Constant(1)), Expression.AddAssign(i, Expression.Constant(2))) },
                { "default(Int32)", Expression.Default(typeof(int)) },
                { "while {goto end}", Expression.Loop(Expression.Break(end), end) },
                { "end:", Expression.Label(end) },
                { "goto end", Expression.Goto(end) },
                { "try {1}finally {2}", Expression.TryFinally(Expression.Constant(1), Expression.Constant(2)) },
                { "try {1}catch {2}", Expression.TryFault(Expression.Constant(1), Expression.Constant(2)) },
                { "switch(1) {case 1:default: 0}", Expression.Switch(Expression.Constant(1), Expression.Constant(0), Expression.SwitchCase(Expression.Constant(2), Expression.Constant(1))) },
                { "throw \"System.Exception: failed\"", Expression.Throw(Expression.Constant(new Exception("failed"))) },
                { "++Int32", Expression.PreIncrementAssign(i) },
                { "Int32++", Expression.PostIncrementAssign(i) },
                { "--Int32", Expression.PreDecrementAssign(i) },
                { "Int32--", Expression.PostDecrementAssign(i) },
                { "Int32++", Expression.Increment(i) },
                { "Int32--", Expression.Decrement(i) },
                { "+Int32", Expression.UnaryPlus(i) },
                { "true==true", Expression.IsTrue(Expression.Constant(true)) },
                { "true==false", Expression.IsFalse(Expression.Constant(true)) },
                { "Int32", Expression.ConvertChecked(i, typeof(long)) },
                { "-Int32", Expression.NegateChecked(i) },
                { "Int32+Int32", Expression.AddChecked(i, i) },
                { "Int32-Int32", Expression.SubtractChecked(i, i) },
                { "Int32*Int32", Expression.MultiplyChecked(i, i) },
                { "2^3", Expression.Power(Expression.Constant(2.0), Expression.Constant(3.0)) },
                { "Double^=2", Expression.PowerAssign(Expression.Parameter(typeof(double), "d"), Expression.Constant(2.0)) },
                { "\"a\"==String", Expression.TypeEqual(Expression.Constant("a"), typeof(string)) },
                { "(Int32)\"1\"", Expression.Unbox(Expression.Constant(1, typeof(object)), typeof(int)) },
                { "()=>1", Expression.Quote((Expression<Func<int>>)(() => 1)) },
                { "[1][0]", Expression.ArrayIndex(Expression.Constant(new[] { 1 }), Expression.Constant(0)) },
                { "[1,2][0]", Expression.MakeIndex(Expression.Constant(list), typeof(List<int>).GetProperty("Item"), [Expression.Constant(0)]) },
                { "\"System.Func`2[System.Int32,System.Int32]\"(1)", Expression.Invoke(Expression.Constant((Func<int, int>)(v => v)), Expression.Constant(1)) },
                { "", Expression.DebugInfo(Expression.SymbolDocument("file"), 1, 1, 1, 2) },
                { "(Model x)=>Model.Value+1", (Expression<Func<Model, int>>)(x => x.Value + 1) },
                { "(Model x,Int32 y)=>Model.Value==Int32", (Expression<Func<Model, int, bool>>)((x, y) => x.Value == y) },
                { "(Model x)=>Math.Max(Model.Value,2)", (Expression<Func<Model, int>>)(x => Math.Max(x.Value, 2)) },
                { "(Model x)=>new DateTime(Model.Value, 1, 2)", (Expression<Func<Model, DateTime>>)(x => new DateTime(x.Value, 1, 2)) },
                { "(Model x)=>new Holder(){Int32 Captured}", (Expression<Func<Model, Holder>>)(x => new Holder { Captured = x.Value }) },
                { "(Model x)=>new Dictionary`2(){new (1, Model.Value)}", (Expression<Func<Model, Dictionary<int, int>>>)(x => new Dictionary<int, int> { { 1, x.Value } }) },
                { "(Model x)=>Model.Value==Statics.Holder.Captured", (Expression<Func<Model, bool>>)(x => x.Value == Statics.Holder.Captured) },
                { "(Model x)=>Model.Value==null.Value", (Expression<Func<Model, bool>>)(x => x.Value == nullModel.Model!.Value) },
                { "~Int32", Expression.OnesComplement(i) },
                { "[1,2,3,4][0,1]", Expression.ArrayAccess(Expression.Constant(new[,] { { 1, 2 }, { 3, 4 } }), Expression.Constant(0), Expression.Constant(1)) },
                { "\"System.Func`3[System.Int32,System.Int32,System.Int32]\"(1,2)", Expression.Invoke(Expression.Constant((Func<int, int, int>)((a, b) => a + b)), Expression.Constant(1), Expression.Constant(2)) },
                { "Func`4(1,2)", Expression.Dynamic(new NamedBinder(), typeof(object), Expression.Constant(1), Expression.Constant(2)) },
            };

            foreach (var (type, operation) in new[]
            {
                (ExpressionType.Subtract, "-"), (ExpressionType.Multiply, "*"), (ExpressionType.Divide, "/"), (ExpressionType.Modulo, "%"),
                (ExpressionType.ExclusiveOr, "^"), (ExpressionType.RightShift, ">>"), (ExpressionType.LessThan, "<"), (ExpressionType.LessThanOrEqual, "<="), (ExpressionType.GreaterThanOrEqual, ">="),
                (ExpressionType.SubtractAssign, "-="), (ExpressionType.MultiplyAssign, "*="), (ExpressionType.DivideAssign, "/="), (ExpressionType.ModuloAssign, "%="),
                (ExpressionType.AndAssign, "&="), (ExpressionType.OrAssign, "|="), (ExpressionType.ExclusiveOrAssign, "^="), (ExpressionType.LeftShiftAssign, "<<="),
                (ExpressionType.RightShiftAssign, ">>="), (ExpressionType.AddAssignChecked, "+="), (ExpressionType.SubtractAssignChecked, "-="), (ExpressionType.MultiplyAssignChecked, "*="),
            })
            {
                data.Add($"Int32{operation}1", Expression.MakeBinary(type, i, Expression.Constant(1)));
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Expressions))]
        public void Convert(string expected, Expression expression)
        {
            Assert.Equal(expected, LinqStringConverter.Convert(expression));
        }

        [Fact]
        public void Convert_IgnoresTheCurrentCulture()
        {
            var culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                Assert.Equal("1.5", LinqStringConverter.Convert(Expression.Constant(1.5)));
                Assert.Equal("DateTime.Parse(\"2024-01-02T00:00:00.0000000\")", LinqStringConverter.Convert(Expression.Constant(new DateTime(2024, 1, 2))));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Fact]
        public void Convert_NotSupported_Throws()
        {
            _ = Assert.Throws<NotImplementedException>(() => LinqStringConverter.Convert(Expression.RuntimeVariables(i)));
        }

        [Fact]
        public void WhereBuilder_ToString()
        {
            var builder = new WhereBuilder<Model>(x => x.Value > 1);
            builder.Or(x => x.Name == "a");
            Assert.Equal("(Model $root)=>(Model.Value>1)||(Model.Name==\"a\")", builder.ToString());
        }
    }
}
