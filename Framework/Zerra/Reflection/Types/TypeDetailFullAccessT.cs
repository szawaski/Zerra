// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;

namespace Zerra.Reflection
{
    /// <summary>
    /// The typed version of <see cref="TypeDetailFullAccess"/>, its constructors create a <typeparamref name="T"/>.
    /// Get it from <see cref="TypeDetail{T}.GetFullAccess"/>.
    /// </summary>
    /// <typeparam name="T">The type these details describe.</typeparam>
#if !NETSTANDARD2_0
    [RequiresUnreferencedCode("Full access reads private and non-public members through reflection, trimming can remove them")]
    [RequiresDynamicCode("Full access builds its accessors at runtime")]
#endif
    public sealed class TypeDetailFullAccess<T> : TypeDetailFullAccess
    {
        internal TypeDetailFullAccess(TypeDetail<T> typeDetail)
            : base(typeDetail)
        {
        }

        private IReadOnlyList<ConstructorDetail<T>>? constructors;
        /// <summary>Every instance constructor with its actual parameters.</summary>
        public new IReadOnlyList<ConstructorDetail<T>> Constructors
        {
            get
            {
                if (constructors == null)
                {
                    lock (locker)
                        constructors ??= base.Constructors.Cast<ConstructorDetail<T>>().ToArray();
                }
                return constructors;
            }
        }
    }
}
