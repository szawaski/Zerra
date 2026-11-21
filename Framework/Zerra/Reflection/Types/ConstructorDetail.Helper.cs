// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Zerra.Reflection
{
    public partial class ConstructorDetail
    {
        private ConstructorInfo? constructorInfo;
        /// <summary>
        /// Gets the reflection <see cref="ConstructorInfo"/> for this constructor.
        /// Uses reflection to look up the constructor by its parameter types.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown when dynamic code generation is not supported in the current build configuration.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the <see cref="ConstructorInfo"/> cannot be found.</exception>
        public ConstructorInfo GetConstructorInfo
        {
#if !NETSTANDARD2_0
            [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
#endif
            get
            {
                if (constructorInfo == null)
                {
                    constructorInfo = ParentType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Parameters.Select(x => x.Type).ToArray(), null);
                    if (constructorInfo == null)
                        throw new InvalidOperationException($"ConstructorInfo was not found.");
                }
                return constructorInfo;
            }
        }
    }
}
