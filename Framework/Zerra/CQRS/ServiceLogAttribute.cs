// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.CQRS
{
    /// <summary>
    /// An attribute to indicate that the event, command, query interface, or query interface method will be logged.
    /// The <see cref="Bus"/> must have a <see cref="IBusLogger"/> added through the <see cref="Bus.AddLogger(IBusLogger)"/> method.
    /// Note that this has a slightly degraded performance impact.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
    public sealed class ServiceLogAttribute : Attribute
    {
        /// <summary>
        /// The level of logging interception.
        /// </summary>
        public BusLogging BusLogging { get; }
        /// <summary>
        /// Creates a new service log attribute.
        /// </summary>
        /// <param name="busLogging">The level of logging interception.</param>
        public ServiceLogAttribute(BusLogging busLogging)
        {
            BusLogging = busLogging;
        }
    }
}
