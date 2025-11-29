// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.CQRS;
using Zerra.SourceGeneration.Reflection;

namespace Zerra.SourceGeneration
{
    internal static class BusRouters
    {
        private static readonly ConcurrentFactoryDictionary<Type, Func<IBusInternal, string, object>> interfaceRouterCreators = new();
        private static readonly ConcurrentFactoryDictionary<Type, DispatchToBus> dispatchers = new();

        public static object GetBusCaller(Type interfaceType, IBusInternal bus, string source)
        {
            var creator = interfaceRouterCreators.GetOrAdd(interfaceType, GenerateBusCaller);
            var handler = creator.Invoke(bus, source);
            return handler;
        }
        public static DispatchToBus GetBusDispatcher(Type commandType)
        {
            return dispatchers.GetOrAdd(commandType, GenerateBusDispatcher);
        }

        private static Func<IBusInternal, string, object> GenerateBusCaller(Type interfaceType)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new NotSupportedException($"Cannot generate caller for {interfaceType.Name}. Dynamic code generation is not supported in this build configuration.");

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            return BusRouterGenerator.GenerateBusCaller(interfaceType);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        }

        private static DispatchToBus GenerateBusDispatcher(Type commandType)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new NotSupportedException($"Cannot generate dispatcher for {commandType.Name}. Dynamic code generation is not supported in this build configuration.");

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            return BusRouterGenerator.GenerateBusDispatcher(commandType);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        }

        internal static void Register(Type interfaceType, Func<IBusInternal, string, object> creator)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");
            if (!interfaceRouterCreators.TryAdd(interfaceType, creator))
                throw new InvalidOperationException($"Caller for {interfaceType.Name} is already registered");
        }

        public static void Register(Type commandType, Func<IBusInternal, ICommand, Type, string, CancellationToken, Task> dispatcher, Func<object, object?> taskResult)
        {
            var dispatchToBus = new DispatchToBus(dispatcher, taskResult);
            if (!dispatchers.TryAdd(commandType, dispatchToBus))
                throw new InvalidOperationException($"Dispatcher for {commandType.Name} is already registered");
        }

        public sealed class DispatchToBus
        {
            public readonly Func<IBusInternal, ICommand, Type, string, CancellationToken, Task> Method;
            public readonly Func<object, object?> TaskResult;
            public DispatchToBus(Func<IBusInternal, ICommand, Type, string, CancellationToken, Task> method, Func<object, object?> taskResult)
            {
                this.Method = method;
                this.TaskResult = taskResult;
            }
        }
    }
}
