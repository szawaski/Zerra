// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
#if !NETSTANDARD2_0
            [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
            [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
#endif
            get
            {
                if (methodInfo == null)
                {
                    if (GenericArguments.Count == 0)
                    {
                        //non-public and inherited statics too, full access lists them, and an interface's methods include those of the interfaces it extends
                        methodInfo = ParentType.GetMethod(Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, null, Parameters.Select(x => x.Type).ToArray(), null);
                        if (methodInfo == null && ParentType.IsInterface)
                        {
                            foreach (var i in ParentType.GetInterfaces())
                            {
                                methodInfo = i.GetMethod(Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Parameters.Select(x => x.Type).ToArray(), null);
                                if (methodInfo != null)
                                    break;
                            }
                        }
                        if (methodInfo == null)
                            throw new InvalidOperationException($"MethodInfo '{Name}' was not found.");
                        return methodInfo;
                    }

                    var methodCandidates = ParentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy).Where(x =>
                        x.Name == Name &&
                        x.GetGenericArguments().Length == GenericArguments.Count &&
                        x.GetParameters().Length == Parameters.Count
                    ).ToArray();
                    if (methodCandidates.Length == 0 && ParentType.IsInterface)
                    {
                        foreach (var i in ParentType.GetInterfaces())
                        {
                            methodCandidates = i.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x =>
                                x.Name == Name &&
                                x.GetGenericArguments().Length == GenericArguments.Count &&
                                x.GetParameters().Length == Parameters.Count
                            ).ToArray();
                            if (methodCandidates.Length > 0)
                                break;
                        }
                    }

                    var isGenericMethodDefinition = GenericArguments.All(x => x.IsGenericParameter);

                    if (methodCandidates.Length == 1)
                    {
                        methodInfo = isGenericMethodDefinition ? methodCandidates[0] : methodCandidates[0].MakeGenericMethod(GenericArguments.ToArray());
                        return methodInfo;
                    }

                    foreach (var candidate in methodCandidates)
                    {
                        try
                        {
                            methodInfo = isGenericMethodDefinition ? candidate : candidate.MakeGenericMethod(GenericArguments.ToArray());
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
