// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Repository.Reflection
{
    /// <summary>
    /// Provides cached reflection-based analysis of repository model types, exposing identity,
    /// foreign-identity, and LINQ expression helpers.
    /// </summary>
    public static class ModelAnalyzer
    {
        private static readonly ConcurrentFactoryDictionary<Type, ModelDetail> modelInfos = new();
        /// <summary>
        /// Returns the cached <see cref="ModelDetail"/> for the specified type, creating it on first access.
        /// </summary>
        /// <param name="type">The model type to analyse.</param>
        /// <returns>The <see cref="ModelDetail"/> for <paramref name="type"/>.</returns>
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
        private static Func<object, object?>? GetGetterFunctionByName(Type type, string propertyNames)
        {
            var key = new TypeKey(propertyNames, type);

            var getter = getterFunctionsByAttribute.GetOrAdd(key, type, propertyNames, static (type, propertyNames) => GenerateGetterFunctionByName(type, propertyNames));

            var expression = (Func<object, object>?)getter;
            return expression;
        }
        private static Func<object, object?>? GetGetterFunctionByAttribute(Type type)
        {
            var key = new TypeKey(type);

            var getter = getterFunctionsByAttribute.GetOrAdd(key, type, static (type) => GenerateGetterFunctionByAttribute(type));

            var expression = (Func<object, object>?)getter;
            return expression;
        }
        private static Func<object, object>? GenerateGetterFunctionByName(Type type, string propertyNames)
        {
            var propertyNamesArray = propertyNames is null ? null : propertyNames.Split(',');

            var members = GetModel(type).TypeDetail.Members.Where(x => propertyNamesArray.Contains(x.Name)).ToArray();

            if (members.Length == 1)
            {
                var member = members[0];
                return member.GetterBoxed!;
            }
            else if (members.Length > 1)
            {
                return (object obj) =>
                {
                    var values = new object?[members.Length];
                    for (var x = 0; x < members.Length; x++)
                    {
                        var v = members[x].GetterBoxed!.Invoke(obj);
                        values[x] = v;
                    }
                    return values;
                };
            }
            else
            {
                throw new InvalidOperationException($"Getter function not found on {type.Name}");
            }
        }
        private static Func<object, object>? GenerateGetterFunctionByAttribute(Type type)
        {
            var sourceExpression = Expression.Parameter(typeof(object), "x");

            var identityProperties = GetModel(type).IdentityProperties;

            if (identityProperties.Count == 1)
            {
                var identityProperty = identityProperties[0];
                return identityProperty.MemberDetail.GetterBoxed!;
            }
            else if (identityProperties.Count > 1)
            {
                return (object obj) =>
                {
                    var values = new object?[identityProperties.Count];
                    for (var x = 0; x < identityProperties.Count; x++)
                    {
                        var v = identityProperties[x].GetterBoxed!.Invoke(obj);
                        values[x] = v;
                    }
                    return values;
                };
            }
            else
            {
                throw new InvalidOperationException($"Getter function not found on {type.Name}");
            }
        }

        private static readonly ConcurrentFactoryDictionary<TypeKey, object> setterFunctionsByAttribute = new();
        private static Action<object, object?> GetSetterFunctionByName(Type type, string propertyNames)
        {
            var key = new TypeKey(propertyNames, type);

            var setter = setterFunctionsByAttribute.GetOrAdd(key, type, propertyNames, static (type, propertyNames) => GenerateSetterFunctionByName(type, propertyNames));

            var expression = (Action<object, object?>)setter;
            return expression;
        }
        private static Action<object, object?> GetSetterFunctionByAttribute(Type type)
        {
            var key = new TypeKey(type);

            var setter = setterFunctionsByAttribute.GetOrAdd(key, type, static (type) => GenerateSetterFunctionByAttribute(type));

            var expression = (Action<object, object?>)setter;
            return expression;
        }
        private static Action<object, object?> GenerateSetterFunctionByName(Type type, string propertyNames)
        {
            var propertyNamesArray = propertyNames.Split(',').ToHashSet();

            var members = GetModel(type).TypeDetail.Members.Where(x => propertyNamesArray.Contains(x.Name)).ToArray();

            if (members.Length == 1)
            {
                var member = members[0];
                return member.SetterBoxed!;
            }
            else if (members.Length > 1)
            {
                return (object obj, object? value) =>
                {
                    var values = (object?[]?)value;
                    for (var x = 0; x < members.Length; x++)
                    {
                        var v = values?[x];
                        members[x].SetterBoxed!.Invoke(obj, v);
                    }
                };
            }
            else
            {
                throw new InvalidOperationException($"Setter function not found on {type.Name}");
            }
        }
        private static Action<object, object?> GenerateSetterFunctionByAttribute(Type type)
        {
            var identityProperties = GetModel(type).IdentityProperties;

            if (identityProperties.Count == 1)
            {
                var identityProperty = identityProperties[0];
                return identityProperty.MemberDetail.SetterBoxed!;
            }
            else if (identityProperties.Count > 1)
            {
                return (object obj, object? value) =>
                {
                    var values = (object?[]?)value;
                    for (var x = 0; x < identityProperties.Count; x++)
                    {
                        var v = values?[x];
                        identityProperties[x].SetterBoxed!.Invoke(obj, v);
                    }
                };
            }
            else
            {
                throw new InvalidOperationException($"Setter function not found on {type.Name}");
            }
        }

        private static readonly ConcurrentFactoryDictionary<Type, string[]> identityPropertyNames = new();
        /// <summary>
        /// Returns the names of all properties on the specified type that are marked with <see cref="IdentityAttribute"/>.
        /// </summary>
        /// <param name="type">The model type to inspect.</param>
        /// <returns>An array of identity property names.</returns>
        /// <exception cref="Exception">Thrown when the type has no identity properties.</exception>
        public static string[] GetIdentityPropertyNames(Type type)
        {
            var names = identityPropertyNames.GetOrAdd(type, GenerateIdentityPropertyNames);
            if (names.Length == 0)
                throw new Exception($"Model {type} missing Identity");
            return names;
        }
        private static string[] GenerateIdentityPropertyNames(Type type)
        {
            var properties = new List<string>();
            var typeDetail = type.GetTypeDetail();
            foreach (var member in typeDetail.Members)
            {
                if (member.Attributes.Any(x => x is IdentityAttribute))
                    properties.Add(member.Name);
            }
            return properties.ToArray();
        }

        /// <summary>
        /// Retrieves the identity value(s) from a model instance.
        /// For composite identities the result is an <see cref="object"/> array.
        /// </summary>
        /// <param name="type">The model type.</param>
        /// <param name="model">The model instance to read from.</param>
        /// <returns>The identity value, or an array of values for composite identities.</returns>
        /// <exception cref="Exception">Thrown when the type has no identity or the identity value is <see langword="null"/>.</exception>
        public static object GetIdentity(Type type, object model)
        {
            var modelIdentityAccessor = GetGetterFunctionByAttribute(type);
            if (modelIdentityAccessor is null)
                throw new Exception($"Model {type.Name} missing Identity");
            var id = modelIdentityAccessor.Invoke(model);
            if (id is null)
                throw new Exception($"Model {type.Name} missing Identity");
            return id;
        }

        /// <summary>
        /// Sets the identity value(s) on a model instance.
        /// </summary>
        /// <param name="type">The model type.</param>
        /// <param name="model">The model instance to write to.</param>
        /// <param name="identity">The identity value to assign, or an array of values for composite identities.</param>
        public static void SetIdentity(Type type, object model, object? identity)
        {
            var setter = GetSetterFunctionByAttribute(type);
            setter.Invoke(model, identity);
        }

        /// <summary>
        /// Retrieves the foreign-identity value(s) from a model instance using a comma-separated list of property names.
        /// </summary>
        /// <param name="type">The model type.</param>
        /// <param name="foreignIdentityNames">A comma-separated list of foreign-identity property names.</param>
        /// <param name="model">The model instance to read from.</param>
        /// <returns>The foreign-identity value, or an array of values for composite keys.</returns>
        /// <exception cref="Exception">Thrown when no matching property is found.</exception>
        public static object? GetForeignIdentity(Type type, string foreignIdentityNames, object model)
        {
            var modelIdentityAccessor = GetGetterFunctionByName(type, foreignIdentityNames);
            if (modelIdentityAccessor is null)
                throw new Exception($"Model {type.Name} missing Foreign Identity");
            var id = modelIdentityAccessor.Invoke(model);
            return id;
        }

        /// <summary>
        /// Sets the foreign-identity value(s) on a model instance using a comma-separated list of property names.
        /// </summary>
        /// <param name="type">The model type.</param>
        /// <param name="foreignIdentityNames">A comma-separated list of foreign-identity property names.</param>
        /// <param name="model">The model instance to write to.</param>
        /// <param name="identity">The foreign-identity value to assign, or an array of values for composite keys.</param>
        public static void SetForeignIdentity(Type type, string foreignIdentityNames, object model, object identity)
        {
            var setter = GetSetterFunctionByName(type, foreignIdentityNames);
            setter.Invoke(model, identity);
        }

        /// <summary>
        /// Compares two identity values for equality, supporting both scalar and composite (array) identities.
        /// </summary>
        /// <param name="identity1">The first identity value.</param>
        /// <param name="identity2">The second identity value.</param>
        /// <returns><see langword="true"/> if the identities are equal; otherwise <see langword="false"/>.</returns>
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

        /// <summary>
        /// Builds a LINQ predicate expression that matches a model by its identity value(s).
        /// </summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="identity">The identity value, or an array of values for composite identities.</param>
        /// <returns>
        /// A <see cref="Expression{TDelegate}"/> of <c>Func&lt;TModel, bool&gt;</c> that filters by identity,
        /// or <see langword="null"/> if the type has no identity properties.
        /// </returns>
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