// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    public partial class MemberDetail
    {
        /// <summary>
        /// Gets the cached type detail for this member's type.
        /// Lazily initializes and caches the type detail information for serialization and reflection.
        /// </summary>
        public TypeDetail TypeDetailBoxed
        {
            get
            {
                field ??= TypeAnalyzer.GetTypeDetail(this.Type);
                return field;
            }
        }
    }
}
