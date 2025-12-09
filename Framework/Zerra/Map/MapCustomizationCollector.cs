// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Linq;

namespace Zerra.Map
{
    internal sealed class MapCustomizationCollector<TSource, TTarget> : IMapSetup<TSource, TTarget>
    {
        public readonly List<MapCustomizationInfo> Results = new();

        public void Define<TPropertyValue, TValue>(Expression<Func<TTarget, TPropertyValue?>> property, Expression<Func<TSource, TValue?>> value)
        {
            var (targetName, targetResultType, targetSetter) = CreateTargetSetter(property);
            var (sourceResultType, sourceGetter) = CreateSourceGetter(value);
            Results.Add(new(false, targetName, targetResultType, targetSetter, sourceResultType, sourceGetter));
        }

        public void DefineTwoWay<TPropertyValue1, TPropertyValue2>(Expression<Func<TTarget, TPropertyValue1?>> property1, Expression<Func<TSource, TPropertyValue2?>> property2)
        {
            {
                var (property1Name, property1ResultType, property1Setter) = CreateTargetSetter(property1);
                var (property2ResultType, property2Getter) = CreateSourceGetter(property2);
                Results.Add(new(false, property1Name, property1ResultType, property1Setter, property2ResultType, property2Getter));
            }
            {
                var (property2Name, property2ResultType, property2Setter) = CreateTargetSetter(property2);
                var (property1ResultType, property1Getter) = CreateSourceGetter(property1);
                Results.Add(new(true, property2Name, property2ResultType, property2Setter, property1ResultType, property1Getter));
            }
        }

        public void DefineReverse<TPropertyValue, TValue>(Expression<Func<TSource, TPropertyValue?>> property, Expression<Func<TTarget, TValue?>> value)
        {
            var (targetName, targetResultType, targetSetter) = CreateTargetSetter(property);
            var (sourceResultType, sourceGetter) = CreateSourceGetter(value);
            Results.Add(new(true, targetName, targetResultType, targetSetter, sourceResultType, sourceGetter));
        }

        private static (Type, Func<object, V?>) CreateSourceGetter<T, V>(Expression<Func<T, V?>> value)
        {
            var sourceParameterParent = Expression.Parameter(typeof(object), "parent");
            var rebindedExpression = LinqRebinder.RebindExpression(value.Body, value.Parameters[0], Expression.Convert(sourceParameterParent, value.Parameters[0].Type));
            var getterExpression = Expression.Lambda<Func<object, V?>>(rebindedExpression, sourceParameterParent);
            var sourceResultType = typeof(V);
            var getter = getterExpression.Compile();
            return (sourceResultType, getter);
        }

        private static (string, Type, Action<object, V?>) CreateTargetSetter<T, V>(Expression<Func<T, V?>> property)
        {
            string? targetName = null;
            if (property.Body is not MemberExpression memberExpression)
                throw new NotSupportedException($"Map property expression must be a member accessor");

            targetName = memberExpression.Member.Name;
            var targetResultType = typeof(V);

            var targetParameterParent = Expression.Parameter(typeof(object), "parent");
            var targetParameterValue = Expression.Parameter(targetResultType, "value");

            var rebindedExpression = LinqRebinder.RebindExpression(memberExpression, property.Parameters[0], Expression.Convert(targetParameterParent, property.Parameters[0].Type));

            var assignExpression = Expression.Assign(rebindedExpression, targetParameterValue);

            var setterExpression = Expression.Lambda<Action<object, V?>>(assignExpression, targetParameterParent, targetParameterValue);
            var setter = setterExpression.Compile();

            return (targetName, targetResultType, setter);
        }
    }
}
