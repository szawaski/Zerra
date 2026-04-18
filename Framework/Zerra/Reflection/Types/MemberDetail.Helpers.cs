// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Zerra.Reflection
{
    public partial class MemberDetail
    {
        /// <summary>
        /// Gets the cached type detail for this member's type.
        /// Lazily initializes and caches the type detail information for serialization and reflection.
        /// </summary>
        public TypeDetail TypeDetail
        {
            get
            {
                if (field == null)
                    field ??= TypeAnalyzer.GetTypeDetail(this.Type);
                return field;
            }
        }

        private MemberInfo? memberInfo;
        /// <summary>
        /// Gets the reflection MemberInfo for this member.
        /// Uses reflection to lookup the member by name with public, non-public, instance, and static binding flags.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the MemberInfo for the specified name cannot be found.</exception>
        public MemberInfo MemberInfo
        {
            get
            {
                //Expression trees often need MemberInfo.
                //SourceGeneration explicitly calls typeof().GetMembers() so the compiler can guarentee existance.
                //Using an unknown variable type.GetMembers() does not work, it must be explicit to the compiler.

                if (memberInfo == null)
                {
#pragma warning disable IL2080 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The source field does not have matching annotations.
                    memberInfo = ParentType.GetMember(Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).FirstOrDefault();
#pragma warning restore IL2080 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The source field does not have matching annotations.
                    if (memberInfo == null)
                        throw new InvalidOperationException($"MemberInfo '{Name}' was not found.");
                }
                return memberInfo;
            }
        }
    }
}
