// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Reflection;

namespace Zerra.CQRS
{
    /// <summary>
    /// Seperate class so they are not loaded if never used.
    /// </summary>
    internal static class BusRouterMethods
    {
        public static readonly ConstructorInfo NotSupportedExceptionConstructor = typeof(NotSupportedException).GetConstructor([typeof(string)]) ?? throw new Exception($"{nameof(NotSupportedException)} constructor not found");
        public static readonly ConstructorInfo ObjectConstructor = typeof(object).GetConstructor(Array.Empty<Type>()) ?? throw new Exception($"{nameof(Object)} constructor not found");
        public static readonly MethodInfo TypeOfMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)) ?? throw new Exception($"{nameof(Type)}.{nameof(Type.GetTypeFromHandle)} not found");

        public static readonly MethodInfo CallInternalMethodNonGeneric = typeof(Bus).GetMethod(nameof(Bus._CallMethod), BindingFlags.Static | BindingFlags.Public) ?? throw new Exception($"{nameof(Bus)}.{nameof(Bus._CallMethod)} not found");

        public static readonly Type CommandHandlerType = typeof(ICommandHandler<>);
        public static readonly Type CommandHandlerWithResultType = typeof(ICommandHandler<,>);
        public static readonly Type EventHandlerType = typeof(IEventHandler<>);
        public static readonly MethodInfo DispatchCommandInternalAsyncMethod = typeof(Bus).GetMethod(nameof(Bus._DispatchCommandInternalAsync), BindingFlags.Static | BindingFlags.Public) ?? throw new Exception($"{nameof(Bus)}.{nameof(Bus._DispatchCommandInternalAsync)} not found");
        public static readonly MethodInfo DispatchCommandWithResultInternalAsyncMethod = typeof(Bus).GetMethod(nameof(Bus._DispatchCommandWithResultInternalAsync), BindingFlags.Static | BindingFlags.Public) ?? throw new Exception($"{nameof(Bus)}.{nameof(Bus._DispatchCommandInternalAsync)} not found");
        public static readonly MethodInfo DispatchEventInternalAsyncMethod = typeof(Bus).GetMethod(nameof(Bus._DispatchEventInternalAsync), BindingFlags.Static | BindingFlags.Public) ?? throw new Exception($"{nameof(Bus)}.{nameof(Bus._DispatchEventInternalAsync)} not found");
    }
}
