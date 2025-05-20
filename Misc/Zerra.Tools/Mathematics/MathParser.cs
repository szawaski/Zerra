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
        public static IEnumerable<string> MethodOperators => methodOperators.Select(x => x.Token);
        public static IEnumerable<string> TertiaryOperators => tertiaryOperators.Select(x => x.Token1 + x.Token2);
        public static IEnumerable<string> BinaryOperators => binaryOperators.Select(x => x.Token);
        public static IEnumerable<string> UnaryOperators => unaryOperators.Select(x => x.Token);

        private static readonly Dictionary<char, char> groupSets = new() { { '(', ')' }, { '[', ']' } };
        private static readonly HashSet<char> groupOpeners = "([".ToHashSet();
        private static readonly HashSet<char> groupClosers = ")]".ToHashSet();
        private static readonly HashSet<char> numbers = "0123456789.".ToHashSet();
        private static readonly HashSet<char> numbersAndLetters = "0123456789.ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToHashSet();

        private static readonly List<UnaryOperator> unaryOperators = new();
        private static readonly List<BinaryOperator> binaryOperators = new();
        private static readonly List<TertiaryOperator> tertiaryOperators = new();
        private static readonly List<MethodOperator> methodOperators = new();

        private static int operatorStringsMaxLength = 0;

        private static readonly ConcurrentFactoryDictionary<string, CompiledExpression> cache = new();

        private readonly CompiledExpression mathExpression;

        public IEnumerable<string> Parameters { get { return mathExpression.Parameters.Select(x => x.Name ?? throw new InvalidOperationException("Parameter does not have a name")); } }
        public string Equation { get { return mathExpression.ExpressionString; } }

        public MathParser(string expression)
        {
            mathExpression = BuildMathExpression(expression);
        }

        public double Evalutate(IDictionary<string, double> parameters)
        {
            var args = new object[mathExpression.Parameters.Length];
            for (var i = 0; i < mathExpression.Parameters.Length; i++)
            {
                var name = mathExpression.Parameters[i].Name;
                if (name is null)
                    throw new MathParserException($"Parameter has no name");
                if (!parameters.TryGetValue(name, out var arg))
                    throw new MathParserException($"Missing parameter {name}");
                args[i] = arg;
            }
            var result = (double)mathExpression.Expression.DynamicInvoke(args)!;
            return result;
        }

        public double Evalutate(params double[] parameters)
        {
            if (parameters.Length != mathExpression.Parameters.Length)
                throw new MathParserException($"Invalid number of parameters");

            var args = new object[mathExpression.Parameters.Length];
            for (var i = 0; i < mathExpression.Parameters.Length; i++)
                args[i] = parameters[i];
            var result = (double)mathExpression.Expression.DynamicInvoke(args)!;
            return result;
        }

        private static CompiledExpression BuildMathExpression(string expression)
        {
            var compiledExpression = cache.GetOrAdd(expression, (key) =>
            {
                var context = new ParserContext(key);
                context.Next();
                var part = ParseParts(ref context);
                if (context.GroupStack.Count > 0)
                {
                    var lastGroupToken = context.GroupStack.Pop();
                    var groupEnder = groupSets[lastGroupToken];
                    throw new MathParserException($"Ended without closing group {groupEnder}");
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

        private static StatementPart ParseParts(ref ParserContext context)
        {
            var subparts = new List<StatementPart>();
            var startIndex = context.Index;
            while (context.Index < context.Chars.Length)
            {
                while (Char.IsWhiteSpace(context.Current) && context.Index < context.Chars.Length)
                    context.Next();
                var subpart = ParseOperators(ref context);
                if (subpart is null)
                    break;
                subparts.Add(subpart);
            }
            while (Char.IsWhiteSpace(context.Current) && context.Index < context.Chars.Length)
                context.Next();
            var part = new StatementPart(startIndex, subparts);
            return part;
        }
        private static StatementPart? ParseOperators(ref ParserContext context)
        {
            var startIndex = context.Index;
            UnaryOperator? unaryFound = null;
            BinaryOperator? binaryFound = null;
            TertiaryOperator? tertiaryFound1 = null;
            TertiaryOperator? tertiaryFound2 = null;
            MethodOperator? methodFound = null;
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
                foreach (var tertiaryOperator in tertiaryOperators)
                {
                    if (partTokens.SequenceEqual(tertiaryOperator.Token1.AsSpan()))
                    {
                        tertiaryFound1 = tertiaryOperator;
                        break;
                    }
                    if (partTokens.SequenceEqual(tertiaryOperator.Token2.AsSpan()))
                    {
                        tertiaryFound2 = tertiaryOperator;
                        break;
                    }
                }

                if (methodFound is not null)
                    break; //ending ( guarentees can't be longer
            }
            if (methodFound is not null)
            {
                var argumentParts = new List<StatementPart>();
                while (context.Index < context.Chars.Length)
                {
                    //context.GroupStack.Push(MethodOperator.ArgumentOpener);
                    var argumentPart = ParseParts(ref context);
                    argumentParts.Add(argumentPart);

                    if (context.Current == MethodOperator.ArgumentCloser)
                    {
                        context.Next();
                        break;
                    }
                    if (context.Current == MethodOperator.ArgumentSeperator)
                    {
                        context.Next();
                        continue;
                    }
                    throw new MathParserException($"Unexpected token '{context.Current}' at {context.Index}");
                }

                var part = new StatementPart(startIndex, methodFound, argumentParts);
                return part;
            }
            if (unaryFound is not null && binaryFound is not null)
            {
                if (unaryFound.Token.Length < binaryFound.Token.Length)
                    binaryFound = null;
                else if (binaryFound.Token.Length > unaryFound.Token.Length)
                    unaryFound = null;

                if (unaryFound is not null && binaryFound is not null)
                {
                    context.Reset(startIndex + unaryFound.Token.Length); //possibly looked for a longer operator name
                    var part = new StatementPart(startIndex, unaryFound, binaryFound);
                    return part;
                }
            }
            if (unaryFound is not null)
            {
                context.Reset(startIndex + unaryFound.Token.Length); //possibly looked for a longer operator name
                var part = new StatementPart(startIndex, unaryFound);
                return part;
            }
            if (binaryFound is not null)
            {
                context.Reset(startIndex + binaryFound.Token.Length); //possibly looked for a longer operator name
                var part = new StatementPart(startIndex, binaryFound);
                return part;
            }
            if (tertiaryFound1 is not null)
            {
                context.Reset(startIndex + tertiaryFound1.Token1.Length); //possibly looked for a longer operator name
                var part = new StatementPart(startIndex, tertiaryFound1, false);
                return part;
            }
            if (tertiaryFound2 is not null)
            {
                context.Reset(startIndex + tertiaryFound2.Token2.Length); //possibly looked for a longer operator name
                var part = new StatementPart(startIndex, tertiaryFound2, true);
                return part;
            }

            context.Reset(startIndex);
            return ParseGroup(ref context);
        }
        private static StatementPart? ParseGroup(ref ParserContext context)
        {
            if (groupOpeners.Contains(context.Current))
            {
                context.GroupStack.Push(context.Current);
                context.Next();
                return ParseParts(ref context);
            }
            if (groupClosers.Contains(context.Current) && context.GroupStack.Count > 0)
            {
                var lastGroupToken = context.GroupStack.Pop();
                var groupEnder = groupSets[lastGroupToken];
                if (groupEnder != context.Current)
                    throw new MathParserException($"Unexpected token '{context.Current}' at {context.Index}");
                context.Next();
                return null;
            }
            return ParseNumbersAndVariables(ref context);
        }
        private static StatementPart? ParseNumbersAndVariables(ref ParserContext context)
        {
            if (numbersAndLetters.Contains(context.Current))
            {
                var startIndex = context.Index;
                while (context.Index < context.Chars.Length)
                {
                    context.Next();
                    if (!numbersAndLetters.Contains(context.Current))
                    {
                        var token = context.Chars[startIndex..context.Index].ToString();
                        var part = new StatementPart(startIndex, token);
                        return part;
                    }
                }
            }
            return null;
        }

        private static Expression BuildLinqExpression(ref ParserContext context, StatementPart part)
        {
            if (part.SubParts is not null)
                return BuildLinqExpression(ref context, part.SubParts, part.SubParts.Count);

            if (part.Variable is not null)
            {
                var existing = context.LinqParameters.FirstOrDefault(x => x.Name == part.Variable);
                if (existing is not null)
                    return existing;

                var expression = Expression.Parameter(typeof(double), part.Variable);
                context.LinqParameters.Add(expression);
                return expression;
            }
            else if (part.Number is not null)
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
        private static Expression BuildLinqExpression(ref ParserContext context, IEnumerable<StatementPart> parts, int length)
        {
            foreach (var tertiaryOperator in tertiaryOperators)
            {
                var i = 0;
                foreach (var part in parts)
                {
                    if (part.TertiaryOperator1 == tertiaryOperator)
                    {
                        if (i == 0 || i == length - 1)
                            break;
                        var firstOperand = BuildLinqExpression(ref context, parts.Take(i), i);

                        var i2 = i + 1;
                        foreach (var p in parts.Skip(i + 1))
                        {
                            if (p.TertiaryOperator2 == tertiaryOperator)
                                break;
                            i2++;
                        }
                        if (i2 == length)
                            throw new MathParserException($"Tertiary Operator missing second token in this sequence of expressions {String.Join("", parts.Select(x => x.ToString()))}");

                        var secondOperand = BuildLinqExpression(ref context, parts.Skip(i + 1).Take(i2 - i - 1), i2 - i - 1);
                        var thirdOperand = BuildLinqExpression(ref context, parts.Skip(i2 + 1), length - i2 - 1);

                        var replacements = new Dictionary<Expression, Expression>
                        {
                            { part.TertiaryOperator1.Operation.Parameters[0], firstOperand },
                            { part.TertiaryOperator1.Operation.Parameters[1], secondOperand },
                            { part.TertiaryOperator1.Operation.Parameters[2], thirdOperand }
                        };
                        var expression = LinqRebinder.Rebind(part.TertiaryOperator1.Operation.Body, replacements);
                        return expression;
                    }
                    i++;
                }
            }

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
                        var secondOperand = BuildLinqExpression(ref context, parts.Skip(i + 1), length - i - 1);

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
                            var operand = BuildLinqExpression(ref context, parts.Skip(i + 1), length - i - 1);

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

            foreach (var part in parts)
            {
                if (part.SubParts is null || part.MethodOperator is null)
                    continue;

                var replacements = new Dictionary<string, Expression>();

                for (var j = 0; j < part.SubParts.Count; j++)
                {
                    var operand = BuildLinqExpression(ref context, part.SubParts[j]);
                    var expressionString = Expression.ArrayIndex(part.MethodOperator.Operation.Parameters[0], Expression.Constant(j, typeof(int))).ToString();
                    replacements.Add(expressionString, operand);
                }

                foreach (var parameter in part.MethodOperator.Operation.Parameters)
                {
                    replacements.Add(parameter.ToString(), Expression.Constant(Array.Empty<double>(), typeof(double[])));
                }
                var expressionArrayLengthString = Expression.ArrayLength(part.MethodOperator.Operation.Parameters[0]).ToString();
                replacements.Add(expressionArrayLengthString, Expression.Constant(part.SubParts.Count, typeof(int)));
                var expression = LinqRebinder.Rebind(part.MethodOperator.Operation.Body, replacements);
                return expression;
            }

            if (length == 1)
            {
                return BuildLinqExpression(ref context, parts.First());
            }

            throw new MathParserException($"Operator not found in this sequence of expressions {String.Join("", parts.Select(x => x.ToString()))}");
        }

        public override string ToString()
        {
            return mathExpression.ExpressionString;
        }
    }
}
