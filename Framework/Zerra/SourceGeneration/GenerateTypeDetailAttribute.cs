// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration
{
    /// <summary>
    /// Marks a type for detailed source generation processing including serialization and routing metadata.
    /// When applied to a class, interface, struct, or enum, the source generator will perform comprehensive
    /// analysis and generate metadata, proxy implementations, serialization code, or other code artifacts for that type.
    /// </summary>
    /// <remarks>
    /// This attribute is useful for:
    /// - Types that require generated proxy implementations (e.g., query interfaces for the CQRS bus)
    /// - Types that participate in serialization/deserialization workflows
    /// - Types where reflection metadata should be generated for runtime use
    /// - Types that participate in the Zerra framework's message routing and serialization
    /// - Explicitly opting in types for source generation when needed
    /// 
    /// By default, many types are processed automatically, but this attribute can be used to ensure
    /// a type receives special attention from the source generator or to opt in types that might otherwise
    /// be skipped by optimization filters.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum)]
    public sealed class GenerateTypeDetailAttribute : Attribute
    {

    }
}
