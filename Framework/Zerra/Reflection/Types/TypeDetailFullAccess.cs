// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using Zerra.Reflection.Dynamic;

namespace Zerra.Reflection
{
    /// <summary>
    /// Every member, method, and constructor of a type, read through reflection: private, internal, static, and explicit interface implementations,
    /// with accessors for each. A base type's private members are on the base type's own detail.
    /// Details the type's <see cref="TypeDetail"/> already has in full are the same instances.
    /// Get it from <see cref="TypeDetail.GetFullAccess"/>.
    /// </summary>
#if !NETSTANDARD2_0
    [RequiresUnreferencedCode("Full access reads private and non-public members through reflection, trimming can remove them")]
    [RequiresDynamicCode("Full access builds its accessors at runtime")]
#endif
    public partial class TypeDetailFullAccess
    {
#if NETSTANDARD2_0
        private protected readonly object locker = new();
#else
        private protected readonly Lock locker = new();
#endif

        private readonly TypeDetail typeDetail;

        internal TypeDetailFullAccess(TypeDetail typeDetail)
        {
            this.typeDetail = typeDetail;
        }

        /// <summary>The type these details describe.</summary>
        public Type Type => typeDetail.Type;

        private IReadOnlyList<MemberDetail>? members;
        /// <summary>Every property and field.</summary>
        public IReadOnlyList<MemberDetail> Members
        {
            get
            {
                if (members == null)
                {
                    lock (locker)
                        members ??= TypeDetailGenerator.GenerateFullAccessMembers(typeDetail.Type, typeDetail.Interfaces, typeDetail.Members);
                }
                return members;
            }
        }

        private IReadOnlyList<MethodDetail>? methods;
        /// <summary>Every method, including static, property and event accessors, operators, explicit interface implementations, and methods with ref or out parameters.</summary>
        public IReadOnlyList<MethodDetail> Methods
        {
            get
            {
                if (methods == null)
                {
                    lock (locker)
                        methods ??= TypeDetailGenerator.GenerateFullAccessMethods(typeDetail.Type, typeDetail.Interfaces, typeDetail.Methods);
                }
                return methods;
            }
        }

        private IReadOnlyList<ConstructorDetail>? constructors;
        /// <summary>Every instance constructor with its actual parameters.</summary>
        public IReadOnlyList<ConstructorDetail> Constructors
        {
            get
            {
                if (constructors == null)
                {
                    lock (locker)
                        constructors ??= TypeDetailGenerator.GenerateFullAccessConstructors(typeDetail.Type, typeDetail.Constructors);
                }
                return constructors;
            }
        }
    }
}
