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
        /// <summary>
        /// Registers a query or event router for the specified interface type.
        /// </summary>
        /// <param name="interfaceType">The query or event interface type to register.</param>
        /// <param name="creator">A function that creates instances of the router for the given bus and route key.</param>
        public static void Router(Type interfaceType, Func<IBusInternal, string, object> creator) => BusRouters.Register(interfaceType, creator);
        /// <summary>
        /// Registers a command router dispatcher for the specified command type.
        /// </summary>
        /// <param name="commandType">The command type to register.</param>
        /// <param name="dispatcher">A function that dispatches the command through the bus.</param>
        /// <param name="taskResult">A function that extracts the result from an async task, or null for fire-and-forget commands.</param>
        public static void Router(Type commandType, Func<IBusInternal, ICommand, Type, string, CancellationToken, Task> dispatcher, Func<object, object?> taskResult) => BusRouters.Register(commandType, dispatcher, taskResult);
        /// <summary>
        /// Registers a query or event handler for the specified interface type.
        /// </summary>
        /// <param name="interfaceType">The handler interface type to register.</param>
        /// <param name="name">The name of the handler method.</param>
        /// <param name="isTask">Whether the handler method returns a Task.</param>
        /// <param name="taskInnerType">The inner type of the Task if <paramref name="isTask"/> is true; otherwise null.</param>
        /// <param name="parameterTypes">The parameter types of the handler method.</param>
        /// <param name="method">A delegate that invokes the handler method.</param>
        /// <param name="taskResult">A function that extracts the result from an async task, or null if <paramref name="isTask"/> is false.</param>
        public static void Handler(Type interfaceType, string name, bool isTask, Type? taskInnerType, Type[] parameterTypes, Func<object, object?[]?, object?> method, Func<object, object?>? taskResult) => BusHandlers.Register(interfaceType, name, isTask, taskInnerType, parameterTypes, method, taskResult);
        /// <summary>
        /// Registers metadata about command and event types for a bus interface.
        /// </summary>
        /// <param name="interfaceType">The bus interface type.</param>
        /// <param name="interfaceName">The name of the bus interface.</param>
        /// <param name="commandTypes">An array of command types handled by this bus.</param>
        /// <param name="eventTypes">An array of event types raised by this bus.</param>
        public static void CommandOrEventInfo(Type interfaceType, string interfaceName, Type[] commandTypes, Type[] eventTypes) => BusCommandOrEventInfo.Register(interfaceType, interfaceName, commandTypes, eventTypes);
        /// <summary>
        /// Registers a type to be discoverable by the type finder.
        /// </summary>
        /// <param name="type">The type to register.</param>
        public static void Finder(Type type) => TypeFinder.Register(type);
        /// <summary>
        /// Registers detailed type information for reflection-free access.
        /// </summary>
        /// <param name="typeInfo">The type detail information to register.</param>
        public static void Type(TypeDetail typeInfo) => TypeAnalyzer.Register(typeInfo);
        /// <summary>
        /// Registers metadata for an enumeration type.
        /// </summary>
        /// <param name="type">The enumeration type to register.</param>
        /// <param name="underlyingType">The underlying numeric type of the enumeration.</param>
        /// <param name="hasFlagsAttribute">Whether the enumeration has the <see cref="FlagsAttribute"/>.</param>
        /// <param name="fields">An array of enumeration field information.</param>
        /// <param name="creator">A function that creates a default instance of the enumeration.</param>
        /// <param name="bitOr">A function that performs bitwise OR operation on enumeration values.</param>
        public static void Enum(Type type, CoreEnumType underlyingType, bool hasFlagsAttribute, EnumName.EnumFieldInfo[] fields, Func<object> creator, Func<object, object, object> bitOr) => EnumName.Register(type, underlyingType, hasFlagsAttribute, fields, creator, bitOr);
        /// <summary>
        /// Registers an implementation type for an interface that should have an empty default implementation.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <param name="implementationType">The implementation type to register.</param>
        public static void EmptyImplementation(Type interfaceType, Type implementationType) => EmptyImplementations.Register(interfaceType, implementationType);
        /// <summary>
        /// Registers serializers and mappers for the specified generic type and collection/dictionary parameters.
        /// </summary>
        /// <typeparam name="TType">The primary type to register serializers for.</typeparam>
        /// <typeparam name="TEnumerableType">The enumerable collection type.</typeparam>
        /// <typeparam name="TDictionaryKey">The dictionary key type.</typeparam>
        /// <typeparam name="TDictionaryValue">The dictionary value type.</typeparam>
        public static void SerializersAndMap<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>() where TDictionaryKey : notnull
        {
            ByteConverterFactory.RegisterCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>();
            JsonConverterFactory.RegisterCreator<TType, TEnumerableType, TDictionaryKey, TDictionaryValue>();
            MapConverterFactory.RegisterCreator<TType, TType, TEnumerableType, TEnumerableType, TDictionaryKey, TDictionaryValue, TDictionaryKey, TDictionaryValue>();
        }
        /// <summary>
        /// Registers a custom map definition for converting between two types.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TTarget">The target type.</typeparam>
        /// <typeparam name="TSourceEnumerable">The source enumerable type.</typeparam>
        /// <typeparam name="TTargetEnumerable">The target enumerable type.</typeparam>
        /// <typeparam name="TSourceKey">The source dictionary key type.</typeparam>
        /// <typeparam name="TSourceValue">The source dictionary value type.</typeparam>
        /// <typeparam name="TTargetKey">The target dictionary key type.</typeparam>
        /// <typeparam name="TTargetValue">The target dictionary value type.</typeparam>
        /// <param name="mapDefinition">The map definition that specifies how to convert between the types.</param>
        public static void CustomMap<TSource, TTarget, TSourceEnumerable, TTargetEnumerable, TSourceKey, TSourceValue, TTargetKey, TTargetValue>(IMapDefinition<TSource, TTarget> mapDefinition) where TSourceKey : notnull where TTargetKey : notnull
            => MapDefinition.Register<TSource, TTarget, TSourceEnumerable, TTargetEnumerable, TSourceKey, TSourceValue, TTargetKey, TTargetValue>(mapDefinition);
    }
}
