// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.CQRS.Reflection.Dynamic;

namespace Zerra.CQRS.Reflection
{
    internal static class BusCommandOrEventInfo
    {
        private static readonly ConcurrentFactoryDictionary<Type, CommandOrEventInfo> byType = new();

        public static CommandOrEventInfo GetByType(Type interfaceOrCommandOrEventType, IEnumerable<Type>? typesToSearch)
        {
            if (!byType.TryGetValue(interfaceOrCommandOrEventType, out var messageInfo))
            {
                messageInfo = GenerateMessageInfo(interfaceOrCommandOrEventType, typesToSearch);
                _ = byType.TryAdd(messageInfo.InterfaceType, messageInfo);
                foreach (var command in messageInfo.CommandTypes)
                    _ = byType.TryAdd(command, messageInfo);
                foreach (var @event in messageInfo.EventTypes)
                    _ = byType.TryAdd(@event, messageInfo);
            }
            return messageInfo;
        }

        public static void Register(Type interfaceType, string interfaceName, Type[] commandTypes, Type[] eventTypes)
        {
            if (!interfaceType.IsInterface)
                throw new ArgumentException($"Type {interfaceType.Name} is not an interface");
            var messageInfo = new CommandOrEventInfo(interfaceType, interfaceName, commandTypes, eventTypes);
            if (!byType.TryAdd(messageInfo.InterfaceType, messageInfo))
                throw new ArgumentException($"Interface type {interfaceType.Name} is already registered");
            foreach (var command in messageInfo.CommandTypes)
                _ = byType.TryAdd(command, messageInfo);
            foreach (var @event in messageInfo.EventTypes)
                _ = byType.TryAdd(@event, messageInfo);
        }

        private static CommandOrEventInfo GenerateMessageInfo(Type interfaceOrCommandOrEventType, IEnumerable<Type>? typesToSearch)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new NotSupportedException($"Cannot generate command or event info for {interfaceOrCommandOrEventType.Name}. Dynamic code generation is not supported in this build configuration.");

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            return BusCommandOrEventInfoGenerator.GenerateMessageInfo(interfaceOrCommandOrEventType, typesToSearch);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        }

        public sealed class CommandOrEventInfo
        {
            public readonly Type InterfaceType;
            public readonly string InterfaceName;
            public readonly IReadOnlyList<Type> CommandTypes;
            public readonly IReadOnlyList<Type> EventTypes;
            public CommandOrEventInfo(Type interfaceType, string interfaceName, IReadOnlyList<Type> commandTypes, IReadOnlyList<Type> eventTypes)
            {
                this.InterfaceType = interfaceType;
                this.InterfaceName = interfaceName;
                this.CommandTypes = commandTypes;
                this.EventTypes = eventTypes;
            }
        }
    }
}
