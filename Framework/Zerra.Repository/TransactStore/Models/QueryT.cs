// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public class Query<TModel> : Query where TModel : class, new()
    {
        public Query(QueryOperation operation, Type modelType)
            : base(operation, modelType)
        {
        }

        public Query(Query<TModel> query)
            : base(query)
        {
        }
    }
}