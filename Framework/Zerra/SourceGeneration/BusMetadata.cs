// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.CQRS;

namespace Zerra.SourceGeneration
{
    internal static class BusMetadata
    {
        private static readonly ConcurrentFactoryDictionary<Type, HandlerMetadata> metadata = new();
        public static HandlerMetadata GetByType(Type interfaceType)
        {
            return metadata.GetOrAdd(interfaceType, GenerateMetadata);
        }

        private static HandlerMetadata GenerateMetadata(Type interfaceType)
        {
            var attributes = interfaceType.GetCustomAttributes(true);
            BusLogging busLogging = default;
            foreach(var attribute in attributes)
            {
                if (attribute is ServiceLogAttribute busLoggingAttribute)
                    busLogging = busLoggingAttribute.BusLogging;
            }
            return new HandlerMetadata(busLogging);
        }

        public static void Register(Type interfaceType, HandlerMetadata handlerMetadata)
        {
            if (!metadata.TryAdd(interfaceType, handlerMetadata))
                throw new InvalidOperationException($"Metadata for {interfaceType.Name} is already registered");
        }
    }
}
