// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS;
using Zerra.SourceGeneration.Types;

namespace Zerra.SourceGeneration
{
    public static class Register
    {
        public static void Router(Type interfaceType, Func<IBusInternal, string, object> creator) => BusRouters.Register(interfaceType, creator);
        public static void Router(Type commandType, Func<IBusInternal, ICommand, Type, string, CancellationToken, Task> dispatcher, Func<object, object?> taskResult) => BusRouters.Register(commandType, dispatcher, taskResult);
        public static void Handler(Type interfaceType, string name, bool isTask, Type? taskInnerType, Type[] parameterTypes, Func<object, object?[]?, object?> method, Func<object, object?>? taskResult) => BusHandlers.Register(interfaceType, name, isTask, taskInnerType, parameterTypes, method, taskResult);
        public static void CommandOrEventInfo(Type interfaceType, string interfaceName, Type[] commandTypes, Type[] eventTypes) => BusCommandOrEventInfo.Register(interfaceType, interfaceName, commandTypes, eventTypes);
        public static void NiceName(Type type, string niceName, string niceFullName) => TypeHelper.Register(type, niceName, niceFullName);
        public static void Type(TypeDetail typeInfo) => TypeAnalyzer.Register(typeInfo);
        public static void Enum(Type type, CoreEnumType underlyingType, bool hasFlagsAttribute, EnumName.EnumFieldInfo[] fields, Func<object> creator, Func<object, object, object> bitOr) => EnumName.Register(type, underlyingType, hasFlagsAttribute, fields, creator, bitOr);
        public static void EmptyImplementation(Type interfaceType, Type implementationType) => EmptyImplementations.Register(interfaceType, implementationType);
    }
}
