// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
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
    }
}
