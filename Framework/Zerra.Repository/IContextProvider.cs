// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Provides a <see cref="DataContext"/> instance for use by a repository.
    /// </summary>
    public interface IContextProvider
    {
        /// <summary>
        /// Returns the <see cref="DataContext"/> for this provider.
        /// </summary>
        /// <returns>The <see cref="DataContext"/> instance.</returns>
        DataContext GetContext();
    }
}
