﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Zerra.Linq
{
    public static partial class LinqStringConverter
    {
        private sealed class ConvertContext
        {
            public StringBuilder Builder { get; }
            public Stack<MemberExpression> MemberAccessStack { get; }

            public ConvertContext(StringBuilder sb)
            {
                this.Builder = sb;
                this.MemberAccessStack = new Stack<MemberExpression>();
            }
        }
    }
}
