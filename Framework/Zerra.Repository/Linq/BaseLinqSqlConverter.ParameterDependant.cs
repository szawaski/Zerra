using System;
using System.Collections.Generic;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract partial class BaseLinqSqlConverter
    {
        protected sealed class ParameterDependant
        {
            public ModelDetail ModelDetail;
            public ModelPropertyDetail ParentMember;
            public Dictionary<Type, ParameterDependant> Dependants;

            public ParameterDependant(ModelDetail modelDetail, ModelPropertyDetail parentMember)
            {
                this.ModelDetail = modelDetail;
                this.ParentMember = parentMember;
                this.Dependants = new Dictionary<Type, ParameterDependant>();
            }
        }
    }
}
