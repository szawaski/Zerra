// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Zerra.Reflection;

namespace Zerra.Repository
{
    public sealed class EventStoreEntityAttribute : BaseGenerateAttribute
    {
        private readonly Type entityType;
        private readonly bool? eventLinking;
        private readonly bool? queryLinking;
        private readonly bool? persistLinking;

        public EventStoreEntityAttribute(Type entityType)
        {
            this.entityType = entityType;
            this.eventLinking = null;
            this.queryLinking = null;
            this.persistLinking = null;
        }

        public EventStoreEntityAttribute(Type entityType, bool linking)
        {
            this.entityType = entityType;
            this.eventLinking = linking;
            this.queryLinking = linking;
            this.persistLinking = linking;
        }

        public EventStoreEntityAttribute(Type entityType, bool eventLinking, bool queryLinking, bool persistLinking)
        {
            this.entityType = entityType;
            this.eventLinking = eventLinking;
            this.queryLinking = queryLinking;
            this.persistLinking = persistLinking;
        }

        private static readonly Type providerType = typeof(EventStoreAsTransactStoreProvider<,>);
        private static readonly Type entityAttributeType = typeof(EntityAttribute);
        private static readonly Type dataContextType = typeof(DataContext);
        public override Type Generate(Type type)
        {
            var entityTypeDetail = TypeAnalyzer.GetTypeDetail(entityType);
            if (!entityTypeDetail.Attributes.Select(x => x.GetType()).Contains(entityAttributeType))
                throw new Exception($"{nameof(TransactStoreEntityAttribute)} {nameof(entityType)} argument {type.Name} does not inherit {entityAttributeType.Name}");

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            if (!typeDetail.BaseTypes.Contains(dataContextType))
                throw new Exception($"{nameof(TransactStoreEntityAttribute)} is not placed on a {dataContextType.Name}");

            var baseType = TypeAnalyzer.GetGenericType(providerType, type, entityType);

            var typeSignature = $"{entityType.Name}_{type.Name}_Provider";

            var moduleBuilder = GeneratedAssembly.GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, baseType);

            _ = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var properties = new HashSet<Tuple<PropertyInfo, bool>>();
            var transactProviderTypeDetails = TypeAnalyzer.GetTypeDetail(baseType);

            var methods = transactProviderTypeDetails.MethodDetails.Select(x => x.MethodInfo).ToArray();

            if (eventLinking.HasValue)
            {
                var eventLinkingProperty = transactProviderTypeDetails.GetMember("EventLinking");
                _ = properties.Add(new Tuple<PropertyInfo, bool>((PropertyInfo)eventLinkingProperty.MemberInfo, eventLinking.Value));
            }

            if (queryLinking.HasValue)
            {
                var queryLinkingProperty = transactProviderTypeDetails.GetMember("QueryLinking");
                _ = properties.Add(new Tuple<PropertyInfo, bool>((PropertyInfo)queryLinkingProperty.MemberInfo, queryLinking.Value));
            }

            if (persistLinking.HasValue)
            {
                var eventLinkingProperty = transactProviderTypeDetails.GetMember("PersistLinking");
                _ = properties.Add(new Tuple<PropertyInfo, bool>((PropertyInfo)eventLinkingProperty.MemberInfo, persistLinking.Value));
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
                var getMethod = methods.FirstOrDefault(x => x.Name == getMethodName);
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

            var objectType = typeBuilder.CreateTypeInfo();
            return objectType;
        }
    }
}
