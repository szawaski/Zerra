using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Zerra.Collections;
using Zerra.Linq;

namespace Zerra.Mathematics
{
    public partial class MathParser
    {
        public static IEnumerable<string> MethodOperators => methodOperators.Select(x => x.Token);
        public static IEnumerable<string> BinaryOperators => binaryOperators.Select(x => x.Token);
        public static IEnumerable<string> UnaryOperators => unaryOperators.Select(x => x.Token);

        private static readonly char[][] groupSets = new char[][] { new char[2] { '(', ')' }, new char[2] { '[', ']' } };
        private static readonly char[] groupOpeners = "([".ToArray();
        private static readonly char[] groupClosers = ")]".ToArray();
        private static readonly char[] numbers = "0123456789.".ToArray();
        private static readonly char[] numbersAndLetters = "0123456789.ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToArray();

        private static readonly HashSet<MethodOperator> methodOperators = new HashSet<MethodOperator>();
        private static readonly List<UnaryOperator> unaryOperators = new List<UnaryOperator>();
        private static readonly List<BinaryOperator> binaryOperators = new List<BinaryOperator>();

        private static int operatorStringsMaxLength = 0;

        private static readonly ConcurrentFactoryDictionary<string, CompiledExpression> cache = new ConcurrentFactoryDictionary<string, CompiledExpression>();

        private static bool initialized = false;
        private static readonly object initializeLock = new object();
        public static void Initialize()
        {
            lock (initializeLock)
            {
                if (initialized)
                    return;
                initialized = true;

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

                LambdaExpression increment;
                {
                    var parameters = new ParameterExpression[] { Expression.Parameter(typeof(double), "x") };
                    increment = Expression.Lambda(typeof(Func<double, double>), Expression.PostIncrementAssign(parameters[0]), parameters);
                }
                LambdaExpression decrement;
                {
                    var parameters = new ParameterExpression[] { Expression.Parameter(typeof(double), "x") };
                    decrement = Expression.Lambda(typeof(Func<double, double>), Expression.PostDecrementAssign(parameters[0]), parameters);
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

                methodOperators.Add(new MethodOperator("e", (args) => Math.Exp(args[0])));
                methodOperators.Add(new MethodOperator("log", (args) => Math.Log(args[0], args.Length > 1 ? args[1] : 10)));
                methodOperators.Add(new MethodOperator("ln", (args) => Math.Log(args[0])));
                methodOperators.Add(new MethodOperator("sin", (args) => Math.Sin(args[0])));
                methodOperators.Add(new MethodOperator("cos", (args) => Math.Cos(args[0])));
                methodOperators.Add(new MethodOperator("tan", (args) => Math.Tan(args[0])));
                methodOperators.Add(new MethodOperator("asin", (args) => Math.Asin(args[0])));
                methodOperators.Add(new MethodOperator("acos", (args) => Math.Acos(args[0])));
                methodOperators.Add(new MethodOperator("atan", (args) => Math.Atan(args[0])));
                methodOperators.Add(new MethodOperator("sinh", (args) => Math.Sinh(args[0])));
                methodOperators.Add(new MethodOperator("cosh", (args) => Math.Cosh(args[0])));
                methodOperators.Add(new MethodOperator("tanh", (args) => Math.Tanh(args[0])));
#if !NETSTANDARD2_0
                methodOperators.Add(new MethodOperator("asinh", (args) => Math.Asinh(args[0])));
                methodOperators.Add(new MethodOperator("acosh", (args) => Math.Acosh(args[0])));
                methodOperators.Add(new MethodOperator("atanh", (args) => Math.Atanh(args[0])));
#endif
                methodOperators.Add(new MethodOperator("abs", (args) => Math.Abs(args[0])));
                methodOperators.Add(new MethodOperator("round", (args) => Math.Round(args[0], args.Length > 1 ? (int)args[1] : 0)));
                methodOperators.Add(new MethodOperator("ceiling", (args) => Math.Ceiling(args[0])));
                methodOperators.Add(new MethodOperator("floor", (args) => Math.Floor(args[0])));
                methodOperators.Add(new MethodOperator("truncate", (args) => Math.Truncate(args[0])));
                methodOperators.Add(new MethodOperator("min", (args) => Math.Min(args[0], args[1])));
                methodOperators.Add(new MethodOperator("max", (args) => Math.Max(args[0], args[1])));
                methodOperators.Add(new MethodOperator("if", (args) => args[0] > 0 ? args[1] : args[2]));

                //Last To First
                unaryOperators.Add(new UnaryOperator("!", true, exponential));
                unaryOperators.Add(new UnaryOperator("-", false, (x) => -x));
                unaryOperators.Add(new UnaryOperator("++", true, increment));
                unaryOperators.Add(new UnaryOperator("--", false, decrement));
                binaryOperators.Add(new BinaryOperator("+=", addAssign));
                binaryOperators.Add(new BinaryOperator("-=", subtractAssign));
                binaryOperators.Add(new BinaryOperator("*=", multiplyAssign));
                binaryOperators.Add(new BinaryOperator("/=", divideAssign));
                binaryOperators.Add(new BinaryOperator("=", assign));
                binaryOperators.Add(new BinaryOperator("&", (x, y) => x > 0 & y > 0 ? 1 : 0));
                binaryOperators.Add(new BinaryOperator("&&", (x, y) => x > 0 && y > 0 ? 1 : 0));
                binaryOperators.Add(new BinaryOperator("|", (x, y) => x > 0 | y > 0 ? 1 : 0));
                binaryOperators.Add(new BinaryOperator("||", (x, y) => x > 0 || y > 0 ? 1 : 0));
                binaryOperators.Add(new BinaryOperator("==", (x, y) => x == y ? 1 : 0));
                binaryOperators.Add(new BinaryOperator("<", (x, y) => x < y ? 1 : 0));
                binaryOperators.Add(new BinaryOperator("<=", (x, y) => x <= y ? 1 : 0));
                binaryOperators.Add(new BinaryOperator(">", (x, y) => x > y ? 1 : 0));
                binaryOperators.Add(new BinaryOperator(">=", (x, y) => x >= y ? 1 : 0));
                binaryOperators.Add(new BinaryOperator("+", (x, y) => x + y));
                binaryOperators.Add(new BinaryOperator("-", (x, y) => x - y));
                binaryOperators.Add(new BinaryOperator("*", (x, y) => x * y));
                binaryOperators.Add(new BinaryOperator("/", (x, y) => x / y));
                binaryOperators.Add(new BinaryOperator("^", (x, y) => Math.Pow(x, y)));
                binaryOperators.Add(new BinaryOperator("%", (x, y) => x % y));

                CalculateMaxOperationLength();
            }
        }

        public static void AddBinaryOperatorFirst(string token, Expression<Func<double, double, double>> operation)
        {
            Initialize();
            lock (initializeLock)
            {
                if (cache.Count > 0)
                    throw new MathParserException($"Operators must be added before creating any instances");
                if (binaryOperators.Select(x => x.Token).Contains(token))
                    throw new MathParserException($"Operator already exisist \"{token}\"");
                binaryOperators.Add(new BinaryOperator(token, operation));
                CalculateMaxOperationLength();
            }
        }
        public static void AddBinaryOperatOperatorLast(string token, Expression<Func<double, double, double>> operation)
        {
            Initialize();
            lock (initializeLock)
            {
                if (cache.Count > 0)
                    throw new MathParserException($"Operators must be added before creating any instances");
                if (binaryOperators.Select(x => x.Token).Contains(token))
                    throw new MathParserException($"Operator already exisist \"{token}\"");
                binaryOperators.Insert(0, new BinaryOperator(token, operation));
                CalculateMaxOperationLength();
            }
        }
        public static void AddUnaryOperatorFirst(string token, bool leftSideOperand, Expression<Func<double, double>> operation)
        {
            Initialize();
            lock (initializeLock)
            {
                if (cache.Count > 0)
                    throw new MathParserException($"Operators must be added before creating any instances");
                if (unaryOperators.Select(x => x.Token).Contains(token))
                    throw new MathParserException($"Operator already exisist \"{token}\"");
                unaryOperators.Add(new UnaryOperator(token, leftSideOperand, operation));
                CalculateMaxOperationLength();
            }
        }
        public static void AddUnaryOperatorLast(string token, bool leftSideOperand, Expression<Func<double, double>> operation)
        {
            Initialize();
            lock (initializeLock)
            {
                if (cache.Count > 0)
                    throw new MathParserException($"Operators must be added before creating any instances");
                if (unaryOperators.Select(x => x.Token).Contains(token))
                    throw new MathParserException($"Operator already exisist \"{token}\"");
                unaryOperators.Insert(0, new UnaryOperator(token, leftSideOperand, operation));
                CalculateMaxOperationLength();
            }
        }
        public static void AddMethod(string token, Expression<Func<double[], double>> operation)
        {
            Initialize();
            lock (initializeLock)
            {
                if (cache.Count > 0)
                    throw new MathParserException($"Operators must be added before creating any instances");
                if (methodOperators.Select(x => x.Token).Contains(token))
                    throw new MathParserException($"Method already exisist \"{token}\"");
                _ = methodOperators.Add(new MethodOperator(token, operation));
                CalculateMaxOperationLength();
            }
        }

        public static void AddBinaryOperatorFirst(string token, LambdaExpression operation)
        {
            Initialize();
            lock (initializeLock)
            {
                if (cache.Count > 0)
                    throw new MathParserException($"Operators must be added before creating any instances");
                if (binaryOperators.Select(x => x.Token).Contains(token))
                    throw new MathParserException($"Operator already exisist \"{token}\"");
                binaryOperators.Add(new BinaryOperator(token, operation));
                CalculateMaxOperationLength();
            }
        }
        public static void AddBinaryOperatOperatorLast(string token, LambdaExpression operation)
        {
            Initialize();
            lock (initializeLock)
            {
                if (cache.Count > 0)
                    throw new MathParserException($"Operators must be added before creating any instances");
                if (binaryOperators.Select(x => x.Token).Contains(token))
                    throw new MathParserException($"Operator already exisist \"{token}\"");
                binaryOperators.Insert(0, new BinaryOperator(token, operation));
                CalculateMaxOperationLength();
            }
        }
        public static void AddUnaryOperatorFirst(string token, bool leftSideOperand, LambdaExpression operation)
        {
            Initialize();
            lock (initializeLock)
            {
                if (cache.Count > 0)
                    throw new MathParserException($"Operators must be added before creating any instances");
                if (unaryOperators.Select(x => x.Token).Contains(token))
                    throw new MathParserException($"Operator already exisist \"{token}\"");
                unaryOperators.Add(new UnaryOperator(token, leftSideOperand, operation));
                CalculateMaxOperationLength();
            }
        }
        public static void AddUnaryOperatorLast(string token, bool leftSideOperand, LambdaExpression operation)
        {
            Initialize();
            lock (initializeLock)
            {
                if (cache.Count > 0)
                    throw new MathParserException($"Operators must be added before creating any instances");
                if (unaryOperators.Select(x => x.Token).Contains(token))
                    throw new MathParserException($"Operator already exisist \"{token}\"");
                unaryOperators.Insert(0, new UnaryOperator(token, leftSideOperand, operation));
                CalculateMaxOperationLength();
            }
        }
        public static void AddMethod(string token, LambdaExpression operation)
        {
            Initialize();
            lock (initializeLock)
            {
                if (cache.Count > 0)
                    throw new MathParserException($"Operators must be added before creating any instances");
                if (methodOperators.Select(x => x.Token).Contains(token))
                    throw new MathParserException($"Method already exisist \"{token}\"");
                _ = methodOperators.Add(new MethodOperator(token, operation));
                CalculateMaxOperationLength();
            }
        }

        private static void CalculateMaxOperationLength()
        {
            operatorStringsMaxLength = Math.Max(methodOperators.Max(x => x.Token.Length), Math.Max(unaryOperators.Max(x => x.Token.Length), binaryOperators.Max(x => x.Token.Length)));
        }

        private readonly CompiledExpression mathExpression = null;

        public IEnumerable<string> Parameters { get { return mathExpression.Parameters.Select(x => x.Name); } }
        public string Equation { get { return mathExpression.ExpressionString; } }

        public MathParser(string expression)
        {
            Initialize();
            mathExpression = BuildMathExpression(expression);
        }

        public double Evalutate(IDictionary<string, double> variables)
        {
            var args = new object[mathExpression.Parameters.Length];
            for (var i = 0; i < mathExpression.Parameters.Length; i++)
            {
                if (!variables.TryGetValue(mathExpression.Parameters[i].Name, out var arg))
                    throw new MathParserException($"Missing parameter {mathExpression.Parameters[i].Name}");
                args[i] = arg;
            }
            var result = (double)mathExpression.Expression.DynamicInvoke(args);
            return result;
        }

        public double Evalutate(params double[] variables)
        {
            if (variables.Length != mathExpression.Parameters.Length)
                throw new MathParserException($"Invalid number of parameters");

            var args = new object[mathExpression.Parameters.Length];
            for (var i = 0; i < mathExpression.Parameters.Length; i++)
                args[i] = variables[i];
            var result = (double)mathExpression.Expression.DynamicInvoke(args);
            return result;
        }

        private CompiledExpression BuildMathExpression(string expression)
        {
            var compiledExpression = cache.GetOrAdd(expression, (key) =>
            {
                var context = new ParserContext(key);
                context.Next();
                var part = ParseParts(ref context);
                if (context.GroupStack.Count > 0)
                {
                    var lastGroupToken = context.GroupStack.Pop();
                    var groupSet = groupSets.First(x => x[0] == lastGroupToken);
                    throw new MathParserException($"Ended without closing group {groupSet[1]}");
                }
                var body = BuildLinqExpression(ref context, part);
                var parameters = context.LinqParameters.OrderBy(x => x.Name).ToArray();
                var lambda = Expression.Lambda(body, parameters);
                var compiled = lambda.Compile();

                var sb = new StringBuilder();
                _ = sb.Append("f(");
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (i > 0)
                        _ = sb.Append(',');
                    _ = sb.Append(parameters[i].Name);
                }
                _ = sb.Append(") = ");
                _ = sb.Append(part);

                return new CompiledExpression()
                {
                    ExpressionString = sb.ToString(),
                    Expression = compiled,
                    Parameters = parameters
                };
            });

            return compiledExpression;
        }

        private StatementPart ParseParts(ref ParserContext context)
        {
            var subparts = new List<StatementPart>();
            var startIndex = context.Index;
            while (context.Index < context.Chars.Length)
            {
                while (Char.IsWhiteSpace(context.Current) && context.Index < context.Chars.Length)
                    context.Next();
                var subpart = ParseOperators(ref context);
                if (subpart == null)
                    break;
                subparts.Add(subpart);
            }
            while (Char.IsWhiteSpace(context.Current) && context.Index < context.Chars.Length)
                context.Next();
            var part = new StatementPart(startIndex, subparts);
            return part;
        }
        private StatementPart ParseOperators(ref ParserContext context)
        {
            var startIndex = context.Index;
            MethodOperator methodFound = null;
            UnaryOperator unaryFound = null;
            BinaryOperator binaryFound = null;
            while (context.Index < context.Chars.Length)
            {
                context.Next();

                var length = context.Index - startIndex;
                if (length > operatorStringsMaxLength)
                    break;
                var partTokens = context.Chars.Slice(startIndex, length);
                foreach (var methodOperator in methodOperators)
                {
                    if (partTokens.SequenceEqual(methodOperator.TokenWithOpener.AsSpan()))
                    {
                        methodFound = methodOperator;
                        break;
                    }
                }
                foreach (var unaryOperator in unaryOperators)
                {
                    if (partTokens.SequenceEqual(unaryOperator.Token.AsSpan()))
                    {
                        unaryFound = unaryOperator;
                        break;
                    }
                }
                foreach (var binaryOperator in binaryOperators)
                {
                    if (partTokens.SequenceEqual(binaryOperator.Token.AsSpan()))
                    {
                        binaryFound = binaryOperator;
                        break;
                    }
                }

                if (methodFound != null)
                    break; //ending ( guarentees can't be longer
            }
            if (methodFound != null)
            {
                var argumentParts = new List<StatementPart>();
                while (context.Index < context.Chars.Length)
                {
                    context.GroupStack.Push(MethodOperator.ArgumentOpener);
                    var argumentPart = ParseParts(ref context);
                    argumentParts.Add(argumentPart);

                    if (context.Current != MethodOperator.ArgumentSeperator)
                        break;

                    context.Next();
                }

                var part = new StatementPart(startIndex, methodFound, argumentParts);
                return part;
            }
            if (unaryFound != null && binaryFound != null)
            {
                if (unaryFound.Token.Length < binaryFound.Token.Length)
                    binaryFound = null;
                else if (unaryFound.Token.Length > binaryFound.Token.Length)
                    unaryFound = null;

                if (unaryFound != null && binaryFound != null)
                {
                    context.Reset(startIndex + unaryFound.Token.Length); //possibly looked for a longer operator name
                    var part = new StatementPart(startIndex, unaryFound, binaryFound);
                    return part;
                }
            }
            if (unaryFound != null)
            {
                context.Reset(startIndex + unaryFound.Token.Length); //possibly looked for a longer operator name
                var part = new StatementPart(startIndex, unaryFound);
                return part;
            }
            if (binaryFound != null)
            {
                context.Reset(startIndex + binaryFound.Token.Length); //possibly looked for a longer operator name
                var part = new StatementPart(startIndex, binaryFound);
                return part;
            }

            context.Reset(startIndex);
            return ParseGroupOpen(ref context);
        }
        private StatementPart ParseGroupOpen(ref ParserContext context)
        {
            if (groupOpeners.Contains(context.Current))
            {
                context.GroupStack.Push(context.Current);
                context.Next();
                return ParseParts(ref context);
            }
            return ParseGroupClose(ref context);
        }
        private StatementPart ParseGroupClose(ref ParserContext context)
        {
            if (groupClosers.Contains(context.Current) && context.GroupStack.Count > 0)
            {
                var lastGroupToken = context.GroupStack.Pop();
                var groupSet = groupSets.First(x => x[0] == lastGroupToken);
                if (groupSet[1] != context.Current)
                    throw new MathParserException($"Unexpected token '{context.Current}' at {context.Index}");
                context.Next();
                return null;
            }
            return ParseNumbersAndVariables(ref context);
        }
        private StatementPart ParseNumbersAndVariables(ref ParserContext context)
        {
            if (numbersAndLetters.Contains(context.Current))
            {
                var startIndex = context.Index;
                while (context.Index < context.Chars.Length)
                {
                    context.Next();
                    if (!numbersAndLetters.Contains(context.Current))
                    {
                        var length = context.Index - startIndex;
                        var token = context.Chars.Slice(startIndex, length).ToString();
                        var part = new StatementPart(startIndex, token);
                        return part;
                    }
                }
            }
            return null;
        }

        private Expression BuildLinqExpression(ref ParserContext context, StatementPart part)
        {
            if (part.SubParts != null)
                return BuildLinqExpression(ref context, part.SubParts, part.SubParts.Count);

            if (part.Variable != null)
            {
                var existing = context.LinqParameters.FirstOrDefault(x => x.Name == part.Variable);
                if (existing != null)
                    return existing;

                var expression = Expression.Parameter(typeof(double), part.Variable);
                context.LinqParameters.Add(expression);
                return expression;
            }
            else if (part.Number != null)
            {
                if (!Double.TryParse(part.Number, out var value))
                    throw new MathParserException($"Invalid expression \"{part.Number}\" at \"{part.Index}\"");
                var expression = Expression.Constant(value, typeof(double));
                return expression;
            }
            else
            {
                throw new MathParserException($"Invalid expression \"{part.Token}\" at \"{part.Index}\"");
            }
        }
        private Expression BuildLinqExpression(ref ParserContext context, IEnumerable<StatementPart> parts, int length)
        {
            foreach (var binaryOperator in binaryOperators)
            {
                var i = 0;
                foreach (var part in parts)
                {
                    if (part.BinaryOperator == binaryOperator)
                    {
                        if (i == 0 || i == length - 1)
                            break;
                        var firstOperand = BuildLinqExpression(ref context, parts.Take(i), i);
                        var secondOperand = BuildLinqExpression(ref context, parts.Skip(i + 1).Take(length - i - 1), length - i - 1);

                        var replacements = new Dictionary<Expression, Expression>
                        {
                            { part.BinaryOperator.Operation.Parameters[0], firstOperand },
                            { part.BinaryOperator.Operation.Parameters[1], secondOperand }
                        };
                        var expression = LinqRebinder.Rebind(part.BinaryOperator.Operation.Body, replacements);
                        return expression;
                    }
                    i++;
                }
            }

            foreach (var unaryOperator in unaryOperators)
            {
                var i = 0;
                foreach (var part in parts)
                {
                    if (part.UnaryOperator == unaryOperator)
                    {
                        if (part.UnaryOperator.LeftSideOperand)
                        {
                            if (i == 0)
                                break;
                            var operand = BuildLinqExpression(ref context, parts.Take(i), i);

                            var replacements = new Dictionary<Expression, Expression>
                            {
                                { part.UnaryOperator.Operation.Parameters[0], operand }
                            };
                            var expression = LinqRebinder.Rebind(part.UnaryOperator.Operation.Body, replacements);
                            return expression;
                        }
                        else
                        {
                            if (i == length - 1)
                                break;
                            var operand = BuildLinqExpression(ref context, parts.Skip(i + 1).Take(length - i - 1), length - i - 1);

                            var replacements = new Dictionary<Expression, Expression>
                            {
                                { part.UnaryOperator.Operation.Parameters[0], operand }
                            };
                            var expression = LinqRebinder.Rebind(part.UnaryOperator.Operation.Body, replacements);
                            return expression;
                        }
                    }
                    i++;
                }
            }

            {
                var i = 0;
                foreach (var part in parts)
                {
                    if (part.MethodOperator != null)
                    {
                        var replacements = new Dictionary<string, Expression>();
                        for (var j = 0; j < part.SubParts.Count; j++)
                        {
                            var operand = BuildLinqExpression(ref context, part.SubParts[j]);
                            var expressionString = Expression.ArrayIndex(part.MethodOperator.Operation.Parameters[0], Expression.Constant(j, typeof(int))).ToString();
                            replacements.Add(expressionString, operand);
                        };
                        foreach (var parameter in part.MethodOperator.Operation.Parameters)
                        {
                            replacements.Add(parameter.ToString(), Expression.Constant(Array.Empty<double>(), typeof(double[])));
                        }
                        var expressionArrayLengthString = Expression.ArrayLength(part.MethodOperator.Operation.Parameters[0]).ToString();
                        replacements.Add(expressionArrayLengthString, Expression.Constant(part.SubParts.Count, typeof(int)));
                        var expression = LinqRebinder.Rebind(part.MethodOperator.Operation.Body, replacements);
                        return expression;
                    }
                    i++;
                }
            }

            if (length == 1)
            {
                return BuildLinqExpression(ref context, parts.First());
            }

            var tokensStrings = String.Join("", parts.Select(x => x.ToString()));
            throw new MathParserException($"Operator not found in this sequence of expressions {tokensStrings}");
        }

        public override string ToString()
        {
            return mathExpression.ExpressionString;
        }
    }
}
