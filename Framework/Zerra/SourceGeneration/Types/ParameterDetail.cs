// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    public sealed class ParameterDetail
    {
        public readonly Type Type;
        public readonly string Name;
        public ParameterDetail(Type type, string name)
        {
            this.Type = type;
            this.Name = name;
        }

        public TypeDetail TypeDetail
        {
            get
            {
                field ??= TypeAnalyzer.GetTypeDetail(this.Type);
                return field;
            }
        }
    }
}
