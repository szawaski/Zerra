// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra.Repository.Reflection
{
    public static class ModelAnalyzer
    {
        private static readonly ConcurrentFactoryDictionary<Type, ModelDetail> modelInfos = new();
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

        public static void SetIdentity(Type type, object model, object? identity)
        {
            var setter = GetSetterFunctionByAttribute(type);
            setter.Invoke(model, identity);
        }

        public static object? GetForeignIdentity(Type type, string foreignIdentityNames, object model)
        {
            var modelIdentityAccessor = GetGetterFunctionByName(type, foreignIdentityNames);
            if (modelIdentityAccessor is null)
                throw new Exception($"Model {type.Name} missing Foreign Identity");
            var id = modelIdentityAccessor.Invoke(model);
            return id;
        }

        public static void SetForeignIdentity(Type type, string foreignIdentityNames, object model, object identity)
        {
            var setter = GetSetterFunctionByName(type, foreignIdentityNames);
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