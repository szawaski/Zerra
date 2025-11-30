// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.SourceGeneration;

/// <summary>
/// Provides extension methods for type name generation using the Zerra framework.
/// Enables convenient access to type naming utilities via extension method syntax on <see cref="Type"/> objects.
/// </summary>
public static class TypeHelperExtensions
{
    /// <summary>
    /// Gets a human-readable simple name for the type using extension method syntax.
    /// This is a convenience wrapper around <see cref="TypeHelper.GetNiceName(Type)"/>.
    /// For generic types, includes type parameters as 'T'. For example, "List&lt;T&gt;".
    /// </summary>
    /// <param name="it">The type to get the name for.</param>
    /// <returns>A human-readable simple name.</returns>
    public static string GetNiceName(this Type it) => TypeHelper.GetNiceName(it);

    /// <summary>
    /// Gets a human-readable fully-qualified name for the type using extension method syntax.
    /// This is a convenience wrapper around <see cref="TypeHelper.GetNiceFullName(Type)"/>.
    /// For generic types, includes type parameters as 'T'. For example, "System.Collections.Generic.List&lt;T&gt;".
    /// </summary>
    /// <param name="it">The type to get the name for.</param>
    /// <returns>A human-readable fully-qualified name.</returns>
    public static string GetNiceFullName(this Type it) => TypeHelper.GetNiceFullName(it);
}
