using Zerra.CQRS;

namespace Store.Common
{
    /// <summary>
    /// Forwards one event to several downstream producers. <see cref="Zerra.CQRS.Bus"/> only allows one producer to be
    /// registered per event type, so having more than one downstream service subscribe to the same events over direct
    /// TCP/HTTP producers (rather than a message broker, which fans out on its own) means composing them behind one
    /// <see cref="IEventProducer"/> like this and registering that instead.
    /// </summary>
    public sealed class MultiEventProducer : IEventProducer
    {
        private readonly IEventProducer[] producers;

        public MultiEventProducer(params IEventProducer[] producers)
        {
            this.producers = producers;
        }

        public string MessageHost => String.Join(", ", producers.Select(x => x.MessageHost));

        public void RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            foreach (var producer in producers)
                producer.RegisterEventType(maxConcurrent, topic, type);
        }

        public Task DispatchAsync(IEvent @event, string source, CancellationToken cancellationToken)
            => Task.WhenAll(producers.Select(x => x.DispatchAsync(@event, source, cancellationToken)));
    }
}
