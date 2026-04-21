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
        /// <summary>Gets the name of the foreign identity (key) field on the related entity.</summary>
        public string ForeignIdentity { get; }

        /// <summary>Initializes a new instance of <see cref="RelationAttribute"/> with the specified foreign identity name.</summary>
        /// <param name="foreignIdentity">The name of the foreign identity field on the related entity. Must contain only letters, digits, underscores, or backticks.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="foreignIdentity"/> contains invalid characters.</exception>
        public RelationAttribute(string foreignIdentity)
        {
            if (!foreignIdentity.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                throw new ArgumentException($"{nameof(RelationAttribute)}.{nameof(ForeignIdentity)}={foreignIdentity}");
            this.ForeignIdentity = foreignIdentity;
        }
    }
}
