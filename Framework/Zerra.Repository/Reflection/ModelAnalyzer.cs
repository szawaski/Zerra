// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zerra;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Repository.Reflection
{
    public static class ModelAnalyzer
    {
        private static readonly ConcurrentFactoryDictionary<Type, ModelDetail> modelInfos = new ConcurrentFactoryDictionary<Type, ModelDetail>();
        public static ModelDetail GetModel<TModel>()
        {
            Type type = typeof(TModel);

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

        private static readonly ConcurrentFactoryDictionary<TypeKey, object> getterFunctionsByAttribute = new ConcurrentFactoryDictionary<TypeKey, object>();
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
            Type type = typeof(T);

            string[] propertyNamesArray = propertyNames == null ? null : propertyNames.Split(',');

            ParameterExpression sourceExpression = Expression.Parameter(type, "x");

            PropertyInfo[] typeProperties = type.GetProperties();
            List<Expression> propertyExpressions = new List<Expression>();
            foreach (PropertyInfo typeProperty in typeProperties)
            {
                if (propertyNamesArray != null && propertyNamesArray.Contains(typeProperty.Name))
                {
                    Expression propertyExpression = Expression.Property(sourceExpression, typeProperty);
                    propertyExpressions.Add(propertyExpression);
                }
                else if (attributeType != null)
                {
                    Attribute attribute = typeProperty.GetCustomAttribute(attributeType, true);
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

            Expression<Func<T, object>> lambda = Expression.Lambda<Func<T, object>>(accessor, sourceExpression);
            return lambda.Compile();
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, object> setterFunctionsByAttribute = new ConcurrentFactoryDictionary<TypeKey, object>();
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
            Type type = typeof(T);

            string[] propertyNamesArray = propertyNames == null ? null : propertyNames.Split(',');

            ParameterExpression sourceExpression = Expression.Parameter(type, "x");
            ParameterExpression parameterValue = Expression.Parameter(typeof(object), "y");

            PropertyInfo[] typeProperties = type.GetProperties();
            List<Expression> propertyExpressions = new List<Expression>();
            foreach (PropertyInfo typeProperty in typeProperties)
            {
                if (propertyNamesArray != null && propertyNamesArray.Contains(typeProperty.Name))
                {
                    Expression propertyExpression = Expression.Property(sourceExpression, typeProperty);
                    propertyExpressions.Add(propertyExpression);
                }
                else if (attributeType != null)
                {
                    Attribute attribute = typeProperty.GetCustomAttribute(attributeType, true);
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
                List<Expression> setters = new List<Expression>();
                int index = 0;
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

            Expression<Action<T, object>> lambda = Expression.Lambda<Action<T, object>>(setter, sourceExpression, parameterValue);
            return lambda.Compile();
        }

        private static readonly ConcurrentFactoryDictionary<Type, string[]> identityPropertyNames = new ConcurrentFactoryDictionary<Type, string[]>();
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
            List<PropertyInfo> properties = new List<PropertyInfo>();
            PropertyInfo[] typeProperties = type.GetProperties();
            foreach (PropertyInfo typeProperty in typeProperties)
            {
                Attribute attribute = typeProperty.GetCustomAttribute(typeof(IdentityAttribute), true);
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
            Func<TModel, object> modelIdentityAccessor = GetGetterFunctionByNameOrAttribute<TModel>(null, typeof(IdentityAttribute));
            object id = modelIdentityAccessor.Invoke(model);
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
            Action<TModel, object> setter = GetSetterFunctionByNameOrAttribute<TModel>(null, typeof(IdentityAttribute));
            setter.Invoke(model, identity);
        }
        public static void SetIdentity(Type type, object model, object identity)
        {
            var genericSetIdentityMethod = TypeAnalyzer.GetGenericMethodDetail(setIdentityMethod, type);
            genericSetIdentityMethod.Caller(null, new object[] { model, identity });
        }

        private static readonly MethodInfo getForeignIdentityMethod = typeof(ModelAnalyzer).GetMethods(BindingFlags.Public | BindingFlags.Static).First(x => x.Name == nameof(ModelAnalyzer.GetForeignIdentity) && x.IsGenericMethod);
        public static object GetForeignIdentity<TModel>(string foreignIdentityNames, TModel model) where TModel : class, new()
        {
            Func<TModel, object> modelIdentityAccessor = GetGetterFunctionByNameOrAttribute<TModel>(foreignIdentityNames, null);
            object id = modelIdentityAccessor.Invoke(model);
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
            Action<TModel, object> setter = GetSetterFunctionByNameOrAttribute<TModel>(foreignIdentityNames, null);
            setter.Invoke(model, identity);
        }
        public static void SetForeignIdentity(Type type, string foreignIdentityNames, object model, object identity)
        {
            var genericSetForeignIdentityMethod = TypeAnalyzer.GetGenericMethodDetail(setForeignIdentityMethod, type);
            genericSetForeignIdentityMethod.Caller(null, new object[] { foreignIdentityNames, model, identity });
        }

        public static bool CompareIdentities(object identity1, object identity2)
        {
            return identity1?.GetHashCode() == identity2?.GetHashCode();
            //return identity1.ToStringIfArray() = identity2.ToStringIfArray();
        }

        public static Expression<Func<TModel, bool>> GetIdentityExpression<TModel>(object identity)
        {
            Type type = typeof(TModel);
            var queryExpressionParameter = Expression.Parameter(type, "x");
            var identityProperties = GetModel(type).IdentityProperties;

            if (identityProperties.Count == 0)
                return null;

            if (!(identity is object[] ids))
                ids = new object[] { identity };

            Expression where = null;
            int i = 0;
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