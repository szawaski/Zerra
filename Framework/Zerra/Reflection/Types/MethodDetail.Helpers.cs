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
                {
                    lock (locker)
                        field ??= TypeAnalyzer.GetTypeDetail(ReturnType);
                }
                return field;
            }
        }

        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
        public MethodInfo GetMemberInfo()
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
