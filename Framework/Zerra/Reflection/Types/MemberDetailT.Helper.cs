// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Reflection
{
    public partial class MemberDetail<T>
    {
        /// <summary>
        /// Gets the cached strongly-typed type detail for this member's type <typeparamref name="T"/>.
        /// Lazily initializes and casts the base type detail to the generic form without boxing.
        /// </summary>
        public new TypeDetail<T> TypeDetail
        {
            get
            {
                if (field == null)
                        field ??= (TypeDetail<T>)base.TypeDetail;
                return field;
            }
        }
    }
}
