﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zerra.Reflection;

namespace Zerra.Repository.Reflection
{
    public sealed class ModelDetail
    {
        public Type Type { get; }
        public string Name { get; }
        private readonly string? dataSourceEntityName;
        public bool IsDataSourceEntity => dataSourceEntityName != null;
        public string DataSourceEntityName => dataSourceEntityName ?? throw new Exception($"{Type.GetNiceName()} does not have an EntityAttribute");
        public IReadOnlyList<ModelPropertyDetail> Properties { get; }
        public IReadOnlyList<ModelPropertyDetail> IdentityProperties { get; }
        public IReadOnlyList<ModelPropertyDetail> IdentityAutoGeneratedProperties { get; }
        public IReadOnlyList<ModelPropertyDetail> RelatedProperties { get; }
        public IReadOnlyList<ModelPropertyDetail> RelatedEnumerableProperties { get; }
        public IReadOnlyList<ModelPropertyDetail> RelatedNonEnumerableProperties { get; }
        public IReadOnlyList<ModelPropertyDetail> NonAutoGeneratedNonRelationProperties { get; }

        private readonly IDictionary<string, ModelPropertyDetail> propertiesByName;
        public ModelPropertyDetail GetProperty(string name)
        {
            if (!this.propertiesByName.TryGetValue(name, out var property))
                throw new Exception($"{nameof(ModelDetail)} for {Type.Name} does not contain property {name}");
            return property;
        }
        public bool TryGetProperty(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out ModelPropertyDetail property)
        {
            return this.propertiesByName.TryGetValue(name, out property);
        }

        private readonly IDictionary<string, ModelPropertyDetail> propertiesByNameLower;
        public ModelPropertyDetail GetPropertyLower(string name)
        {
            if (!this.propertiesByNameLower.TryGetValue(name, out var property))
                throw new Exception($"{nameof(ModelDetail)} for {Type.Name} does not contain property {name}");
            return property;
        }
        public bool TryGetPropertyLower(string name,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out ModelPropertyDetail property)
        {
            return this.propertiesByNameLower.TryGetValue(name, out property);
        }

        public Func<object> Creator { get; set; }

        public override string ToString()
        {
            return Type.GetNiceFullName();
        }

        internal ModelDetail(TypeDetail typeDetails)
        {
            this.Type = typeDetails.Type;
            while (typeDetails.InnerTypes.Count == 1)
            {
                this.Type = typeDetails.InnerType;
                typeDetails = typeDetails.InnerTypeDetail;
            }

            var sourceEntityAttribute = typeDetails.Attributes.Select(x => x as EntityAttribute).Where(x => x != null).FirstOrDefault();
            if (sourceEntityAttribute != null)
            {
                var storeName = sourceEntityAttribute.StoreName;
                if (String.IsNullOrWhiteSpace(storeName))
                    storeName = this.Type.Name;

                if (!storeName.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                    throw new ArgumentException($"{nameof(EntityAttribute)}.{nameof(EntityAttribute.StoreName)}={this.DataSourceEntityName} is not a valid name");
                this.dataSourceEntityName = storeName;
            }

            var modelProperties = new List<ModelPropertyDetail>();
            foreach (var member in typeDetails.MemberDetails)
            {
                var notSourcePropertyAttribute = member.Attributes.Select(x => x as StoreExcludeAttribute).Where(x => x != null).FirstOrDefault();
                if (notSourcePropertyAttribute == null)
                {
                    var modelPropertyInfo = new ModelPropertyDetail(member, this.IsDataSourceEntity);
                    modelProperties.Add(modelPropertyInfo);
                }
            }

            this.Name = typeDetails.Type.Name;
            this.Properties = modelProperties.ToArray();

            this.propertiesByName = this.Properties.ToDictionary(x => x.Name);
            this.propertiesByNameLower = this.Properties.ToDictionary(x => x.Name.ToLower());
            this.IdentityProperties = this.Properties.Where(x => x.IsIdentity).ToArray();
            this.IdentityAutoGeneratedProperties = this.Properties.Where(x => x.IsIdentity && x.IsIdentityAutoGenerated).ToArray();
            this.RelatedProperties = this.Properties.Where(x => x.IsRelated).ToArray();
            this.RelatedEnumerableProperties = this.Properties.Where(x => x.IsRelated && x.IsEnumerable).ToArray();
            this.RelatedNonEnumerableProperties = this.Properties.Where(x => x.IsRelated && !x.IsEnumerable).ToArray();
            this.NonAutoGeneratedNonRelationProperties = this.Properties.Where(x => !x.IsIdentityAutoGenerated && !x.IsRelated).ToArray();

            this.Creator = typeDetails.CreatorBoxed;
            if (this.Creator == null)
                throw new Exception($"{Type.Name} must have a parameterless constructor to be a model.");
        }
    }
}
