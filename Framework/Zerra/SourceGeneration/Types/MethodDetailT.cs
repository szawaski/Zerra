// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    /// <summary>
    /// Generic version of method metadata with strongly-typed delegate for improved type safety.
    /// </summary>
    /// <typeparam name="T">The return type of this method.</typeparam>
    public sealed class MethodDetail<T> : MethodDetail
    {
        /// <summary>The strongly-typed delegate for invoking this method.</summary>
        public readonly new Func<object?, object?[]?, T> Caller;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodDetail{T}"/> class with strongly-typed method metadata.
        /// </summary>
        /// <param name="parentType">The parent type that owns this method.</param>
        /// <param name="name">The name of the method.</param>
        /// <param name="parameters">The parameters required by this method.</param>
        /// <param name="caller">The strongly-typed delegate for invoking this method.</param>
        /// <param name="callerBoxed">Boxed delegate for invoking this method.</param>
        /// <param name="attributes">Custom attributes applied to the method.</param>
        /// <param name="isStatic">Whether this method is static.</param>
        /// <param name="isExplicitFromInterface">Whether this method is an explicit interface implementation.</param>
        public MethodDetail(Type parentType, string name, IReadOnlyList<ParameterDetail> parameters, Func<object?, object?[]?, T> caller, Func<object?, object?[]?, object?> callerBoxed, IReadOnlyList<Attribute> attributes, bool isStatic, bool isExplicitFromInterface)
            : base(parentType, name, typeof(T), parameters, caller, callerBoxed, attributes, isStatic, isExplicitFromInterface)
        {
            this.Caller = caller;
        }
    }
}
