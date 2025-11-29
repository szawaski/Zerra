// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    public class ConstructorDetail
    {
        public readonly IReadOnlyList<ParameterDetail> Parameters;
        public readonly Func<object?[], object> CreatorBoxed;
        public ConstructorDetail(IReadOnlyList<ParameterDetail> parameters, Func<object?[], object> creatorBoxed)
        {
            this.Parameters = parameters;
            this.CreatorBoxed = creatorBoxed;
        }
    }
}
