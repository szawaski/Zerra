// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.RabbitMQ
{
    public sealed class RabbitMQCommandMessage
    {
        public ICommand? Message { get; set; }
        public string[][]? Claims { get; set; }
        public string? Source { get; set; }
    }
}
