using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Zerra.Collections;
using Zerra.Linq;

namespace Zerra.Mathematics
{
    public sealed partial class MathParser
    {
        static MathParser()
        {
            LambdaExpression assign;
            {
                var parameters = new ParameterExpression[] { Expression.Parameter(typeof(double), "x"), Expression.Parameter(typeof(double), "y") };
                var returnLabel = Expression.Label(typeof(double), "return");
                assign = Expression.Lambda(typeof(Func<double, double, double>), Expression.Block(
                    parameters,
                    Expression.Assign(parameters[0], parameters[1]),
                    Expression.Label(returnLabel, parameters[0])
                ), parameters);
            }

            LambdaExpression addAssign;
            {
                var parameters = new ParameterExpression[] { Expression.Parameter(typeof(double), "x"), Expression.Parameter(typeof(double), "y") };
                addAssign = Expression.Lambda(typeof(Func<double, double, double>), Expression.AddAssign(parameters[0], parameters[1]), parameters);
            }
            LambdaExpression subtractAssign;
            {
                var parameters = new ParameterExpression[] { Expression.Parameter(typeof(double), "x"), Expression.Parameter(typeof(double), "y") };
                subtractAssign = Expression.Lambda(typeof(Func<double, double, double>), Expression.SubtractAssign(parameters[0], parameters[1]), parameters);
            }
            LambdaExpression multiplyAssign;
            {
                var parameters = new ParameterExpression[] { Expression.Parameter(typeof(double), "x"), Expression.Parameter(typeof(double), "y") };
                multiplyAssign = Expression.Lambda(typeof(Func<double, double, double>), Expression.MultiplyAssign(parameters[0], parameters[1]), parameters);
            }
            LambdaExpression divideAssign;
            {
                var parameters = new ParameterExpression[] { Expression.Parameter(typeof(double), "x"), Expression.Parameter(typeof(double), "y") };
                divideAssign = Expression.Lambda(typeof(Func<double, double, double>), Expression.DivideAssign(parameters[0], parameters[1]), parameters);
            }

            LambdaExpression exponential;
            {
                var parameters = new ParameterExpression[] { Expression.Parameter(typeof(double), "x") };
                var returnLabel = Expression.Label(typeof(double), "return");
                var i = Expression.Variable(typeof(int), "i");
                var result = Expression.Variable(typeof(double), "result");
                exponential = Expression.Lambda(typeof(Func<double, double>), Expression.Block(
                    new[] { i, result },
                    Expression.Assign(i, Expression.Convert(parameters[0], typeof(int))),
                    Expression.Assign(result, Expression.Constant(1d, typeof(double))),
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.GreaterThan(i, Expression.Constant(1, typeof(int))),
                            Expression.MultiplyAssign(result, Expression.Convert(Expression.PostDecrementAssign(i), typeof(double))),
                            Expression.Return(returnLabel, result)
                        )
                    ),
                    Expression.Label(returnLabel, result)
                ), parameters);
            }

            methodOperators.Add(new MethodOperator("e", 1, 1, static (args) => Math.Exp(args[0])));
            methodOperators.Add(new MethodOperator("log", 1, 2, static (args) => Math.Log(args[0], args.Length > 1 ? args[1] : 10)));
            methodOperators.Add(new MethodOperator("ln", 1, 1, static (args) => Math.Log(args[0])));
            methodOperators.Add(new MethodOperator("sin", 1, 1, static (args) => Math.Sin(args[0])));
            methodOperators.Add(new MethodOperator("cos", 1, 1, static (args) => Math.Cos(args[0])));
            methodOperators.Add(new MethodOperator("tan", 1, 1, static (args) => Math.Tan(args[0])));
            methodOperators.Add(new MethodOperator("asin", 1, 1, static (args) => Math.Asin(args[0])));
            methodOperators.Add(new MethodOperator("acos", 1, 1, static (args) => Math.Acos(args[0])));
            methodOperators.Add(new MethodOperator("atan", 1, 1, static (args) => Math.Atan(args[0])));
            methodOperators.Add(new MethodOperator("sinh", 1, 1, static (args) => Math.Sinh(args[0])));
            methodOperators.Add(new MethodOperator("cosh", 1, 1, static (args) => Math.Cosh(args[0])));
            methodOperators.Add(new MethodOperator("tanh", 1, 1, static (args) => Math.Tanh(args[0])));
#if !NETSTANDARD2_0
            methodOperators.Add(new MethodOperator("asinh", 1, 1, static (args) => Math.Asinh(args[0])));
            methodOperators.Add(new MethodOperator("acosh", 1, 1, static (args) => Math.Acosh(args[0])));
            methodOperators.Add(new MethodOperator("atanh", 1, 1, static (args) => Math.Atanh(args[0])));
#endif
            methodOperators.Add(new MethodOperator("abs", 1, 1, static (args) => Math.Abs(args[0])));
            methodOperators.Add(new MethodOperator("round", 1, 2, static (args) => Math.Round(args[0], args.Length > 1 ? (int)args[1] : 0)));
            methodOperators.Add(new MethodOperator("ceiling", 1, 1, static (args) => Math.Ceiling(args[0])));
            methodOperators.Add(new MethodOperator("floor", 1, 1, static (args) => Math.Floor(args[0])));
            methodOperators.Add(new MethodOperator("truncate", 1, 1, static (args) => Math.Truncate(args[0])));
            methodOperators.Add(new MethodOperator("min", 2, 2, static (args) => Math.Min(args[0], args[1])));
            methodOperators.Add(new MethodOperator("max", 2, 2, static (args) => Math.Max(args[0], args[1])));
            methodOperators.Add(new MethodOperator("if", 3, 3, static (args) => args[0] > 0 ? args[1] : args[2]));

            //Last To First Order of Operations
            unaryOperators.Add(new UnaryOperator("!", true, exponential));
            unaryOperators.Add(new UnaryOperator("-", false, (x) => -x));

            binaryOperators.Add(new BinaryOperator("+=", addAssign));
            binaryOperators.Add(new BinaryOperator("-=", subtractAssign));
            binaryOperators.Add(new BinaryOperator("*=", multiplyAssign));
            binaryOperators.Add(new BinaryOperator("/=", divideAssign));
            binaryOperators.Add(new BinaryOperator("=", assign));
            binaryOperators.Add(new BinaryOperator("&", static (x, y) => x > 0 & y > 0 ? 1 : 0));
            binaryOperators.Add(new BinaryOperator("&&", static (x, y) => x > 0 && y > 0 ? 1 : 0));
            binaryOperators.Add(new BinaryOperator("|", static (x, y) => x > 0 | y > 0 ? 1 : 0));
            binaryOperators.Add(new BinaryOperator("||", static (x, y) => x > 0 || y > 0 ? 1 : 0));
            binaryOperators.Add(new BinaryOperator("==", static (x, y) => x == y ? 1 : 0));
            binaryOperators.Add(new BinaryOperator("<", static (x, y) => x < y ? 1 : 0));
            binaryOperators.Add(new BinaryOperator("<=", static (x, y) => x <= y ? 1 : 0));
            binaryOperators.Add(new BinaryOperator(">", static (x, y) => x > y ? 1 : 0));
            binaryOperators.Add(new BinaryOperator(">=", static (x, y) => x >= y ? 1 : 0));
            binaryOperators.Add(new BinaryOperator("+", static (x, y) => x + y));
            binaryOperators.Add(new BinaryOperator("-", static (x, y) => x - y));
            binaryOperators.Add(new BinaryOperator("*", static (x, y) => x * y));
            binaryOperators.Add(new BinaryOperator("/", static (x, y) => x / y));
            binaryOperators.Add(new BinaryOperator("^", static (x, y) => Math.Pow(x, y)));
            binaryOperators.Add(new BinaryOperator("%", static (x, y) => x % y));

            tertiaryOperators.Add(new TertiaryOperator("?", ":", static (x, y, z) => x > 0 ? y : z));

            CalculateMaxOperationLength();
        }

        public static void AddBinaryOperatorFirst(string token, Expression<Func<double, double, double>> operation)
        {
            if (cache.Count > 0)
                throw new MathParserException($"Operators must be added before creating any instances");
            if (binaryOperators.Select(x => x.Token).Contains(token))
                throw new MathParserException($"Operator already exisist \"{token}\"");
            binaryOperators.Add(new BinaryOperator(token, operation));
            CalculateMaxOperationLength();
        }
        public static void AddBinaryOperatOperatorLast(string token, Expression<Func<double, double, double>> operation)
        {
            if (cache.Count > 0)
                throw new MathParserException($"Operators must be added before creating any instances");
            if (binaryOperators.Select(x => x.Token).Contains(token))
                throw new MathParserException($"Operator already exisist \"{token}\"");
            binaryOperators.Insert(0, new BinaryOperator(token, operation));
            CalculateMaxOperationLength();
        }
        public static void AddUnaryOperatorFirst(string token, bool leftSideOperand, Expression<Func<double, double>> operation)
        {
            if (cache.Count > 0)
                throw new MathParserException($"Operators must be added before creating any instances");
            if (unaryOperators.Select(x => x.Token).Contains(token))
                throw new MathParserException($"Operator already exisist \"{token}\"");
            unaryOperators.Add(new UnaryOperator(token, leftSideOperand, operation));
            CalculateMaxOperationLength();
        }
        public static void AddUnaryOperatorLast(string token, bool leftSideOperand, Expression<Func<double, double>> operation)
        {
            if (cache.Count > 0)
                throw new MathParserException($"Operators must be added before creating any instances");
            if (unaryOperators.Select(x => x.Token).Contains(token))
                throw new MathParserException($"Operator already exisist \"{token}\"");
            unaryOperators.Insert(0, new UnaryOperator(token, leftSideOperand, operation));
            CalculateMaxOperationLength();
        }
        public static void AddMethod(string token, int minArgumentCount, int maxArgumentCount, Expression<Func<double[], double>> operation)
        {
            if (cache.Count > 0)
                throw new MathParserException($"Operators must be added before creating any instances");
            if (methodOperators.Select(x => x.Token).Contains(token))
                throw new MathParserException($"Method already exisist \"{token}\"");
            methodOperators.Add(new MethodOperator(token, minArgumentCount, maxArgumentCount, operation));
            CalculateMaxOperationLength();
        }

        public static void AddBinaryOperatorFirst(string token, LambdaExpression operation)
        {
            if (cache.Count > 0)
                throw new MathParserException($"Operators must be added before creating any instances");
            if (binaryOperators.Select(x => x.Token).Contains(token))
                throw new MathParserException($"Operator already exisist \"{token}\"");
            binaryOperators.Add(new BinaryOperator(token, operation));
            CalculateMaxOperationLength();
        }
        public static void AddBinaryOperatOperatorLast(string token, LambdaExpression operation)
        {
            if (cache.Count > 0)
                throw new MathParserException($"Operators must be added before creating any instances");
            if (binaryOperators.Select(x => x.Token).Contains(token))
                throw new MathParserException($"Operator already exisist \"{token}\"");
            binaryOperators.Insert(0, new BinaryOperator(token, operation));
            CalculateMaxOperationLength();
        }
        public static void AddUnaryOperatorFirst(string token, bool leftSideOperand, LambdaExpression operation)
        {
            if (cache.Count > 0)
                throw new MathParserException($"Operators must be added before creating any instances");
            if (unaryOperators.Select(x => x.Token).Contains(token))
                throw new MathParserException($"Operator already exisist \"{token}\"");
            unaryOperators.Add(new UnaryOperator(token, leftSideOperand, operation));
            CalculateMaxOperationLength();
        }
        public static void AddUnaryOperatorLast(string token, bool leftSideOperand, LambdaExpression operation)
        {
            if (cache.Count > 0)
                throw new MathParserException($"Operators must be added before creating any instances");
            if (unaryOperators.Select(x => x.Token).Contains(token))
                throw new MathParserException($"Operator already exisist \"{token}\"");
            unaryOperators.Insert(0, new UnaryOperator(token, leftSideOperand, operation));
            CalculateMaxOperationLength();
        }
        public static void AddMethod(string token, int minArgumentCount, int maxArgumentCount, LambdaExpression operation)
        {
            if (cache.Count > 0)
                throw new MathParserException($"Operators must be added before creating any instances");
            if (methodOperators.Select(x => x.Token).Contains(token))
                throw new MathParserException($"Method already exisist \"{token}\"");
            methodOperators.Add(new MethodOperator(token, minArgumentCount, maxArgumentCount, operation));
            CalculateMaxOperationLength();
        }

        private static void CalculateMaxOperationLength()
        {
            operatorStringsMaxLength = Math.Max(methodOperators.Max(x => x.Token.Length), Math.Max(unaryOperators.Max(x => x.Token.Length), binaryOperators.Max(x => x.Token.Length)));
        }
    }
}
