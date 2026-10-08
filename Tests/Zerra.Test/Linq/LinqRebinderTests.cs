// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Xunit;
using Zerra.Linq;

namespace Zerra.Test.Linq
{
    public class LinqRebinderTests
    {
        // Binary Expression Tests
        [Fact]
        public void RebindExpression_Add_WithCurrentAndReplacement()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var addExpr = Expression.Add(paramX, paramY);
            var constantFive = Expression.Constant(5);

            var result = LinqRebinder.RebindExpression(addExpr, paramX, constantFive);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Add, result.NodeType);
            var binaryResult = Assert.IsAssignableFrom<BinaryExpression>(result);
            Assert.IsAssignableFrom<ConstantExpression>(binaryResult.Left);
            Assert.Equal(5, ((ConstantExpression)binaryResult.Left).Value);
        }

        [Fact]
        public void RebindExpression_Subtract()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var subtractExpr = Expression.Subtract(paramX, paramY);
            var constantTen = Expression.Constant(10);

            var result = LinqRebinder.RebindExpression(subtractExpr, paramX, constantTen);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Subtract, result.NodeType);
            var binaryResult = Assert.IsAssignableFrom<BinaryExpression>(result);
            Assert.NotNull(binaryResult.Left);
            Assert.NotNull(binaryResult.Right);
        }

        [Fact]
        public void RebindExpression_Multiply()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var multiplyExpr = Expression.Multiply(paramX, paramY);
            var constantTwo = Expression.Constant(2);

            var result = LinqRebinder.RebindExpression(multiplyExpr, paramX, constantTwo);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Multiply, result.NodeType);
            var binaryResult = Assert.IsAssignableFrom<BinaryExpression>(result);
            Assert.NotNull(binaryResult);
        }

        [Fact]
        public void RebindExpression_Divide()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var divideExpr = Expression.Divide(paramX, paramY);
            var constantThree = Expression.Constant(3);

            var result = LinqRebinder.RebindExpression(divideExpr, paramX, constantThree);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Divide, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_Modulo()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var moduloExpr = Expression.Modulo(paramX, paramY);
            var constantFour = Expression.Constant(4);

            var result = LinqRebinder.RebindExpression(moduloExpr, paramX, constantFour);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Modulo, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // Comparison Expression Tests
        [Fact]
        public void RebindExpression_Equal()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var equalExpr = Expression.Equal(paramX, paramY);
            var constantZero = Expression.Constant(0);

            var result = LinqRebinder.RebindExpression(equalExpr, paramX, constantZero);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Equal, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_NotEqual()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var notEqualExpr = Expression.NotEqual(paramX, paramY);
            var constantOne = Expression.Constant(1);

            var result = LinqRebinder.RebindExpression(notEqualExpr, paramX, constantOne);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.NotEqual, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_GreaterThan()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var greaterThanExpr = Expression.GreaterThan(paramX, paramY);
            var constantFive = Expression.Constant(5);

            var result = LinqRebinder.RebindExpression(greaterThanExpr, paramX, constantFive);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.GreaterThan, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_GreaterThanOrEqual()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var greaterThanOrEqualExpr = Expression.GreaterThanOrEqual(paramX, paramY);
            var constantTen = Expression.Constant(10);

            var result = LinqRebinder.RebindExpression(greaterThanOrEqualExpr, paramX, constantTen);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.GreaterThanOrEqual, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_LessThan()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var lessThanExpr = Expression.LessThan(paramX, paramY);
            var constantTwo = Expression.Constant(2);

            var result = LinqRebinder.RebindExpression(lessThanExpr, paramX, constantTwo);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.LessThan, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_LessThanOrEqual()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var lessThanOrEqualExpr = Expression.LessThanOrEqual(paramX, paramY);
            var constantThree = Expression.Constant(3);

            var result = LinqRebinder.RebindExpression(lessThanOrEqualExpr, paramX, constantThree);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.LessThanOrEqual, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // Logical Expression Tests
        [Fact]
        public void RebindExpression_And()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var andExpr = Expression.And(paramX, paramY);
            var constantOne = Expression.Constant(1);

            var result = LinqRebinder.RebindExpression(andExpr, paramX, constantOne);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.And, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_Or()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var orExpr = Expression.Or(paramX, paramY);
            var constantTwo = Expression.Constant(2);

            var result = LinqRebinder.RebindExpression(orExpr, paramX, constantTwo);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Or, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_AndAlso()
        {
            var paramX = Expression.Parameter(typeof(bool), "x");
            var paramY = Expression.Parameter(typeof(bool), "y");
            var andAlsoExpr = Expression.AndAlso(paramX, paramY);
            var constantTrue = Expression.Constant(true);

            var result = LinqRebinder.RebindExpression(andAlsoExpr, paramX, constantTrue);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.AndAlso, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_OrElse()
        {
            var paramX = Expression.Parameter(typeof(bool), "x");
            var paramY = Expression.Parameter(typeof(bool), "y");
            var orElseExpr = Expression.OrElse(paramX, paramY);
            var constantFalse = Expression.Constant(false);

            var result = LinqRebinder.RebindExpression(orElseExpr, paramX, constantFalse);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.OrElse, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // Unary Expression Tests
        [Fact]
        public void RebindExpression_Negate()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var negateExpr = Expression.Negate(paramX);
            var constantFive = Expression.Constant(5);

            var result = LinqRebinder.RebindExpression(negateExpr, paramX, constantFive);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Negate, result.NodeType);
            Assert.IsAssignableFrom<UnaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_Not()
        {
            var paramX = Expression.Parameter(typeof(bool), "x");
            var notExpr = Expression.Not(paramX);
            var constantTrue = Expression.Constant(true);

            var result = LinqRebinder.RebindExpression(notExpr, paramX, constantTrue);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Not, result.NodeType);
            Assert.IsAssignableFrom<UnaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_Convert()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var convertExpr = Expression.Convert(paramX, typeof(long));
            var constantFive = Expression.Constant(5);

            var result = LinqRebinder.RebindExpression(convertExpr, paramX, constantFive);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Convert, result.NodeType);
            var unaryResult = Assert.IsAssignableFrom<UnaryExpression>(result);
            Assert.Equal(typeof(long), unaryResult.Type);
        }

        [Fact]
        public void RebindExpression_Increment()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var incrementExpr = Expression.Increment(paramX);
            var constantTen = Expression.Constant(10);

            var result = LinqRebinder.RebindExpression(incrementExpr, paramX, constantTen);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Increment, result.NodeType);
            Assert.IsAssignableFrom<UnaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_Decrement()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var decrementExpr = Expression.Decrement(paramX);
            var constantFive = Expression.Constant(5);

            var result = LinqRebinder.RebindExpression(decrementExpr, paramX, constantFive);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Decrement, result.NodeType);
            Assert.IsAssignableFrom<UnaryExpression>(result);
        }

        // Member Access Tests
        [Fact]
        public void RebindExpression_MemberAccess()
        {
            var testClass = typeof(TestMemberClass);
            var paramObj = Expression.Parameter(testClass, "obj");
            var propertyInfo = testClass.GetProperty(nameof(TestMemberClass.Value))!;
            var memberExpr = Expression.MakeMemberAccess(paramObj, propertyInfo);
            
            // Replace the parameter that the member access is built on
            var newParam = Expression.Parameter(testClass, "newObj");

            var result = LinqRebinder.RebindExpression(memberExpr, paramObj, newParam);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.MemberAccess, result.NodeType);
            var memberResult = Assert.IsAssignableFrom<MemberExpression>(result);
            // Verify the expression was rebinded to use the new parameter
            Assert.NotNull(memberResult.Expression);
        }

        private class TestMemberClass
        {
            public int Value { get; set; }
        }

        private class TestInitClass
        {
            public string? Text { get; set; }
            public TestInitChildClass Child { get; } = new();
            public List<int> Items { get; } = new();
        }

        private class TestInitChildClass
        {
            public string? Text { get; set; }
        }

        private sealed class TestCtorClass
        {
            public int Value { get; }
            public TestCtorClass(int value) => Value = value;
        }

        [Fact]
        public void RebindExpression_New_WithArguments()
        {
            //a constructor with arguments has no Members (only anonymous types do), and New must not be given null members
            Expression<Func<TestMemberClass, TestCtorClass>> lambda = x => new TestCtorClass(x.Value);
            var parent = Expression.Parameter(typeof(object), "parent");

            var result = LinqRebinder.RebindExpression(lambda.Body, lambda.Parameters[0], Expression.Convert(parent, typeof(TestMemberClass)));
            var getter = Expression.Lambda<Func<object, TestCtorClass>>(result, parent).Compile();

            Assert.Equal(5, getter(new TestMemberClass() { Value = 5 }).Value);
        }

        [Fact]
        public void RebindExpression_New_AnonymousType()
        {
            Expression<Func<TestMemberClass, object>> lambda = x => new { x.Value };
            var parent = Expression.Parameter(typeof(object), "parent");

            var result = LinqRebinder.RebindExpression(lambda.Body, lambda.Parameters[0], Expression.Convert(parent, typeof(TestMemberClass)));
            var getter = Expression.Lambda<Func<object, object>>(result, parent).Compile();

            Assert.Equal(new { Value = 5 }.ToString(), getter(new TestMemberClass() { Value = 5 }).ToString());
        }

        [Fact]
        public void RebindExpression_MemberInit_RebindsBindings()
        {
            //the parameter appears only inside the initializer's bindings, as in a map definition x => new Target() { Text = x.Value.ToString() }
            Expression<Func<TestMemberClass, TestInitClass>> lambda = x => new TestInitClass() { Text = x.Value.ToString(), Child = { Text = (x.Value + 1).ToString() }, Items = { x.Value, 7 } };
            var parent = Expression.Parameter(typeof(object), "parent");

            var result = LinqRebinder.RebindExpression(lambda.Body, lambda.Parameters[0], Expression.Convert(parent, typeof(TestMemberClass)));
            var getter = Expression.Lambda<Func<object, TestInitClass>>(result, parent).Compile();

            var value = getter(new TestMemberClass() { Value = 5 });
            Assert.Equal("5", value.Text);
            Assert.Equal("6", value.Child.Text);
            Assert.Equal([5, 7], value.Items);
        }

        // Constant Expression Tests
        [Fact]
        public void RebindExpression_Constant_NoReplacement()
        {
            var constantExpr = Expression.Constant(42);
            var newConstant = Expression.Constant(100);

            var result = LinqRebinder.RebindExpression(constantExpr, constantExpr, newConstant);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Constant, result.NodeType);
            var constResult = Assert.IsAssignableFrom<ConstantExpression>(result);
            Assert.Equal(100, constResult.Value);
        }

        // Dictionary-based Rebinding Tests
        [Fact]
        public void Rebind_WithExpressionDictionary()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var addExpr = Expression.Add(paramX, paramY);
            var constantFive = Expression.Constant(5);
            var constantTen = Expression.Constant(10);

            var replacements = new Dictionary<Expression, Expression>
            {
                { paramX, constantFive },
                { paramY, constantTen }
            };

            var result = LinqRebinder.Rebind(addExpr, replacements);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Add, result.NodeType);
            var binaryResult = Assert.IsAssignableFrom<BinaryExpression>(result);
            var leftConstant = Assert.IsAssignableFrom<ConstantExpression>(binaryResult.Left);
            Assert.Equal(5, leftConstant.Value);
            var rightConstant = Assert.IsAssignableFrom<ConstantExpression>(binaryResult.Right);
            Assert.Equal(10, rightConstant.Value);
        }

        [Fact]
        public void Rebind_WithStringDictionary()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var addExpr = Expression.Add(paramX, paramY);
            var constantFive = Expression.Constant(5);

            var replacements = new Dictionary<string, Expression>
            {
                { paramX.ToString(), constantFive }
            };

            var result = LinqRebinder.Rebind(addExpr, replacements);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Add, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // Complex Expression Tests
        [Fact]
        public void RebindExpression_NestedBinaryExpression()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var paramZ = Expression.Parameter(typeof(int), "z");
            
            // (x + y) * z
            var addExpr = Expression.Add(paramX, paramY);
            var multiplyExpr = Expression.Multiply(addExpr, paramZ);
            var constantFive = Expression.Constant(5);

            var result = LinqRebinder.RebindExpression(multiplyExpr, paramX, constantFive);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Multiply, result.NodeType);
            var binaryResult = Assert.IsAssignableFrom<BinaryExpression>(result);
            Assert.IsAssignableFrom<BinaryExpression>(binaryResult.Left);
        }

        [Fact]
        public void RebindExpression_ConditionalExpression()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var testExpr = Expression.Equal(paramX, Expression.Constant(0));
            var conditionalExpr = Expression.Condition(testExpr, paramY, Expression.Constant(10));
            var constantFive = Expression.Constant(5);

            var result = LinqRebinder.RebindExpression(conditionalExpr, paramX, constantFive);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Conditional, result.NodeType);
            var condResult = Assert.IsAssignableFrom<ConditionalExpression>(result);
            Assert.NotNull(condResult.Test);
            Assert.NotNull(condResult.IfTrue);
            Assert.NotNull(condResult.IfFalse);
        }

        [Fact]
        public void RebindExpression_Coalesce()
        {
            var paramX = Expression.Parameter(typeof(int?), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var coalesceExpr = Expression.Coalesce(paramX, paramY);
            var constantTen = Expression.Constant(10, typeof(int?));

            var result = LinqRebinder.RebindExpression(coalesceExpr, paramX, constantTen);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Coalesce, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // Bitwise Expression Tests
        [Fact]
        public void RebindExpression_ExclusiveOr()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var xorExpr = Expression.ExclusiveOr(paramX, paramY);
            var constantOne = Expression.Constant(1);

            var result = LinqRebinder.RebindExpression(xorExpr, paramX, constantOne);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.ExclusiveOr, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_LeftShift()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var leftShiftExpr = Expression.LeftShift(paramX, paramY);
            var constantTwo = Expression.Constant(2);

            var result = LinqRebinder.RebindExpression(leftShiftExpr, paramX, constantTwo);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.LeftShift, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_RightShift()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var rightShiftExpr = Expression.RightShift(paramX, paramY);
            var constantOne = Expression.Constant(1);

            var result = LinqRebinder.RebindExpression(rightShiftExpr, paramX, constantOne);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.RightShift, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // Power Expression Tests
        [Fact]
        public void RebindExpression_Power()
        {
            var paramX = Expression.Parameter(typeof(double), "x");
            var paramY = Expression.Parameter(typeof(double), "y");
            var powerExpr = Expression.Power(paramX, paramY);
            var constantTwo = Expression.Constant(2.0);

            var result = LinqRebinder.RebindExpression(powerExpr, paramX, constantTwo);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Power, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // Type Test Expressions
        [Fact]
        public void RebindExpression_TypeIs()
        {
            var paramObj = Expression.Parameter(typeof(object), "obj");
            var typeIsExpr = Expression.TypeIs(paramObj, typeof(string));
            var constantValue = Expression.Constant(new object());

            var result = LinqRebinder.RebindExpression(typeIsExpr, paramObj, constantValue);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.TypeIs, result.NodeType);
            Assert.IsAssignableFrom<TypeBinaryExpression>(result);
        }

        [Fact]
        public void RebindExpression_TypeAs()
        {
            var paramObj = Expression.Parameter(typeof(object), "obj");
            var typeAsExpr = Expression.TypeAs(paramObj, typeof(string));
            var constantValue = Expression.Constant(new object());

            var result = LinqRebinder.RebindExpression(typeAsExpr, paramObj, constantValue);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.TypeAs, result.NodeType);
            Assert.IsAssignableFrom<UnaryExpression>(result);
        }

        // Assignment Expression Tests
        [Fact]
        public void RebindExpression_Assign()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var assignExpr = Expression.Assign(paramX, paramY);
            var constantTen = Expression.Constant(10);

            var result = LinqRebinder.RebindExpression(assignExpr, paramY, constantTen);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Assign, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // OnesComplement Expression Tests
        [Fact]
        public void RebindExpression_OnesComplement()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var onesComplementExpr = Expression.OnesComplement(paramX);
            var constantFive = Expression.Constant(5);

            var result = LinqRebinder.RebindExpression(onesComplementExpr, paramX, constantFive);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.OnesComplement, result.NodeType);
            Assert.IsAssignableFrom<UnaryExpression>(result);
        }

        // UnaryPlus Expression Tests
        [Fact]
        public void RebindExpression_UnaryPlus()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var unaryPlusExpr = Expression.UnaryPlus(paramX);
            var constantTen = Expression.Constant(10);

            var result = LinqRebinder.RebindExpression(unaryPlusExpr, paramX, constantTen);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.UnaryPlus, result.NodeType);
            Assert.IsAssignableFrom<UnaryExpression>(result);
        }

        // ArrayLength Expression Tests
        [Fact]
        public void RebindExpression_ArrayLength()
        {
            var arrayParam = Expression.Parameter(typeof(int[]), "arr");
            var arrayLengthExpr = Expression.ArrayLength(arrayParam);
            var newArrayExpr = Expression.NewArrayInit(typeof(int), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));

            var result = LinqRebinder.RebindExpression(arrayLengthExpr, arrayParam, newArrayExpr);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.ArrayLength, result.NodeType);
            Assert.IsAssignableFrom<UnaryExpression>(result);
        }

        // Empty Dictionary Tests
        [Fact]
        public void Rebind_WithEmptyExpressionDictionary()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var constantExpr = Expression.Constant(5);
            var addExpr = Expression.Add(paramX, constantExpr);

            var replacements = new Dictionary<Expression, Expression>();
            var result = LinqRebinder.Rebind(addExpr, replacements);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Add, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        [Fact]
        public void Rebind_WithEmptyStringDictionary()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var constantExpr = Expression.Constant(5);
            var addExpr = Expression.Add(paramX, constantExpr);

            var replacements = new Dictionary<string, Expression>();
            var result = LinqRebinder.Rebind(addExpr, replacements);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.Add, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // AddAssign Tests
        [Fact]
        public void RebindExpression_AddAssign()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var addAssignExpr = Expression.AddAssign(paramX, paramY);
            var constantFive = Expression.Constant(5);

            var result = LinqRebinder.RebindExpression(addAssignExpr, paramY, constantFive);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.AddAssign, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // SubtractAssign Tests
        [Fact]
        public void RebindExpression_SubtractAssign()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var subtractAssignExpr = Expression.SubtractAssign(paramX, paramY);
            var constantTwo = Expression.Constant(2);

            var result = LinqRebinder.RebindExpression(subtractAssignExpr, paramY, constantTwo);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.SubtractAssign, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // MultiplyAssign Tests
        [Fact]
        public void RebindExpression_MultiplyAssign()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var multiplyAssignExpr = Expression.MultiplyAssign(paramX, paramY);
            var constantThree = Expression.Constant(3);

            var result = LinqRebinder.RebindExpression(multiplyAssignExpr, paramY, constantThree);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.MultiplyAssign, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // DivideAssign Tests
        [Fact]
        public void RebindExpression_DivideAssign()
        {
            var paramX = Expression.Parameter(typeof(int), "x");
            var paramY = Expression.Parameter(typeof(int), "y");
            var divideAssignExpr = Expression.DivideAssign(paramX, paramY);
            var constantFour = Expression.Constant(4);

            var result = LinqRebinder.RebindExpression(divideAssignExpr, paramY, constantFour);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.DivideAssign, result.NodeType);
            Assert.IsAssignableFrom<BinaryExpression>(result);
        }

        // ComplexNested Expression Test
        [Fact]
        public void RebindExpression_MultipleReplacements_InNestedExpression()
        {
            var paramA = Expression.Parameter(typeof(int), "a");
            var paramB = Expression.Parameter(typeof(int), "b");
            var paramC = Expression.Parameter(typeof(int), "c");
            
            // ((a + b) * c) > 0
            var addExpr = Expression.Add(paramA, paramB);
            var multiplyExpr = Expression.Multiply(addExpr, paramC);
            var greaterThanExpr = Expression.GreaterThan(multiplyExpr, Expression.Constant(0));

            var replacements = new Dictionary<Expression, Expression>
            {
                { paramA, Expression.Constant(2) },
                { paramB, Expression.Constant(3) },
                { paramC, Expression.Constant(4) }
            };

            var result = LinqRebinder.Rebind(greaterThanExpr, replacements);

            Assert.NotNull(result);
            Assert.Equal(ExpressionType.GreaterThan, result.NodeType);
            var binaryResult = Assert.IsAssignableFrom<BinaryExpression>(result);
            Assert.IsAssignableFrom<BinaryExpression>(binaryResult.Left);
        }

        public readonly struct Money
        {
            public readonly decimal Amount;
            public Money(decimal amount) => Amount = amount;
            public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount);
            public static Money operator -(Money a) => new(-a.Amount);
            public static bool operator ==(Money a, Money b) => a.Amount == b.Amount;
            public static bool operator !=(Money a, Money b) => a.Amount != b.Amount;
            public static explicit operator decimal(Money a) => a.Amount;
            public override bool Equals(object? obj) => obj is Money m && m.Amount == Amount;
            public override int GetHashCode() => Amount.GetHashCode();
        }

        public class Model
        {
            public int Int { get; set; }
            public int? NullableInt { get; set; }
            public int? NullInt { get; set; }
            public long Long { get; set; }
            public bool Flag { get; set; }
            public string? Text { get; set; }
            public string? NullText { get; set; }
            public object? Obj { get; set; }
            public int[] Array { get; set; } = [];
            public List<int> List { get; set; } = [];
            public Model? Child { get; set; }
            public Money Money { get; set; }
            public Money? NullableMoney { get; set; }
            public Settings Options { get; set; } = new();
        }

        public class Settings
        {
            public int Value { get; set; }
        }

        public class Wrapper
        {
            public Model Model { get; set; } = null!;
            public Model? Other { get; set; }
            public Model[] Models { get; set; } = [];
            public bool Flag { get; set; }
            public int Index { get; set; }
            public object? Obj { get; set; }

            public static Model Identity(Model model) => model;
        }

        private static Model CreateModel() => new()
        {
            Int = 7,
            NullableInt = 3,
            Long = 1L << 40,
            Flag = true,
            Text = "text",
            Obj = "object",
            Array = [4, 5, 6],
            List = [1, 5, 9, 12],
            Child = new() { Int = 11, Child = new() { Int = 13 } },
            Money = new(2.5m),
            NullableMoney = new(1.5m)
        };

        private static void AssertRebinds<T>(Expression<Func<Model, T>> lambda)
        {
            var model = CreateModel();
            var expected = System.Text.Json.JsonSerializer.Serialize(lambda.Compile()(model));

            var parameter = Expression.Parameter(typeof(Model), "y");
            var rebound = (LambdaExpression)LinqRebinder.RebindExpression(lambda, lambda.Parameters[0], parameter);
            Assert.Equal(lambda.Body.Type, rebound.Body.Type);
            Assert.Equal(lambda.Type, rebound.Type);
            Assert.Equal(expected, System.Text.Json.JsonSerializer.Serialize(rebound.Compile().DynamicInvoke(model)));

            var wrapper = Expression.Parameter(typeof(Wrapper), "w");
            var wrapped = (LambdaExpression)LinqRebinder.RebindExpression(lambda, lambda.Parameters[0], Expression.Property(wrapper, nameof(Wrapper.Model)));
            Assert.Equal(wrapper, Assert.Single(wrapped.Parameters));
            Assert.Equal(expected, System.Text.Json.JsonSerializer.Serialize(wrapped.Compile().DynamicInvoke(new Wrapper() { Model = model })));
        }

        [Fact]
        public void RebindCompiles_Arithmetic()
        {
            AssertRebinds(x => x.Int + 1);
            AssertRebinds(x => checked(x.Int + 1));
            AssertRebinds(x => x.Int - 1);
            AssertRebinds(x => checked(x.Int - 1));
            AssertRebinds(x => x.Int * 3);
            AssertRebinds(x => checked(x.Int * 3));
            AssertRebinds(x => x.Int / 2);
            AssertRebinds(x => x.Int % 4);
            AssertRebinds(x => -x.Int);
            AssertRebinds(x => checked(-x.Int));
            AssertRebinds(x => ~x.Int);
            AssertRebinds(x => x.Int & 3);
            AssertRebinds(x => x.Int | 8);
            AssertRebinds(x => x.Int ^ 5);
            AssertRebinds(x => x.Long << 2);
            AssertRebinds(x => x.Long >> 3);
        }

        [Fact]
        public void RebindCompiles_Logic()
        {
            AssertRebinds(x => !x.Flag);
            AssertRebinds(x => x.Flag && x.Int > 5);
            AssertRebinds(x => x.Flag || x.Int > 5);
            AssertRebinds(x => x.Flag & x.Int > 5);
            AssertRebinds(x => x.Flag ^ x.Int > 5);
            AssertRebinds(x => x.Int == 7);
            AssertRebinds(x => x.Int != 7);
            AssertRebinds(x => x.Int < 7);
            AssertRebinds(x => x.Int <= 7);
            AssertRebinds(x => x.Int > 7);
            AssertRebinds(x => x.Int >= 7);
            AssertRebinds(x => x.Flag ? x.Int : -1);
            AssertRebinds(x => x.Child == null);
        }

        [Fact]
        public void RebindCompiles_Nullable()
        {
            AssertRebinds(x => x.NullableInt + 1);
            AssertRebinds(x => x.NullInt + 1);
            AssertRebinds(x => x.NullableInt == 3);
            AssertRebinds(x => x.NullInt < 3);
            AssertRebinds(x => x.NullInt ?? -1);
            AssertRebinds(x => x.NullText ?? x.Text);
            AssertRebinds(x => (int?)x.Int);
            AssertRebinds(x => x.NullableInt!.Value);
            AssertRebinds(x => x.NullableInt.HasValue);
        }

        [Fact]
        public void RebindCompiles_UserDefinedOperators()
        {
            AssertRebinds(x => (x.Money + x.Money).Amount);
            AssertRebinds(x => (-x.Money).Amount);
            AssertRebinds(x => x.Money == x.Money);
            AssertRebinds(x => x.Money != new Money(1));
            AssertRebinds(x => (decimal)x.Money);
            AssertRebinds(x => x.NullableMoney == x.Money);
            AssertRebinds(x => (x.NullableMoney + x.Money)!.Value.Amount);
        }

        [Fact]
        public void RebindCompiles_Conversions()
        {
            AssertRebinds(x => (long)x.Int);
            AssertRebinds(x => checked((byte)x.Int));
            AssertRebinds(x => (object)x.Int);
            AssertRebinds(x => x.Obj as string);
            AssertRebinds(x => x.Obj is string);
            AssertRebinds(x => (string)x.Obj!);
        }

        [Fact]
        public void RebindCompiles_MembersAndCalls()
        {
            AssertRebinds(x => x.Child!.Child!.Int);
            AssertRebinds(x => x.Text!.Length);
            AssertRebinds(x => x.Text!.ToUpperInvariant());
            AssertRebinds(x => x.Text!.Substring(1, 2));
            AssertRebinds(x => string.Concat(x.Text, x.Int));
            AssertRebinds(x => x.List[2]);
            AssertRebinds(x => x.Array[1]);
            AssertRebinds(x => x.Array.Length);
            AssertRebinds(x => x.List.Count(i => i > x.Int));
            AssertRebinds(x => x.List.Where(i => i > x.Int).Select(i => i * x.Child!.Int).ToArray());
            AssertRebinds(x => x.List.AsQueryable().Where(i => i > x.Int).Count());
            AssertRebinds(x => ((Func<int, int>)(i => i + x.Int))(5));
        }

        [Fact]
        public void RebindCompiles_New()
        {
            AssertRebinds(x => new { x.Int, x.Text });
            AssertRebinds(x => new Money(x.Int));
            AssertRebinds(x => new Model { Int = x.Int, Text = x.Text, List = { x.Int, 2 }, Options = { Value = x.Int } }.Options.Value + x.List.Count);
            AssertRebinds(x => new Model { Int = x.Int, Child = new Model { Int = x.Long > 0 ? 1 : 2 } }.Child!.Int);
            AssertRebinds(x => new List<int> { x.Int, x.Child!.Int });
            AssertRebinds(x => new Dictionary<string, int> { { x.Text!, x.Int } });
            AssertRebinds(x => new[] { x.Int, 2 });
            AssertRebinds(x => new int[x.Int].Length);
            AssertRebinds(x => new int[x.Int, 2].Length);
        }

        [Fact]
        public void RebindCompiles_Statements()
        {
            //in the expression being rebound
            var x = Expression.Parameter(typeof(Model), "x");
            var v = Expression.Variable(typeof(int), "v");
            var e = Expression.Variable(typeof(Exception), "e");
            var exit = Expression.Label("exit");
            var skip = Expression.Label("skip");
            var result = Expression.Label(typeof(int), "result");
            var array = Expression.Variable(typeof(int[]), "array");

            var body = Expression.Block(
                [v, array],
                Expression.Assign(v, Expression.Property(x, nameof(Model.Int))),
                Expression.AddAssign(v, Expression.Constant(1)),
                Expression.AddAssignChecked(v, Expression.Constant(1)),
                Expression.SubtractAssign(v, Expression.Constant(1)),
                Expression.SubtractAssignChecked(v, Expression.Constant(1)),
                Expression.MultiplyAssign(v, Expression.Constant(2)),
                Expression.MultiplyAssignChecked(v, Expression.Constant(1)),
                Expression.DivideAssign(v, Expression.Constant(2)),
                Expression.ModuloAssign(v, Expression.Constant(100)),
                Expression.AndAssign(v, Expression.Constant(0xFF)),
                Expression.OrAssign(v, Expression.Constant(0)),
                Expression.ExclusiveOrAssign(v, Expression.Constant(0)),
                Expression.LeftShiftAssign(v, Expression.Constant(1)),
                Expression.RightShiftAssign(v, Expression.Constant(1)),
                Expression.PreIncrementAssign(v),
                Expression.PostIncrementAssign(v),
                Expression.PreDecrementAssign(v),
                Expression.PostDecrementAssign(v),
                Expression.Increment(v),
                Expression.Decrement(v),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.GreaterThan(v, Expression.Constant(20)),
                        Expression.Break(exit),
                        Expression.PreIncrementAssign(v)),
                    exit),
                Expression.Goto(skip),
                Expression.Assign(v, Expression.Constant(-1)),
                Expression.Label(skip),
                Expression.TryCatchFinally(
                    Expression.Throw(Expression.New(typeof(InvalidOperationException))),
                    Expression.Block(typeof(void), Expression.AddAssign(v, Expression.Constant(100))),
                    Expression.Catch(e, Expression.Block(typeof(void), Expression.AddAssign(v, Expression.Constant(10))))),
                Expression.TryFault(Expression.Empty(), Expression.Empty()),
                Expression.Switch(
                    Expression.Property(x, nameof(Model.Int)),
                    Expression.Empty(),
                    Expression.SwitchCase(Expression.Block(typeof(void), Expression.AddAssign(v, Expression.Constant(1000))), Expression.Constant(7))),
                Expression.Assign(array, Expression.NewArrayInit(typeof(int), v)),
                Expression.Assign(Expression.ArrayAccess(array, Expression.Constant(0)), Expression.Add(Expression.ArrayAccess(array, Expression.Constant(0)), Expression.Default(typeof(int)))),
                Expression.Return(result, Expression.ArrayIndex(array, Expression.Constant(0))),
                Expression.Label(result, Expression.Constant(0)));

            var lambda = Expression.Lambda<Func<Model, int>>(body, x);
            var model = CreateModel();
            var expected = lambda.Compile()(model);
            Assert.Equal(1131, expected);

            var y = Expression.Parameter(typeof(Model), "y");
            var rebound = (Expression<Func<Model, int>>)LinqRebinder.RebindExpression(lambda, x, y);
            Assert.Equal(expected, rebound.Compile()(model));
        }

        [Fact]
        public void RebindCompiles_Dictionaries()
        {
            Expression<Func<Model, int>> lambda = x => x.Int + x.Child!.Int;
            var y = Expression.Parameter(typeof(Model), "y");
            var model = CreateModel();

            var byExpression = (LambdaExpression)LinqRebinder.Rebind(lambda, new Dictionary<Expression, Expression>() { { lambda.Parameters[0], y } });
            Assert.Equal(18, byExpression.Compile().DynamicInvoke(model));

            var byString = (LambdaExpression)LinqRebinder.Rebind(lambda, new Dictionary<string, Expression>() { { "x.Child.Int", Expression.Constant(100) } });
            Assert.Equal(107, byString.Compile().DynamicInvoke(model));
        }

        private static void AssertReplacement(Expression<Func<Wrapper, Model>> replacement, Wrapper wrapper)
        {
            Expression<Func<Model, int>> lambda = x => x.Int + x.Child!.Int;
            var expected = lambda.Compile()(replacement.Compile()(wrapper));

            var rebound = (LambdaExpression)LinqRebinder.RebindExpression(lambda, lambda.Parameters[0], replacement.Body);
            Assert.Equal(replacement.Parameters[0], Assert.Single(rebound.Parameters));
            Assert.Equal(expected, rebound.Compile().DynamicInvoke(wrapper));
        }

        [Fact]
        public void RebindCompiles_ComplexReplacements()
        {
            var model = CreateModel();
            var wrapper = new Wrapper() { Model = model, Other = model, Models = [model, model], Flag = true, Index = 1, Obj = model };

            AssertReplacement(w => w.Model, wrapper);
            AssertReplacement(w => w.Flag ? w.Model : w.Other!, wrapper);
            AssertReplacement(w => w.Other ?? w.Model, wrapper);
            AssertReplacement(w => Wrapper.Identity(w.Model), wrapper);
            AssertReplacement(w => ((Func<Wrapper, Model>)(y => y.Model))(w), wrapper);
            AssertReplacement(w => (Model)w.Obj!, wrapper);
            AssertReplacement(w => (w.Obj as Model)!, wrapper);
            AssertReplacement(w => w.Obj is Model ? w.Model : w.Other!, wrapper);
            AssertReplacement(w => w.Models[w.Index * 2 - 1 + w.Index % 1 - (w.Index & 0) + (w.Index | 0) - (w.Index ^ w.Index) - w.Index + (w.Index << 1 >> 1) - w.Index], wrapper);
            AssertReplacement(w => w.Models[checked(-(-w.Index) + 0 * w.Index)], wrapper);
            AssertReplacement(w => w.Models[w.Models.Length - 1], wrapper);
            AssertReplacement(w => w.Models[w.Flag && !(w.Index > 5) || w.Index < 0 ? 0 : 1], wrapper);
            AssertReplacement(w => w.Models[w.Index >= 1 && w.Index <= 1 && w.Index != 2 && w.Index == 1 ? 0 : 1], wrapper);
            AssertReplacement(w => new Model() { Int = w.Model.Int, Child = w.Model.Child, List = { w.Index }, Options = { Value = w.Index } }, wrapper);
            AssertReplacement(w => new List<Model>() { w.Model, w.Other! }[0], wrapper);
            AssertReplacement(w => new Dictionary<int, Model>() { { w.Index, w.Model } }[w.Index], wrapper);
            AssertReplacement(w => new[] { w.Model, w.Other! }[0], wrapper);
            AssertReplacement(w => w.Models.Where(m => m.Int == w.Model.Int).First(), wrapper);
            AssertReplacement(w => w.Models.AsQueryable().Where(m => m.Int == w.Index * 7).First(), wrapper);
            AssertReplacement(w => new Model[w.Index + 1].Length > 0 ? w.Model : w.Other!, wrapper);

            var w = Expression.Parameter(typeof(Wrapper), "w");
            var v = Expression.Variable(typeof(int), "v");
            var exit = Expression.Label(typeof(Model), "exit");
            var models = Expression.Property(w, nameof(Wrapper.Models));
            var block = Expression.Block(
                [v],
                Expression.Assign(v, Expression.Property(w, nameof(Wrapper.Index))),
                Expression.AddAssign(v, Expression.Constant(1)),
                Expression.SubtractAssign(v, Expression.Constant(1)),
                Expression.PreIncrementAssign(v),
                Expression.PostDecrementAssign(v),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.GreaterThanOrEqual(v, Expression.Constant(0)),
                        Expression.Break(exit, Expression.ArrayAccess(models, v)),
                        Expression.PreIncrementAssign(v)),
                    exit));
            var tryBlock = Expression.TryCatch(block, Expression.Catch(typeof(Exception), Expression.Property(w, nameof(Wrapper.Model))));
            var switchBlock = Expression.Switch(Expression.Property(w, nameof(Wrapper.Index)), Expression.Property(w, nameof(Wrapper.Model)), Expression.SwitchCase(tryBlock, Expression.Constant(1)));
            AssertReplacement(Expression.Lambda<Func<Wrapper, Model>>(switchBlock, w), wrapper);
        }

        //every statement and operator node, the assignments cancel out so the block returns the value it started with
        private static BlockExpression AllStatements(Expression source)
        {
            var v = Expression.Variable(typeof(int), "v");
            var d = Expression.Variable(typeof(double), "d");
            var label = Expression.Label("label");
            var one = Expression.Constant(1);
            var zero = Expression.Constant(0);
            var boxed = Expression.Convert(v, typeof(object));
            return Expression.Block(
                [v, d],
                Expression.Assign(v, source),
                Expression.AddAssignChecked(v, one),
                Expression.SubtractAssignChecked(v, one),
                Expression.MultiplyAssign(v, one),
                Expression.MultiplyAssignChecked(v, one),
                Expression.DivideAssign(v, one),
                Expression.ModuloAssign(v, Expression.Constant(int.MaxValue)),
                Expression.AndAssign(v, Expression.Constant(-1)),
                Expression.OrAssign(v, zero),
                Expression.ExclusiveOrAssign(v, zero),
                Expression.LeftShiftAssign(v, zero),
                Expression.RightShiftAssign(v, zero),
                Expression.PostIncrementAssign(v),
                Expression.PreDecrementAssign(v),
                Expression.Assign(v, Expression.Decrement(Expression.Increment(v))),
                Expression.Assign(v, Expression.Negate(Expression.Negate(Expression.UnaryPlus(v)))),
                Expression.Assign(v, Expression.OnesComplement(Expression.OnesComplement(v))),
                Expression.Assign(v, Expression.ConvertChecked(Expression.ConvertChecked(v, typeof(long)), typeof(int))),
                Expression.Assign(v, Expression.Add(v, Expression.Default(typeof(int)))),
                Expression.Assign(v, Expression.Divide(v, one)),
                Expression.Assign(d, Expression.Power(Expression.Convert(v, typeof(double)), Expression.Constant(1.0))),
                Expression.PowerAssign(d, Expression.Constant(1.0)),
                Expression.Assign(v, Expression.Condition(Expression.IsTrue(Expression.GreaterThanOrEqual(v, zero)), v, v)),
                Expression.Assign(v, Expression.Condition(Expression.IsFalse(Expression.TypeEqual(boxed, typeof(int))), zero, Expression.Unbox(boxed, typeof(int)))),
                Expression.Assign(v, Expression.SubtractChecked(v, zero)),
                Expression.Assign(v, Expression.Label(Expression.Label(typeof(int), "valued"), v)),
                Expression.Assign(v, Expression.Add(v, Expression.New(typeof(int)))),
                Expression.Assign(v, Expression.Add(v, Expression.Multiply(Expression.Property(null, typeof(Environment).GetProperty(nameof(Environment.ProcessorCount))!), zero))),
                Expression.Assign(v, Expression.Add(v, new ZeroExtension())),
                Expression.Assign(v, Expression.Convert(Expression.Dynamic(new PassThroughBinder(), typeof(object), boxed), typeof(int))),
                Expression.IfThen(Expression.LessThan(v, Expression.Constant(int.MinValue)), Expression.Throw(Expression.New(typeof(InvalidOperationException).GetConstructor([typeof(string)])!, Expression.Call(boxed, typeof(object).GetMethod(nameof(ToString))!)))),
                Expression.TryFinally(Expression.Assign(v, v), Expression.Assign(v, v)),
                Expression.TryFault(Expression.Assign(v, v), Expression.Assign(v, v)),
                Expression.TryCatch(Expression.Assign(v, v), Expression.Catch(Expression.Parameter(typeof(Exception), "ex"), Expression.Assign(v, zero), Expression.Equal(v, zero))),
                Expression.RuntimeVariables(v),
                Expression.Label(label),
                Expression.DebugInfo(Expression.SymbolDocument("rebinder"), 1, 1, 1, 2),
                Expression.ClearDebugInfo(Expression.SymbolDocument("rebinder")),
                Expression.Convert(d, typeof(int)));
        }

        [Fact]
        public void RebindCompiles_EveryStatement()
        {
            //each returns a different value, before or after the change, so a mixed up kind changes the result
            var x = Expression.Parameter(typeof(Model), "x");
            AssertRebinds(Expression.Lambda<Func<Model, int>>(AllStatements(Expression.Property(x, nameof(Model.Int))), x));

            //and in the replacement, whose parameters are found by walking it
            var model = CreateModel();
            var wrapper = new Wrapper() { Model = model, Other = model, Models = [model, model], Flag = true, Index = 1, Obj = model };
            var w = Expression.Parameter(typeof(Wrapper), "w");
            var index = Expression.Property(w, nameof(Wrapper.Index));
            AssertReplacement(Expression.Lambda<Func<Wrapper, Model>>(Expression.ArrayIndex(Expression.Property(w, nameof(Wrapper.Models)), Expression.Subtract(AllStatements(index), index)), w), wrapper);
        }

        [Fact]
        public void RebindCompiles_IncrementsAndDecrements()
        {
            var x = Expression.Parameter(typeof(Model), "x");
            var v = Expression.Variable(typeof(int), "v");
            Expression<Func<Model, int>> Lambda(Func<ParameterExpression, Expression> change) => Expression.Lambda<Func<Model, int>>(
                Expression.Block([v], Expression.Assign(v, Expression.Property(x, nameof(Model.Int))), Expression.Add(Expression.Multiply(change(v), Expression.Constant(100)), v)), x);

            AssertRebinds(Lambda(Expression.PreIncrementAssign));
            AssertRebinds(Lambda(Expression.PostIncrementAssign));
            AssertRebinds(Lambda(Expression.PreDecrementAssign));
            AssertRebinds(Lambda(Expression.PostDecrementAssign));
        }

        //reduces to 0, rebinding leaves it as it is
        private sealed class ZeroExtension : Expression
        {
            public override ExpressionType NodeType => ExpressionType.Extension;
            public override Type Type => typeof(int);
            public override bool CanReduce => true;
            public override Expression Reduce() => Constant(0);
        }

        //a dynamic call that returns its argument
        private sealed class PassThroughBinder : System.Runtime.CompilerServices.CallSiteBinder
        {
            public override Expression Bind(object[] args, System.Collections.ObjectModel.ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel)
                => Expression.Return(returnLabel, parameters[0]);
        }
    }
}
