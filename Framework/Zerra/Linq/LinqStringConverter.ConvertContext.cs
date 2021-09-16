// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Zerra.Linq
{
    public static partial class LinqStringConverter
    {
        private class ConvertContext
        {
            public StringBuilder Builder { get; private set; }
            public bool UseIt { get; private set; }
            public Stack<string> ItStack { get; private set; }
            public Stack<MemberExpression> MemberAccessStack { get; private set; }

            public ConvertContext(StringBuilder sb, bool useIt)
            {
                this.Builder = sb;
                this.UseIt = useIt;
                this.ItStack = new Stack<string>();
                this.MemberAccessStack = new Stack<MemberExpression>();
            }
        }
    }
}
