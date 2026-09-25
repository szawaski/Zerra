// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    internal sealed class MapDefinitionInfo
    {
        //the converter cache key, built once here: the name alone isn't unique across type pairs with same-typed members
        public string Id { get; }
        public bool IsReverse { get; init; }
        public string Name { get; init; }
        public Type TargetType { get; init; }
        public Delegate TargetSetter { get; init; }
        public Type SourceType { get; init; }
        public Delegate SourceGetter { get; init; }

        public MapDefinitionInfo(Type parentSourceType, Type parentTargetType, bool isReverse, string name, Type targetType, Delegate targetSetter, Type sourceType, Delegate sourceGetter)
        {
            this.Id = $"{parentSourceType.FullName} to {parentTargetType.FullName}.{name}";
            this.IsReverse = isReverse;
            this.Name = name;
            this.TargetType = targetType;
            this.TargetSetter = targetSetter;
            this.SourceType = sourceType;
            this.SourceGetter = sourceGetter;
        }
    }
}
