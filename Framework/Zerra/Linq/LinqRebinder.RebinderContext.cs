// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Linq
{
    public sealed partial class LinqRebinder
    {
        private sealed class RebinderContext
        {
            public Expression? Current { get; init; }
            public Expression? Replacement { get; init; }
            public Dictionary<Expression, Expression>? Replacements { get; init; }
            public Dictionary<string, Expression>? StringReplacements { get; init; }
            public RebinderContext(Expression? current, Expression? replacement, Dictionary<Expression, Expression>? replacements, Dictionary<string, Expression>? stringReplacements)
            {
                this.Current = current;
                this.Replacement = replacement; 
                this.Replacements = replacements;
                this.StringReplacements = stringReplacements;
            }
        }
    }
}