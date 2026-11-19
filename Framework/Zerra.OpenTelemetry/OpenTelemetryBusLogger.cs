// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Zerra.CQRS;

namespace Zerra.OpenTelemetry
{
    /// <summary>
    /// An <see cref="IBusLogger"/> that records a span and a duration for every query, command, and event the bus sends or handles.
    /// </summary>
    /// <remarks>
    /// Spans come from the <see cref="ActivitySource"/> and durations from the <see cref="Meter"/> named <see cref="SourceName"/>.
    /// A handler's span continues the trace of the service that sent the message.
    /// Arguments and message contents are never recorded.
    /// </remarks>
    public sealed class OpenTelemetryBusLogger : IBusLogger
    {
        /// <summary>
        /// The name of the <see cref="ActivitySource"/> and <see cref="Meter"/>, used by <c>AddZerraInstrumentation</c>.
        /// </summary>
        public const string SourceName = "Zerra";

        private const string messageProperty = "Zerra.Message";

        private static readonly string? version = typeof(OpenTelemetryBusLogger).Assembly.GetName().Version?.ToString();
        private static readonly ActivitySource activitySource = new(SourceName, version);
        private static readonly Meter meter = new(SourceName, version);
        private static readonly Histogram<double> duration = meter.CreateHistogram<double>("zerra.bus.duration", "s", "How long queries, commands, and events took to send or handle.");

        private readonly IBusLogger? busLogger;

        /// <summary>
        /// Creates the logger.
        /// </summary>
        /// <param name="busLogger">Another bus logger to call as well, so logging and tracing can be used together. Optional.</param>
        public OpenTelemetryBusLogger(IBusLogger? busLogger = null)
        {
            this.busLogger = busLogger;
        }

        /// <inheritdoc />
        public void BeginCall(Type interfaceType, string methodName, object[] arguments, string service, string source, bool handled)
        {
            if (activitySource.HasListeners())
            {
                var activity = Start($"{interfaceType.Name}/{methodName}", handled ? ActivityKind.Server : ActivityKind.Client, arguments);
                if (activity is not null && activity.IsAllDataRequested)
                {
                    _ = activity.SetTag("rpc.system", "zerra");
                    _ = activity.SetTag("rpc.service", interfaceType.Name);
                    _ = activity.SetTag("rpc.method", methodName);
                    _ = activity.SetTag("zerra.service", service);
                    _ = activity.SetTag("zerra.source", source);
                }
            }
            busLogger?.BeginCall(interfaceType, methodName, arguments, service, source, handled);
        }

        /// <inheritdoc />
        public void BeginCommand(Type commandType, ICommand command, string service, string source, bool handled)
        {
            if (activitySource.HasListeners())
            {
                var activity = Start(handled ? $"process {commandType.Name}" : $"send {commandType.Name}", handled ? ActivityKind.Consumer : ActivityKind.Producer, command);
                if (activity is not null && activity.IsAllDataRequested)
                {
                    _ = activity.SetTag("messaging.system", "zerra");
                    _ = activity.SetTag("messaging.operation.type", handled ? "process" : "send");
                    _ = activity.SetTag("messaging.destination.name", commandType.Name);
                    _ = activity.SetTag("zerra.message.kind", "command");
                    _ = activity.SetTag("zerra.service", service);
                    _ = activity.SetTag("zerra.source", source);
                }
            }
            busLogger?.BeginCommand(commandType, command, service, source, handled);
        }

        /// <inheritdoc />
        public void BeginEvent(Type eventType, IEvent @event, string service, string source, bool handled)
        {
            if (activitySource.HasListeners())
            {
                var activity = Start(handled ? $"process {eventType.Name}" : $"send {eventType.Name}", handled ? ActivityKind.Consumer : ActivityKind.Producer, @event);
                if (activity is not null && activity.IsAllDataRequested)
                {
                    _ = activity.SetTag("messaging.system", "zerra");
                    _ = activity.SetTag("messaging.operation.type", handled ? "process" : "send");
                    _ = activity.SetTag("messaging.destination.name", eventType.Name);
                    _ = activity.SetTag("zerra.message.kind", "event");
                    _ = activity.SetTag("zerra.service", service);
                    _ = activity.SetTag("zerra.source", source);
                }
            }
            busLogger?.BeginEvent(eventType, @event, service, source, handled);
        }

        /// <inheritdoc />
        public void EndCall(Type interfaceType, string methodName, object[] arguments, object? result, string service, string source, bool handled, long milliseconds, Exception? ex)
        {
            busLogger?.EndCall(interfaceType, methodName, arguments, result, service, source, handled, milliseconds, ex);
            var seconds = Stop(arguments, ex);
            if (duration.Enabled)
            {
                var tags = new TagList
                {
                    { "rpc.service", interfaceType.Name },
                    { "rpc.method", methodName },
                    { "zerra.handled", handled }
                };
                if (ex is not null)
                    tags.Add("error.type", ex.GetType().FullName);
                duration.Record(seconds >= 0 ? seconds : milliseconds / 1000d, tags);
            }
        }

        /// <inheritdoc />
        public void EndCommand(Type commandType, ICommand command, string service, string source, bool handled, long milliseconds, Exception? ex)
        {
            busLogger?.EndCommand(commandType, command, service, source, handled, milliseconds, ex);
            var seconds = Stop(command, ex);
            if (duration.Enabled)
            {
                var tags = new TagList
                {
                    { "messaging.destination.name", commandType.Name },
                    { "zerra.message.kind", "command" },
                    { "zerra.handled", handled }
                };
                if (ex is not null)
                    tags.Add("error.type", ex.GetType().FullName);
                duration.Record(seconds >= 0 ? seconds : milliseconds / 1000d, tags);
            }
        }

        /// <inheritdoc />
        public void EndEvent(Type eventType, IEvent @event, string service, string source, bool handled, long milliseconds, Exception? ex)
        {
            busLogger?.EndEvent(eventType, @event, service, source, handled, milliseconds, ex);
            var seconds = Stop(@event, ex);
            if (duration.Enabled)
            {
                var tags = new TagList
                {
                    { "messaging.destination.name", eventType.Name },
                    { "zerra.message.kind", "event" },
                    { "zerra.handled", handled }
                };
                if (ex is not null)
                    tags.Add("error.type", ex.GetType().FullName);
                duration.Record(seconds >= 0 ? seconds : milliseconds / 1000d, tags);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Activity? Start(string name, ActivityKind kind, object message)
        {
            //the received trace is the parent unless an activity is running, a finished one can still be current where it was captured, such as a server's receive loop started during startup
            ActivityContext parent = default;
            var traceParent = CqrsTrace.TraceParent;
            var current = Activity.Current;
            if (traceParent is not null && (current is null || current.IsStopped))
                _ = ActivityContext.TryParse(traceParent, null, true, out parent);

            var activity = activitySource.StartActivity(name, kind, parent);
            activity?.SetCustomProperty(messageProperty, message);
            return activity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Stop(object message, Exception? ex)
        {
            //Begin and End share no state, the span is found by the message it was started for, past any activity the handler left open
            var activity = Activity.Current;
            while (activity is not null && !ReferenceEquals(activity.GetCustomProperty(messageProperty), message))
                activity = activity.Parent;
            if (activity is null)
                return -1;

            if (ex is not null)
            {
                _ = activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                _ = activity.SetTag("error.type", ex.GetType().FullName);
#if !NETSTANDARD2_0
                _ = activity.AddException(ex);
#endif
            }
            activity.Stop();
            return activity.Duration.TotalSeconds;
        }
    }
}
