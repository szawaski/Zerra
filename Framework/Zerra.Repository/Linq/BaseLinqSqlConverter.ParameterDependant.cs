using System;
using System.Collections.Generic;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract partial class BaseLinqSqlConverter
    {
        /// <summary>
        /// Tracks a model parameter and its related JOIN dependencies discovered during LINQ-to-SQL conversion.
        /// </summary>
        protected sealed class ParameterDependant
        {
            /// <summary>The metadata for the model represented by this parameter.</summary>
            public ModelDetail ModelDetail;

            /// <summary>The property on the parent model that navigates to this dependant, or <see langword="null"/> for the root.</summary>
            public ModelPropertyDetail? ParentMember;

            /// <summary>Child dependants keyed by their model type, representing further JOIN relationships.</summary>
            public Dictionary<Type, ParameterDependant> Dependants;

            /// <summary>
            /// Initializes a new <see cref="ParameterDependant"/> for the given model and optional parent navigation property.
            /// </summary>
            /// <param name="modelDetail">The metadata for this parameter's model.</param>
            /// <param name="parentMember">The parent navigation property, or <see langword="null"/> for the root parameter.</param>
            public ParameterDependant(ModelDetail modelDetail, ModelPropertyDetail? parentMember)
            {
                this.ModelDetail = modelDetail;
                this.ParentMember = parentMember;
                this.Dependants = new Dictionary<Type, ParameterDependant>();
            }
        }
    }
}
