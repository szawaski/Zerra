// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;
using Zerra.Repository.EventStore;

namespace Zerra.Repository
{
    public interface IEventStoreEngine
    {
        ulong Append(EventStoreAppend eventStoreAppend);
        ulong Terminate(EventStoreTerminate eventStoreTerminate);
        EventStoreEventData[] Read(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate);
        EventStoreEventData[] ReadBackwards(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate);

        Task<ulong> AppendAsync(EventStoreAppend eventStoreAppend);
        Task<ulong> TerminateAsync(EventStoreTerminate eventStoreTerminate);
        Task<EventStoreEventData[]> ReadAsync(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate);
        Task<EventStoreEventData[]> ReadBackwardsAsync(string streamName, ulong? startEventNumber, long? eventCount, ulong? endEventNumber, DateTime? startEventDate, DateTime? endEventDate);
    }
}
