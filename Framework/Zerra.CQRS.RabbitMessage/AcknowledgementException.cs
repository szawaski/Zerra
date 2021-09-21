﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.RabbitMessage
{
    public class AcknowledgementException : Exception
    {
        public string Exchange { get; private set; }
        public Acknowledgement Acknowledgement { get; private set; }
        public AcknowledgementException(Acknowledgement acknowledgement, string exchange) : base(acknowledgement.ErrorMessage)
        {
            this.Acknowledgement = acknowledgement ?? throw new ArgumentNullException(nameof(acknowledgement));
            if (string.IsNullOrWhiteSpace(exchange))
                throw new ArgumentNullException(nameof(exchange));
            
            this.Exchange = exchange;
        }
    }
}
