// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using System.Reflection;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Repository.Reflection
{
    public static class ModelAnalyzer
    {
        private static readonly ConcurrentFactoryDictionary<Type, ModelDetail> modelInfos = new();
        public static ModelDetail GetModel<TModel>()
        {
            var type = typeof(TModel);

            return GetModel(type);
        }
        public static ModelDetail GetModel(Type type)
        {
            var modelInfo = modelInfos.GetOrAdd(type, static (type) =>
            {
                var typeDetails = TypeAnalyzer.GetTypeDetail(type);
                return new ModelDetail(typeDetails);
            });
            return modelInfo;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, object?> getterFunctionsByAttribute = new();
        private static Func<object, object?>? GetGetterFunctionByNameOrAttribute(Type type, string? propertyNames, Type? attributeType)
        {
            TypeKey key;
            if (attributeType is not null)
                key = new TypeKey(propertyNames, type, attributeType);
            else
                key = new TypeKey(propertyNames, type);

            var getter = getterFunctionsByAttribute.GetOrAdd(key, type, propertyNames, attributeType, static (type, propertyNames, attributeType) => GenerateGetterFunctionByNameOrAttribute(type, propertyNames, attributeType));

            var expression = (Func<object, object>?)getter;
            return expression;
        }
        private static Func<object, object>? GenerateGetterFunctionByNameOrAttribute(Type type, string? propertyNames, Type? attributeType)
        {
            var propertyNamesArray = propertyNames is null ? null : propertyNames.Split(',');

            var sourceExpression = Expression.Parameter(type, "x");

            var typeProperties = type.GetProperties();
            var propertyExpressions = new List<Expression>();
            foreach (var typeProperty in typeProperties)
            {
                if (propertyNamesArray is not null && propertyNamesArray.Contains(typeProperty.Name))
                {
                    Expression propertyExpression = Expression.Property(sourceExpression, typeProperty);
                    propertyExpressions.Add(propertyExpression);
                }
                else if (attributeType is not null)
                {
                    var attribute = typeProperty.GetCustomAttribute(attributeType, true);
                    if (attribute is not null)
                    {
                        Expression propertyExpression = Expression.Property(sourceExpression, typeProperty);
                        propertyExpressions.Add(propertyExpression);
                    }
                }
            }

            Expression? accessor = null;
            if (propertyExpressions.Count == 1)
            {
                accessor = Expression.Convert(propertyExpressions.Single(), typeof(object));
            }
            else if (propertyExpressions.Count > 1)
            {
                accessor = Expression.NewArrayInit(typeof(object), propertyExpressions.Select(x => Expression.Convert(x, typeof(object))));
            }
            else
            {
                //return null;
                throw new InvalidOperationException($"Getter function not found on {type.Name}");
            }

            var lambda = Expression.Lambda<Func<object, object>>(accessor, sourceExpression);
            return lambda.Compile();
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, object> setterFunctionsByAttribute = new();
        private static Action<object, object?> GetSetterFunctionByNameOrAttribute(Type type,string? propertyNames, Type? attributeType)
        {
            TypeKey key;
            if (attributeType is not null)
                key = new TypeKey(propertyNames, type, attributeType);
            else
                key = new TypeKey(propertyNames, type);

            var setter = setterFunctionsByAttribute.GetOrAdd(key, type, propertyNames, attributeType, static (type, propertyNames, attributeType) => GenerateSetterFunctionByNameOrAttribute(type, propertyNames, attributeType));

            var expression = (Action<object, object?>)setter;
            return expression;
        }
        private static Action<object, object?> GenerateSetterFunctionByNameOrAttribute(Type type, string? propertyNames, Type? attributeType)
        {
            var propertyNamesArray = propertyNames is null ? null : propertyNames.Split(',');

            var sourceExpression = Expression.Parameter(typeof(object), "x");
            var parameterValue = Expression.Parameter(typeof(object), "y");

            var typeProperties = type.GetProperties();
            var propertyExpressions = new List<Expression>();
            foreach (var typeProperty in typeProperties)
            {
                if (propertyNamesArray is not null && propertyNamesArray.Contains(typeProperty.Name))
                {
                    Expression propertyExpression = Expression.Property(sourceExpression, typeProperty);
                    propertyExpressions.Add(propertyExpression);
                }
                else if (attributeType is not null)
                {
                    var attribute = typeProperty.GetCustomAttribute(attributeType, true);
                    if (attribute is not null)
                    {
                        Expression propertyExpression = Expression.Property(sourceExpression, typeProperty);
                        propertyExpressions.Add(propertyExpression);
                    }
                }
            }

            Expression setter;
            if (propertyExpressions.Count == 1)
            {
                setter = Expression.Assign(propertyExpressions.Single(), Expression.Convert(parameterValue, propertyExpressions.Single().Type));
            }
            else if (propertyExpressions.Count > 1)
            {
                var setters = new List<Expression>();
                var index = 0;
                foreach (var propertyExpression in propertyExpressions)
                {
                    Expression parameterValueIndex = Expression.ArrayIndex(parameterValue, Expression.Constant(index));
                    Expression subsetter = Expression.Assign(propertyExpression, Expression.Convert(parameterValueIndex, propertyExpression.Type));
                    setters.Add(subsetter);
                    index++;
                }
                setter = Expression.Block(setters.ToArray());
            }
            else
            {
                throw new InvalidOperationException($"Setter function not found on {type.Name}");
            }

            var lambda = Expression.Lambda<Action<object, object?>>(setter, sourceExpression, parameterValue);
            return lambda.Compile();
        }

        private static readonly ConcurrentFactoryDictionary<Type, string[]> identityPropertyNames = new();
        public static string[] GetIdentityPropertyNames(Type type)
        {
            var names = identityPropertyNames.GetOrAdd(type, GenerateIdentityPropertyNames);
            if (names.Length == 0)
                throw new Exception($"Model {type} missing Identity");
            return names;
        }
        private static string[] GenerateIdentityPropertyNames(Type type)
        {
            var properties = new List<PropertyInfo>();
            var typeProperties = type.GetProperties();
            foreach (var typeProperty in typeProperties)
            {
                var attribute = typeProperty.GetCustomAttribute(typeof(IdentityAttribute), true);
                if (attribute is not null)
                {
                    properties.Add(typeProperty);
                }
            }
            return properties.Select(x => x.Name).ToArray();
        }

        public static object GetIdentity(Type type, object model)
        {
            var modelIdentityAccessor = GetGetterFunctionByNameOrAttribute(type, null, typeof(IdentityAttribute));
            if (modelIdentityAccessor is null)
                throw new Exception($"Model {type.Name} missing Identity");
            var id = modelIdentityAccessor.Invoke(model);
            if (id is null)
                throw new Exception($"Model {type.Name} missing Identity");
            return id;
        }

        public static void SetIdentity(Type type, object model, object? identity)
        {
            var setter = GetSetterFunctionByNameOrAttribute(type, null, typeof(IdentityAttribute));
            setter.Invoke(model, identity);
        }

        public static object? GetForeignIdentity(Type type, string foreignIdentityNames, object model)
        {
            var modelIdentityAccessor = GetGetterFunctionByNameOrAttribute(type, foreignIdentityNames, null);
            if (modelIdentityAccessor is null)
                throw new Exception($"Model {type.Name} missing Foreign Identity");
            var id = modelIdentityAccessor.Invoke(model);
            return id;
        }

        public static void SetForeignIdentity(Type type, string foreignIdentityNames, object model, object identity)
        {
            var setter = GetSetterFunctionByNameOrAttribute(type, foreignIdentityNames, null);
            setter.Invoke(model, identity);
        }

        public static bool CompareIdentities(object? identity1, object? identity2)
        {
            if (identity1 is null)
                return identity2 is null;
            if (identity2 is null)
                return false;

            if (identity1.Equals(identity2))
                return true;

            if (identity1 is object?[] array1)
            {
                if (identity2 is not object?[] array2)
                    return false;
                if (array1.Length != array2.Length)
                    return false;
                for (var i = 0; i < array1.Length; i++)
                {
                    var value1 = array1[i];
                    var value2 = array2[i];

                    if (value1 is null)
                    {
                        if (value2 is null)
                            continue;
                        return false;
                    }
                    if (value2 is null)
                        return false;
                    if (!value1.Equals(value2))
                        return false;
                }
                return true;
            }

            return false;
        }

        public static Expression<Func<TModel, bool>>? GetIdentityExpression<TModel>(object identity)
        {
            var type = typeof(TModel);
            var queryExpressionParameter = Expression.Parameter(type, "x");
            var identityProperties = GetModel(type).IdentityProperties;

            if (identityProperties.Count == 0)
                return null;

            if (identity is not object[] ids)
                ids = [identity];

            if (identityProperties.Count != ids.Length)
                throw new InvalidOperationException($"{identity} values do not match {type.Name} identity properties");

            Expression? where = null;
            var i = 0;
            foreach (var identityProperty in identityProperties)
            {
                var id = ids[i];

                Expression equals = Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, identityProperty.MemberInfo), Expression.Constant(id, identityProperty.Type));
                if (where is null)
                {
                    where = equals;
                }
                else
                {
                    where = Expression.And(where, equals);
                }

                i++;
            }

            if (where is null)
                return null;

            var queryExpression = Expression.Lambda<Func<TModel, bool>>(where, queryExpressionParameter);

            return queryExpression;
        }
    }
}