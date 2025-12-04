using Zerra.CQRS;

namespace Pets.Service
{
    public sealed class BusLogger : IBusLog
    {
        public void BeginCall(Type interfaceType, string methodName, object[] arguments, string service, string source, bool handled)
        {
            //Console.WriteLine($"{service} {(handled ? "Handling" : "Sending")} {nameof(BeginCall)} - {interfaceType.Name}.{methodName} from {source}");
        }

        public void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled)
        {
            //Console.WriteLine($"{service} {(handled ? "Handling" : "Sending")} {nameof(BeginCommand)} - {commandType.Name} from {source}");
        }

        public void BeginEvent(Type eventType, IEvent @event, string service, string source, bool handled)
        {
            //Console.WriteLine($"{service} {(handled ? "Handling" : "Sending")} {nameof(BeginEvent)} - {eventType.Name} from {source}");
        }

        public void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex)
        {
            //Console.WriteLine($"{service} {(handled ? "Handling" : "Sending")} {nameof(EndCall)} - {interfaceType.Name}.{methodName} from {source}");
        }

        public void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex)
        {
            //Console.WriteLine($"{service} {(handled ? "Handling" : "Sending")} {nameof(EndCommand)} - {commandType.Name} from {source}");
        }

        public void EndEvent(Type eventType, IEvent @event, string service, string source, bool handled, long milliseconds, Exception? ex)
        {
            //Console.WriteLine($"{service} {(handled ? "Handling" : "Sending")} {nameof(EndEvent)} - {eventType.Name} from {source}");
        }
    }
}
