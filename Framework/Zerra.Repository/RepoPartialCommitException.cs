// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// An engine failed to commit after another had already committed, so the work is only partly saved.
    /// </summary>
    public sealed class RepoPartialCommitException : Exception
    {
        /// <summary>The engines whose work is saved.</summary>
        public IReadOnlyList<string> Committed { get; }
        /// <summary>The engines whose work is not saved.</summary>
        public IReadOnlyList<string> NotCommitted { get; }

        internal RepoPartialCommitException(IReadOnlyList<string> committed, IReadOnlyList<string> notCommitted, Exception innerException)
            : base($"Commit partly failed. Committed: {String.Join(", ", committed)}. Not committed: {String.Join(", ", notCommitted)}.", innerException)
        {
            this.Committed = committed;
            this.NotCommitted = notCommitted;
        }
    }
}
