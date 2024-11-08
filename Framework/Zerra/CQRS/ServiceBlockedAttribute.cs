// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    /// <summary>
    /// Indicates that a method of a query interface is blocked from external exposure.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ServiceBlockedAttribute : Attribute
    {
        /// <summary>
        /// The level of external exposure indicated by the name of the enum.
        /// </summary>
        public NetworkType NetworkType { get; }
        /// <summary>
        /// Instantiates a service blocked attribute with the default level being <see cref="NetworkType.Api"/>.
        /// </summary>
        /// <param name="networkType">The level of external exposure.</param>
        public ServiceBlockedAttribute(NetworkType networkType)
        {
            this.NetworkType = networkType;
        }
    }
}
