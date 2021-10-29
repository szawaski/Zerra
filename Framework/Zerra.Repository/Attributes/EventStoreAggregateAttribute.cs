// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Reflection;
using Zerra.Reflection;

namespace Zerra.Repository
{
    public class EventStoreAggregateAttribute : BaseGenerateAttribute
    {
        private readonly Type aggregateType;

        public EventStoreAggregateAttribute(Type aggregateType)
        {
            this.aggregateType = aggregateType;
        }

        private static readonly Type aggregateRootType = typeof(AggregateRoot);
        private static readonly Type dataContextType = typeof(DataContext);
        private static readonly Type eventProviderType = typeof(BaseEventStoreContextProvider<,>);
        public override Type Generate(Type type)
        {
            var aggregateTypeDetails = TypeAnalyzer.GetType(aggregateType);
            if (!aggregateTypeDetails.BaseTypes.Contains(aggregateRootType))
                throw new Exception($"{nameof(TransactStoreEntityAttribute)} {nameof(aggregateType)} argument {type.Name} does not inherit {aggregateRootType.Name}");

            var typeDetail = TypeAnalyzer.GetType(type);
            if (!typeDetail.BaseTypes.Contains(dataContextType))
                throw new Exception($"{nameof(TransactStoreEntityAttribute)} is not placed on a {dataContextType.Name}");

            var baseType = TypeAnalyzer.GetGenericType(eventProviderType, type, aggregateType);

            var typeSignature = $"{aggregateType.Name}_{type.Name}_Provider";

            var moduleBuilder = GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, baseType);

            _ = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var objectType = typeBuilder.CreateTypeInfo();
            return objectType;
        }
    }
}
