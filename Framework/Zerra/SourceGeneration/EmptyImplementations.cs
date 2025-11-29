// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.SourceGeneration.Reflection;

namespace Zerra.SourceGeneration
{
    internal static class EmptyImplementations
    {
        private static readonly ConcurrentFactoryDictionary<Type, Type> creatorsByType = new();

        public static Type GetType(Type interfaceType)
        {
            var type = creatorsByType.GetOrAdd(interfaceType, GenerateEmptyImplementation);
            return type;
        }

        private static Type GenerateEmptyImplementation(Type interfaceType)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new NotSupportedException($"Cannot generate empty implementation for {interfaceType.Name}. Dynamic code generation is not supported in this build configuration.");

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            return EmptyImplementationGenerator.GenerateEmptyImplementation(interfaceType);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        }

        public static void Register(Type interfaceType, Type emptyImplementationType)
        {
            _ = creatorsByType.TryAdd(interfaceType, emptyImplementationType);
        }
    }
}
