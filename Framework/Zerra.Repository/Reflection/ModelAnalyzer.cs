// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
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
            var modelInfo = modelInfos.GetOrAdd(type, (t) =>
            {
                var typeDetails = TypeAnalyzer.GetTypeDetail(type);
                return new ModelDetail(typeDetails);
            });
            return modelInfo;
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, object> getterFunctionsByAttribute = new();
        private static Func<T, object> GetGetterFunctionByNameOrAttribute<T>(string propertyNames, Type attributeType)
        {
            TypeKey key = default;
            if (attributeType != null)
                key = new TypeKey(propertyNames, typeof(T), attributeType);
            else
                key = new TypeKey(propertyNames, typeof(T));

            var getter = getterFunctionsByAttribute.GetOrAdd(key, (factoryKey) =>
            {
                return GenerateGetterFunctionByNameOrAttribute<T>(propertyNames, attributeType);
            });

            var expression = (Func<T, object>)getter;
            return expression;
        }
        private static Func<T, object> GenerateGetterFunctionByNameOrAttribute<T>(string propertyNames, Type attributeType)
        {
            var type = typeof(T);

            var propertyNamesArray = propertyNames == null ? null : propertyNames.Split(',');

            var sourceExpression = Expression.Parameter(type, "x");

            var typeProperties = type.GetProperties();
            var propertyExpressions = new List<Expression>();
            foreach (var typeProperty in typeProperties)
            {
                if (propertyNamesArray != null && propertyNamesArray.Contains(typeProperty.Name))
                {
                    Expression propertyExpression = Expression.Property(sourceExpression, typeProperty);
                    propertyExpressions.Add(propertyExpression);
                }
                else if (attributeType != null)
                {
                    var attribute = typeProperty.GetCustomAttribute(attributeType, true);
                    if (attribute != null)
                    {
                        Expression propertyExpression = Expression.Property(sourceExpression, typeProperty);
                        propertyExpressions.Add(propertyExpression);
                    }
                }
            }

            Expression accessor = null;
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
                return null;
                //throw new InvalidOperationException(String.Format("Attribute {0} on property not found in object {1}.", attributeType.FullName, type.FullName));
            }

            var lambda = Expression.Lambda<Func<T, object>>(accessor, sourceExpression);
            return lambda.Compile();
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, object> setterFunctionsByAttribute = new();
        private static Action<T, object> GetSetterFunctionByNameOrAttribute<T>(string propertyNames, Type attributeType)
        {
            TypeKey key = default;
            if (attributeType != null)
                key = new TypeKey(propertyNames, typeof(T), attributeType);
            else
                key = new TypeKey(propertyNames, typeof(T));

            var setter = setterFunctionsByAttribute.GetOrAdd(key, (factoryKey) =>
            {
                return GenerateSetterFunctionByNameOrAttribute<T>(propertyNames, attributeType);
            });

            var expression = (Action<T, object>)setter;
            return expression;
        }
        private static Action<T, object> GenerateSetterFunctionByNameOrAttribute<T>(string propertyNames, Type attributeType)
        {
            var type = typeof(T);

            var propertyNamesArray = propertyNames == null ? null : propertyNames.Split(',');

            var sourceExpression = Expression.Parameter(type, "x");
            var parameterValue = Expression.Parameter(typeof(object), "y");

            var typeProperties = type.GetProperties();
            var propertyExpressions = new List<Expression>();
            foreach (var typeProperty in typeProperties)
            {
                if (propertyNamesArray != null && propertyNamesArray.Contains(typeProperty.Name))
                {
                    Expression propertyExpression = Expression.Property(sourceExpression, typeProperty);
                    propertyExpressions.Add(propertyExpression);
                }
                else if (attributeType != null)
                {
                    var attribute = typeProperty.GetCustomAttribute(attributeType, true);
                    if (attribute != null)
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
                throw new InvalidOperationException($"Attribute {attributeType.FullName} on property not found in object {type.FullName}.");
            }

            var lambda = Expression.Lambda<Action<T, object>>(setter, sourceExpression, parameterValue);
            return lambda.Compile();
        }

        private static readonly ConcurrentFactoryDictionary<Type, string[]> identityPropertyNames = new();
        public static string[] GetIdentityPropertyNames(Type type)
        {
            var names = identityPropertyNames.GetOrAdd(type, (key) =>
            {
                return GenerateIdentityPropertyNames(key);
            });
            return names;
        }
        private static string[] GenerateIdentityPropertyNames(Type type)
        {
            var properties = new List<PropertyInfo>();
            var typeProperties = type.GetProperties();
            foreach (var typeProperty in typeProperties)
            {
                var attribute = typeProperty.GetCustomAttribute(typeof(IdentityAttribute), true);
                if (attribute != null)
                {
                    properties.Add(typeProperty);
                }
            }
            return properties.Select(x => x.Name).ToArray();
        }

        private static readonly MethodInfo getIdentityMethod = typeof(ModelAnalyzer).GetMethods(BindingFlags.Public | BindingFlags.Static).First(x => x.Name == nameof(ModelAnalyzer.GetIdentity) && x.IsGenericMethod);
        public static object GetIdentity<TModel>(TModel model) where TModel : class, new()
        {
            var modelIdentityAccessor = GetGetterFunctionByNameOrAttribute<TModel>(null, typeof(IdentityAttribute));
            var id = modelIdentityAccessor.Invoke(model);
            return id;
        }
        public static object GetIdentity(Type type, object model)
        {
            var genericGetIdentityMethod = TypeAnalyzer.GetGenericMethodDetail(getIdentityMethod, type);
            return genericGetIdentityMethod.Caller(null, new object[] { model });
        }

        private static readonly MethodInfo setIdentityMethod = typeof(ModelAnalyzer).GetMethods(BindingFlags.Public | BindingFlags.Static).First(x => x.Name == nameof(ModelAnalyzer.SetIdentity) && x.IsGenericMethod);
        public static void SetIdentity<TModel>(TModel model, object identity) where TModel : class, new()
        {
            var setter = GetSetterFunctionByNameOrAttribute<TModel>(null, typeof(IdentityAttribute));
            setter.Invoke(model, identity);
        }
        public static void SetIdentity(Type type, object model, object identity)
        {
            var genericSetIdentityMethod = TypeAnalyzer.GetGenericMethodDetail(setIdentityMethod, type);
            _ = genericSetIdentityMethod.Caller(null, new object[] { model, identity });
        }

        private static readonly MethodInfo getForeignIdentityMethod = typeof(ModelAnalyzer).GetMethods(BindingFlags.Public | BindingFlags.Static).First(x => x.Name == nameof(ModelAnalyzer.GetForeignIdentity) && x.IsGenericMethod);
        public static object GetForeignIdentity<TModel>(string foreignIdentityNames, TModel model) where TModel : class, new()
        {
            var modelIdentityAccessor = GetGetterFunctionByNameOrAttribute<TModel>(foreignIdentityNames, null);
            var id = modelIdentityAccessor.Invoke(model);
            return id;
        }
        public static object GetForeignIdentity(Type type, string foreignIdentityNames, object model)
        {
            var genericGetForeignIdentityMethod = TypeAnalyzer.GetGenericMethodDetail(getForeignIdentityMethod, type);
            return genericGetForeignIdentityMethod.Caller(null, new object[] { foreignIdentityNames, model });
        }

        private static readonly MethodInfo setForeignIdentityMethod = typeof(ModelAnalyzer).GetMethods(BindingFlags.Public | BindingFlags.Static).First(x => x.Name == nameof(ModelAnalyzer.SetForeignIdentity) && x.IsGenericMethod);
        public static void SetForeignIdentity<TModel>(string foreignIdentityNames, TModel model, object identity) where TModel : class, new()
        {
            var setter = GetSetterFunctionByNameOrAttribute<TModel>(foreignIdentityNames, null);
            setter.Invoke(model, identity);
        }
        public static void SetForeignIdentity(Type type, string foreignIdentityNames, object model, object identity)
        {
            var genericSetForeignIdentityMethod = TypeAnalyzer.GetGenericMethodDetail(setForeignIdentityMethod, type);
            _ = genericSetForeignIdentityMethod.Caller(null, new object[] { foreignIdentityNames, model, identity });
        }

        public static bool CompareIdentities(object identity1, object identity2)
        {
            return identity1?.GetHashCode() == identity2?.GetHashCode();
            //return identity1.ToStringIfArray() = identity2.ToStringIfArray();
        }

        public static Expression<Func<TModel, bool>> GetIdentityExpression<TModel>(object identity)
        {
            var type = typeof(TModel);
            var queryExpressionParameter = Expression.Parameter(type, "x");
            var identityProperties = GetModel(type).IdentityProperties;

            if (identityProperties.Count == 0)
                return null;

            if (!(identity is object[] ids))
                ids = new object[] { identity };

            Expression where = null;
            var i = 0;
            foreach (var identityProperty in identityProperties)
            {
                var id = ids[i];

                Expression equals = Expression.Equal(Expression.MakeMemberAccess(queryExpressionParameter, identityProperty.MemberInfo), Expression.Constant(id, identityProperty.Type));
                if (where == null)
                {
                    where = equals;
                }
                else
                {
                    where = Expression.And(where, equals);
                }

                i++;
            }

            var queryExpression = Expression.Lambda<Func<TModel, bool>>(where, queryExpressionParameter);

            return queryExpression;
        }
    }
}