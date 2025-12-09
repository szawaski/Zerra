// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Reflection;
using Zerra.Reflection.Dynamic;

namespace Zerra.Map
{
    [RequiresUnreferencedCode("Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
    [RequiresDynamicCode("Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling")]
    public static class MapDiscovery
    {
        public static void Initialize()
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new NotSupportedException($"{nameof(MapDiscovery)}.{nameof(Initialize)} not supported.  Dynamic code generation is not supported in this build configuration.");

            var types = Discovery.GetClassesByInterface(typeof(IMapDefinition<,>));
            if (types.Count == 0)
                return;

            var args = new Type[8];
            foreach (var type in types)
            {
                if (type.ContainsGenericParameters)
                    continue;

                foreach (var i in type.GetInterfaces().Where(x => x.Name == "IMapDefinition`2"))
                {
                    var genericArgs = i.GetGenericArguments();
                    var sourceType = genericArgs[0].GetTypeDetail();
                    var targetType = genericArgs[1].GetTypeDetail();

                    args[0] = genericArgs[0];
                    args[1] = genericArgs[1];
                    args[2] = sourceType.IEnumerableGenericInnerType ?? typeof(object);
                    args[3] = targetType.IEnumerableGenericInnerType ?? typeof(object);
                    args[4] = sourceType.DictionaryInnerTypeDetail?.InnerTypes[0] ?? typeof(object);
                    args[5] = sourceType.DictionaryInnerTypeDetail?.InnerTypes[1] ?? typeof(object);
                    args[6] = targetType.DictionaryInnerTypeDetail?.InnerTypes[0] ?? typeof(object);
                    args[7] = targetType.DictionaryInnerTypeDetail?.InnerTypes[1] ?? typeof(object);

                    var method = typeof(MapDefinition).GetMethods(BindingFlags.Static | BindingFlags.Public).First(x => x.Name == "Register" && x.GetGenericArguments().Length == 8)!.MakeGenericMethod(args);
                    var converter = Activator.CreateInstance(type);
                    _ = method.Invoke(null, [converter]);
                }
            }
        }
    }
}
