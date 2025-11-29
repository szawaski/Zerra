// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    public sealed class ConstructorDetail<T> : ConstructorDetail
    {
        public readonly Func<object?[], T> Creator;
        public ConstructorDetail(IReadOnlyList<ParameterDetail> parameters, Func<object?[], T> creator, Func<object?[], object> creatorBoxed)
            : base(parameters, creatorBoxed)
        {
            this.Creator = creator;
        }
    }
}
