// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using Zerra.Reflection;

namespace Zerra.Repository
{
    //elements combined in memory compare by their values as a store compares rows: a value by its own equality, an object by its members
    internal sealed class LinqElementComparer : IEqualityComparer<object?>
    {
        public static readonly LinqElementComparer Instance = new();

        private static readonly ConcurrentDictionary<Type, Func<object, object?>[]?> gettersByType = new();

        private LinqElementComparer() { }

        private static Func<object, object?>[]? Getters(Type type)
        {
            return gettersByType.GetOrAdd(type, static type =>
            {
                if (type.IsValueType || type == typeof(string))
                    return null;
                var members = TypeAnalyzer.GetTypeDetail(type).SerializableMembers;
                var getters = new Func<object, object?>[members.Count];
                for (var i = 0; i < getters.Length; i++)
                    getters[i] = members[i].GetterBoxed!;
                return getters;
            });
        }

        public new bool Equals(object? x, object? y)
        {
            if (x is null)
                return y is null;
            if (y is null)
                return false;
            var type = x.GetType();
            if (type != y.GetType())
                return false;
            var getters = Getters(type);
            if (getters is null)
                return x.Equals(y);
            foreach (var getter in getters)
            {
                if (!Object.Equals(getter(x), getter(y)))
                    return false;
            }
            return true;
        }

        public int GetHashCode(object? element)
        {
            if (element is null)
                return 0;
            var getters = Getters(element.GetType());
            if (getters is null)
                return element.GetHashCode();
            var hash = new HashCode();
            foreach (var getter in getters)
                hash.Add(getter(element));
            return hash.ToHashCode();
        }
    }
}
