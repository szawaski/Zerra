// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.IO;

namespace Zerra.CQRS.Network
{
    //for a stream owned by something else, such as an uploaded request body given to a handler or HttpClient's request stream
    internal sealed class LeaveOpenStream : StreamWrapper
    {
        public LeaveOpenStream(Stream stream) : base(stream, true) { }
    }
}
