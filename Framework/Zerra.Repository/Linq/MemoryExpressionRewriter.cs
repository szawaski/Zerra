// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Zerra.Repository.Linq
{
    internal sealed class MemoryExpressionRewriter : ExpressionVisitor
    {
        private static readonly MethodInfo arrayContainsMethod = typeof(MemoryExpressionRewriter).GetMethod(nameof(ArrayContains), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo stringEqualsMethod = typeof(MemoryExpressionRewriter).GetMethod(nameof(StringEquals), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo elementMethod = typeof(MemoryExpressionRewriter).GetMethod(nameof(Element), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo enumNameMethod = typeof(EnumName).GetMethod(nameof(EnumName.GetName), [typeof(Type), typeof(object)])!;

        private readonly bool caseSensitive;
        //the parameters are elements of a join or a set, which may be a row that isn't there
        private readonly bool liftParameters;
        public MemoryExpressionRewriter(bool caseSensitive, bool liftParameters = false)
        {
            this.caseSensitive = caseSensitive;
            this.liftParameters = liftParameters;
        }

        public static TExpression Rewrite<TExpression>(TExpression expression, bool caseSensitive, bool liftParameters = false) where TExpression : Expression
            => (TExpression)new MemoryExpressionRewriter(caseSensitive, liftParameters).Visit(expression)!;

        //a value read from a row, which a database reads as null when the row or the value isn't there; a value the query is given is never lifted
        private bool MayBeMissing(Expression owner)
        {
            if (owner.Type.IsValueType)
                return false;
            var root = owner;
            while (true)
            {
                if (root is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs } convert)
                    root = convert.Operand;
                else if (root is MemberExpression { Expression: not null } member)
                    root = member.Expression;
                else
                    break;
            }
            if (root is not ParameterExpression)
                return false;
            return liftParameters || owner is not ParameterExpression && !(owner is UnaryExpression { Operand: ParameterExpression });
        }
        //the owner read into a variable, missing when it is null or anything it was read through is
        private Expression LiftOwner(Expression owner)
        {
            var source = VisitChild(owner, out var outer);
            var row = Expression.Variable(owner.Type, "row");
            var lift = new Lift();
            lift.Variables.Add(row);
            lift.Assignments.Add(Expression.Assign(row, outer is null ? source : Expression.Condition(outer.Missing, Expression.Constant(null, owner.Type), source)));
            lift.Missing = Expression.ReferenceEqual(row, Expression.Constant(null, owner.Type));
            pending = Lift.Merge(pending, Lift.Merge(outer, lift));
            return row;
        }

        //One row of a related collection, such as x.Lines.OrderBy(l => l.Price).First(), is a row a database reads as null when there is none, where LINQ fails or gives a default.
        //The row is read into a variable and whatever is computed from it carries that the row may be missing up to where a null makes a difference:
        //a comparison is false (not equal is true, as C# compares nulls), a condition is false, a value is null, and ?? takes its other side.
        private sealed class Lift
        {
            public readonly List<ParameterExpression> Variables = new();
            public readonly List<Expression> Assignments = new();
            public Expression Missing = null!;

            public static Lift? Merge(Lift? a, Lift? b)
            {
                if (a is null)
                    return b;
                if (b is null)
                    return a;
                var merged = new Lift();
                merged.Variables.AddRange(a.Variables);
                merged.Variables.AddRange(b.Variables);
                merged.Assignments.AddRange(a.Assignments);
                merged.Assignments.AddRange(b.Assignments);
                merged.Missing = Expression.OrElse(a.Missing, b.Missing);
                return merged;
            }
        }

        //the lifts of the children of the node being visited, which the node consumes or passes up
        private Lift? pending;

        public override Expression? Visit(Expression? node)
        {
            if (node is null)
                return null;
            var outer = pending;
            pending = null;
            var result = base.Visit(node);
            var inner = pending;
            pending = Lift.Merge(outer, inner);
            return result;
        }

        private Expression VisitChild(Expression child, out Lift? lift)
        {
            var saved = pending;
            pending = null;
            var result = Visit(child)!;
            lift = pending;
            pending = saved;
            return result;
        }

        private static Expression Consume(Expression value, Lift lift, Expression whenMissing)
        {
            var expressions = new Expression[lift.Assignments.Count + 1];
            lift.Assignments.CopyTo(expressions);
            expressions[^1] = Expression.Condition(lift.Missing, whenMissing, value);
            return Expression.Block(value.Type, lift.Variables, expressions);
        }

        private static bool IsElement(MethodCallExpression call)
            => call.Method.DeclaringType == typeof(Enumerable) && call.Method.Name is "First" or "FirstOrDefault" or "Single" or "SingleOrDefault" or "Last" or "LastOrDefault" or "ElementAt" or "ElementAtOrDefault";

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            //a query a condition looks in is read once for the whole run
            if (node.Object is ConstantExpression { Value: LinqSubquery subquery })
                return Expression.Call(Expression.Constant(subquery.Read()), typeof(HashSet<object>).GetMethod(nameof(HashSet<object>.Contains))!, Visit(node.Arguments[0])!);
            if (node.Method.DeclaringType == typeof(MemoryExtensions)
                && node.Method.Name == nameof(MemoryExtensions.Contains)
                && (node.Arguments.Count == 2 || (node.Arguments.Count == 3 && node.Arguments[2] is ConstantExpression { Value: null }))
                && node.Arguments[0] is MethodCallExpression spanConversion
                && spanConversion.Method.Name == "op_Implicit"
                && spanConversion.Arguments.Count == 1
                && spanConversion.Arguments[0].Type.IsArray)
            {
                var array = Visit(spanConversion.Arguments[0])!;
                var value = Visit(node.Arguments[1])!;
                return Expression.Call(arrayContainsMethod, array, Expression.Convert(value, typeof(object)));
            }
            //flags are written as EnumName writes them, the text a store keeps
            if (node.Method.Name == nameof(ToString) && node.Arguments.Count == 0 && node.Object is not null)
            {
                var flags = node.Object;
                while (flags is UnaryExpression { NodeType: ExpressionType.Convert } convert)
                    flags = convert.Operand;
                if (flags.Type.IsEnum && flags.Type.IsDefined(typeof(FlagsAttribute), false))
                    return Expression.Call(enumNameMethod, Expression.Constant(flags.Type), Expression.Convert(Visit(flags)!, typeof(object)));
            }
            if (IsElement(node))
            {
                var source = Visit(node.Arguments[0])!;
                Expression? predicate = null;
                Expression index = Expression.Constant(0);
                if (node.Method.Name is "ElementAt" or "ElementAtOrDefault")
                {
                    index = Visit(node.Arguments[1])!;
                }
                else if (node.Arguments.Count == 2)
                {
                    var argument = node.Arguments[1];
                    if (argument.NodeType == ExpressionType.Quote)
                        argument = ((UnaryExpression)argument).Operand;
                    var lambda = (LambdaExpression)Visit(argument)!;
                    var item = Expression.Parameter(typeof(object), "item");
                    predicate = Expression.Lambda<Func<object?, bool>>(Expression.Invoke(lambda, Expression.Convert(item, lambda.Parameters[0].Type)), item);
                }
                var element = Expression.Call(elementMethod, Expression.Convert(source, typeof(IEnumerable)), predicate ?? Expression.Constant(null, typeof(Func<object?, bool>)), index, Expression.Constant(node.Method.Name is "Last" or "LastOrDefault"));
                if (node.Type.IsValueType)
                    return Expression.Convert(element, node.Type);

                var row = Expression.Variable(node.Type, "row");
                var lift = new Lift();
                lift.Variables.Add(row);
                lift.Assignments.Add(Expression.Assign(row, Expression.Convert(element, node.Type)));
                lift.Missing = Expression.ReferenceEqual(row, Expression.Constant(null, node.Type));
                pending = Lift.Merge(pending, lift);
                return row;
            }
            //a call on a value of a row, such as x.Name.StartsWith("a"), is null when the value is, as in a database
            if (node.Object is not null && MayBeMissing(node.Object))
            {
                var instance = LiftOwner(node.Object);
                return node.Update(instance, Visit(node.Arguments));
            }
            return base.VisitMethodCall(node);
        }

        //a member of a related model, of an element of a join, or of a value of a row reads as null when that isn't there, as a database reads it
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is null || !MayBeMissing(node.Expression))
                return base.VisitMember(node);
            return Expression.MakeMemberAccess(LiftOwner(node.Expression), node.Member);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = VisitChild(node.Left, out var leftLift);
            var right = VisitChild(node.Right, out var rightLift);
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    {
                        Expression comparison;
                        if (!caseSensitive && node.NodeType == ExpressionType.Equal && left.Type == typeof(string) && right.Type == typeof(string))
                            comparison = Expression.Call(stringEqualsMethod, left, right);
                        else
                            comparison = node.Update(left, node.Conversion, right);
                        var lift = Lift.Merge(leftLift, rightLift);
                        return lift is null ? comparison : Consume(comparison, lift, Expression.Constant(node.NodeType == ExpressionType.NotEqual));
                    }
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.ExclusiveOr:
                    if (node.Type == typeof(bool))
                    {
                        if (leftLift is not null)
                            left = Consume(left, leftLift, Expression.Constant(false));
                        if (rightLift is not null)
                            right = Consume(right, rightLift, Expression.Constant(false));
                        return node.Update(left, node.Conversion, right);
                    }
                    pending = Lift.Merge(leftLift, rightLift);
                    return node.Update(left, node.Conversion, right);
                case ExpressionType.Coalesce:
                    if (leftLift is not null)
                        left = Consume(left, leftLift, Expression.Constant(null, left.Type));
                    pending = rightLift;
                    return node.Update(left, node.Conversion, right);
                default:
                    pending = Lift.Merge(leftLift, rightLift);
                    return node.Update(left, node.Conversion, right);
            }
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var test = VisitChild(node.Test, out var testLift);
            if (testLift is not null)
                test = Consume(test, testLift, Expression.Constant(false));
            //a branch is only read when it's taken, as CASE reads it
            var ifTrue = VisitChild(node.IfTrue, out var trueLift);
            if (trueLift is not null)
                ifTrue = Consume(ifTrue, trueLift, Missing(ifTrue.Type));
            var ifFalse = VisitChild(node.IfFalse, out var falseLift);
            if (falseLift is not null)
                ifFalse = Consume(ifFalse, falseLift, Missing(ifFalse.Type));
            return node.Update(test, ifTrue, ifFalse);
        }

        //each member of an object built is a column of its own, null when what it reads isn't there
        protected override Expression VisitNew(NewExpression node)
        {
            var arguments = new Expression[node.Arguments.Count];
            for (var i = 0; i < arguments.Length; i++)
            {
                arguments[i] = VisitChild(node.Arguments[i], out var lift);
                if (lift is not null)
                    arguments[i] = Consume(arguments[i], lift, Missing(arguments[i].Type));
            }
            return node.Update(arguments);
        }
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            var value = VisitChild(node.Expression, out var lift);
            if (lift is not null)
                value = Consume(value, lift, Missing(value.Type));
            return node.Update(value);
        }
        private static Expression Missing(Type type) => type == typeof(bool) ? Expression.Constant(false) : Expression.Default(type);

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var body = VisitChild(node.Body, out var lift);
            if (lift is not null)
                body = Consume(body, lift, body.Type == typeof(bool) ? Expression.Constant(false) : Expression.Default(body.Type));
            return node.Update(body, node.Parameters);
        }

        //not generic so nothing needs to be generated for each element type, Array.IndexOf compares with Equals like Enumerable.Contains
        private static bool ArrayContains(Array array, object? value) => Array.IndexOf(array, value) >= 0;
        private static bool StringEquals(string s1, string s2) => String.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase);
        //the row a database picks: the first matching the condition after skipping index of them, or the last, and null when there is none
        private static object? Element(IEnumerable? source, Func<object?, bool>? predicate, int index, bool last)
        {
            if (source is null)
                return null;
            object? found = null;
            var skipped = 0;
            foreach (var item in source)
            {
                if (predicate is not null && !predicate(item))
                    continue;
                if (last)
                {
                    found = item;
                    continue;
                }
                if (skipped++ == index)
                    return item;
            }
            return found;
        }
    }
}
