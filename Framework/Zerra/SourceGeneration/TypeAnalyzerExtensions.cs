// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.SourceGeneration.Types;

namespace Zerra.SourceGeneration
{
    public static class TypeAnalyzerExtensions
    {
        public static TypeDetail GetTypeDetail(this Type it) => TypeAnalyzer.GetTypeDetail(it);
    }
}
