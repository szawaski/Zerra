// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Defines the type of write operation to perform against the data store.
    /// </summary>
    public enum PersistOperation : byte
    {
        /// <summary>Inserts new records.</summary>
        Create = 1,
        /// <summary>Updates existing records.</summary>
        Update = 2,
        /// <summary>Removes records.</summary>
        Delete = 3
    }
}
