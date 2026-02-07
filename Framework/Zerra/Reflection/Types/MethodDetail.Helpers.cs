// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection
{
    public partial class MethodDetail
    {
        /// <summary>
        /// Gets the <see cref="TypeDetail"/> for the method's return type, lazily initializing it if needed.
        /// </summary>
        public TypeDetail ReturnTypeDetail
        {
            get
            {
                if (field == null)
                    field ??= TypeAnalyzer.GetTypeDetail(ReturnType);
                return field;
            }
        }

        /// <summary>
        /// Gets the reflection MethodInfo for this method.
        /// Uses reflection to lookup the method by name, generic argument count, and parameter types.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown when dynamic code generation is not supported in the current build configuration.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the MethodInfo for the specified method cannot be found.</exception>
        public MethodInfo MethodInfo
        {
            [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
            [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
            get
            {
                if (!RuntimeFeature.IsDynamicCodeSupported)
                    throw new NotSupportedException($"Cannot get member info.  Dynamic code generation is not supported in this build configuration.");
                var method = ParentType.GetMethod(Name, GenericArgumentCount, Parameters.Select(x => x.Type).ToArray());
                if (method == null)
                    throw new InvalidOperationException($"MethodInfo '{Name}' with {GenericArgumentCount} generic parameters was not found.");
                return method;
            }
        }
    }
}
