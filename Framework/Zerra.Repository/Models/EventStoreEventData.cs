// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Repository
{
    public class EventStoreEventData
    {
        public Guid EventID { get; set; }
        public string EventName { get; set; }
        public ReadOnlyMemory<byte> Data { get; set; }

        public DateTime Date { get; set; }
        public ulong Number { get; set; }
        public bool Deleted { get; set; }
    }
}
