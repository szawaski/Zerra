// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Zerra.Reflection;

namespace Zerra.Repository.Reflection
{
    public static class RepositoryProviderGenerator
    {
        private static readonly Type transactStoreProviderType = typeof(TransactStoreProvider<,>);
        private static readonly Type eventStoreProviderType = typeof(EventStoreAsTransactStoreProvider<,>);
        private static readonly Type aggregateRootType = typeof(AggregateRoot);
        private static readonly Type eventProviderType = typeof(BaseEventStoreContextProvider<,>);
        private static readonly Type entityAttributeType = typeof(EntityAttribute);
        private static readonly Type dataContextType = typeof(DataContext);

        private static readonly Type iTransactStoreProviderType = typeof(ITransactStoreProvider<>);
        private static readonly Type iAggregateRootContextProviderType = typeof(IAggregateRootContextProvider<>);

        //the linking properties are protected, TypeDetail members only include public ones
        private const BindingFlags linkingPropertyFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static Type? GenerateTransactStoreProvider<T>(Type type, bool eventLinking, bool queryLinking, bool persistLinking)
        {
            var entityType = typeof(T);

            var interfaceType = iTransactStoreProviderType.MakeGenericType(entityType);
            if (Discovery.GetClassByInterface(interfaceType, false) is not null)
                return null;

            var entityTypeDetail = TypeAnalyzer.GetTypeDetail(entityType);
            if (!entityTypeDetail.Attributes.Select(x => x.GetType()).Contains(entityAttributeType))
                throw new Exception($"{nameof(TransactStoreEntityAttribute<T>)} argument {entityType.Name} does not have the attribute {entityAttributeType.Name}");

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            if (!typeDetail.BaseTypes.Contains(dataContextType))
                throw new Exception($"{nameof(TransactStoreEntityAttribute<T>)} is not placed on a {dataContextType.Name}");

            var baseType = TypeAnalyzer.GetGenericType(transactStoreProviderType, type, entityType);

            var typeSignature = $"{entityType.Name}_{type.Name}_Provider";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, baseType);

            _ = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var properties = new HashSet<Tuple<PropertyInfo, bool>>();
            var transactProviderTypeDetails = TypeAnalyzer.GetTypeDetail(baseType);

            var methods = transactProviderTypeDetails.MethodDetailsBoxed.Select(x => x.MethodInfo).ToArray();

            if (eventLinking)
            {
                var eventLinkingProperty = baseType.GetProperty("EventLinking", linkingPropertyFlags)!;
                _ = properties.Add(new Tuple<PropertyInfo, bool>(eventLinkingProperty, eventLinking));
            }

            if (queryLinking)
            {
                var queryLinkingProperty = baseType.GetProperty("QueryLinking", linkingPropertyFlags)!;
                _ = properties.Add(new Tuple<PropertyInfo, bool>(queryLinkingProperty, queryLinking));
            }

            if (persistLinking)
            {
                var eventLinkingProperty = baseType.GetProperty("PersistLinking", linkingPropertyFlags)!;
                _ = properties.Add(new Tuple<PropertyInfo, bool>(eventLinkingProperty, persistLinking));
            }

            foreach (var prop in properties)
            {
                var property = prop.Item1;
                var value = prop.Item2;

                var propertyBuilder = typeBuilder.DefineProperty(
                    property.Name,
                    PropertyAttributes.HasDefault,
                    property.PropertyType,
                    null
                );

                var getMethodName = "get_" + property.Name;
                var getMethod = methods.First(x => x.Name == getMethodName);
                var getMethodBuilder = typeBuilder.DefineMethod(
                    getMethodName,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    property.PropertyType,
                    Type.EmptyTypes
                );
                var getMethodBuilderIL = getMethodBuilder.GetILGenerator();
                if (value)
                    getMethodBuilderIL.Emit(OpCodes.Ldc_I4_1);
                else
                    getMethodBuilderIL.Emit(OpCodes.Ldc_I4_0);
                getMethodBuilderIL.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(getMethodBuilder, getMethod);

                propertyBuilder.SetGetMethod(getMethodBuilder);
            }

            var objectType = typeBuilder.CreateTypeInfo()!;
            return objectType;
        }

        public static Type? GenerateEventStoreAsTransactStoreProvider<T>(Type type, bool eventLinking, bool queryLinking, bool persistLinking)
        {
            var entityType = typeof(T);

            var interfaceType = iTransactStoreProviderType.MakeGenericType(entityType);
            if (Discovery.GetClassByInterface(interfaceType, false) is not null)
                return null;

            var entityTypeDetail = TypeAnalyzer.GetTypeDetail(entityType);
            if (!entityTypeDetail.Attributes.Select(x => x.GetType()).Contains(entityAttributeType))
                throw new Exception($"{nameof(EventStoreEntityAttribute<T>)} {nameof(entityType)} argument {type.Name} does not inherit {entityAttributeType.Name}");

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            if (!typeDetail.BaseTypes.Contains(dataContextType))
                throw new Exception($"{nameof(EventStoreEntityAttribute<T>)} is not placed on a {dataContextType.Name}");

            var baseType = TypeAnalyzer.GetGenericType(eventStoreProviderType, type, entityType);

            var typeSignature = $"{entityType.Name}_{type.Name}_Provider";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, baseType);

            _ = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var properties = new HashSet<Tuple<PropertyInfo, bool>>();
            var transactProviderTypeDetails = TypeAnalyzer.GetTypeDetail(baseType);

            var methods = transactProviderTypeDetails.MethodDetailsBoxed.Select(x => x.MethodInfo).ToArray();

            if (eventLinking)
            {
                var eventLinkingProperty = baseType.GetProperty("EventLinking", linkingPropertyFlags)!;
                _ = properties.Add(new Tuple<PropertyInfo, bool>(eventLinkingProperty, eventLinking));
            }

            if (queryLinking)
            {
                var queryLinkingProperty = baseType.GetProperty("QueryLinking", linkingPropertyFlags)!;
                _ = properties.Add(new Tuple<PropertyInfo, bool>(queryLinkingProperty, queryLinking));
            }

            if (persistLinking)
            {
                var eventLinkingProperty = baseType.GetProperty("PersistLinking", linkingPropertyFlags)!;
                _ = properties.Add(new Tuple<PropertyInfo, bool>(eventLinkingProperty, persistLinking));
            }

            foreach (var prop in properties)
            {
                var property = prop.Item1;
                var value = prop.Item2;

                var propertyBuilder = typeBuilder.DefineProperty(
                    property.Name,
                    PropertyAttributes.HasDefault,
                    property.PropertyType,
                    null
                );

                var getMethodName = "get_" + property.Name;
                var getMethod = methods.First(x => x.Name == getMethodName);
                var getMethodBuilder = typeBuilder.DefineMethod(
                    getMethodName,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    property.PropertyType,
                    Type.EmptyTypes
                );
                var getMethodBuilderIL = getMethodBuilder.GetILGenerator();
                if (value)
                    getMethodBuilderIL.Emit(OpCodes.Ldc_I4_1);
                else
                    getMethodBuilderIL.Emit(OpCodes.Ldc_I4_0);
                getMethodBuilderIL.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(getMethodBuilder, getMethod);

                propertyBuilder.SetGetMethod(getMethodBuilder);
            }

            var objectType = typeBuilder.CreateTypeInfo()!;
            return objectType;
        }

        public static Type? GenerateEventStoreProvider<T>(Type type)
        {
            var aggregateType = typeof(T);

            var interfaceType = iAggregateRootContextProviderType.MakeGenericType(aggregateType);
            if (Discovery.GetClassByInterface(interfaceType, false) is not null)
                return null;

            var aggregateTypeDetails = TypeAnalyzer.GetTypeDetail(aggregateType);
            if (!aggregateTypeDetails.BaseTypes.Contains(aggregateRootType))
                throw new Exception($"{nameof(EventStoreAggregateAttribute<T>)} {nameof(aggregateType)} argument {type.Name} does not inherit {aggregateRootType.Name}");

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            if (!typeDetail.BaseTypes.Contains(dataContextType))
                throw new Exception($"{nameof(EventStoreAggregateAttribute<T>)} is not placed on a {dataContextType.Name}");

            var baseType = TypeAnalyzer.GetGenericType(eventProviderType, type, aggregateType);

            var typeSignature = $"{aggregateType.Name}_{type.Name}_Provider";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, baseType);

            _ = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var objectType = typeBuilder.CreateTypeInfo()!;
            return objectType;
        }
    }
}
