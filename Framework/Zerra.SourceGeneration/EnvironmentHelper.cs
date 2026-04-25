// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration
{
    /// <summary>
    /// Provides environment-related constants for use in source generators.
    /// The System.Environment class is banned in source generators due to determinism requirements,
    /// so this helper provides safe, deterministic alternatives.
    /// </summary>
    public static class EnvironmentHelper 
    {
        /// <summary>
        /// Gets a constant newline string ("\r\n") for use in source generators.
        /// Do not use Environment.NewLine in source generators as it can cause non-deterministic output.
        /// </summary>
        public static readonly string NewLine = "\r\n";
    }
}

