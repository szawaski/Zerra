// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    //the order of the rows of a related collection a projection selects from, read from the OrderBy calls before its Select
    internal sealed class ProjectionOrder(OrderExpression[] expressions) : QueryOrder(expressions)
    {
    }
}
