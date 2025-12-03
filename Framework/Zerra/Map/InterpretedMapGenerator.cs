// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.SourceGeneration;
using Zerra.SourceGeneration.Types;

namespace Zerra.Map
{
    public static class InterpretedMapGenerator
    {
        public static Func<TSource, TTarget?, Graph?, TTarget?> Generate<TSource, TTarget>()
            where TSource : notnull
            where TTarget : notnull
            => Map;

        private static TTarget? Map<TSource, TTarget>(TSource source, TTarget? target, Graph? graph)
            where TSource : notnull
            where TTarget : notnull
        {
            var sourceTypeDetail = TypeAnalyzer<TSource>.GetTypeDetail();
            var targetTypeDetail = TypeAnalyzer<TTarget>.GetTypeDetail();
            var converter = (MapConverter<TSource, TTarget>)MapConverterFactory.GetRoot(sourceTypeDetail, targetTypeDetail);
            var resultTarget = converter.Map(source, target, graph);
            return resultTarget;
        }

        //private static object MapOld(object source, object? target, Graph? graph, TypeDetail sourceType, TypeDetail targetType)
        //{
        //    if (source == null)
        //        throw new ArgumentNullException(nameof(source));

        //    if (sourceType.CoreType != null || targetType.CoreType != null)
        //    {
        //        if (sourceType.CoreType == targetType.CoreType)
        //            return source;
        //        return TypeAnalyzer.Convert(source, targetType.Type)!;
        //    }
        //    else if (sourceType.EnumUnderlyingType != null && targetType.EnumUnderlyingType != null)
        //    {
        //        if (sourceType.EnumUnderlyingType == targetType.EnumUnderlyingType)
        //            return source;
        //        return TypeAnalyzer.Convert(source, targetType.Type)!;
        //    }
        //    else if (sourceType.HasIEnumerable && targetType.HasIEnumerable)
        //    {
        //        var enumerableSource = (IEnumerable)source;

        //        if (targetType.Type.IsArray)
        //        {
        //            //target array
        //            int length;
        //            if (enumerableSource is ICollection collection)
        //            {
        //                length = collection.Count;
        //            }
        //            else
        //            {
        //                length = 0;
        //                var enumeratorCount = enumerableSource.GetEnumerator();
        //                while (enumeratorCount.MoveNext())
        //                    length++;
        //            }

        //            Array? array = (Array?)target;
        //            if (target == null || array == null || array.Length != length)
        //            {
        //                array = Array.CreateInstanceFromArrayType(targetType.Type, length);
        //                target = array;
        //            }

        //            var i = 0;
        //            foreach (var item in enumerableSource)
        //            {
        //                var mappedItem = Map(item, null, graph, sourceType.IEnumerableGenericInnerTypeDetail!, targetType.IEnumerableGenericInnerTypeDetail!);
        //                array.SetValue(mappedItem, i++);
        //            }
        //            return target;
        //        }
        //        else if (targetType.HasIListGeneric || targetType.HasIReadOnlyListGeneric)
        //        {
        //            //target list
        //            if (target == null)
        //            {
        //                if (targetType.Type.IsInterface)
        //                {
        //                    var listType = typeof(List<>).MakeGenericType(targetType.IEnumerableGenericInnerTypeDetail!.Type);
        //                    targetType = listType.GetTypeDetail();
        //                    target = Activator.CreateInstance(listType)!;
        //                }
        //                else
        //                {
        //                    if (!targetType.HasCreatorBoxed)
        //                        throw new InvalidOperationException($"Type {targetType.Type.FullName} does not have a parameterless constructor");
        //                    target = targetType.CreatorBoxed!();
        //                }
        //            }
        //            var methodAdd = targetType.Methods.FirstOrDefault(x => x.Name == "Add" && x.Parameters.Count == 1);
        //            if (methodAdd == null)
        //                throw new InvalidOperationException($"Type {targetType.Type.Name} does not have an Add method");
        //            foreach (var item in enumerableSource)
        //            {
        //                var mappedItem = Map(item, null, graph, sourceType.IEnumerableGenericInnerTypeDetail!, targetType.IEnumerableGenericInnerTypeDetail!);
        //                _ = methodAdd.CallerBoxed(target, [mappedItem]);
        //            }
        //            return target;
        //        }
        //        else if (targetType.HasISetGeneric || targetType.HasIReadOnlySetGeneric)
        //        {
        //            //target set
        //            if (target == null)
        //            {
        //                if (targetType.Type.IsInterface)
        //                {
        //                    var hashSetType = typeof(HashSet<>).MakeGenericType(targetType.IEnumerableGenericInnerTypeDetail!.Type);
        //                    targetType = hashSetType.GetTypeDetail();
        //                    target = Activator.CreateInstance(hashSetType)!;
        //                }
        //                else
        //                {
        //                    if (!targetType.HasCreatorBoxed)
        //                        throw new InvalidOperationException($"Type {targetType.Type.FullName} does not have a parameterless constructor");
        //                    target = targetType.CreatorBoxed!();
        //                }
        //            }
        //            var methodAdd = targetType.Methods.FirstOrDefault(x => x.Name == "Add" && x.Parameters.Count == 1);
        //            if (methodAdd == null)
        //                throw new InvalidOperationException($"Type {targetType.Type.Name} does not have an Add method");
        //            foreach (var item in enumerableSource)
        //            {
        //                var mappedItem = Map(item, null, graph, sourceType.IEnumerableGenericInnerTypeDetail!, targetType.IEnumerableGenericInnerTypeDetail!);
        //                _ = methodAdd.CallerBoxed(target, [mappedItem]);
        //            }
        //            return target;
        //        }
        //        else if (targetType.HasIDictionaryGeneric || targetType.HasIReadOnlyDictionaryGeneric)
        //        {
        //            //target dictionary
        //            if (target == null)
        //            {
        //                if (targetType.Type.IsInterface)
        //                {
        //                    var dictionaryType = typeof(Dictionary<,>).MakeGenericType(targetType.DictionaryInnerTypeDetail!.InnerTypes![0], targetType.DictionaryInnerTypeDetail!.InnerTypes![1]);
        //                    targetType = dictionaryType.GetTypeDetail();
        //                    target = Activator.CreateInstance(dictionaryType)!;
        //                }
        //                else
        //                {
        //                    if (!targetType.HasCreatorBoxed)
        //                        throw new InvalidOperationException($"Type {targetType.Type.FullName} does not have a parameterless constructor");
        //                    target = targetType.CreatorBoxed!();
        //                }
        //            }
        //            var methodAdd = targetType.Methods.FirstOrDefault(x => x.Name == "Add" && x.Parameters.Count == 1);
        //            if (methodAdd == null)
        //                throw new InvalidOperationException($"Type {targetType.Type.Name} does not have an Add method");
        //            foreach (var item in enumerableSource)
        //            {
        //                var mappedItem = Map(item, null, graph, sourceType.IEnumerableGenericInnerTypeDetail!, targetType.IEnumerableGenericInnerTypeDetail!);
        //                _ = methodAdd.CallerBoxed(target, [mappedItem]);
        //            }
        //            return target;
        //        }
        //        else if (targetType.HasICollection || targetType.HasICollectionGeneric)
        //        {
        //            //target list
        //            if (target == null)
        //            {
        //                if (targetType.Type.IsInterface)
        //                {
        //                    var listType = typeof(List<>).MakeGenericType(targetType.IEnumerableGenericInnerTypeDetail!.Type);
        //                    targetType = listType.GetTypeDetail();
        //                    target = Activator.CreateInstance(listType)!;
        //                }
        //                else
        //                {
        //                    if (!targetType.HasCreatorBoxed)
        //                        throw new InvalidOperationException($"Type {targetType.Type.FullName} does not have a parameterless constructor");
        //                    target = targetType.CreatorBoxed!();
        //                }
        //            }
        //            var methodAdd = targetType.Methods.FirstOrDefault(x => x.Name == "Add" && x.Parameters.Count == 1);
        //            if (methodAdd == null)
        //                throw new InvalidOperationException($"Type {targetType.Type.Name} does not have an Add method");
        //            foreach (var item in enumerableSource)
        //            {
        //                var mappedItem = Map(item, null, graph, sourceType.IEnumerableGenericInnerTypeDetail!, targetType.IEnumerableGenericInnerTypeDetail!);
        //                _ = methodAdd.CallerBoxed(target, [mappedItem]);
        //            }
        //            return target;
        //        }
        //        else
        //        {
        //            //target array
        //            int length;
        //            if (enumerableSource is ICollection collection)
        //            {
        //                length = collection.Count;
        //            }
        //            else
        //            {
        //                length = 0;
        //                var enumeratorCount = enumerableSource.GetEnumerator();
        //                while (enumeratorCount.MoveNext())
        //                    length++;
        //            }

        //            Array? array = (Array?)target;
        //            if (target == null || array == null || array.Length != length)
        //            {
        //                array = Array.CreateInstanceFromArrayType(targetType.IEnumerableGenericInnerType.MakeArrayType(), length);
        //                target = array;
        //            }

        //            var i = 0;
        //            foreach (var item in enumerableSource)
        //            {
        //                var mappedItem = Map(item, null, graph, sourceType.IEnumerableGenericInnerTypeDetail!, targetType.IEnumerableGenericInnerTypeDetail!);
        //                array.SetValue(mappedItem, i++);
        //            }
        //            return target;
        //        }
        //    }
        //    else
        //    {
        //        //target object
        //        var targetCreated = false;
        //        if (target == null)
        //        {
        //            if (!targetType.HasCreatorBoxed)
        //                throw new InvalidOperationException($"Type {targetType.Type.FullName} does not have a parameterless constructor");
        //            target = targetType.CreatorBoxed!();
        //        }

        //        foreach (var sourceMember in sourceType.SerializableMemberDetails)
        //        {
        //            if (graph != null && !graph.HasMember(sourceMember.Name))
        //                continue;

        //            if (!sourceMember.HasGetterBoxed)
        //                continue;

        //            var targetMember = targetType.Members.FirstOrDefault(m => String.Equals(m.Name, sourceMember.Name, StringComparison.Ordinal));
        //            if (targetMember == null || !targetMember.HasSetterBoxed)
        //                continue;

        //            var sourceValue = sourceMember.GetterBoxed!(source);
        //            if (sourceValue == null)
        //                continue;

        //            object? targetValue = null;
        //            if (!targetCreated && targetMember.HasGetterBoxed)
        //                targetValue = targetMember.GetterBoxed!(target);

        //            var childGraph = graph?.GetChildGraph(sourceMember.Name);
        //            var mappedValue = Map(sourceValue, targetValue, childGraph, sourceMember.TypeDetailBoxed, targetMember.TypeDetailBoxed);

        //            targetMember.SetterBoxed!(target, mappedValue);
        //        }
        //        return target;
        //    }
        //}
    }
}
