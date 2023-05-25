// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Kafka
{
    public sealed class KafkaEventMessage
    {
        public IEvent Message { get; set; }
        public string[][] Claims { get; set; }
    }
}
