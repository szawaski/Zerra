// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.Network
{
    public class CQRSRequestAbortedException : Exception
    {
        public CQRSRequestAbortedException() : base("Request Aborted from Stream Ending") { }
        public CQRSRequestAbortedException(string message) : base(message) { }
    }
}
