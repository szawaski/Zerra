// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.ComponentModel;
using Zerra.Reflection;

namespace Zerra.Repository.Linq
{
    /// <summary>The elements a relation's statement read, by the key of the parent each belongs to, held by the key's own type so a store that reads it typed doesn't box it for every row.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class LinqPieceElements
    {
        /// <summary>Adds an element to those of the parent with the key.</summary>
        /// <param name="key">The parent's key.</param>
        /// <param name="element">The element.</param>
        public abstract void Add(object key, object? element);
        /// <summary>The elements of the parent with the key.</summary>
        /// <param name="key">The parent's key.</param>
        /// <returns>The elements, or <see langword="null"/> for none.</returns>
        public abstract List<object?>? Find(object key);

        /// <summary>A container for keys of the type, typed for the key types stores read typed.</summary>
        /// <param name="keyType">The type of the related model's member holding the parent's key.</param>
        /// <returns>The container.</returns>
        public static LinqPieceElements New(Type keyType)
        {
            var underlying = Nullable.GetUnderlyingType(keyType) ?? keyType;
            if (underlying == typeof(Guid))
                return new LinqPieceElements<Guid>();
            if (underlying == typeof(int))
                return new LinqPieceElements<int>();
            if (underlying == typeof(long))
                return new LinqPieceElements<long>();
            if (underlying == typeof(string))
                return new LinqPieceElements<string>();
            return new LinqPieceElements<object>();
        }
    }

    /// <summary>The elements a relation's statement read, by the parent's key of <typeparamref name="TKey"/>.</summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LinqPieceElements<TKey> : LinqPieceElements where TKey : notnull
    {
        private readonly Dictionary<TKey, List<object?>> elements = new();

        /// <summary>Adds an element to those of the parent with the key.</summary>
        /// <param name="key">The parent's key.</param>
        /// <param name="element">The element.</param>
        public void Add(TKey key, object? element)
        {
            if (!elements.TryGetValue(key, out var list))
            {
                list = new List<object?>();
                elements.Add(key, list);
            }
            list.Add(element);
        }
        /// <summary>The elements of the parent with the key.</summary>
        /// <param name="key">The parent's key.</param>
        /// <returns>The elements, or <see langword="null"/> for none.</returns>
        public List<object?>? Find(TKey key) => elements.TryGetValue(key, out var list) ? list : null;

        //a column of another type than the member, such as a smallint for an int, holds the same key
        /// <inheritdoc/>
        public override void Add(object key, object? element) => Add(key is TKey typed ? typed : (TKey)TypeAnalyzer.Convert(key, typeof(TKey))!, element);
        /// <inheritdoc/>
        public override List<object?>? Find(object key) => elements.TryGetValue(key is TKey typed ? typed : (TKey)TypeAnalyzer.Convert(key, typeof(TKey))!, out var list) ? list : null;
    }
}
