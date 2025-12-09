// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using static Zerra.CQRS.Reflection.BusCommandOrEventInfo;

namespace Zerra.CQRS.Reflection.Dynamic
{
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    internal static class BusCommandOrEventInfoGenerator
    {
        public static CommandOrEventInfo GenerateMessageInfo(Type interfaceOrCommandOrEventType, IEnumerable<Type>? typesToSearch)
        {
            Type? interfaceType = null;
            var commandTypes = new List<Type>();
            var eventTypes = new List<Type>();

            if (interfaceOrCommandOrEventType.IsInterface)
            {
                interfaceType = interfaceOrCommandOrEventType;
            }
            else
            {
                if (typesToSearch == null)
                    throw new Exception($"Could not find message handler interface for command or event {interfaceOrCommandOrEventType.FullName}");

                foreach (var type in typesToSearch)
                {
                    foreach (var i in type.GetInterfaces())
                    {
                        if (!i.IsGenericType)
                            continue;
                        if (i.Name == "ICommandHandler`1" || i.Name == "ICommandHandler`2")
                        {
                            var commandType = i.GetGenericArguments()[0];
                            if (commandType == interfaceOrCommandOrEventType)
                            {
                                interfaceType = type;
                                break;
                            }
                        }
                        else if (i.Name == "IEventHandler`1")
                        {
                            var eventType = i.GetGenericArguments()[0];
                            if (eventType == interfaceOrCommandOrEventType)
                            {
                                interfaceType = type;
                                break;
                            }
                        }
                    }
                    if (interfaceType != null)
                        break;
                }
                if (interfaceType == null)
                    throw new Exception($"Could not find message handler interface for command or event {interfaceOrCommandOrEventType.FullName}");
            }

            foreach (var i in interfaceType.GetInterfaces())
            {
                if (!i.IsGenericType)
                    continue;

                if (i.Name == "ICommandHandler`1" || i.Name == "ICommandHandler`2")
                {
                    var commandType = i.GetGenericArguments()[0];
                    commandTypes.Add(commandType);
                }
                else if (i.Name == "IEventHandler`1")
                {
                    var eventType = i.GetGenericArguments()[0];
                    eventTypes.Add(eventType);
                }
            }

            var messageInfo = new CommandOrEventInfo(interfaceType, interfaceType.Name, commandTypes.ToArray(), eventTypes.ToArray());
            return messageInfo;
        }
}
}
