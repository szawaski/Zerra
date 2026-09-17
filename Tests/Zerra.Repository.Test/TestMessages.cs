// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.CQRS;

namespace Zerra.Repository.Test
{
    public sealed class TestCommand : ICommand
    {
        public Guid ID { get; set; }
        public int Value { get; set; }
        public string? Text { get; set; }
        public bool Throw { get; set; }
    }

    public sealed class TestCommandWithResult : ICommand<int>
    {
        public Guid ID { get; set; }
        public int Value { get; set; }
        public bool Throw { get; set; }
    }

    public sealed class TestEvent : IEvent
    {
        public Guid ID { get; set; }
        public int Value { get; set; }
        public string? Text { get; set; }
    }
}
