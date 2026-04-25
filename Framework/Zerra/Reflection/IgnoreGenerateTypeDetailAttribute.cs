// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Reflection
{
    /// <summary>
    /// Marks a type to be excluded from source generation type detail processing.
    /// When applied to a class, interface, struct, or enum, the source generator will skip detailed type analysis
    /// and code generation for that type, treating it as a black box during source generation.
    /// </summary>
    /// <remarks>
    /// This attribute is useful for:
    /// - Types that should not have generated metadata or proxy code
    /// - Types with complex or dynamic members that source generators cannot analyze
    /// - Third-party types where code generation is not desired
    /// - Performance optimization by excluding types from generation
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum)]
    public sealed class IgnoreGenerateTypeDetailAttribute : Attribute
    {

    }
}
