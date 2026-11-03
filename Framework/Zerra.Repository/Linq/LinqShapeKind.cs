// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Linq
{
    /// <summary>How an element of a LINQ query is built.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum LinqShapeKind : byte
    {
        /// <summary>One column's value.</summary>
        Column,
        /// <summary>The model, from a column per member.</summary>
        Model,
        /// <summary>An object made with a constructor and members, each from a shape of its own.</summary>
        Object,
        /// <summary>A relation of the model, read by a statement of its own after the rows.</summary>
        Relation
    }
}
