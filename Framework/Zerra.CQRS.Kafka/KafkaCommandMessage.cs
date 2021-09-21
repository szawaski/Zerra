// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Kafka
{
    public class KafkaCommandMessage
    {
        public ICommand Message { get; set; }
        public string[][] Claims { get; set; }
    }
}
