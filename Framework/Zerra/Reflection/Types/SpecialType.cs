// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Reflection
{
    /// <summary>
    /// Types that require unique handling for operations such as serialization, or method invocation.
    /// Used by the source generator and runtime type analysis to provide efficient handling.
    /// </summary>
    public enum SpecialType : byte
    {
        /// <summary>System.Threading.Tasks.Task - represents an asynchronous operation without a return value.</summary>
        Task,
        /// <summary>System.Type - represents type metadata and is used for runtime type inspection and routing.</summary>
        Type,
        /// <summary>System.Collections.Generic.Dictionary&lt;TKey, TValue&gt; - generic key-value collection type.</summary>
        Dictionary,
        /// <summary>System.Object - base type for all .NET types; used for untyped or polymorphic values.</summary>
        Object,
        /// <summary>System.Void - represents no return value; used in method signatures and reflection.</summary>
        Void,
        /// <summary>Pointer types - unmanaged pointers used in interop scenarios.</summary>
        Pointer,
        /// <summary>System.Threading.CancellationToken - used to signal cancellation of asynchronous operations.</summary>
        CancellationToken
    }
}
