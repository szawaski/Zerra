// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;

namespace Zerra.Test
{
    //tests that assert how long something takes run alone, the rest of the suite running beside them slows them past their limits
    [CollectionDefinition(nameof(TimingSensitive), DisableParallelization = true)]
    public sealed class TimingSensitive { }
}
