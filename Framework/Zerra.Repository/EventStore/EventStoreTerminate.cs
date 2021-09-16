// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository.EventStore
{
    public class EventStoreTerminate
    {
        public Guid EventID { get; set; }
        public string EventName { get; set; }
        public string StreamName { get; set; }
        public ulong? ExpectedEventNumber { get; set; }
        public EventStoreState ExpectedState { get; set; }
    }
}
