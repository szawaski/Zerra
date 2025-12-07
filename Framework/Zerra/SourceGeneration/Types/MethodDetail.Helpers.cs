// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    public partial class MethodDetail
    {
        private TypeDetail? returnTypeDetail = null;
        /// <summary>
        /// Gets the <see cref="TypeDetail"/> for the method's return type, lazily initializing it if needed.
        /// </summary>
        public TypeDetail ReturnTypeDetail
        {
            get
            {
                returnTypeDetail ??= TypeAnalyzer.GetTypeDetail(ReturnType);
                return returnTypeDetail;
            }
        }
    }
}
