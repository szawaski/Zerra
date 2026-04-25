// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    internal sealed class MapDefinitionInfo
    {
        public bool IsReverse { get; init; }
        public string Name { get; init; }
        public Type TargetType { get; init; }
        public Delegate TargetSetter { get; init; }
        public Type SourceType { get; init; }
        public Delegate SourceGetter { get; init; }

        public MapDefinitionInfo(bool isReverse, string name, Type targetType, Delegate targetSetter, Type sourceType, Delegate sourceGetter)
        {
            this.IsReverse = isReverse;
            this.Name = name;
            this.TargetType = targetType;
            this.TargetSetter = targetSetter;
            this.SourceType = sourceType;
            this.SourceGetter = sourceGetter;
        }
    }
}
