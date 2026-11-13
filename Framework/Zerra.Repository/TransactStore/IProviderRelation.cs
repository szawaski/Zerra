// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// Defines how a provider takes part when another model's query reads its rows as a relation.
    /// </summary>
    public interface IProviderRelation
    {
        /// <summary>Treats the models of this provider read as a relation in another model's graph query: a layer runs its <c>OnGet</c> or decrypts them, a table gives them as they are.</summary>
        /// <param name="models">The models read.</param>
        /// <param name="graph">The graph of the relation.</param>
        /// <returns>The models to give, the same collection when none changed.</returns>
        IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph);

        /// <summary>Gets whether <see cref="OnGetIncludingBase"/> can filter or change models, so the models read as a relation are gathered for it.</summary>
        bool TreatsReadModels { get; }

        /// <summary>Returns the combined where expression from all providers in the chain.</summary>
        /// <param name="graph">The graph for the current query.</param>
        /// <returns>A combined lambda where expression, or <see langword="null"/> if none applies.</returns>
        LambdaExpression? GetWhereExpressionIncludingBase(Graph? graph);

        /// <summary>The store the provider writes to, compared by reference with a related model's to tell whether a condition through the relation can run in one statement; <see langword="null"/> when unknown.</summary>
        object? Engine { get; }

        /// <summary>Gets whether a query of another model can read this model in its own statement when they share an engine. A layer or an event store reads through itself, so its rows load through this provider instead.</summary>
        bool ReadsInStore { get; }

        /// <summary>Gets whether another model's graph query can read this model's rows in its statement when they share an engine. A layer adds its condition to the rows and treats them with <see cref="OnGetIncludingBase"/> after.</summary>
        bool ReadsGraphInStore { get; }

        /// <summary>Gets whether a relation of the model is on another engine, so a condition through it can't be written in a statement of this engine.</summary>
        bool RelatesAcrossEngines => false;
    }
}
