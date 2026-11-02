// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Marks a property or field as a foreign-key relation to another data store entity.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class RelationAttribute : Attribute
    {
        /// <summary>Gets the names of the members holding the foreign key, one for each identity member of the model it refers to and in their order: on this model for a single relation, on the related model for a collection.</summary>
        public string[] ForeignIdentities { get; }

        /// <summary>Initializes a new instance of <see cref="RelationAttribute"/> with the members holding the foreign key.</summary>
        /// <param name="foreignIdentities">The members holding the foreign key, one for each identity member of the model it refers to and in their order. Each must contain only letters, digits, underscores, or backticks.</param>
        /// <exception cref="ArgumentException">Thrown when there is no member or a name contains invalid characters.</exception>
        public RelationAttribute(params string[] foreignIdentities)
        {
            if (foreignIdentities.Length == 0)
                throw new ArgumentException($"{nameof(RelationAttribute)} names the members holding the foreign key");
            foreach (var foreignIdentity in foreignIdentities)
            {
                if (String.IsNullOrEmpty(foreignIdentity) || !foreignIdentity.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                    throw new ArgumentException($"{nameof(RelationAttribute)}.{nameof(ForeignIdentities)}={foreignIdentity}");
            }
            this.ForeignIdentities = foreignIdentities;
        }
    }
}
