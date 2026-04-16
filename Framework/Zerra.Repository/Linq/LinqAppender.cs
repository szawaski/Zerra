// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using System.Reflection;
using Zerra.Reflection;

namespace Zerra.Linq
{
    public static class LinqAppender
    {
        public static Expression<Func<T, bool>> AppendAnd<T>(LambdaExpression it, LambdaExpression expression)
        {
            var exp = it.Body;
            var parameter = it.Parameters[0];

            var convertedExpression = LinqRebinder.RebindExpression(expression.Body, expression.Parameters[0], parameter);
            exp = Expression.AndAlso(exp, convertedExpression);

            return Expression.Lambda<Func<T, bool>>(exp, it.Parameters);
        }

        private static readonly MethodInfo anyMethod1 = typeof(Enumerable).GetMethods().First(m => m.Name == "Any" && m.GetParameters().Length == 1);
        private static readonly MethodInfo anyMethod2 = typeof(Enumerable).GetMethods().First(m => m.Name == "Any" && m.GetParameters().Length == 2);
        public static Expression<Func<T, bool>> AppendExpressionOnMember<T>(Expression<Func<T, bool>> it, MemberInfo member, LambdaExpression expression)
        {
            PropertyInfo? propertyInfo = null;
            FieldInfo? fieldInfo = null;
            Type type;
            if (member.MemberType == MemberTypes.Property)
            {
                propertyInfo = (PropertyInfo)member;
                type = propertyInfo.PropertyType;
            }
            else if (member.MemberType == MemberTypes.Field)
            {
                fieldInfo = (FieldInfo)member;
                type = fieldInfo.FieldType;
            }
            else
            {
                throw new ArgumentException("Member is not a property or a field");
            }

            var itLambda = (LambdaExpression)it;
            var exp = itLambda.Body;
            var parameter = itLambda.Parameters[0];

            var typeDetails = TypeAnalyzer.GetTypeDetail(type);
            if (typeDetails.HasIEnumerable)
            {
                Expression memberExpression = member.MemberType == MemberTypes.Property ?
                    Expression.Property(parameter, propertyInfo!) :
                    Expression.Field(parameter, fieldInfo!);

                var anyMethod2Generic = anyMethod2.MakeGenericMethod(typeDetails.InnerType);
                var callAny2 = Expression.Call(anyMethod2Generic, memberExpression, expression);

                var anyMethod1Generic = anyMethod1.MakeGenericMethod(typeDetails.InnerType);
                var callAny1 = Expression.Call(anyMethod1Generic, memberExpression);

                Expression emptyCheckExpression = Expression.OrElse(Expression.Not(callAny1), callAny2);

                if (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value?.ToString()?.ToBoolean() == true)
                    exp = emptyCheckExpression;
                else
                    exp = Expression.AndAlso(exp, emptyCheckExpression);
            }
            else
            {
                Expression memberExpression = member.MemberType == MemberTypes.Property ?
                    Expression.Property(parameter, propertyInfo!) :
                    Expression.Field(parameter, fieldInfo!);

                var convertedExpressionLambda = (LambdaExpression)LinqRebinder.RebindExpression(expression, expression.Parameters[0], memberExpression);

                Expression nullCheckExpression = Expression.OrElse(Expression.Equal(memberExpression, Expression.Constant(null)), convertedExpressionLambda.Body);

                if (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value?.ToString()?.ToBoolean() == true)
                    exp = nullCheckExpression;
                else
                    exp = Expression.AndAlso(exp, nullCheckExpression);
            }
            return Expression.Lambda<Func<T, bool>>(exp, itLambda.Parameters);
        }
    }
}
