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

        private MethodInfo? methodInfo;
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
                if (methodInfo == null)
                {
                    if (!RuntimeFeature.IsDynamicCodeSupported)
                        throw new NotSupportedException($"Cannot get member info.  Dynamic code generation is not supported in this build configuration.");

                    if (GenericArguments.Count == 0)
                    {
                        methodInfo = ParentType.GetMethod(Name, Parameters.Select(x => x.Type).ToArray());
                        if (methodInfo == null)
                            throw new InvalidOperationException($"MethodInfo '{Name}' was not found.");
                        return methodInfo;
                    }

                    var methodCandidates = ParentType.GetMethods().Where(x =>
                        x.Name == Name &&
                        x.GetGenericArguments().Length == GenericArguments.Count &&
                        x.GetParameters().Length == Parameters.Count
                    ).ToArray();

                    if (methodCandidates.Length == 1)
                    {
                        return methodCandidates[0].MakeGenericMethod(GenericArguments.ToArray());
                    }

                    foreach (var candidate in methodCandidates)
                    {
                        try
                        {
                            methodInfo = candidate.MakeGenericMethod(GenericArguments.ToArray());
                        }
                        catch
                        {
                            continue;
                        }

                        if (methodInfo.ReturnType != ReturnType)
                        {
                            methodInfo = null;
                            continue;
                        }
                        if (!methodInfo.GetParameters().Select(x => x.ParameterType).SequenceEqual(Parameters.Select(x => x.Type)))
                        {
                            methodInfo = null;
                            continue;
                        }
                        if (!methodInfo.GetGenericArguments().SequenceEqual(GenericArguments))
                        {
                            methodInfo = null;
                            continue;
                        }
                        break;
                    }

                    if (methodInfo == null)
                        throw new InvalidOperationException($"MethodInfo '{Name}' with generic parameters was not found.");
                }

                return methodInfo;
            }
        }
    }
}
