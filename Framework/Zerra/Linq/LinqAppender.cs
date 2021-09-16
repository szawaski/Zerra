// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Reflection;

namespace Zerra.Linq
{
    public static class LinqAppender
    {
        public static Expression<Func<T, bool>> AppendAnd<T>(Expression<Func<T, bool>> it, params Expression<Func<T, bool>>[] expressions)
        {
            LambdaExpression itLambda = it as LambdaExpression;
            Expression exp = itLambda.Body;
            ParameterExpression parameter = itLambda.Parameters[0];
            foreach (Expression expression in expressions)
            {
                LambdaExpression expressionLambda = expression as LambdaExpression;
                Expression convertedExpression = LinqRebinder.RebindExpression(expressionLambda.Body, expressionLambda.Parameters[0], parameter);
                exp = Expression.AndAlso(exp, convertedExpression);
            }
            return Expression.Lambda<Func<T, bool>>(exp, itLambda.Parameters);
        }

        public static Expression<Func<T, bool>> AppendOr<T>(Expression<Func<T, bool>> it, params Expression<Func<T, bool>>[] expressions)
        {
            LambdaExpression itLambda = it as LambdaExpression;
            Expression exp = itLambda.Body;
            ParameterExpression parameter = itLambda.Parameters[0];
            foreach (Expression expression in expressions)
            {
                LambdaExpression expressionLambda = expression as LambdaExpression;
                Expression convertedExpression = LinqRebinder.RebindExpression(expressionLambda.Body, expressionLambda.Parameters[0], parameter);
                exp = Expression.AndAlso(exp, convertedExpression);
            }
            return Expression.Lambda<Func<T, bool>>(exp, itLambda.Parameters);
        }

        private static readonly MethodInfo anyMethod1 = typeof(Enumerable).GetMethods().First(m => m.Name == "Any" && m.GetParameters().Length == 1);
        private static readonly MethodInfo anyMethod2 = typeof(Enumerable).GetMethods().First(m => m.Name == "Any" && m.GetParameters().Length == 2);
        public static Expression<Func<T, bool>> AppendExpressionOnMember<T>(Expression<Func<T, bool>> it, MemberInfo member, params Expression[] expressions)
        {
            PropertyInfo propertyInfo = null;
            FieldInfo fieldInfo = null;
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

            LambdaExpression itLambda = (LambdaExpression)it;
            Expression exp = itLambda.Body;
            ParameterExpression parameter = itLambda.Parameters[0];
            foreach (Expression expression in expressions)
            {
                LambdaExpression expressionLambda = expression as LambdaExpression;

                var typeDetails = TypeAnalyzer.GetType(type);
                Type elementType = null;
                if (type.IsArray)
                {
                    elementType = typeDetails.InnerTypes[0];
                }
                else
                {
                    if (typeDetails.InnerTypes.Count == 1)
                    {
                        elementType = typeDetails.InnerTypes[0];
                    }
                }

                if (elementType != null && typeDetails.IsIEnumerable)
                {
                    Expression memberExpression = member.MemberType == MemberTypes.Property ?
                        Expression.Property(parameter, propertyInfo) :
                        Expression.Field(parameter, fieldInfo);

                    MethodInfo anyMethod2Generic = TypeAnalyzer.GetGenericMethod(anyMethod2, elementType).MethodInfo;
                    MethodCallExpression callAny2 = Expression.Call(anyMethod2Generic, memberExpression, expressionLambda);

                    MethodInfo anyMethod1Generic = TypeAnalyzer.GetGenericMethod(anyMethod1, elementType).MethodInfo;
                    MethodCallExpression callAny1 = Expression.Call(anyMethod1Generic, memberExpression);

                    Expression emptyCheckExpression = Expression.OrElse(Expression.Not(callAny1), callAny2);

                    if (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value?.ToString().ToBoolean() == true)
                        exp = emptyCheckExpression;
                    else
                        exp = Expression.AndAlso(exp, emptyCheckExpression);
                }
                else
                {
                    Expression memberExpression = member.MemberType == MemberTypes.Property ?
                        Expression.Property(parameter, propertyInfo) :
                        Expression.Field(parameter, fieldInfo);

                    LambdaExpression convertedExpressionLambda = (LambdaExpression)LinqRebinder.RebindExpression(expressionLambda, expressionLambda.Parameters[0], memberExpression);

                    Expression nullCheckExpression = Expression.OrElse(Expression.Equal(memberExpression, Expression.Constant(null)), convertedExpressionLambda.Body);

                    if (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value?.ToString().ToBoolean() == true)
                        exp = nullCheckExpression;
                    else
                        exp = Expression.AndAlso(exp, nullCheckExpression);
                }
            }
            return Expression.Lambda<Func<T, bool>>(exp, itLambda.Parameters);
        }
    }
}
