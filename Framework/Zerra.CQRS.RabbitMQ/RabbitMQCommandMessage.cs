// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.RabbitMQ
{
    public class RabbitMQCommandMessage
    {
        public ICommand Message { get; set; }
        public string[][] Claims { get; set; }
    }
}
