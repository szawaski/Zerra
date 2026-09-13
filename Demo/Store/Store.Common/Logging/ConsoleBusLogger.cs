using Zerra.CQRS;

namespace Store.Common.Logging
{
    /// <summary>
    /// Writes one line per query, command, and event so each service window shows the traffic it sends and handles.
    /// </summary>
    public sealed class ConsoleBusLogger : IBusLogger
    {
        public void BeginCall(Type interfaceType, string methodName, object[] arguments, string service, string source, bool handled) { }
        public void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled) { }
        public void BeginEvent(Type eventType, IEvent @event, string service, string source, bool handled) { }

        public void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex)
            => Write("Query", $"{interfaceType.Name}.{methodName}", source, handled, milliseconds, ex);

        public void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex)
            => Write("Command", commandType.Name, source, handled, milliseconds, ex);

        public void EndEvent(Type eventType, IEvent @event, string service, string source, bool handled, long milliseconds, Exception? ex)
            => Write("Event", eventType.Name, source, handled, milliseconds, ex);

        private static void Write(string kind, string name, string source, bool handled, long milliseconds, Exception? ex)
        {
            var direction = handled ? $"Handled {kind} {name} from {source}" : $"Sent {kind} {name}";
            if (ex is null)
                ConsoleLogger.Write(handled ? ConsoleColor.Green : ConsoleColor.Cyan, handled ? "IN" : "OUT", $"{direction} ({milliseconds} ms)", null);
            else
                ConsoleLogger.Write(ConsoleColor.Red, handled ? "IN" : "OUT", $"{direction} ({milliseconds} ms) failed:", ex);
        }
    }
}
