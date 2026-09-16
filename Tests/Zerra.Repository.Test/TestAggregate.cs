// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS;

namespace Zerra.Repository.Test
{
    public sealed class TestAggregateCreated : IEvent
    {
        public string Name { get; set; } = null!;
        public int Amount { get; set; }
    }

    public sealed class TestAggregateAmountAdded : IEvent
    {
        public int Amount { get; set; }
    }

    public sealed class TestAggregateRenamed : IEvent
    {
        public string Name { get; set; } = null!;
    }

    public sealed class TestAggregateRemoved : IEvent
    {
        public string Reason { get; set; } = null!;
    }

    public interface ITestAggregateEventHandler :
        IEventHandler<TestAggregateCreated>,
        IEventHandler<TestAggregateAmountAdded>,
        IEventHandler<TestAggregateRenamed>,
        IEventHandler<TestAggregateRemoved>
    { }

    public sealed class TestAggregateEventHandler : BaseHandler, ITestAggregateEventHandler
    {
        public List<IEvent> Dispatched { get; } = new();

        public Task Handle(TestAggregateCreated @event)
        {
            Dispatched.Add(@event);
            return Task.CompletedTask;
        }
        public Task Handle(TestAggregateAmountAdded @event)
        {
            Dispatched.Add(@event);
            return Task.CompletedTask;
        }
        public Task Handle(TestAggregateRenamed @event)
        {
            Dispatched.Add(@event);
            return Task.CompletedTask;
        }
        public Task Handle(TestAggregateRemoved @event)
        {
            Dispatched.Add(@event);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A second aggregate that accepts the same events as <see cref="TestAggregate"/> with its own methods and its own stream.
    /// </summary>
    public sealed class TestOtherAggregate : AggregateRoot
    {
        public TestOtherAggregate(Guid id, IEventStoreEngine eventStore)
            : base(id, eventStore) { }

        public string? Label { get; private set; }

        public Task On(TestAggregateCreated @event)
        {
            Label = $"Other-{@event.Name}";
            return Task.CompletedTask;
        }
    }

    public sealed class TestAggregate : AggregateRoot
    {
        public TestAggregate(Guid id, IEventStoreEngine eventStore)
            : base(id, eventStore) { }

        public string? Name { get; private set; }
        public int Amount { get; private set; }
        public int AppliedCount { get; private set; }
        public string? RemovedReason { get; private set; }

        public Task On(TestAggregateCreated @event)
        {
            Name = @event.Name;
            Amount = @event.Amount;
            AppliedCount++;
            return Task.CompletedTask;
        }
        public Task On(TestAggregateAmountAdded @event)
        {
            Amount += @event.Amount;
            AppliedCount++;
            return Task.CompletedTask;
        }
        public Task On(TestAggregateRenamed @event)
        {
            Name = @event.Name;
            AppliedCount++;
            return Task.CompletedTask;
        }
        public Task On(TestAggregateRemoved @event)
        {
            RemovedReason = @event.Reason;
            AppliedCount++;
            return Task.CompletedTask;
        }
    }
}
