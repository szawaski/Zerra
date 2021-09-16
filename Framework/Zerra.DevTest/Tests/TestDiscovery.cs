// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;

namespace Zerra.DevTest
{
    public static class TestDiscovery
    {
        public static void Test()
        {
            var typePrimitiveInstantiator = Instantiator.CreateInstance<byte>();

            var typeA = typeof(Tuple<,>).MakeGenericType(typeof(string[]), typeof(List<int>)).MakeArrayType().MakeArrayType(1).MakeArrayType(2);
            var name = typeA.GetNiceFullName();
            var typeB = Discovery.GetTypeFromName(typeA.FullName);
            var typeC = Discovery.GetTypeFromName(name);
            if (typeA != typeB || typeA != typeC)
                throw new Exception();
        }
    }
}
