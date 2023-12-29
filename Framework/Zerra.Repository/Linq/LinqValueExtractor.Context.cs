// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    internal static partial class LinqValueExtractor
    {
        private sealed class Context
        {
            public Type PropertyModelType { get; private set; }
            public string[] PropertyNames { get; private set; }
            public Dictionary<string, List<object?>> Values { get; private set; }

            public Stack<ModelDetail> ModelStack { get; private set; }
            public Stack<MemberExpression> MemberAccessStack { get; private set; }

            public int InvertStack { get; set; }
            public bool Inverted { get { return InvertStack % 2 != 0; } }

            public Context(Type propertyModelType, string[] propertyNames)
            {
                this.PropertyModelType = propertyModelType;
                this.PropertyNames = propertyNames;
                this.Values = new();
                foreach (var propertyName in PropertyNames)
                {
                    this.Values.Add(propertyName, new());
                }

                this.ModelStack = new Stack<ModelDetail>();
                this.MemberAccessStack = new Stack<MemberExpression>();

                this.InvertStack = 0;
            }
        }
    }
}