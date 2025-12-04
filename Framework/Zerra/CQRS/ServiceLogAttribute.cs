// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// An attribute to indicate if the event, command, or query interface will be logged.
    /// The <see cref="Bus"/> must have a <see cref="IBusLog"/>.
    /// Note that logging has a slightly degraded performance impact.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
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
        /// <summary>
        /// Creates a new service log attribute.
        /// </summary>
        public ServiceLogAttribute()
        {
            BusLogging = BusLogging.SenderAndHandler;
        }
    }
}
