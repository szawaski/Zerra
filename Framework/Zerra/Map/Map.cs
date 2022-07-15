// Copyright © KaKush LLC
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

namespace Zerra
{
    public class Map<TSource, TTarget> : IMapSetup<TSource, TTarget>
    {
        private const int maxBuildDepthBeforeCall = 3;

        private readonly TypeDetail sourceType;
        private readonly TypeDetail targetType;
        private readonly Dictionary<string, Tuple<Expression<Func<TSource, object>>, Expression<Func<TTarget, object>>>> memberMaps;
        private readonly ConcurrentFactoryDictionary<Graph, Func<TSource, TTarget, Dictionary<MapRecursionKey, object>, TTarget>> compiledGraphMaps;
        private Func<TSource, TTarget, Dictionary<MapRecursionKey, object>, TTarget> compiledMap;

        private static readonly Type genericMapType = typeof(Map<,>);
        private static readonly Type genericListType = typeof(List<>);
        private static readonly Type genericHashSetType = typeof(HashSet<>);
        private static readonly Type genericEnumerableType = typeof(IEnumerable<>);
        private static readonly Type genericEnumeratorType = typeof(IEnumerator<>);
        private static readonly TypeDetail enumeratorTypeDetail = TypeAnalyzer.GetTypeDetail(typeof(IEnumerator));
        private static readonly Type collectionType = typeof(ICollection);
        private static readonly Type collectionGenericType = typeof(ICollection<>);
        private static readonly Type intType = typeof(int);
        private static readonly Type objectType = typeof(object);
        private static readonly Type graphType = typeof(Graph);
        private static readonly Type tType = typeof(TSource);
        private static readonly Type uType = typeof(TTarget);
        private static readonly Type mapDefinitionsType = typeof(IMapDefinition<TSource, TTarget>);
        private static readonly Type recursionDictionaryType = typeof(Dictionary<MapRecursionKey, object>);
        private static readonly Type exceptionType = typeof(Exception);
        private static readonly ConstructorInfo newException = TypeAnalyzer.GetTypeDetail(typeof(Exception)).GetConstructor(typeof(string), typeof(Exception)).ConstructorInfo;
        private static readonly MethodInfo dictionaryAddMethod = TypeAnalyzer.GetTypeDetail(recursionDictionaryType).GetMethod("Add").MethodInfo;
        private static readonly MethodInfo dictionaryRemoveMethod = TypeAnalyzer.GetTypeDetail(recursionDictionaryType).GetMethod("Remove").MethodInfo;
        private static readonly MethodInfo dictionaryTryGetMethod = TypeAnalyzer.GetTypeDetail(recursionDictionaryType).GetMethod("TryGetValue").MethodInfo;
        private static readonly ConstructorInfo newRecursionKey = TypeAnalyzer.GetTypeDetail(typeof(MapRecursionKey)).GetConstructor(typeof(object), typeof(Type)).ConstructorInfo;
        private static readonly MethodInfo toStringMethod = TypeAnalyzer.GetTypeDetail(objectType).GetMethod("ToString").MethodInfo;


        private static readonly ConcurrentFactoryDictionary<TypeKey, object> mapsStore = new();
        public static Map<TSource, TTarget> GetMap()
        {
            var key = new TypeKey(tType, uType);
            var map = (Map<TSource, TTarget>)mapsStore.GetOrAdd(key, (k) => { return new Map<TSource, TTarget>(); });
            return map;
        }

        private Map()
        {
            sourceType = TypeAnalyzer.GetTypeDetail(tType);
            targetType = TypeAnalyzer.GetTypeDetail(uType);
            memberMaps = new Dictionary<string, Tuple<Expression<Func<TSource, object>>, Expression<Func<TTarget, object>>>>();
            compiledGraphMaps = new ConcurrentFactoryDictionary<Graph, Func<TSource, TTarget, Dictionary<MapRecursionKey, object>, TTarget>>();
            GenerateDefaultMemberMaps();
            RunInitializers();
        }

        void IMapSetup<TSource, TTarget>.Define(Expression<Func<TTarget, object>> property, Expression<Func<TSource, object>> value)
        {
            lock (this)
            {
                if (compiledMap != null || compiledGraphMaps.Count > 0)
                    throw new MapException("Map already complied. Define must be called before Maps are used. Create a class that inherits IMapDefiner<T, U>.");

                if (sourceType.CoreType.HasValue || targetType.CoreType.HasValue)
                    throw new MapException("Cannot add specific mappings to core types");
                if (sourceType.Type.IsEnum || (sourceType.IsNullable && sourceType.InnerTypeDetails[0].Type.IsEnum) || sourceType.Type.IsEnum || (sourceType.IsNullable && targetType.InnerTypeDetails[0].Type.IsEnum))
                    throw new MapException("Cannot add specific mappings to enum types");
                if (sourceType.IsIEnumerable || targetType.IsIEnumerable)
                    throw new MapException("Cannot add specific mappings to enumerable types");

                var name = property.ReadMemberName();
                if (memberMaps.ContainsKey(name))
                    _ = memberMaps.Remove(name);
                memberMaps.Add(name, new Tuple<Expression<Func<TSource, object>>, Expression<Func<TTarget, object>>>(value, property));
            }
        }
        void IMapSetup<TSource, TTarget>.DefineTwoWay(Expression<Func<TTarget, object>> propertyU, Expression<Func<TSource, object>> propertyT)
        {
            ((IMapSetup<TSource, TTarget>)this).Define(propertyU, propertyT);

            var otherWay = (IMapSetup<TTarget, TSource>)Map<TTarget, TSource>.GetMap();
            otherWay.Define(propertyT, propertyU);
        }
        void IMapSetup<TSource, TTarget>.Undefine(Expression<Func<TTarget, object>> property)
        {
            lock (this)
            {
                if (compiledMap != null || compiledGraphMaps.Count > 0)
                    throw new MapException("Map already complied. Define must be called before Maps are used. Create a class that inherits IMapDefiner<T, U>.");

                if (sourceType.CoreType.HasValue || targetType.CoreType.HasValue)
                    throw new MapException("Cannot add specific mappings to core types");
                if (sourceType.Type.IsEnum || (sourceType.IsNullable && sourceType.InnerTypeDetails[0].Type.IsEnum) || sourceType.Type.IsEnum || (sourceType.IsNullable && targetType.InnerTypeDetails[0].Type.IsEnum))
                    throw new MapException("Cannot add specific mappings to enum types");
                if (sourceType.IsIEnumerable || targetType.IsIEnumerable)
                    throw new MapException("Cannot add specific mappings to enumerable types");

                var name = property.ReadMemberName();
                if (memberMaps.ContainsKey(name))
                    _ = memberMaps.Remove(name);
            }
        }
        void IMapSetup<TSource, TTarget>.UndefineTwoWay(Expression<Func<TTarget, object>> propertyU, Expression<Func<TSource, object>> propertyT)
        {
            ((IMapSetup<TSource, TTarget>)this).Undefine(propertyU);

            var otherWay = (IMapSetup<TTarget, TSource>)Map<TTarget, TSource>.GetMap();
            otherWay.Undefine(propertyT);
        }
        void IMapSetup<TSource, TTarget>.UndefineAll()
        {
            lock (this)
            {
                memberMaps.Clear();
            }
        }

        public TTarget Copy(TSource source, Graph graph = null)
        {
            if (source == null)
                return default;

            TTarget target;
            if (sourceType.IsIEnumerable && targetType.IsIEnumerable)
            {
                if (targetType.Type.IsArray)
                {
                    int length;
                    if (sourceType.IsICollection)
                    {
                        length = ((ICollection)source).Count;
                    }
                    else
                    {
                        length = 0;
                        foreach (var item in (IEnumerable)source)
                            length++;
                    }
                    target = (TTarget)(object)Array.CreateInstance(targetType.InnerTypes[0], length);
                }
                else if (targetType.IsIList && targetType.Type.IsInterface)
                {
                    var listType = TypeAnalyzer.GetGenericTypeDetail(genericListType, targetType.InnerTypes[0]);
                    target = (TTarget)listType.Creator();
                }
                else if (targetType.IsIList && !targetType.Type.IsInterface)
                {
                    target = (TTarget)targetType.Creator();
                }
                else if (targetType.IsISet && targetType.Type.IsInterface)
                {
                    var setType = TypeAnalyzer.GetGenericTypeDetail(genericHashSetType, targetType.InnerTypes[0]);
                    target = (TTarget)setType.Creator();
                }
                else if (targetType.IsISet && !targetType.Type.IsInterface)
                {
                    target = (TTarget)targetType.Creator();
                }
                else if (sourceType.IsICollection)
                {
                    var length = ((ICollection)source).Count;
                    target = (TTarget)(object)Array.CreateInstance(targetType.InnerTypes[0], length);
                }
                else if (sourceType.IsICollectionGeneric)
                {
                    var length = (int)sourceType.GetMember("Count").Getter(source);
                    target = (TTarget)(object)Array.CreateInstance(targetType.InnerTypes[0], length);
                }
                else
                {
                    var listType = TypeAnalyzer.GetGenericTypeDetail(genericListType, targetType.InnerTypes[0]);
                    target = (TTarget)listType.Creator();
                }
            }
            else
            {
                target = (TTarget)targetType.Creator();
            }

            if (graph == null)
            {
                if (compiledMap == null)
                {
                    lock (this)
                    {
                        if (compiledMap == null)
                        {
                            compiledMap = CompileMap(null);
                        }
                    }
                }
                return compiledMap(source, target, new Dictionary<MapRecursionKey, object>());
            }
            else
            {
                var map = compiledGraphMaps.GetOrAdd(graph ?? Graph.Empty(), (g) => { return CompileMap(g); });
                return map(source, target, new Dictionary<MapRecursionKey, object>());
            }
        }

        public TTarget CopyTo(TSource source, TTarget target, Graph graph = null)
        {
            if (source == null)
                return default;
            return CopyInternal(source, target, graph, new Dictionary<MapRecursionKey, object>());
        }

        internal TTarget CopyInternal(TSource source, TTarget target, Graph graph, Dictionary<MapRecursionKey, object> recursionDictionary)
        {
            if (graph == null)
            {
                if (compiledMap == null)
                {
                    lock (this)
                    {
                        if (compiledMap == null)
                        {
                            compiledMap = CompileMap(null);
                        }
                    }
                }
                return compiledMap(source, target, recursionDictionary);
            }
            else
            {
                var map = compiledGraphMaps.GetOrAdd(graph ?? Graph.Empty(), (g) => { return CompileMap(g); });
                return map(source, target, recursionDictionary);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateDefaultMemberMaps()
        {
            if (sourceType.CoreType.HasValue || targetType.CoreType.HasValue)
                return;

            if (sourceType.Type.IsEnum || (sourceType.IsNullable && sourceType.InnerTypeDetails[0].Type.IsEnum) || sourceType.Type.IsEnum || (sourceType.IsNullable && targetType.InnerTypeDetails[0].Type.IsEnum))
                return;

            if (sourceType.IsIEnumerable || targetType.IsIEnumerable)
                return;

            var sourceParameter = Expression.Parameter(sourceType.Type, "source");
            var targetParameter = Expression.Parameter(targetType.Type, "target");
            foreach (var memberT in sourceType.MemberDetails)
            {
                if (targetType.TryGetMember(memberT.Name, out var memberU))
                {
                    Expression sourceMemberAccess;
                    if (memberT.MemberInfo.MemberType == MemberTypes.Property)
                        sourceMemberAccess = Expression.Property(sourceParameter, (PropertyInfo)memberT.MemberInfo);
                    else
                        sourceMemberAccess = Expression.Field(sourceParameter, (FieldInfo)memberT.MemberInfo);
                    var convertSourceMemberAccess = Expression.Convert(sourceMemberAccess, objectType);
                    var sourceLambda = Expression.Lambda<Func<TSource, object>>(convertSourceMemberAccess, sourceParameter);

                    Expression targetMemberAccess;
                    if (memberU.MemberInfo.MemberType == MemberTypes.Property)
                        targetMemberAccess = Expression.Property(targetParameter, (PropertyInfo)memberU.MemberInfo);
                    else
                        targetMemberAccess = Expression.Field(targetParameter, (FieldInfo)memberU.MemberInfo);
                    var convertTargetMemberAccess = Expression.Convert(targetMemberAccess, objectType);
                    var targetLambda = Expression.Lambda<Func<TTarget, object>>(convertTargetMemberAccess, targetParameter);

                    var name = sourceLambda.ReadMemberName();
                    memberMaps.Add(name, new Tuple<Expression<Func<TSource, object>>, Expression<Func<TTarget, object>>>(sourceLambda, targetLambda));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RunInitializers()
        {
            var instanceTypes = Discovery.GetImplementationTypes(mapDefinitionsType);
            foreach (var instanceType in instanceTypes)
            {
                var instance = (IMapDefinition<TSource, TTarget>)Instantiator.CreateInstance(instanceType);
                instance.Define(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Func<TSource, TTarget, Dictionary<MapRecursionKey, object>, TTarget> CompileMap(Graph graph)
        {
            var depth = 0;
            var sourceParameter = Expression.Parameter(sourceType.Type, "source");
            var targetParameter = Expression.Parameter(targetType.Type, "target");
            var recursionDictionaryParameter = Expression.Parameter(recursionDictionaryType, "recursionDictionary");
            var block = GenerateMap(graph, sourceParameter, targetParameter, recursionDictionaryParameter, ref depth);
            var lambda = Expression.Lambda<Func<TSource, TTarget, Dictionary<MapRecursionKey, object>, TTarget>>(block, sourceParameter, targetParameter, recursionDictionaryParameter);
            return lambda.Compile();
        }

        private Expression GenerateMap(Graph graph, Expression source, Expression target, Expression recursionDictionary, ref int depth)
        {
            if (sourceType.CoreType.HasValue || targetType.CoreType.HasValue)
            {
                Expression assigner;
                if (sourceType.CoreType.HasValue && targetType.CoreType.HasValue && sourceType.CoreType.Value != targetType.CoreType.Value)
                {
                    if (targetType.CoreType == CoreType.String)
                    {
                        //special case convert to string
                        if (sourceType.IsNullable)
                        {
                            assigner = Expression.Assign(target, Expression.Condition(Expression.MakeMemberAccess(source, sourceType.GetMember("HasValue").MemberInfo), Expression.Call(Expression.MakeMemberAccess(source, sourceType.GetMember("Value").MemberInfo), toStringMethod), Expression.Constant(null, targetType.Type)));
                        }
                        else
                        {
                            assigner = Expression.Assign(target, Expression.Call(source, toStringMethod));
                        }
                    }
                    else if (sourceType.CoreType == CoreType.String)
                    {
                        Expression convert = targetType.CoreType switch
                        {
                            CoreType.Boolean or CoreType.Byte or CoreType.SByte or CoreType.Int16 or CoreType.UInt16 or CoreType.Int32 or CoreType.UInt32 or CoreType.Int64 or CoreType.UInt64 or CoreType.Single or CoreType.Double or CoreType.Decimal or CoreType.Char or CoreType.DateTime or CoreType.DateTimeOffset or CoreType.TimeSpan or CoreType.Guid => Expression.Call(TypeAnalyzer.GetMethodDetail(targetType.Type, "Parse").MethodInfo, source),
                            CoreType.BooleanNullable or CoreType.ByteNullable or CoreType.SByteNullable or CoreType.Int16Nullable or CoreType.UInt16Nullable or CoreType.Int32Nullable or CoreType.UInt32Nullable or CoreType.Int64Nullable or CoreType.UInt64Nullable or CoreType.SingleNullable or CoreType.DoubleNullable or CoreType.DecimalNullable or CoreType.CharNullable or CoreType.DateTimeNullable or CoreType.DateTimeOffsetNullable or CoreType.TimeSpanNullable or CoreType.GuidNullable => Expression.Condition(Expression.Equal(source, Expression.Constant(null)), Expression.Constant(null, targetType.Type), Expression.Convert(Expression.Call(TypeAnalyzer.GetMethodDetail(targetType.InnerTypes[0], "Parse").MethodInfo, source), targetType.Type)),
                            CoreType.String => throw new Exception("Should not happen"),
                            _ => throw new NotImplementedException(),
                        };
                        assigner = Expression.Assign(target, convert);
                    }
                    else
                    {
                        assigner = Expression.Assign(target, Expression.Convert(source, targetType.Type));
                    }
                }
                else
                {
                    assigner = Expression.Assign(target, source);
                }

                if (Mapper.DebugMode)
                {
                    var tryCatch = Expression.TryCatch(assigner, Expression.Catch(exceptionType, Expression.Throw(Expression.Constant(new MapException($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}")), assigner.Type)));
                    return tryCatch;
                }
                else
                {
                    return assigner;
                }
            }
            else if (sourceType.Type.IsEnum || (sourceType.IsNullable && sourceType.InnerTypeDetails[0].Type.IsEnum) || sourceType.Type.IsEnum || (sourceType.IsNullable && targetType.InnerTypeDetails[0].Type.IsEnum))
            {
                Expression assigner;
                if (sourceType.Type != targetType.Type)
                {
                    assigner = Expression.Assign(target, Expression.Convert(source, targetType.Type));
                }
                else
                {
                    assigner = Expression.Assign(target, source);
                }

                if (Mapper.DebugMode)
                {
                    var tryCatch = Expression.TryCatch(assigner, Expression.Catch(exceptionType, Expression.Throw(Expression.Constant(new MapException($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}")), assigner.Type)));
                    return tryCatch;
                }
                else
                {
                    return assigner;
                }
            }

            var blockExpressions = new List<Expression>();
            var blockParameters = new List<ParameterExpression>();

            depth++;

            if (sourceType.IsIEnumerable && targetType.IsIEnumerable)
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
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, targetElement, recursionDictionary, ref depth);

                    var increment = Expression.AddAssign(loopIndex, Expression.Constant(1, intType));

                    var loopBlock = Expression.Block(breakCondition, newElementBlock, increment);
                    var loop = Expression.Loop(loopBlock, loopBreakTarget);

                    var newArrayBlock = Expression.Block(new[] { loopIndex }, assignLoopIndexVariable, loop);
                    var conditionalNewArrayBlock = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null))), newArrayBlock);

                    if (Mapper.DebugMode)
                    {
                        var tryCatch = Expression.TryCatch(conditionalNewArrayBlock, Expression.Catch(exceptionType, Expression.Throw(Expression.Constant(new MapException($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}")), conditionalNewArrayBlock.Type)));
                        blockExpressions.Add(tryCatch);
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
                    var getEnumeratorMethod = enumerableGeneric.GetMethod("GetEnumerator");
                    var moveNextMethod = enumeratorTypeDetail.GetMethod("MoveNext");
                    var currentMethod = enumeratorGeneric.GetMethod("get_Current");

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

                    var sourceElement = Expression.Convert(Expression.Call(enumerator, currentMethod.MethodInfo), sourceType.IEnumerableGenericInnerType);
                    var targetElement = Expression.ArrayAccess(target, loopIndex);
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, targetElement, recursionDictionary, ref depth);

                    var increment = Expression.AddAssign(loopIndex, Expression.Constant(1, intType));

                    var loopBlock = Expression.Block(moveNextOrBreak, newElementBlock, increment);
                    var loop = Expression.Loop(loopBlock, loopBreakTarget);

                    var newArrayBlock = Expression.Block(new[] { enumerator, loopCount, loopIndex }, assignEnumeratorVariable, assignLoopCountVariable, loopCounter, reassignEnumeratorVariable, assignLoopIndexVariable, loop);
                    var conditionalNewArrayBlock = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null))), newArrayBlock);

                    if (Mapper.DebugMode)
                    {
                        var tryCatch = Expression.TryCatch(conditionalNewArrayBlock, Expression.Catch(exceptionType, Expression.Throw(Expression.Constant(new MapException($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}")), conditionalNewArrayBlock.Type)));
                        blockExpressions.Add(tryCatch);
                    }
                    else
                    {
                        blockExpressions.Add(conditionalNewArrayBlock);
                    }
                }
                else if (targetType.IsIList || targetType.IsISet)
                {
                    //source enumerable, target list or set (has Add method)
                    var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                    var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                    var getEnumeratorMethod = enumerableGeneric.GetMethod("GetEnumerator");
                    var moveNextMethod = enumeratorTypeDetail.GetMethod("MoveNext");
                    var currentMethod = enumeratorGeneric.GetMethod("get_Current");
                    var addMethod = targetType.GetMethod("Add");

                    var enumerable = Expression.Convert(source, enumerableGeneric.Type);
                    var enumerator = Expression.Variable(enumeratorGeneric.Type, "enumerator");
                    var assignEnumeratorVariable = Expression.Assign(enumerator, Expression.Call(enumerable, getEnumeratorMethod.MethodInfo));

                    var loopIndex = Expression.Variable(intType, "index");
                    var assignLoopIndexVariable = Expression.Assign(loopIndex, Expression.Constant(0, intType));

                    var listItem = Expression.Variable(targetType.InnerTypes[0], "listitem");

                    var loopBreakTarget = Expression.Label();
                    var moveNextOrBreak = Expression.IfThen(Expression.Not(Expression.Call(enumerator, moveNextMethod.MethodInfo)), Expression.Break(loopBreakTarget));

                    var sourceElement = Expression.Convert(Expression.Call(enumerator, currentMethod.MethodInfo), sourceType.IEnumerableGenericInnerType);
                    var targetElement = listItem;
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, targetElement, recursionDictionary, ref depth);
                    var addElementToList = Expression.Call(target, addMethod.MethodInfo, listItem);

                    var increment = Expression.AddAssign(loopIndex, Expression.Constant(1, intType));

                    var loopBlock = Expression.Block(moveNextOrBreak, newElementBlock, addElementToList, increment);
                    var loop = Expression.Loop(loopBlock, loopBreakTarget);

                    var newArrayBlock = Expression.Block(new[] { enumerator, listItem, loopIndex }, assignEnumeratorVariable, assignLoopIndexVariable, loop);
                    var conditionalNewArrayBlock = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null))), newArrayBlock);

                    if (Mapper.DebugMode)
                    {
                        var tryCatch = Expression.TryCatch(conditionalNewArrayBlock, Expression.Catch(exceptionType, Expression.Throw(Expression.Constant(new MapException($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}")), conditionalNewArrayBlock.Type)));
                        blockExpressions.Add(tryCatch);
                    }
                    else
                    {
                        blockExpressions.Add(conditionalNewArrayBlock);
                    }
                }
                else if (sourceType.IsICollection)
                {
                    //source enumerable, target array but needs casted
                    var arrayType = Discovery.GetTypeFromName(targetType.InnerTypes[0] + "[]");
                    var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                    var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                    var getEnumeratorMethod = enumerableGeneric.GetMethod("GetEnumerator");
                    var moveNextMethod = enumeratorTypeDetail.GetMethod("MoveNext");
                    var currentMethod = enumeratorGeneric.GetMethod("get_Current");

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

                    var sourceElement = Expression.Convert(Expression.Call(enumerator, currentMethod.MethodInfo), sourceType.IEnumerableGenericInnerType);
                    var castedElement = Expression.ArrayAccess(casted, loopIndex);
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, castedElement, recursionDictionary, ref depth);

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
                else if (sourceType.IsICollectionGeneric)
                {
                    //source enumerable, target array but needs casted
                    var arrayType = Discovery.GetTypeFromName(targetType.InnerTypes[0] + "[]");
                    var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                    var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                    var getEnumeratorMethod = enumerableGeneric.GetMethod("GetEnumerator");
                    var moveNextMethod = enumeratorTypeDetail.GetMethod("MoveNext");
                    var currentMethod = enumeratorGeneric.GetMethod("get_Current");

                    var collectionTypeDetails = TypeAnalyzer.GetTypeDetail(collectionGenericType);
                    var collectionGenericTypeDetails = TypeAnalyzer.GetGenericTypeDetail(collectionTypeDetails, sourceType.IEnumerableGenericInnerType);
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

                    var sourceElement = Expression.Convert(Expression.Call(enumerator, currentMethod.MethodInfo), sourceType.IEnumerableGenericInnerType);
                    var castedElement = Expression.ArrayAccess(casted, loopIndex);
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, castedElement, recursionDictionary, ref depth);

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
                    var listType = TypeAnalyzer.GetGenericTypeDetail(genericListType, targetType.InnerTypes[0]);
                    var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                    var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                    var getEnumeratorMethod = enumerableGeneric.GetMethod("GetEnumerator");
                    var moveNextMethod = enumeratorTypeDetail.GetMethod("MoveNext");
                    var currentMethod = enumeratorGeneric.GetMethod("get_Current");
                    var addMethod = listType.GetMethod("Add");

                    var list = Expression.Convert(target, listType.Type);
                    var casted = Expression.Variable(listType.Type, "casted");
                    var assignCastedVariable = Expression.Assign(casted, list);

                    var enumerable = Expression.Convert(source, enumerableGeneric.Type);
                    var enumerator = Expression.Variable(enumeratorGeneric.Type, "enumerator");
                    var assignEnumeratorVariable = Expression.Assign(enumerator, Expression.Call(enumerable, getEnumeratorMethod.MethodInfo));

                    var loopIndex = Expression.Variable(intType, "index");
                    var assignLoopIndexVariable = Expression.Assign(loopIndex, Expression.Constant(0, intType));

                    var listItem = Expression.Variable(targetType.InnerTypes[0], "listitem");

                    var loopBreakTarget = Expression.Label();
                    var moveNextOrBreak = Expression.IfThen(Expression.Not(Expression.Call(enumerator, moveNextMethod.MethodInfo)), Expression.Break(loopBreakTarget));

                    var sourceElement = Expression.Convert(Expression.Call(enumerator, currentMethod.MethodInfo), sourceType.IEnumerableGenericInnerType);
                    var targetElement = listItem;
                    var newElementBlock = GenerateMapAssignTarget(graph, sourceElement, targetElement, recursionDictionary, ref depth);
                    var addElementToList = Expression.Call(casted, addMethod.MethodInfo, listItem);

                    var increment = Expression.AddAssign(loopIndex, Expression.Constant(1, intType));

                    var loopBlock = Expression.Block(moveNextOrBreak, newElementBlock, addElementToList, increment);
                    var loop = Expression.Loop(loopBlock, loopBreakTarget);

                    var newArrayBlock = Expression.Block(new[] { casted, enumerator, listItem, loopIndex }, assignCastedVariable, assignEnumeratorVariable, assignLoopIndexVariable, loop);
                    var conditionalNewArrayBlock = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null))), newArrayBlock);

                    if (Mapper.DebugMode)
                    {
                        var tryCatch = Expression.TryCatch(conditionalNewArrayBlock, Expression.Catch(exceptionType, Expression.Throw(Expression.Constant(new MapException($"Failed mapping {source} of {sourceType.Type} to {target} of {targetType.Type}")), conditionalNewArrayBlock.Type)));
                        blockExpressions.Add(tryCatch);
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

                    if (graph != null)
                    {
                        var sourceMemberType = TypeAnalyzer.GetTypeDetail(sourceMember.Type);
                        var targetMemberType = TypeAnalyzer.GetTypeDetail(sourceMember.Type);
                        if (sourceMemberType.IsGraphLocalProperty && targetMemberType.IsGraphLocalProperty)
                        {
                            if (!graph.HasLocalProperty(mapTo.Key))
                                continue;
                        }
                        else
                        {
                            if (!graph.HasChildGraph(mapTo.Key))
                                continue;
                        }
                    }

                    var childGraph = graph?.GetChildGraph(mapTo.Key);

                    Expression newObjectBlock;
                    try
                    {
                        newObjectBlock = GenerateMapAssignTarget(childGraph, sourceMember, targetMember, recursionDictionary, ref depth);
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
        private static Expression GenerateMapAssignTarget(Graph graph, Expression source, Expression target, Expression recursionDictionary, ref int depth)
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
                var sourceMap = sourceMapType.GetMethod("GetMap").MethodInfo.Invoke(null, null);
                var generateMapArgs = new object[] { graph, source, target, recursionDictionary, depth };
                var sourceBlockMap = (Expression)sourceMapType.GetMethod("GenerateMap").MethodInfo.Invoke(sourceMap, generateMapArgs);
                depth = (int)generateMapArgs[generateMapArgs.Length - 1];
                return sourceBlockMap;
            }
            else
            {
                ParameterExpression newTarget;
                Expression assignNewTarget;
                if (sourceType.IsIEnumerable && targetType.IsIEnumerable)
                {
                    if (sourceType.Type.IsArray && targetType.Type.IsArray)
                    {
                        //source array, target array
                        var arrayType = Discovery.GetTypeFromName(targetType.InnerTypes[0] + "[]");
                        newTarget = Expression.Variable(arrayType, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.NewArrayBounds(targetType.InnerTypes[0], Expression.ArrayLength(source)));
                    }
                    else if (targetType.Type.IsArray)
                    {
                        //source enumerable, target array
                        if (sourceType.IsICollection)
                        {
                            var arrayType = Discovery.GetTypeFromName(targetType.InnerTypes[0] + "[]");
                            newTarget = Expression.Variable(arrayType, "newTarget");

                            var countMember = TypeAnalyzer.GetTypeDetail(collectionType).GetMember("Count");
                            var collection = Expression.Convert(source, collectionType);
                            var collectionCount = Expression.MakeMemberAccess(collection, countMember.MemberInfo);

                            assignNewTarget = Expression.Assign(newTarget, Expression.NewArrayBounds(targetType.InnerTypes[0], collectionCount));
                        }
                        else if (sourceType.IsICollectionGeneric)
                        {
                            var arrayType = Discovery.GetTypeFromName(targetType.InnerTypes[0] + "[]");
                            newTarget = Expression.Variable(arrayType, "newTarget");

                            var collectionTypeDetails = TypeAnalyzer.GetTypeDetail(collectionGenericType);
                            var collectionGenericTypeDetails = TypeAnalyzer.GetGenericTypeDetail(collectionTypeDetails, sourceType.IEnumerableGenericInnerType);
                            var countMember = collectionGenericTypeDetails.GetMember("Count");
                            var collection = Expression.Convert(source, collectionGenericTypeDetails.Type);
                            var collectionCount = Expression.MakeMemberAccess(collection, countMember.MemberInfo);

                            assignNewTarget = Expression.Assign(newTarget, Expression.NewArrayBounds(targetType.InnerTypes[0], collectionCount));
                        }
                        else
                        {
                            var arrayType = Discovery.GetTypeFromName(targetType.InnerTypes[0] + "[]");
                            newTarget = Expression.Variable(arrayType, "newTarget");

                            var enumerableGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumerableType, sourceType.IEnumerableGenericInnerType);
                            var enumeratorGeneric = TypeAnalyzer.GetGenericTypeDetail(genericEnumeratorType, sourceType.IEnumerableGenericInnerType);
                            var getEnumeratorMethod = enumerableGeneric.GetMethod("GetEnumerator");
                            var moveNextMethod = enumeratorTypeDetail.GetMethod("MoveNext");

                            var enumerable = Expression.Convert(source, enumerableGeneric.Type);
                            var enumerator = Expression.Variable(enumeratorGeneric.Type, "enumerator");
                            var assignEnumeratorVariable = Expression.Assign(enumerator, Expression.Call(enumerable, getEnumeratorMethod.MethodInfo));

                            var loopCounterBreakTarget = Expression.Label();
                            var loopCount = Expression.Variable(intType, "count");
                            var assignLoopCountVariable = Expression.Assign(loopCount, Expression.Constant(0, intType));

                            var incrementLoopCounter = Expression.AddAssign(loopCount, Expression.Constant(1, intType));
                            var loopCounterBreak = Expression.Break(loopCounterBreakTarget);
                            var loopCounter = Expression.Loop(Expression.IfThenElse(Expression.Call(enumerator, moveNextMethod.MethodInfo), incrementLoopCounter, loopCounterBreak), loopCounterBreakTarget);

                            var assignNewTargetFromLoopCount = Expression.Assign(newTarget, Expression.NewArrayBounds(targetType.InnerTypes[0], loopCount));

                            assignNewTarget = Expression.Block(new[] { enumerator, loopCount }, assignEnumeratorVariable, assignLoopCountVariable, loopCounter, assignNewTargetFromLoopCount);
                        }
                    }
                    else if (targetType.IsIList && !targetType.Type.IsInterface)
                    {
                        //source enumerable, target list 
                        newTarget = Expression.Variable(targetType.Type, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(targetType.Type));
                    }
                    else if (targetType.IsIList && targetType.Type.IsInterface)
                    {
                        //source enumerable, target list interface
                        var listType = TypeAnalyzer.GetGenericType(genericListType, targetType.InnerTypes[0]);
                        newTarget = Expression.Variable(listType, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(listType));
                    }
                    else if (targetType.IsISet && !targetType.Type.IsInterface)
                    {
                        //source enumerable, target set 
                        newTarget = Expression.Variable(targetType.Type, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(targetType.Type));
                    }
                    else if (targetType.IsISet && targetType.Type.IsInterface)
                    {
                        //source enumerable, target set interface
                        var hashSetType = TypeAnalyzer.GetGenericType(genericHashSetType, targetType.InnerTypes[0]);
                        newTarget = Expression.Variable(hashSetType, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(hashSetType));
                    }
                    else
                    {
                        //source enumerable, target list interface
                        var listType = TypeAnalyzer.GetGenericType(genericListType, targetType.InnerTypes[0]);
                        newTarget = Expression.Variable(listType, "newTarget");
                        assignNewTarget = Expression.Assign(newTarget, Expression.New(listType));
                    }
                }
                else
                {
                    newTarget = Expression.Variable(target.Type, "newTarget");
                    assignNewTarget = Expression.Assign(newTarget, Expression.New(target.Type));
                }

                Expression sourceBlockMap;
                var sourceMapType = TypeAnalyzer.GetGenericTypeDetail(genericMapType, source.Type, newTarget.Type);
                if (depth >= maxBuildDepthBeforeCall)
                {
                    var sourceMapExpression = Expression.Call(sourceMapType.GetMethod(nameof(GetMap)).MethodInfo);
                    var graphExpression = Expression.Constant(graph == null ? null : new Graph(graph), graphType);
                    sourceBlockMap = Expression.Call(sourceMapExpression, sourceMapType.GetMethod(nameof(CopyInternal)).MethodInfo, source, newTarget, graphExpression, recursionDictionary);
                }
                else
                {
                    var sourceMap = sourceMapType.GetMethod(nameof(GetMap)).MethodInfo.Invoke(null, null);
                    var generateMapArgs = new object[] { graph, source, newTarget, recursionDictionary, depth };
                    sourceBlockMap = (Expression)sourceMapType.GetMethod(nameof(GenerateMap)).MethodInfo.Invoke(sourceMap, generateMapArgs);
                    depth = (int)generateMapArgs[generateMapArgs.Length - 1];
                }

                Expression assignTarget;
                if (targetType.Type != newTarget.Type)
                    assignTarget = Expression.Assign(target, Expression.Convert(newTarget, targetType.Type));
                else
                    assignTarget = Expression.Assign(target, newTarget);

                var block = Expression.Block(new[] { newTarget }, assignNewTarget, sourceBlockMap, assignTarget);

                var recursionKey = Expression.New(newRecursionKey, source, Expression.Constant(target.Type, typeof(Type)));
                var tryGetValue = Expression.Variable(objectType, "value");
                var recursionTryGet = Expression.Call(recursionDictionary, dictionaryTryGetMethod, recursionKey, tryGetValue);
                var recursionTargetAssign = Expression.Assign(target, Expression.Convert(tryGetValue, targetType.Type));
                var recursionCondition = Expression.IfThenElse(recursionTryGet, recursionTargetAssign, block);
                var recursionCheck = Expression.Block(new[] { tryGetValue }, recursionCondition);

                var nullCheck = Expression.IfThen(Expression.Not(Expression.Equal(source, Expression.Constant(null, source.Type))), recursionCheck);
                return nullCheck;
            }
        }
    }
}
