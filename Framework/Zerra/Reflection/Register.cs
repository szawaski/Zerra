// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS;
using Zerra.CQRS.Reflection;
using Zerra.Map;
using Zerra.Map.Converters;
using Zerra.Serialization.Bytes.Converters;
using Zerra.Serialization.Json.Converters;

namespace Zerra.Reflection
{
    /// <summary>
    /// Internal static methods for registering source-generated type information and CQRS routing metadata.
    /// Called by the Zerra source generator to register routers, handlers, type details, and other framework components
    /// that were generated at compile-time. These registrations populate runtime caches, enabling the framework to access
    /// source-generated code and metadata without reflection.
    /// </summary>
    public static class Register
    {
        public static void Router(Type interfaceType, Func<IBusInternal, string, object> creator) => BusRouters.Register(interfaceType, creator);
        public static void Router(Type commandType, Func<IBusInternal, ICommand, Type, string, CancellationToken, Task> dispatcher, Func<object, object?> taskResult) => BusRouters.Register(commandType, dispatcher, taskResult);
        public static void Handler(Type interfaceType, string name, bool isTask, Type? taskInnerType, Type[] parameterTypes, Func<object, object?[]?, object?> method, Func<object, object?>? taskResult) => BusHandlers.Register(interfaceType, name, isTask, taskInnerType, parameterTypes, method, taskResult);
        public static void CommandOrEventInfo(Type interfaceType, string interfaceName, Type[] commandTypes, Type[] eventTypes) => BusCommandOrEventInfo.Register(interfaceType, interfaceName, commandTypes, eventTypes);
        public static void Finder(Type type) => TypeFinder.Register(type);
        public static void Type(TypeDetail typeInfo) => TypeAnalyzer.Register(typeInfo);
        public static void Enum(Type type, CoreEnumType underlyingType, bool hasFlagsAttribute, EnumName.EnumFieldInfo[] fields, Func<object> creator, Func<object, object, object> bitOr) => EnumName.Register(type, underlyingType, hasFlagsAttribute, fields, creator, bitOr);
        public static void EmptyImplementation(Type interfaceType, Type implementationType) => EmptyImplementations.Register(interfaceType, implementationType);
        public static void SerializersAndMap<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>() where TDictionaryKey : notnull
        {
            ByteConverterFactory.RegisterCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>();
            JsonConverterFactory.RegisterCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>();
            MapConverterFactory.RegisterCreator<TType, TType, TEnumerableType, TEnumerableType, TDictionaryKey, TDictionaryValue, TDictionaryKey, TDictionaryValue>();
        }
        public static void CustomMap<TSource, TTarget, TSourceEnumerable, TTargetEnumerable, TSourceKey, TSourceValue, TTargetKey, TTargetValue>(IMapDefinition<TSource, TTarget> mapDefinition) where TSourceKey : notnull where TTargetKey : notnull
            => MapDefinition.Register<TSource, TTarget, TSourceEnumerable, TTargetEnumerable, TSourceKey, TSourceValue, TTargetKey, TTargetValue>(mapDefinition);
    }
}
