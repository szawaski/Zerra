// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Sql
{
    public abstract class SqlDataContext
    {
        public abstract ISqlEngine GetEngine();

        public void AssureDataStore()
        {
            var engine = GetEngine();

            var modelTypes = Discovery.GetTypesFromAttribute(typeof(DataSourceEntityAttribute));
            var modelDetails = modelTypes.Select(x => ModelAnalyzer.GetModel(x)).ToArray();

            engine.AssureDataStore(modelDetails);
        }
    }
}
