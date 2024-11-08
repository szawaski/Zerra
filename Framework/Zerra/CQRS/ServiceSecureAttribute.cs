// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.CQRS
{
    /// <summary>
    /// A security guard for commands, events, query interfaces, or query interface methods.
    /// This requires a user on the thread to be authenticated and be in any of the specified roles.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method)]
    public sealed class ServiceSecureAttribute : Attribute
    {
        /// <summary>
        /// The roles the user must be in to access what the attribute guards.
        /// </summary>
        public IReadOnlyCollection<string>? Roles { get; }

        /// <summary>
        /// Creates a new security attribute that requires a user to be authenticated.
        /// </summary>
        public ServiceSecureAttribute()
        {
            Roles = null;
        }

        /// <summary>
        /// Creates a new security attribute that requires a user to be authenticated.
        /// </summary>
        /// <param name="roles">The roles the user must be in to access what the attribute guards.</param>
        public ServiceSecureAttribute(params string[] roles)
        {
            if (roles.Length == 0)
                this.Roles = null;
            else
                this.Roles = roles;
        }
        /// <summary>
        /// Creates a new security attribute that requires a user to be authenticated.
        /// </summary>
        /// <param name="roles">The roles the user must be in to access what the attribute guards.</param>
        public ServiceSecureAttribute(IReadOnlyCollection<string> roles)
        {
            if (roles.Count == 0)
                this.Roles = null;
            else
                this.Roles = roles;
        }
    }
}
