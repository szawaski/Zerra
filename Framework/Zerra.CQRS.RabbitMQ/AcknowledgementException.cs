// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.RabbitMQ
{
    public sealed class AcknowledgementException : RemoteServiceException
    {
        public string Exchange { get; private set; }
        public Acknowledgement Acknowledgement { get; private set; }
        public AcknowledgementException(Acknowledgement acknowledgement, string exchange) : base(acknowledgement.ErrorMessage)
        {
            this.Acknowledgement = acknowledgement ?? throw new ArgumentNullException(nameof(acknowledgement));
            if (String.IsNullOrWhiteSpace(exchange))
                throw new ArgumentNullException(nameof(exchange));
            
            this.Exchange = exchange;
        }
    }
}
