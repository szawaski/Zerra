// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.MsSql
{
    //the values of a statement's parameters, with the index of each once there are enough that looking through them for one already taken costs more than a lookup
    internal sealed class MsSqlParameterList : List<object>
    {
        public Dictionary<object, int>? Indexes;
    }
}
