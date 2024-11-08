// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    /// <summary>
    /// Indicates that this command, event, or query interface is allowed to be exposed externally as a service.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ServiceExposedAttribute : Attribute
    {
        /// <summary>
        /// The level of external exposure indicated by the name of the enum.
        /// </summary>
        public NetworkType NetworkType { get; }
        /// <summary>
        /// Instantiates a service exposed attribute with the default level being <see cref="NetworkType.Api"/>.
        /// </summary>
        /// <param name="networkType">The level of external exposure.</param>
        public ServiceExposedAttribute(NetworkType networkType = NetworkType.Api)
        {
            this.NetworkType = networkType;
        }
    }
}
