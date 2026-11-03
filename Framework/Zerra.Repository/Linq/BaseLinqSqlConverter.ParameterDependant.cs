using Zerra.Repository.Reflection;

namespace Zerra.Repository.Linq
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
            public ModelMemberDetail? ParentMember;

            /// <summary>Child dependants keyed by the member that navigates to them, each being a JOIN of its own.</summary>
            public Dictionary<string, ParameterDependant> Dependants;

            /// <summary>The name this parameter's table goes by in the statement: its own, or an alias when the statement already reads that table.</summary>
            public string TableName;

            private readonly ParameterDependant root;
            private HashSet<string>? tables;

            /// <summary>
            /// Initializes a new <see cref="ParameterDependant"/> for the given model and optional parent navigation property.
            /// </summary>
            /// <param name="modelDetail">The metadata for this parameter's model.</param>
            /// <param name="parentMember">The parent navigation property, or <see langword="null"/> for the root parameter.</param>
            /// <param name="parent">The dependant this one joins from, or <see langword="null"/> for the root parameter.</param>
            public ParameterDependant(ModelDetail modelDetail, ModelMemberDetail? parentMember, ParameterDependant? parent = null)
            {
                this.ModelDetail = modelDetail;
                this.ParentMember = parentMember;
                this.Dependants = new Dictionary<string, ParameterDependant>();
                this.TableName = modelDetail.DataSourceEntityName;
                this.root = parent?.root ?? this;
                if (parent is not null)
                {
                    root.tables ??= [root.TableName];
                    if (!root.tables.Add(TableName))
                    {
                        TableName = $"{TableName}_{root.tables.Count}";
                        _ = root.tables.Add(TableName);
                    }
                }
            }
        }
    }
}
