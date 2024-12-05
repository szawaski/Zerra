// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    /// <summary>
    /// Indicates the level of external network an operation is executing from or which a service is exposed.
    /// </summary>
    public enum NetworkType : byte
    {
        /// <summary>
        /// Indicates local to the assembly, no other services.
        /// </summary>
        Local = 0,
        /// <summary>
        /// Indicates internal to the system which may include other services but not exposed to the outside web.
        /// </summary>
        Internal = 1,
        /// <summary>
        /// Indicates an API exposed to the outside web.
        /// </summary>
        Api = 2
    }
}