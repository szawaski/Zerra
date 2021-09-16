// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS.RabbitMessage
{
    public class AcknowledgementException : Exception
    {
        public string Exchange { get; private set; }
        public Acknowledgement Affirmation { get; private set; }
        public AcknowledgementException(Acknowledgement affirmation, string exchange) : base(affirmation.ErrorMessage)
        {
            this.Affirmation = affirmation ?? throw new ArgumentNullException(nameof(affirmation));
            if (string.IsNullOrWhiteSpace(exchange))
                throw new ArgumentNullException(nameof(exchange));
            
            this.Exchange = exchange;
        }
    }
}
