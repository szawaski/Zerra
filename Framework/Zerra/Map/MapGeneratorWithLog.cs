﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.Linq;
using Zerra.Reflection;

namespace Zerra.Map
{
    internal sealed class MapGeneratorWithLog<TSource, TTarget> : IMapSetup<TSource, TTarget>
    {
        private const int maxBuildDepthBeforeCall = 3;

        private readonly object locker = new();

        private readonly TypeDetail<TSource> sourceType;
        private readonly TypeDetail<TTarget> targetType;
        private readonly Dictionary<string, Tuple<Expression<Func<TSource, object?>>, Expression<Func<TTarget, object?>>>> memberMaps;
        private readonly ConcurrentFactoryDictionary<Graph, Func<TSource, TTarget, IMapLogger?, Dictionary<MapRecursionKey, object>, TTarget>> compiledGraphMaps;
        private Func<TSource, TTarget, IMapLogger?, Dictionary<MapRecursionKey, object>, TTarget>? compiledMap;

        private static readonly Type genericMapType = typeof(MapGeneratorWithLog<,>);
        private static readonly Type genericListType = typeof(List<>);
        private static readonly Type genericHashSetType = typeof(HashSet<>);
        private static readonly Type genericDictionaryType = typeof(Dictionary<,>);
        private static readonly Type genericEnumerableType = typeof(IEnumerable<>);
        private static readonly Type genericEnumeratorType = typeof(IEnumerator<>);
        private static readonly TypeDetail enumeratorTypeDetail = TypeAnalyzer.GetTypeDetail(typeof(IEnumerator));
        private static readonly Type collectionType = typeof(ICollection);
        private static readonly Type collectionGenericType = typeof(ICollection<>);
        private static readonly Type readOnlyCollectionGenericType = typeof(IReadOnlyCollection<>);
        private static readonly Type intType = typeof(int);
        private static readonly Type objectType = typeof(object);
        private static readonly Type graphType = typeof(Graph);
        private static readonly Type tType = typeof(TSource);
        private static readonly Type uType = typeof(TTarget);
        private static readonly Type mapDefinitionsType = typeof(IMapDefinition<TSource, TTarget>);
        private static readonly Type recursionDictionaryType = typeof(Dictionary<MapRecursionKey, object>);
        private static readonly Type mapLoggerType = typeof(IMapLogger);
        private static readonly Type exceptionType = typeof(Exception);
        private static readonly ConstructorInfo newException = TypeAnalyzer.GetTypeDetail(typeof(Exception)).GetConstructorBoxed([typeof(string), typeof(Exception)]).ConstructorInfo;
        private static readonly MethodInfo mapLoggerChangeMethod = TypeAnalyzer.GetTypeDetail(mapLoggerType).GetMethodBoxed(nameof(IMapLogger.LogPropertyChange)).MethodInfo;
        private static readonly MethodInfo mapLoggerNoChangeMethod = TypeAnalyzer.GetTypeDetail(mapLoggerType).GetMethodBoxed(nameof(IMapLogger.LogPropertyNoChange)).MethodInfo;
        private static readonly MethodInfo mapLoggerNewObjectMethod = TypeAnalyzer.GetTypeDetail(mapLoggerType).GetMethodBoxed(nameof(IMapLogger.LogNewObject)).MethodInfo;
        private static readonly MethodInfo objectToStringMethod = TypeAnalyzer.GetTypeDetail(typeof(object)).GetMethodBoxed(nameof(ToString)).MethodInfo;
        private static readonly MethodInfo dictionaryAddMethod = TypeAnalyzer.GetTypeDetail(recursionDictionaryType).GetMethodBoxed("Add").MethodInfo;
        private static readonly MethodInfo dictionaryRemoveMethod = TypeAnalyzer.GetTypeDetail(recursionDictionaryType).GetMethodBoxed("Remove", [typeof(MapRecursionKey)]).MethodInfo;
        private static readonly MethodInfo dictionaryTryGetMethod = TypeAnalyzer.GetTypeDetail(recursionDictionaryType).GetMethodBoxed("TryGetValue").MethodInfo;
        private static readonly ConstructorInfo newRecursionKey = TypeAnalyzer.GetTypeDetail(typeof(MapRecursionKey)).GetConstructorBoxed([typeof(object), typeof(Type)]).ConstructorInfo;

        private static readonly ConcurrentFactoryDictionary<TypeKey, object> mapsStore = new();
        public static MapGeneratorWithLog<TSource, TTarget> GetMap()
        {
            var key = new TypeKey(tType, uType);
            var map = (MapGeneratorWithLog<TSource, TTarget>)mapsStore.GetOrAdd(key, static () => new MapGeneratorWithLog<TSource, TTarget>());
            return map;
        }

        private MapGeneratorWithLog()
        {
            sourceType = TypeAnalyzer<TSource>.GetTypeDetail();
            targetType = TypeAnalyzer<TTarget>.GetTypeDetail();
            memberMaps = new();
            compiledGraphMaps = new();
            GenerateDefaultMemberMaps();
            RunInitializers();
        }

        void IMapSetup<TSource, TTarget>.Define(Expression<Func<TTarget, object?>> property, Expression<Func<TSource, object?>> value)
        {
            lock (locker)
            {
                if (compiledMap is not null || compiledGraphMaps.Count > 0)
                    throw new MapException("Map already complied. Define must be called before Maps are used. Create a class that inherits IMapDefiner<T, U>.");

                if (sourceType.CoreType.HasValue || targetType.CoreType.HasValue)
                    throw new MapException("Cannot add specific mappings to core types");
                if (sourceType.Type.IsEnum || sourceType.IsNullable && sourceType.InnerTypeDetail.Type.IsEnum || sourceType.Type.IsEnum || sourceType.IsNullable && targetType.InnerTypeDetail.Type.IsEnum)
                    throw new MapException("Cannot add specific mappings to enum types");
                if (sourceType.HasIEnumerable || targetType.HasIEnumerable)
                    throw new MapException("Cannot add specific mappings to enumerable types");

                if (!property.TryReadMemberName(out var name))
                    throw new MapException("Cannot map to an expression that is not a member accessor");

                if (memberMaps.ContainsKey(name))
                    _ = memberMaps.Remove(name);
                memberMaps.Add(name, new Tuple<Expression<Func<TSource, object?>>, Expression<Func<TTarget, object?>>>(value, property));
            }
        }
        void IMapSetup<TSource, TTarget>.DefineTwoWay(Expression<Func<TTarget, object?>> propertyU, Expression<Func<TSource, object?>> propertyT)
        {
            ((IMapSetup<TSource, TTarget>)this).Define(propertyU, propertyT);

            var otherWay = (IMapSetup<TTarget, TSource>)MapGeneratorWithLog<TTarget, TSource>.GetMap();
            otherWay.Define(propertyT, propertyU);
        }
        void IMapSetup<TSource, TTarget>.Undefine(Expression<Func<TTarget, object?>> property)
        {
            lock (locker)
            {
                if (compiledMap is not null || compiledGraphMaps.Count > 0)
                    throw new MapException("Map already complied. Define must be called before Maps are used. Create a class that inherits IMapDefiner<T, U>.");

                if (sourceType.CoreType.HasValue || targetType.CoreType.HasValue)
                    throw new MapException("Cannot add specific mappings to core types");
                if (sourceType.Type.IsEnum || sourceType.IsNullable && sourceType.InnerTypeDetail.Type.IsEnum || sourceType.Type.IsEnum || sourceType.IsNullable && targetType.InnerTypeDetail.Type.IsEnum)
                    throw new MapException("Cannot add specific mappings to enum types");
                if (sourceType.HasIEnumerable || targetType.HasIEnumerable)
                    throw new MapException("Cannot add specific mappings to enumerable types");

                if (!property.TryReadMemberName(out var name))
                    throw new MapException("Cannot map to an expression that is not a member accessor");

                if (memberMaps.ContainsKey(name))
                    _ = memberMaps.Remove(name);
            }
        }
        void IMapSetup<TSource, TTarget>.UndefineTwoWay(Expression<Func<TTarget, object?>> propertyU, Expression<Func<TSource, object?>> propertyT)
        {
            ((IMapSetup<TSource, TTarget>)this).Undefine(propertyU);

            var otherWay = (IMapSetup<TTarget, TSource>)MapGeneratorWithLog<TTarget, TSource>.GetMap();
            otherWay.Undefine(propertyT);
        }
        void IMapSetup<TSource, TTarget>.UndefineAll()
        {
            lock (locker)
            {
                memberMaps.Clear();
            }
        }

        public TTarget Copy(TSource source, IMapLogger? logger = null, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            TTarget target;

            if (sourceType.CoreType.HasValue || targetType.CoreType.HasValue || sourceType.Type.IsEnum || targetType.Type.IsEnum)
            {
                if (targetType.Type == sourceType.Type)
                    return (TTarget)(object)source;
                else
                    return (TTarget)TypeAnalyzer.Convert(source, targetType.Type)!;
            }

            if (sourceType.HasIEnumerable && targetType.HasIEnumerable)
            {
                if (targetType.Type.IsArray)
                {
                    int length;
                    if (sourceType.HasICollection)
                    {
                        length = ((ICollection)source).Count;
                    }
                    else
                    {
                        length = 0;
                        foreach (var item in (IEnumerable)source)
                            length++;
                    }
                    target = (TTarget)(object)Array.CreateInstance(targetType.InnerType, length);
                }
                else if (targetType.IsIListGeneric)
                {
                    var listType = TypeAnalyzer.GetGenericTypeDetail(genericListType, targetType.InnerType);
                    target = (TTarget)listType.CreatorBoxed();
                }
                else if (targetType.HasIListGeneric)
                {
                    target = targetType.Creator();
                }
                else if (targetType.IsISetGeneric)
                {
                    var setType = TypeAnalyzer.GetGenericTypeDetail(genericHashSetType, targetType.InnerType);
                    target = (TTarget)setType.CreatorBoxed();
                }
                else if (targetType.HasISetGeneric)
                {
                    target = targetType.Creator();
                }
                else if (sourceType.HasICollection)
                {
                    var length = ((ICollection)source).Count;
                    target = (TTarget)(object)Array.CreateInstance(targetType.InnerType, length);
                }
                else if (sourceType.HasICollectionGeneric)
                {
                    var length = (int)sourceType.GetMember("Count").GetterBoxed(source)!;
                    target = (TTarget)(object)Array.CreateInstance(targetType.InnerType, length);
                }
                else
                {
                    var listType = TypeAnalyzer.GetGenericTypeDetail(genericListType, targetType.InnerType);
                    target = (TTarget)listType.CreatorBoxed();
                }
            }
            else
            {
                target = targetType.Creator();
            }

            if (graph is null)
            {
                if (compiledMap is null)
                {
                    lock (locker)
                    {
                        compiledMap ??= CompileMap(null);
                    }
                }
                return compiledMap(source, target, logger, new Dictionary<MapRecursionKey, object>());
            }
            else
            {
                var map = compiledGraphMaps.GetOrAdd(graph, CompileMap);
                return map(source, target, logger, new Dictionary<MapRecursionKey, object>());
            }
        }

        public TTarget CopyTo(TSource source, TTarget target, IMapLogger? logger = null, Graph? graph = null)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            return CopyInternal(source, target, logger, graph, new Dictionary<MapRecursionKey, object>());
        }

        internal TTarget CopyInternal(TSource source, TTarget target, IMapLogger? logger, Graph? graph, Dictionary<MapRecursionKey, object> recursionDictionary)
        {
            if (graph is null)
            {
                if (compiledMap is null)
                {
                    lock (locker)
                    {
                        compiledMap ??= CompileMap(null);
                    }
                }
                return compiledMap(source, target, logger, recursionDictionary);
            }
            else
            {
                var map = compiledGraphMaps.GetOrAdd(graph, CompileMap);
                return map(source, target, logger, recursionDictionary);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateDefaultMemberMaps()
        {
            if (sourceType.CoreType.HasValue || targetType.CoreType.HasValue)
                return;

            if (sourceType.Type.IsEnum || sourceType.IsNullable && sourceType.InnerTypeDetail.Type.IsEnum || sourceType.Type.IsEnum || sourceType.IsNullable && targetType.InnerTypeDetail.Type.IsEnum)
                return;

            if (sourceType.HasIEnumerable || targetType.HasIEnumerable)
                return;

            var sourceParameter = Expression.Parameter(sourceType.Type, "source");
            var targetParameter = Expression.Parameter(targetType.Type, "target");
            foreach (var sourceMember in sourceType.MemberDetails)
            {
                if (sourceMember.IsExplicitFromInterface)
                    continue;
                if (!sourceMember.HasGetterBoxed)
                    continue;

                if (targetType.TryGetMember(sourceMember.Name, out var targetMember))
                {
                    if (!targetMember.HasSetterBoxed)
                        continue;

                    Expression sourceMemberAccess;
                    if (sourceMember.MemberInfo.MemberType == MemberTypes.Property)
                        sourceMemberAccess = Expression.Property(sourceParameter, (PropertyInfo)sourceMember.MemberInfo);
                    else
                        sourceMemberAccess = Expression.Field(sourceParameter, (FieldInfo)sourceMember.MemberInfo);
                    var convertSourceMemberAccess = Expression.Convert(sourceMemberAccess, objectType);
                    var sourceLambda = Expression.Lambda<Func<TSource, object?>>(convertSourceMemberAccess, sourceParameter);

                    Expression targetMemberAccess;
                    if (targetMember.MemberInfo.MemberType == MemberTypes.Property)
                        targetMemberAccess = Expression.Property(targetParameter, (PropertyInfo)targetMember.MemberInfo);
                    else
                        targetMemberAccess = Expression.Field(targetParameter, (FieldInfo)targetMember.MemberInfo);
                    var convertTargetMemberAccess = Expression.Convert(targetMemberAccess, objectType);
                    var targetLambda = Expression.Lambda<Func<TTarget, object?>>(convertTargetMemberAccess, targetParameter);

                    var name = sourceLambda.ReadMemberName();
                    memberMaps.Add(name, new Tuple<Expression<Func<TSource, object?>>, Expression<Func<TTarget, object?>>>(sourceLambda, targetLambda));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RunInitializers()
        {
            var instanceTypes = Discovery.GetClassesByInterface(mapDefinitionsType);
            foreach (var instanceType in instanceTypes)
            {
                var instance = (IMapDefinition<TSource, TTarget>)Instantiator.Create(instanceType);
                instance.Define(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<TSource, TTarget, IMapLogger?, Dictionary<MapRecursionKey, object>, TTarget> CompileMap(Graph? graph)
        {
            var depth = 0;
            var sourceParameter = Expression.Parameter(sourceType.Type, "source");
            var targetParameter = Expression.Parameter(targetType.Type, "target");
            var loggerParameter = Expression.Parameter(mapLoggerType, "logger");
            var recursionDictionaryParameter = Expression.Parameter(recursionDictionaryType, "recursionDictionary");
            var block = GenerateMap(graph, sourceParameter, targetParameter, loggerParameter, recursionDictionaryParameter, ref depth);
            var lambda = Expression.Lambda<Func<TSource, TTarget, IMapLogger?, Dictionary<MapRecursionKey, object>, TTarget>>(block, sourceParameter, targetParameter, loggerParameter, recursionDictionaryParameter);
            return lambda.Compile();
        }

        private Expression GenerateMap(Graph? graph, Expression source, Expression target, Expression logger, Expression recursionDictionary, ref int depth)
        {
            if (sourceType.CoreType.HasValue || targetType.CoreType.HasValue)
            {
                var sourceValue = Expression.Variable(targetType.Type, "sourceValue");
                Expression readSourceValue;
                if (sourceType.CoreType.HasValue && targetType.CoreType.HasValue && sourceType.CoreType.Value != targetType.CoreType.Value)
                {
                    readSourceValue = Expression.Assign(sourceValue, Expression.Convert(source, targetType.Type));
                }
                else
                {
                    readSourceValue = Expression.Assign(sourceValue, source);
                }

                var sourceName = GetMemberName(source);
                var targetName = GetMemberName(target);

                var assigner = Expression.Assign(target, sourceValue);
                var callLogChange = Expression.Call(logger, mapLoggerChangeMethod,
                    Expression.Constant(sourceName),
                    Expression.Condition(Expression.Equal(sourceValue, Expression.Default(targetType.Type)), Expression.Constant("null"), Expression.Call(sourceValue, objectToStringMethod)),
                    Expression.Constant(targetName),
                    Expression.Condition(Expression.Equal(target, Expression.Default(targetType.Type)), Expression.Constant("null"), Expression.Call(target, objectToStringMethod))
                );
                var callLogNoChange = Expression.Call(logger, mapLoggerNoChangeMethod,
                    Expression.Constant(sourceName),
                    Expression.Constant(targetName),
                    Expression.Condition(Expression.Equal(target, Expression.Default(targetType.Type)), Expression.Constant("null"), Expression.Call(target, objectToStringMethod))
                );
                var ifExpression = Expression.Condition(Expression.NotEqual(target, Expression.Convert(source, targetType.Type)),
                    Expression.Block(callLogChange, assigner),
                    Expression.Block(callLogNoChange, assigner));

                if (Mapper.DebugMode)
                {
                    var ex = Expression.Parameter(typeof(Exception), "ex");
                    var tryCatch = Expression.TryCatch(ifExpression, Expression.Catch(ex, Expression.Throw(Expression.New(newException, Expression.Constant($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}"), ex), assigner.Type)));
                    return Expression.Block(new[] { sourceValue, ex }, readSourceValue, tryCatch);
                }
                else
                {
                    return Expression.Block(new[] { sourceValue }, readSourceValue, ifExpression);
                }
            }
            else if (sourceType.Type.IsEnum || sourceType.IsNullable && sourceType.InnerTypeDetail.Type.IsEnum || sourceType.Type.IsEnum || sourceType.IsNullable && targetType.InnerTypeDetail.Type.IsEnum)
            {
                var sourceValue = Expression.Variable(targetType.Type, "sourceValue");
                Expression readSourceValue;
                if (sourceType.Type != targetType.Type)
                {
                    readSourceValue = Expression.Assign(sourceValue, Expression.Convert(source, targetType.Type));
                }
                else
                {
                    readSourceValue = Expression.Assign(sourceValue, source);
                }

                var sourceName = GetMemberName(source);
                var targetName = GetMemberName(target);

                var assigner = Expression.Assign(target, sourceValue);
                var callLogChange = Expression.Call(logger, mapLoggerChangeMethod,
                    Expression.Constant(sourceName),
                    Expression.Condition(Expression.Equal(sourceValue, Expression.Default(targetType.Type)), Expression.Constant("null"), Expression.Call(sourceValue, objectToStringMethod)),
                    Expression.Constant(targetName),
                    Expression.Condition(Expression.Equal(target, Expression.Default(targetType.Type)), Expression.Constant("null"), Expression.Call(target, objectToStringMethod))
                );
                var callLogNoChange = Expression.Call(logger, mapLoggerNoChangeMethod,
                    Expression.Constant(sourceName),
                    Expression.Constant(targetName),
                    Expression.Condition(Expression.Equal(target, Expression.Default(targetType.Type)), Expression.Constant("null"), Expression.Call(target, objectToStringMethod))
                );
                var ifExpression = Expression.Condition(Expression.NotEqual(target, Expression.Convert(source, targetType.Type)),
                    Expression.Block(callLogChange, assigner),
                    Expression.Block(callLogNoChange, assigner));

                if (Mapper.DebugMode)
                {
                    var ex = Expression.Parameter(typeof(Exception), "ex");
                    var tryCatch = Expression.TryCatch(ifExpression, Expression.Catch(ex, Expression.Throw(Expression.New(newException, Expression.Constant($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}"), ex), assigner.Type)));
                    return Expression.Block(new[] { sourceValue, ex }, readSourceValue, tryCatch);
                }
                else
                {
                    return Expression.Block(new[] { sourceValue }, readSourceValue, ifExpression);
                }
            }

            depth++;
            var blockParameters = new List<ParameterExpression>();
            var blockExpressions = new List<Expression>();

            if (sourceType.HasIEnumerable && targetType.HasIEnumerable)
            {
                if (sourceType.Type.IsArray && targetType.Type.IsArray)
                {
                    //source array, target array
                    var loopLength = Expression.ArrayLength(source);
                    var loopIndex = Expression.Variable(intType, "index");
                    var assignLoopIndexVariable = Expression.Assign(loopIndex, Expression.Constant(0, intType));

                    var loopBreakTarget = Expression.Label();

                    var breakCondition = Expression.IfThen(Expression.Equal(loopIndex, loopLength), Expression.Break(loopBreakTarget));

                    var sourceElement = Expression.ArrayAccess(source, loopIndex);
                    var targetElement = Expression.ArrayAccess(target, loopIndex);
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, targetElement, logger, recursionDictionary, ref depth);

                    var increment = Expression.AddAssign(loopIndex, Expression.Constant(1, intType));

                    var loopBlock = Expression.Block(breakCondition, newElementBlock, increment);
                    var loop = Expression.Loop(loopBlock, loopBreakTarget);

                    var newArrayBlock = Expression.Block(new[] { loopIndex }, assignLoopIndexVariable, loop);
                    var conditionalNewArrayBlock = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null))), newArrayBlock);

                    if (Mapper.DebugMode)
                    {
                        var ex = Expression.Parameter(typeof(Exception), "ex");
                        var tryCatch = Expression.TryCatch(conditionalNewArrayBlock, Expression.Catch(ex, Expression.Throw(Expression.New(newException, Expression.Constant($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}"), ex), conditionalNewArrayBlock.Type)));

                        blockExpressions.Add(tryCatch);
                        blockParameters.Add(ex);
                    }
                    else
                    {
                        blockExpressions.Add(conditionalNewArrayBlock);
                    }
                }
                else if (targetType.Type.IsArray)
                {
                    //source enumerable, target array
                    var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                    var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                    var getEnumeratorMethod = enumerableGeneric.GetMethodBoxed("GetEnumerator");
                    var moveNextMethod = enumeratorTypeDetail.GetMethodBoxed("MoveNext");
                    var currentMember = enumeratorGeneric.GetMember("Current");

                    var enumerable = Expression.Convert(source, enumerableGeneric.Type);
                    var enumerator = Expression.Variable(enumeratorGeneric.Type, "enumerator");
                    var assignEnumeratorVariable = Expression.Assign(enumerator, Expression.Call(enumerable, getEnumeratorMethod.MethodInfo));

                    var loopCounterBreakTarget = Expression.Label();
                    var loopCount = Expression.Variable(intType, "count");
                    var assignLoopCountVariable = Expression.Assign(loopCount, Expression.Constant(0, intType));

                    var incrementLoopCounter = Expression.AddAssign(loopCount, Expression.Constant(1, intType));
                    var loopCounterBreak = Expression.Break(loopCounterBreakTarget);
                    var loopCounter = Expression.Loop(Expression.IfThenElse(Expression.Call(enumerator, moveNextMethod.MethodInfo), incrementLoopCounter, loopCounterBreak), loopCounterBreakTarget);

                    var reassignEnumeratorVariable = Expression.Assign(enumerator, Expression.Call(enumerable, getEnumeratorMethod.MethodInfo));

                    var loopIndex = Expression.Variable(intType, "index");
                    var assignLoopIndexVariable = Expression.Assign(loopIndex, Expression.Constant(0, intType));

                    var loopBreakTarget = Expression.Label();
                    var moveNextOrBreak = Expression.IfThen(Expression.Not(Expression.Call(enumerator, moveNextMethod.MethodInfo)), Expression.Break(loopBreakTarget));

                    var sourceElement = Expression.Convert(Expression.MakeMemberAccess(enumerator, currentMember.MemberInfo), sourceType.IEnumerableGenericInnerType);
                    var targetElement = Expression.ArrayAccess(target, loopIndex);
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, targetElement, logger, recursionDictionary, ref depth);

                    var increment = Expression.AddAssign(loopIndex, Expression.Constant(1, intType));

                    var loopBlock = Expression.Block(moveNextOrBreak, newElementBlock, increment);
                    var loop = Expression.Loop(loopBlock, loopBreakTarget);

                    var newArrayBlock = Expression.Block(new[] { enumerator, loopCount, loopIndex }, assignEnumeratorVariable, assignLoopCountVariable, loopCounter, reassignEnumeratorVariable, assignLoopIndexVariable, loop);
                    var conditionalNewArrayBlock = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null))), newArrayBlock);

                    if (Mapper.DebugMode)
                    {
                        var ex = Expression.Parameter(typeof(Exception), "ex");
                        var tryCatch = Expression.TryCatch(conditionalNewArrayBlock, Expression.Catch(ex, Expression.Throw(Expression.New(newException, Expression.Constant($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}"), ex), conditionalNewArrayBlock.Type)));

                        blockExpressions.Add(tryCatch);
                        blockParameters.Add(ex);
                    }
                    else
                    {
                        blockExpressions.Add(conditionalNewArrayBlock);
                    }
                }
                else if (targetType.HasIListGeneric || targetType.HasIReadOnlyListGeneric || targetType.HasISetGeneric || targetType.HasIReadOnlySetGeneric)
                {
                    //source enumerable, target list or set (has Add method)
                    var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                    var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                    var getEnumeratorMethod = enumerableGeneric.GetMethodBoxed("GetEnumerator");
                    var moveNextMethod = enumeratorTypeDetail.GetMethodBoxed("MoveNext");
                    var currentMember = enumeratorGeneric.GetMember("Current");
                    var addMethod = targetType.GetMethodBoxed("Add");

                    var enumerable = Expression.Convert(source, enumerableGeneric.Type);
                    var enumerator = Expression.Variable(enumeratorGeneric.Type, "enumerator");
                    var assignEnumeratorVariable = Expression.Assign(enumerator, Expression.Call(enumerable, getEnumeratorMethod.MethodInfo));

                    var listItem = Expression.Variable(targetType.InnerType, "listitem");
                    var clearListItemForLog = Expression.Assign(listItem, Expression.Default(listItem.Type));

                    var loopBreakTarget = Expression.Label();
                    var moveNextOrBreak = Expression.IfThen(Expression.Not(Expression.Call(enumerator, moveNextMethod.MethodInfo)), Expression.Break(loopBreakTarget));

                    var sourceElement = Expression.Convert(Expression.MakeMemberAccess(enumerator, currentMember.MemberInfo), sourceType.IEnumerableGenericInnerType);
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, listItem, logger, recursionDictionary, ref depth);
                    var addElementToList = Expression.Call(target, addMethod.MethodInfo, listItem);

                    var loopBlock = Expression.Block(moveNextOrBreak, clearListItemForLog, newElementBlock, addElementToList);
                    var loop = Expression.Loop(loopBlock, loopBreakTarget);

                    var newArrayBlock = Expression.Block(new[] { enumerator, listItem }, assignEnumeratorVariable, loop);
                    var conditionalNewArrayBlock = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null))), newArrayBlock);

                    if (Mapper.DebugMode)
                    {
                        var ex = Expression.Parameter(typeof(Exception), "ex");
                        var tryCatch = Expression.TryCatch(conditionalNewArrayBlock, Expression.Catch(ex, Expression.Throw(Expression.New(newException, Expression.Constant($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}"), ex), conditionalNewArrayBlock.Type)));

                        blockExpressions.Add(tryCatch);
                        blockParameters.Add(ex);
                    }
                    else
                    {
                        blockExpressions.Add(conditionalNewArrayBlock);
                    }
                }
                else if (targetType.HasIDictionaryGeneric || targetType.HasIReadOnlyDictionaryGeneric)
                {
                    //source enumerable, target dictionary
                    var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                    var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                    var getEnumeratorMethod = enumerableGeneric.GetMethodBoxed("GetEnumerator");
                    var moveNextMethod = enumeratorTypeDetail.GetMethodBoxed("MoveNext");
                    var currentMember = enumeratorGeneric.GetMember("Current");
                    var addMethod = targetType.GetMethodBoxed("Add");

                    var enumerable = Expression.Convert(source, enumerableGeneric.Type);
                    var enumerator = Expression.Variable(enumeratorGeneric.Type, "enumerator");
                    var assignEnumeratorVariable = Expression.Assign(enumerator, Expression.Call(enumerable, getEnumeratorMethod.MethodInfo));

                    var targetKey = Expression.Variable(targetType.InnerTypes[0], "key");
                    var targetValue = Expression.Variable(targetType.InnerTypes[1], "value");

                    var clearTargetKeyForLog = Expression.Assign(targetKey, Expression.Default(targetType.InnerTypes[0]));
                    var clearTargetValueForLog = Expression.Assign(targetValue, Expression.Default(targetType.InnerTypes[1]));

                    var loopBreakTarget = Expression.Label();
                    var moveNextOrBreak = Expression.IfThen(Expression.Not(Expression.Call(enumerator, moveNextMethod.MethodInfo)), Expression.Break(loopBreakTarget));

                    var sourceElement = Expression.Convert(Expression.MakeMemberAccess(enumerator, currentMember.MemberInfo), sourceType.IEnumerableGenericInnerType);

                    var sourceKey = Expression.MakeMemberAccess(sourceElement, targetType.DictionaryInnerTypeDetail.GetMember("Key").MemberInfo);
                    var sourceValue = Expression.MakeMemberAccess(sourceElement, targetType.DictionaryInnerTypeDetail.GetMember("Value").MemberInfo);

                    var newKeyElementBlock = GenerateMapAssignTarget(graph, sourceKey, targetKey, logger, recursionDictionary, ref depth);
                    var newValueElementBlock = GenerateMapAssignTarget(graph, sourceValue, targetValue, logger, recursionDictionary, ref depth);

                    var addElementToList = Expression.Call(target, addMethod.MethodInfo, targetKey, targetValue);

                    var loopBlock = Expression.Block(moveNextOrBreak, clearTargetKeyForLog, clearTargetValueForLog, newKeyElementBlock, newValueElementBlock, addElementToList);
                    var loop = Expression.Loop(loopBlock, loopBreakTarget);

                    var newArrayBlock = Expression.Block(new[] { enumerator, targetKey, targetValue }, assignEnumeratorVariable, loop);
                    var conditionalNewArrayBlock = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null))), newArrayBlock);

                    if (Mapper.DebugMode)
                    {
                        var ex = Expression.Parameter(typeof(Exception), "ex");
                        var tryCatch = Expression.TryCatch(conditionalNewArrayBlock, Expression.Catch(ex, Expression.Throw(Expression.New(newException, Expression.Constant($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}"), ex), conditionalNewArrayBlock.Type)));

                        blockExpressions.Add(tryCatch);
                        blockParameters.Add(ex);
                    }
                    else
                    {
                        blockExpressions.Add(conditionalNewArrayBlock);
                    }
                }
                else if (sourceType.HasICollection)
                {
                    //source enumerable, target array but needs casted
                    var arrayType = targetType.InnerType.MakeArrayType();
                    var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                    var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                    var getEnumeratorMethod = enumerableGeneric.GetMethodBoxed("GetEnumerator");
                    var moveNextMethod = enumeratorTypeDetail.GetMethodBoxed("MoveNext");
                    var currentMember = enumeratorGeneric.GetMember("Current");

                    var collectionTypeDetails = TypeAnalyzer.GetTypeDetail(collectionType);
                    var countMember = collectionTypeDetails.GetMember("Count");

                    var array = Expression.Convert(target, arrayType);
                    var casted = Expression.Variable(arrayType, "casted");
                    var assignCastedVariable = Expression.Assign(casted, array);

                    var enumerable = Expression.Convert(source, enumerableGeneric.Type);
                    var enumerator = Expression.Variable(enumeratorGeneric.Type, "enumerator");
                    var assignEnumeratorVariable = Expression.Assign(enumerator, Expression.Call(enumerable, getEnumeratorMethod.MethodInfo));

                    var count = Expression.Variable(intType, "count");
                    var collection = Expression.Convert(source, collectionTypeDetails.Type);
                    var assignCountVariable = Expression.Assign(count, Expression.MakeMemberAccess(collection, countMember.MemberInfo));

                    var loopIndex = Expression.Variable(intType, "index");
                    var assignLoopIndexVariable = Expression.Assign(loopIndex, Expression.Constant(0, intType));

                    var loopBreakTarget = Expression.Label();
                    var moveNextOrBreak = Expression.IfThen(Expression.Not(Expression.Call(enumerator, moveNextMethod.MethodInfo)), Expression.Break(loopBreakTarget));

                    var sourceElement = Expression.Convert(Expression.MakeMemberAccess(enumerator, currentMember.MemberInfo), sourceType.IEnumerableGenericInnerType);
                    var castedElement = Expression.ArrayAccess(casted, loopIndex);
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, castedElement, logger, recursionDictionary, ref depth);

                    var increment = Expression.AddAssign(loopIndex, Expression.Constant(1, intType));

                    var loopBlock = Expression.Block(moveNextOrBreak, newElementBlock, increment);
                    var loop = Expression.Loop(loopBlock, loopBreakTarget);

                    var newArrayBlock = Expression.Block(new[] { casted, enumerator, count, loopIndex }, assignCastedVariable, assignEnumeratorVariable, assignCountVariable, assignLoopIndexVariable, loop);
                    var conditionalNewArrayBlock = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null))), newArrayBlock);

                    if (Mapper.DebugMode)
                    {
                        var ex = Expression.Parameter(typeof(Exception), "ex");
                        var tryCatch = Expression.TryCatch(conditionalNewArrayBlock, Expression.Catch(ex, Expression.Throw(Expression.New(newException, Expression.Constant($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}"), ex), conditionalNewArrayBlock.Type)));

                        blockExpressions.Add(tryCatch);
                        blockParameters.Add(ex);
                    }
                    else
                    {
                        blockExpressions.Add(conditionalNewArrayBlock);
                    }
                }
                else if (sourceType.HasICollectionGeneric || sourceType.HasIReadOnlyCollectionGeneric)
                {
                    //source enumerable, target array but needs casted
                    var arrayType = targetType.InnerType.MakeArrayType();
                    var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                    var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                    var getEnumeratorMethod = enumerableGeneric.GetMethodBoxed("GetEnumerator");
                    var moveNextMethod = enumeratorTypeDetail.GetMethodBoxed("MoveNext");
                    var currentMember = enumeratorGeneric.GetMember("Current");

                    var collectionGenericTypeDetails = TypeAnalyzer.GetGenericTypeDetail(collectionGenericType, sourceType.IEnumerableGenericInnerType);
                    var countMember = collectionGenericTypeDetails.GetMember("Count");

                    var array = Expression.Convert(target, arrayType);
                    var casted = Expression.Variable(arrayType, "casted");
                    var assignCastedVariable = Expression.Assign(casted, array);

                    var enumerable = Expression.Convert(source, enumerableGeneric.Type);
                    var enumerator = Expression.Variable(enumeratorGeneric.Type, "enumerator");
                    var assignEnumeratorVariable = Expression.Assign(enumerator, Expression.Call(enumerable, getEnumeratorMethod.MethodInfo));

                    var count = Expression.Variable(intType, "count");
                    var collection = Expression.Convert(source, collectionGenericTypeDetails.Type);
                    var assignCountVariable = Expression.Assign(count, Expression.MakeMemberAccess(collection, countMember.MemberInfo));

                    var loopIndex = Expression.Variable(intType, "index");
                    var assignLoopIndexVariable = Expression.Assign(loopIndex, Expression.Constant(0, intType));

                    var loopBreakTarget = Expression.Label();
                    var moveNextOrBreak = Expression.IfThen(Expression.Not(Expression.Call(enumerator, moveNextMethod.MethodInfo)), Expression.Break(loopBreakTarget));

                    var sourceElement = Expression.Convert(Expression.MakeMemberAccess(enumerator, currentMember.MemberInfo), sourceType.IEnumerableGenericInnerType);
                    var castedElement = Expression.ArrayAccess(casted, loopIndex);
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, castedElement, logger, recursionDictionary, ref depth);

                    var increment = Expression.AddAssign(loopIndex, Expression.Constant(1, intType));

                    var loopBlock = Expression.Block(moveNextOrBreak, newElementBlock, increment);
                    var loop = Expression.Loop(loopBlock, loopBreakTarget);

                    var newArrayBlock = Expression.Block(new[] { casted, enumerator, count, loopIndex }, assignCastedVariable, assignEnumeratorVariable, assignCountVariable, assignLoopIndexVariable, loop);
                    var conditionalNewArrayBlock = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null))), newArrayBlock);

                    if (Mapper.DebugMode)
                    {
                        var ex = Expression.Parameter(typeof(Exception), "ex");
                        var tryCatch = Expression.TryCatch(conditionalNewArrayBlock, Expression.Catch(ex, Expression.Throw(Expression.New(newException, Expression.Constant($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}"), ex), conditionalNewArrayBlock.Type)));

                        blockExpressions.Add(tryCatch);
                        blockParameters.Add(ex);
                    }
                    else
                    {
                        blockExpressions.Add(conditionalNewArrayBlock);
                    }
                }
                else
                {
                    //source enumerable, target list but needs casted
                    var listType = TypeAnalyzer.GetGenericTypeDetail(genericListType, targetType.InnerType);
                    var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                    var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                    var getEnumeratorMethod = enumerableGeneric.GetMethodBoxed("GetEnumerator");
                    var moveNextMethod = enumeratorTypeDetail.GetMethodBoxed("MoveNext");
                    var currentMember = enumeratorGeneric.GetMember("Current");
                    var addMethod = listType.GetMethodBoxed("Add");

                    var list = Expression.Convert(target, listType.Type);
                    var casted = Expression.Variable(listType.Type, "casted");
                    var assignCastedVariable = Expression.Assign(casted, list);

                    var enumerable = Expression.Convert(source, enumerableGeneric.Type);
                    var enumerator = Expression.Variable(enumeratorGeneric.Type, "enumerator");
                    var assignEnumeratorVariable = Expression.Assign(enumerator, Expression.Call(enumerable, getEnumeratorMethod.MethodInfo));

                    var listItem = Expression.Variable(targetType.InnerType, "listitem");
                    var clearListItemForLog = Expression.Assign(listItem, Expression.Default(listItem.Type));

                    var loopBreakTarget = Expression.Label();
                    var moveNextOrBreak = Expression.IfThen(Expression.Not(Expression.Call(enumerator, moveNextMethod.MethodInfo)), Expression.Break(loopBreakTarget));

                    var sourceElement = Expression.Convert(Expression.MakeMemberAccess(enumerator, currentMember.MemberInfo), sourceType.IEnumerableGenericInnerType);
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, listItem, logger, recursionDictionary, ref depth);
                    var addElementToList = Expression.Call(casted, addMethod.MethodInfo, listItem);

                    var loopBlock = Expression.Block(moveNextOrBreak, clearListItemForLog, newElementBlock, addElementToList);
                    var loop = Expression.Loop(loopBlock, loopBreakTarget);

                    var newArrayBlock = Expression.Block(new[] { casted, enumerator, listItem }, assignCastedVariable, assignEnumeratorVariable, loop);
                    var conditionalNewArrayBlock = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null))), newArrayBlock);

                    if (Mapper.DebugMode)
                    {
                        var ex = Expression.Parameter(typeof(Exception), "ex");
                        var tryCatch = Expression.TryCatch(conditionalNewArrayBlock, Expression.Catch(ex, Expression.Throw(Expression.New(newException, Expression.Constant($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}"), ex), conditionalNewArrayBlock.Type)));

                        blockExpressions.Add(tryCatch);
                        blockParameters.Add(ex);
                    }
                    else
                    {
                        blockExpressions.Add(conditionalNewArrayBlock);
                    }
                }
            }
            else
            {
                var recursionKey = Expression.New(newRecursionKey, Expression.Convert(source, objectType), Expression.Constant(target.Type, typeof(Type)));
                var recursionDictionaryAdd = Expression.Call(recursionDictionary, dictionaryAddMethod, recursionKey, Expression.Convert(target, objectType));
                blockExpressions.Add(recursionDictionaryAdd);
                foreach (var mapTo in memberMaps)
                {
                    if (graph is not null && !graph.HasMember(mapTo.Key))
                        continue;

                    var sourceLambda = mapTo.Value.Item1;
                    var targetLambda = mapTo.Value.Item2;

                    var sourceLambdaRebind = (LambdaExpression)LinqRebinder.RebindExpression(sourceLambda, sourceLambda.Parameters[0], source);
                    var targetLambdaRebind = (LambdaExpression)LinqRebinder.RebindExpression(targetLambda, targetLambda.Parameters[0], target);

                    var sourceMember = sourceLambdaRebind.Body;
                    var targetMember = targetLambdaRebind.Body;

                    if (sourceMember.NodeType == ExpressionType.Convert)
                        sourceMember = ((UnaryExpression)sourceMember).Operand;

                    if (targetMember.NodeType == ExpressionType.Convert)
                        targetMember = ((UnaryExpression)targetMember).Operand;

                    var childGraph = graph?.GetChildGraph(mapTo.Key);

                    Expression newObjectBlock;
                    try
                    {
                        newObjectBlock = GenerateMapAssignTarget(childGraph, sourceMember, targetMember, logger, recursionDictionary, ref depth);
                    }
                    catch (Exception ex)
                    {
                        throw new MapException($"Failed mapping {target.Type.Name}.{mapTo.Key} to expression from {source.Type.Name}", ex);
                    }

                    if (Mapper.DebugMode)
                    {
                        var ex = Expression.Parameter(typeof(Exception), "ex");
                        var tryCatch = Expression.TryCatch(newObjectBlock, Expression.Catch(ex, Expression.Throw(Expression.New(newException, Expression.Constant($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}"), ex), newObjectBlock.Type)));

                        blockExpressions.Add(tryCatch);
                        blockParameters.Add(ex);
                    }
                    else
                    {
                        blockExpressions.Add(newObjectBlock);
                    }
                }
                var recursionDictionaryRemove = Expression.Call(recursionDictionary, dictionaryRemoveMethod, recursionKey);
                blockExpressions.Add(recursionDictionaryRemove);
            }

            var returnLabelType = Expression.Label(targetType.Type);
            blockExpressions.Add(Expression.Label(returnLabelType, target));

            depth--;

            Expression block;
            if (blockExpressions.Count > 0)
                block = Expression.Block(blockParameters, blockExpressions);
            else
                block = Expression.Empty();

            return block;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Expression GenerateMapAssignTarget(Graph? graph, Expression source, Expression target, Expression logger, Expression recursionDictionary, ref int depth)
        {
            if (source is null)
                throw new MapException($"{nameof(source)} is null");
            if (target is null)
                throw new MapException($"{nameof(target)} is null");
            if (recursionDictionary is null)
                throw new MapException($"{nameof(recursionDictionary)} is null");
            var sourceType = TypeAnalyzer.GetTypeDetail(source.Type);
            var targetType = TypeAnalyzer.GetTypeDetail(target.Type);

            if (targetType.Type.IsValueType || targetType.CoreType.HasValue)
            {
                var sourceMapType = TypeAnalyzer.GetGenericTypeDetail(genericMapType, source.Type, target.Type);
                var sourceMap = sourceMapType.GetMethodBoxed("GetMap").MethodInfo.Invoke(null, null);
                var generateMapArgs = new object?[] { graph, source, target, logger, recursionDictionary, depth };
                var sourceBlockMap = (Expression)sourceMapType.GetMethodBoxed("GenerateMap").MethodInfo.Invoke(sourceMap, generateMapArgs)!;
                depth = (int)generateMapArgs[generateMapArgs.Length - 1]!;
                return sourceBlockMap;
            }
            else
            {
                ParameterExpression newTarget;
                Expression assignNewTarget;
                if (sourceType.HasIEnumerable && targetType.HasIEnumerable)
                {
                    //enumerable
                    if (sourceType.Type.IsArray && targetType.Type.IsArray)
                    {
                        //source array, target array
                        var arrayType = targetType.InnerType.MakeArrayType();
                        newTarget = Expression.Variable(arrayType, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.NewArrayBounds(targetType.InnerType, Expression.ArrayLength(source)));
                    }
                    else if (targetType.Type.IsArray)
                    {
                        //source enumerable, target array
                        if (sourceType.HasICollection)
                        {
                            var arrayType = targetType.InnerType.MakeArrayType();
                            newTarget = Expression.Variable(arrayType, "newTarget");

                            var countMember = TypeAnalyzer.GetTypeDetail(collectionType).GetMember("Count");
                            var collection = Expression.Convert(source, collectionType);
                            var collectionCount = Expression.MakeMemberAccess(collection, countMember.MemberInfo);

                            assignNewTarget = Expression.Assign(newTarget, Expression.NewArrayBounds(targetType.InnerType, collectionCount));
                        }
                        else if (sourceType.HasICollectionGeneric)
                        {
                            var arrayType = targetType.InnerType.MakeArrayType();
                            newTarget = Expression.Variable(arrayType, "newTarget");

                            var collectionGenericTypeDetails = TypeAnalyzer.GetGenericTypeDetail(collectionGenericType, sourceType.IEnumerableGenericInnerType);
                            var countMember = collectionGenericTypeDetails.GetMember("Count");
                            var collection = Expression.Convert(source, collectionGenericTypeDetails.Type);
                            var collectionCount = Expression.MakeMemberAccess(collection, countMember.MemberInfo);

                            assignNewTarget = Expression.Assign(newTarget, Expression.NewArrayBounds(targetType.InnerType, collectionCount));
                        }
                        else if (sourceType.HasIReadOnlyCollectionGeneric)
                        {
                            var arrayType = targetType.InnerType.MakeArrayType();
                            newTarget = Expression.Variable(arrayType, "newTarget");

                            var readOnlyCollectionGenericTypeDetails = TypeAnalyzer.GetGenericTypeDetail(readOnlyCollectionGenericType, sourceType.IEnumerableGenericInnerType);
                            var countMember = readOnlyCollectionGenericTypeDetails.GetMember("Count");
                            var collection = Expression.Convert(source, readOnlyCollectionGenericTypeDetails.Type);
                            var collectionCount = Expression.MakeMemberAccess(collection, countMember.MemberInfo);

                            assignNewTarget = Expression.Assign(newTarget, Expression.NewArrayBounds(targetType.InnerType, collectionCount));
                        }
                        else //must enumerate to get count
                        {
                            var arrayType = targetType.InnerType.MakeArrayType();
                            newTarget = Expression.Variable(arrayType, "newTarget");

                            var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                            var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                            var getEnumeratorMethod = enumerableGeneric.GetMethodBoxed("GetEnumerator");
                            var moveNextMethod = enumeratorTypeDetail.GetMethodBoxed("MoveNext");

                            var enumerable = Expression.Convert(source, enumerableGeneric.Type);
                            var enumerator = Expression.Variable(enumeratorGeneric.Type, "enumerator");
                            var assignEnumeratorVariable = Expression.Assign(enumerator, Expression.Call(enumerable, getEnumeratorMethod.MethodInfo));

                            var loopCounterBreakTarget = Expression.Label();
                            var loopCount = Expression.Variable(intType, "count");
                            var assignLoopCountVariable = Expression.Assign(loopCount, Expression.Constant(0, intType));

                            var incrementLoopCounter = Expression.AddAssign(loopCount, Expression.Constant(1, intType));
                            var loopCounterBreak = Expression.Break(loopCounterBreakTarget);
                            var loopCounter = Expression.Loop(Expression.IfThenElse(Expression.Call(enumerator, moveNextMethod.MethodInfo), incrementLoopCounter, loopCounterBreak), loopCounterBreakTarget);

                            var assignNewTargetFromLoopCount = Expression.Assign(newTarget, Expression.NewArrayBounds(targetType.InnerType, loopCount));

                            assignNewTarget = Expression.Block(new[] { enumerator, loopCount }, assignEnumeratorVariable, assignLoopCountVariable, loopCounter, assignNewTargetFromLoopCount);
                        }
                    }
                    else if (targetType.HasIListGeneric && !targetType.Type.IsInterface)
                    {
                        //source enumerable, target list 
                        newTarget = Expression.Variable(targetType.Type, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(targetType.Type));
                    }
                    else if (targetType.IsIListGeneric || targetType.IsIReadOnlyListGeneric)
                    {
                        //source enumerable, target list interface
                        var listType = TypeAnalyzer.GetGenericType(genericListType, targetType.InnerType);
                        newTarget = Expression.Variable(listType, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(listType));
                    }
                    else if (targetType.HasISetGeneric && !targetType.Type.IsInterface)
                    {
                        //source enumerable, target set 
                        newTarget = Expression.Variable(targetType.Type, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(targetType.Type));
                    }
                    else if (targetType.IsISetGeneric || targetType.IsIReadOnlySetGeneric)
                    {
                        //source enumerable, target set interface
                        var hashSetType = TypeAnalyzer.GetGenericType(genericHashSetType, targetType.InnerType);
                        newTarget = Expression.Variable(hashSetType, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(hashSetType));
                    }
                    else if (targetType.HasIDictionaryGeneric && !targetType.Type.IsInterface)
                    {
                        //source enumerable, target dictionary
                        newTarget = Expression.Variable(targetType.Type, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(targetType.Type));
                    }
                    else if (targetType.IsIDictionaryGeneric || targetType.IsIReadOnlyDictionaryGeneric)
                    {
                        //source enumerable, target dictionary interface
                        var dictionaryType = TypeAnalyzer.GetGenericType(genericDictionaryType, targetType.InnerTypes[0], targetType.InnerTypes[1]);
                        newTarget = Expression.Variable(dictionaryType, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(dictionaryType));
                    }
                    else if (targetType.IsICollectionGeneric || targetType.IsIReadOnlyCollectionGeneric)
                    {
                        //source enumerable, target list interface
                        var listType = TypeAnalyzer.GetGenericType(genericListType, targetType.InnerType);
                        newTarget = Expression.Variable(listType, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(listType));
                    }
                    else if (targetType.IsIEnumerableGeneric)
                    {
                        //source enumerable, target list interface
                        var listType = TypeAnalyzer.GetGenericType(genericListType, targetType.InnerType);
                        newTarget = Expression.Variable(listType, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(listType));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Map cannot create a target type of {target.Type.GetNiceName()}");
                    }
                }
                else
                {
                    //object
                    if (!targetType.ConstructorDetailsBoxed.Any(x => x.ParameterDetails.Count == 0))
                        return Expression.Constant(null, source.Type);
                    newTarget = Expression.Variable(target.Type, "newTarget");
                    assignNewTarget = Expression.Assign(newTarget, Expression.New(target.Type));
                }

                Expression sourceBlockMap;
                var sourceMapType = TypeAnalyzer.GetGenericTypeDetail(genericMapType, source.Type, newTarget.Type);
                if (depth >= maxBuildDepthBeforeCall)
                {
                    var sourceMapExpression = Expression.Call(sourceMapType.GetMethodBoxed(nameof(GetMap)).MethodInfo);
                    var graphExpression = Expression.Constant(graph, graphType);
                    sourceBlockMap = Expression.Call(sourceMapExpression, sourceMapType.GetMethodBoxed(nameof(CopyInternal)).MethodInfo, source, newTarget, logger, graphExpression, recursionDictionary);
                }
                else
                {
                    var sourceMap = sourceMapType.GetMethodBoxed(nameof(GetMap)).MethodInfo.Invoke(null, null);
                    var generateMapArgs = new object?[] { graph, source, newTarget, logger, recursionDictionary, depth };
                    sourceBlockMap = (Expression)sourceMapType.GetMethodBoxed(nameof(GenerateMap)).MethodInfo.Invoke(sourceMap, generateMapArgs)!;
                    depth = (int)generateMapArgs[generateMapArgs.Length - 1]!;
                }

                var sourceName = GetMemberName(source);
                var targetName = GetMemberName(target);

                Expression assignTarget;
                if (targetType.Type != newTarget.Type)
                    assignTarget = Expression.Assign(target, Expression.Convert(newTarget, targetType.Type));
                else
                    assignTarget = Expression.Assign(target, newTarget);

                var logNewObject = Expression.Call(logger, mapLoggerNewObjectMethod,
                    Expression.Constant(sourceName),
                    Expression.Constant(targetName),
                    Expression.Constant(newTarget.Type.Name)
                );

                var block = Expression.Block(new[] { newTarget }, logNewObject, assignNewTarget, sourceBlockMap, assignTarget);

                var recursionKey = Expression.New(newRecursionKey, source, Expression.Constant(target.Type, typeof(Type)));
                var tryGetValue = Expression.Variable(objectType, "value");
                var recursionTryGet = Expression.Call(recursionDictionary, dictionaryTryGetMethod, recursionKey, tryGetValue);
                var recursionTargetAssign = Expression.Assign(target, Expression.Convert(tryGetValue, targetType.Type));
                var recursionCondition = Expression.IfThenElse(recursionTryGet, recursionTargetAssign, block);
                var recursionCheck = Expression.Block(new[] { tryGetValue }, recursionCondition);

                var nullCheck = Expression.IfThen(Expression.NotEqual(source, Expression.Constant(null, source.Type)), recursionCheck);
                return nullCheck;
            }
        }

        private static string GetMemberName(Expression source)
        {
            if (source is null)
                return "calculated";
            return source.ToLinqString();
        }
    }
}
