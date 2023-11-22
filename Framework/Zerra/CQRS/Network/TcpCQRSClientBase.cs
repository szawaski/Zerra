// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS.Network
{
    public abstract class TcpCqrsClientBase : CqrsClientBase
    {
        public TcpCqrsClientBase(string serviceUrl) : base(serviceUrl)
        {

        }
    }
}