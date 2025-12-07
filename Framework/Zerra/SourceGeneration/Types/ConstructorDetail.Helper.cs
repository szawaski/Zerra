// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.SourceGeneration.Types
{
    public partial class ConstructorDetail
    {
        [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
        [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
        public ConstructorInfo GetConstructorInfo()
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new NotSupportedException($"Cannot get member info.  Dynamic code generation is not supported in this build configuration.");
            var constructor = ParentType.GetConstructor(Parameters.Select(x => x.Type).ToArray());
            if (constructor == null)
                throw new InvalidOperationException($"ConstructorInfo was not found.");
            return constructor;
        }
    }
}
