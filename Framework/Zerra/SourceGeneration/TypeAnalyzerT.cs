// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.SourceGeneration.Types;

namespace Zerra.SourceGeneration
{
    public static class TypeAnalyzer<T>
    {
        private static readonly object typeDetailLock = new object();
        private static TypeDetail<T>? typeDetail = null;
        public static TypeDetail<T> GetTypeDetail()
        {
            if (typeDetail is null)
            {
                lock (typeDetailLock)
                {
                    typeDetail ??= (TypeDetail<T>)TypeAnalyzer.GetTypeDetail(typeof(T));
                }
            }
            return typeDetail;
        }
    }
}
