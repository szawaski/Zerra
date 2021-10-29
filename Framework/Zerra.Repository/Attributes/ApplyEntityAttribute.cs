// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Reflection;
using Zerra.Reflection;
using Zerra.Repository.EventStore;

namespace Zerra.Repository
{
    public class ApplyEntityAttribute : BaseGenerateAttribute
    {
        private readonly Type entityType;

        public ApplyEntityAttribute(Type entityType)
        {
            this.entityType = entityType;
        }

        private static readonly Type entityAttributeType = typeof(EntityAttribute);
        private static readonly Type dataContextTransactType = typeof(DataContext<ITransactStoreEngine>);
        private static readonly Type dataContextEventType = typeof(DataContext<IEventStoreEngine>);
        //private static readonly Type dataContextByteType = typeof(DataContext<IByteStoreEngine>);
        private static readonly Type transactProviderType = typeof(TransactStoreProvider<,>);
        private static readonly Type eventProviderType = typeof(EventStoreAsTransactStoreProvider<,>);
        //private static readonly Type byteProviderType = typeof(DataContext<IByteStoreEngine>);
        public override Type Generate(Type type)
        {
            var entityTypeDetail = TypeAnalyzer.GetType(entityType);
            if (!entityTypeDetail.Attributes.Select(x => x.GetType()).Contains(entityAttributeType))
                throw new Exception($"{nameof(ApplyEntityAttribute)} {nameof(entityType)} argument {type.Name} does not have {entityAttributeType.Name}");

            var typeDetail = TypeAnalyzer.GetType(type);

            Type baseContextType;
            Type genericProviderType;
            if (typeDetail.BaseTypes.Contains(dataContextTransactType))
            {
                baseContextType = dataContextTransactType;
                genericProviderType = transactProviderType;
            }
            else if (typeDetail.BaseTypes.Contains(dataContextEventType))
            {
                baseContextType = dataContextTransactType;
                genericProviderType = eventProviderType;
            }
            //else if (typeDetail.Interfaces.Contains(dataContextByteType))
            //{
            //    baseContextType = dataContextByteType;
            //    genericProviderType = null;
            //}
            else
            {
                throw new NotSupportedException($"{nameof(ApplyEntityAttribute)} does not support {type.Name}");
            }

            var baseType = TypeAnalyzer.GetGenericType(genericProviderType, type, entityType);

            var typeSignature = $"{entityType.Name}_{type.Name}_Provider";

            var moduleBuilder = GetModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, baseType);

            _ = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var objectType = typeBuilder.CreateTypeInfo();
            return objectType;
        }
    }
}
