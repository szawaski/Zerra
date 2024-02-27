// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed class AcknowledgementException : RemoteServiceException
    {
        public string Topic { get; private set; }
        public Acknowledgement Acknowledgement { get; private set; }
        public AcknowledgementException(Acknowledgement acknowledgement, string topic) : base(acknowledgement.ErrorMessage)
        {
            this.Acknowledgement = acknowledgement ?? throw new ArgumentNullException(nameof(acknowledgement));
            if (String.IsNullOrWhiteSpace(topic))
                throw new ArgumentNullException(nameof(topic));
            
            this.Topic = topic;
        }
    }
}
