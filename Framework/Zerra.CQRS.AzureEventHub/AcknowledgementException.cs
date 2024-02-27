// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.AzureEventHub
{
    public sealed class AcknowledgementException : RemoteServiceException
    {
        public string Partition { get; private set; }
        public Acknowledgement Acknowledgement { get; private set; }
        public AcknowledgementException(Acknowledgement acknowledgement, string partition) : base(acknowledgement.ErrorMessage)
        {
            this.Acknowledgement = acknowledgement ?? throw new ArgumentNullException(nameof(acknowledgement));
            if (String.IsNullOrWhiteSpace(partition))
                throw new ArgumentNullException(nameof(partition));
            
            this.Partition = partition;
        }
    }
}
