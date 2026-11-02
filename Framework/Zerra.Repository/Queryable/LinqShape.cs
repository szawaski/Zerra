// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// How an element of a LINQ query is built from a row: a column's value, the model from its columns, an object from the shapes of its members, or a relation read after the rows.
    /// </summary>
    public sealed class LinqShape
    {
        /// <summary>How the element is built.</summary>
        public LinqShapeKind Kind { get; }
        /// <summary>The type of the element.</summary>
        public Type Type { get; }
        /// <summary>The column of a <see cref="LinqShapeKind.Column"/>, or the member expression of a <see cref="LinqShapeKind.Relation"/> for when the query runs in memory, at its index among the relations.</summary>
        public LinqColumn? Column { get; }
        /// <summary>The model of a <see cref="LinqShapeKind.Model"/>.</summary>
        public ModelDetail? Model { get; }
        /// <summary>The members of the model read, each from the column at the same index of <see cref="ModelColumns"/>.</summary>
        public ModelMemberDetail[]? ModelMembers { get; }
        /// <summary>The columns of the model's members.</summary>
        public LinqColumn[]? ModelColumns { get; }
        /// <summary>The relations of the model read with it, each set on the member of its <see cref="Piece"/>.</summary>
        public LinqShape[]? ModelRelations { get; private set; }
        /// <summary>For a model read by a join: the column of its identity, null when there is no related row.</summary>
        public LinqColumn? JoinedKey { get; private set; }
        /// <summary>The member of the parent a relation of a model is set on.</summary>
        public ModelMemberDetail? RelationMember { get; private set; }
        /// <summary>The constructor of a <see cref="LinqShapeKind.Object"/>, or <see langword="null"/> for the parameterless one.</summary>
        public ConstructorInfo? Constructor { get; }
        /// <summary>The shapes of the constructor's arguments.</summary>
        public LinqShape[]? Arguments { get; }
        /// <summary>The names of the constructor's arguments when they are the object's members, as an anonymous type's are.</summary>
        public string[]? ArgumentNames { get; }
        /// <summary>The members set after construction.</summary>
        public MemberDetail[]? Members { get; }
        /// <summary>The shapes of the members set.</summary>
        public LinqShape[]? Values { get; }
        /// <summary>The relation of a <see cref="LinqShapeKind.Relation"/>.</summary>
        public LinqPiece? Piece { get; }
        /// <summary>The index of the relation among the plan's pieces.</summary>
        public int PieceIndex { get; }

        //the element of a collection that is built from an array, needing nothing generated for its type
        private Type? arrayElement;
        //the ToList of the query, which the compiler keeps for a native build as it is named in the expression
        private MethodInfo? toList;

        private LinqShape(LinqShapeKind kind, Type type, LinqColumn? column, ModelDetail? model, ModelMemberDetail[]? modelMembers, LinqColumn[]? modelColumns, ConstructorInfo? constructor, LinqShape[]? arguments, string[]? argumentNames, MemberDetail[]? members, LinqShape[]? values, LinqPiece? piece, int pieceIndex)
        {
            this.Kind = kind;
            this.Type = type;
            this.Column = column;
            this.Model = model;
            this.ModelMembers = modelMembers;
            this.ModelColumns = modelColumns;
            this.Constructor = constructor;
            this.Arguments = arguments;
            this.ArgumentNames = argumentNames;
            this.Members = members;
            this.Values = values;
            this.Piece = piece;
            this.PieceIndex = pieceIndex;
        }
        internal static LinqShape OfColumn(Type type, LinqColumn column) => new(LinqShapeKind.Column, type, column, null, null, null, null, null, null, null, null, null, 0);
        internal static LinqShape OfModel(ModelDetail model, ModelMemberDetail[] members, LinqColumn[] columns, LinqShape[]? relations) => new(LinqShapeKind.Model, model.Type, null, model, members, columns, null, null, null, null, null, null, 0) { ModelRelations = relations };
        internal static LinqShape OfObject(Type type, ConstructorInfo? constructor, LinqShape[]? arguments, string[]? argumentNames, MemberDetail[]? members, LinqShape[]? values) => new(LinqShapeKind.Object, type, null, null, null, null, constructor, arguments, argumentNames, members, values, null, 0);
        internal static LinqShape OfRelation(Type type, LinqColumn column, LinqPiece piece, int pieceIndex) => new(LinqShapeKind.Relation, type, column, null, null, null, null, null, null, null, null, piece, pieceIndex) { arrayElement = ReadOnlyElement(type), RelationMember = piece.Relation };
        internal static LinqShape OfJoined(ModelDetail model, ModelMemberDetail[] members, LinqColumn[] columns, LinqColumn key, LinqColumn relationColumn, ModelMemberDetail relation) => new(LinqShapeKind.Model, model.Type, relationColumn, model, members, columns, null, null, null, null, null, null, 0) { JoinedKey = key, RelationMember = relation };

        internal void ReadWith(MethodInfo toList)
        {
            this.toList = toList;
            this.arrayElement = toList.GetGenericArguments()[0];
        }
        private static Type? ReadOnlyElement(Type type)
        {
            if (!type.IsInterface || !type.IsGenericType)
                return null;
            var definition = type.GetGenericTypeDefinition();
            return definition == typeof(IEnumerable<>) || definition == typeof(IReadOnlyCollection<>) || definition == typeof(IReadOnlyList<>) ? type.GetGenericArguments()[0] : null;
        }

        /// <summary>
        /// Builds the element from a row.
        /// </summary>
        /// <param name="row">The row's values, a column's at its index, and in memory a relation's value after the columns.</param>
        /// <param name="pieces">The elements each relation gave, by the parent's key, or <see langword="null"/> when the row holds them.</param>
        /// <returns>The element.</returns>
        public object? Build(object?[] row, Dictionary<object, List<object?>>[]? pieces)
        {
            switch (Kind)
            {
                case LinqShapeKind.Column:
                    {
                        var value = row[Column!.Index];
                        return value is null ? null : Convert(value, Type);
                    }
                case LinqShapeKind.Model:
                    {
                        if (JoinedKey is not null)
                            return CopyRelation(row[Column!.Index]);
                        var model = Model!.Creator();
                        for (var i = 0; i < ModelMembers!.Length; i++)
                        {
                            var value = row[ModelColumns![i].Index];
                            if (value is not null)
                                ModelMembers[i].SetterBoxed(model, Convert(value, ModelMembers[i].Type));
                        }
                        if (ModelRelations is not null)
                        {
                            foreach (var relation in ModelRelations)
                            {
                                //in memory the row holds the related models, which are read as the graph says
                                var value = pieces is null ? relation.CopyRelation(row[relation.Column!.Index]) : relation.Build(row, pieces);
                                if (value is not null)
                                    relation.RelationMember!.SetterBoxed(model, value);
                            }
                        }
                        return model;
                    }
                case LinqShapeKind.Object:
                    {
                        object instance;
                        if (Arguments is null || Arguments.Length == 0)
                        {
                            instance = Constructor is null ? TypeAnalyzer.GetTypeDetail(Type).CreatorBoxed!() : Constructor.Invoke(Array.Empty<object>());
                        }
                        else
                        {
                            var arguments = new object?[Arguments.Length];
                            for (var i = 0; i < arguments.Length; i++)
                                arguments[i] = Arguments[i].Build(row, pieces);
                            instance = Constructor!.Invoke(arguments);
                        }
                        if (Members is not null)
                        {
                            for (var i = 0; i < Members.Length; i++)
                            {
                                var value = Values![i].Build(row, pieces);
                                if (value is not null)
                                    Members[i].SetterBoxed!(instance, value);
                            }
                        }
                        return instance;
                    }
                default:
                    return pieces is null ? row[Column!.Index] : BuildRelation(row[Piece!.ParentColumn], pieces);
            }
        }

        /// <summary>
        /// Builds the value of a <see cref="LinqShapeKind.Relation"/> from the elements its relation gave: the one with the key, or a collection of those with it.
        /// </summary>
        /// <param name="key">The parent's key, or <see langword="null"/> for no related elements.</param>
        /// <param name="pieces">The elements each relation gave, by the parent's key.</param>
        /// <returns>The related element, or the collection of the type the member takes.</returns>
        public object? BuildRelation(object? key, Dictionary<object, List<object?>>[] pieces)
        {
            List<object?>? elements = null;
            if (key is not null)
                _ = pieces[PieceIndex].TryGetValue(key, out elements);
            if (Piece!.Single)
                return elements is null || elements.Count == 0 ? null : elements[0];
            return CreateCollection(elements ?? (IReadOnlyList<object?>)Array.Empty<object?>());
        }

        private object? CopyRelation(object? value)
        {
            if (JoinedKey is not null)
                return value is null ? null : CopyModel(value);
            var shape = Piece!.Plan.Shape!;
            if (Piece.Single)
                return value is null ? null : shape.CopyModel(value);
            var elements = new List<object?>();
            if (value is not null)
            {
                foreach (var element in (IEnumerable)value)
                    elements.Add(shape.CopyModel(element!));
            }
            return CreateCollection(elements);
        }

        private object CopyModel(object source)
        {
            var model = Model!.Creator();
            foreach (var member in ModelMembers!)
            {
                var value = member.GetterBoxed(source);
                if (value is not null)
                    member.SetterBoxed(model, value);
            }
            if (ModelRelations is not null)
            {
                foreach (var relation in ModelRelations)
                {
                    var value = relation.CopyRelation(relation.RelationMember!.GetterBoxed(source));
                    if (value is not null)
                        relation.RelationMember.SetterBoxed(model, value);
                }
            }
            return model;
        }

        private static object Convert(object value, Type type)
        {
            var valueType = value.GetType();
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (valueType == underlying)
                return value;
            //an enum's smallest and largest are calculated as its number
            if (underlying.IsEnum)
                return Enum.ToObject(underlying, System.Convert.ChangeType(value, Enum.GetUnderlyingType(underlying), System.Globalization.CultureInfo.InvariantCulture));
            return TypeAnalyzer.Convert(value, type)!;
        }

        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Elements are reference types or values the query already made a list of, so the list code is shared or present.")]
        [UnconditionalSuppressMessage("Trimming", "IL2055:Call to 'System.Type.MakeGenericType' can not be statically analyzed.", Justification = "An array or List<T> of an element type the query already reads, so a native build has it")]
        private object CreateCollection(IReadOnlyList<object?> elements)
        {
            if (arrayElement is not null)
            {
                var array = Array.CreateInstance(arrayElement, elements.Count);
                for (var i = 0; i < elements.Count; i++)
                    array.SetValue(elements[i], i);
                return toList is null ? array : toList.Invoke(null, [array])!;
            }
            if (Type.IsArray)
            {
                var array = Array.CreateInstanceFromArrayType(Type, elements.Count);
                for (var i = 0; i < elements.Count; i++)
                    array.SetValue(elements[i], i);
                return array;
            }
            var typeDetail = TypeAnalyzer.GetTypeDetail(Type);
            var listDetail = Type.IsInterface ? TypeAnalyzer.GetTypeDetail(typeof(List<>).MakeGenericType(typeDetail.InnerType!)) : typeDetail;
            var list = (IList)listDetail.CreatorBoxed!();
            for (var i = 0; i < elements.Count; i++)
                _ = list.Add(elements[i]);
            return list;
        }
    }
}
