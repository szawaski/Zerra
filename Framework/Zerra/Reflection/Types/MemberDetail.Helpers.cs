// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        /// <summary>
        /// Gets the reflection MemberInfo for this member.
        /// Uses reflection to lookup the member by name with public, non-public, instance, and static binding flags.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown when dynamic code generation is not supported in the current build configuration.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the MemberInfo for the specified name cannot be found.</exception>
        public MemberInfo MemberInfo
        {
            [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
            [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
            get
            {
                if (!RuntimeFeature.IsDynamicCodeSupported)
                    throw new NotSupportedException($"Cannot get member info.  Dynamic code generation is not supported in this build configuration.");
                var memberInfo = Type.GetMember(Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).FirstOrDefault();
                if (memberInfo == null)
                    throw new InvalidOperationException($"MemberInfo '{Name}' was not found.");
                return memberInfo;
            }
        }
    }
}
