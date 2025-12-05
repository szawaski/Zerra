// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Zerra.Map.Converters;
using Zerra.Map.Converters.General;
using Zerra.SourceGeneration.Types;

namespace Zerra.Map
{
    internal sealed partial class MapConverterObject<TSource, TTarget> : MapConverter<TSource, TTarget>
    {
        private static readonly ConcurrentStack<Dictionary<string, object?>> collectedValuesPool = new();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<string, object?> RentCollectedValues()
        {
            if (collectedValuesPool.TryPop(out var collectedValues))
                return collectedValues;
            return new(MemberNameComparer.Instance);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnCollectedValues(Dictionary<string, object?> collectedValues)
        {
            collectedValues.Clear();
            collectedValuesPool.Push(collectedValues);
        }

        private IEnumerable<MapConverterObjectMember> members = null!;
        private Dictionary<string, MapConverterObjectMember>? membersByName = null!;
        private bool collectValues;
        private ConstructorDetail<TTarget>? parameterConstructor = null;

        protected override sealed void Setup()
        {
            var membersList = new List<MapConverterObjectMember>();
            membersByName ??= new();
            foreach (var sourceMember in sourceTypeDetail.SerializableMemberDetails)
            {
                var targetMember = targetTypeDetail.SerializableMemberDetails.FirstOrDefault(x => String.Equals(x.Name, sourceMember.Name, StringComparison.Ordinal));
                if (targetMember is null)
                    continue;
                var item = new MapConverterObjectMember(sourceTypeDetail, sourceMember, targetTypeDetail, targetMember);
                membersList.Add(item);
                membersByName[targetMember.Name] = item;
            }
            members = membersList;


            if (targetTypeDetail.Type.IsValueType || !targetTypeDetail.HasCreator)
            {
                //find best constructor
                foreach (var constructor in targetTypeDetail.Constructors.OrderByDescending(x => x.Parameters.Count))
                {
                    var skip = false;
                    foreach (var parameter in constructor.Parameters)
                    {
                        //cannot have argument of itself or a null name
                        if (parameter.Type == targetTypeDetail.Type || parameter.Name is null)
                        {
                            skip = true;
                            break;
                        }
                        //must have a matching a member
                        if (!members.Any(x => x.TargetMember.Type == parameter.Type && MemberNameComparer.Instance.Equals(x.TargetMember.Name, parameter.Name)))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip)
                        continue;
                    parameterConstructor = constructor;
                    break;
                }
                collectValues = parameterConstructor is not null;
            }
        }

        public override TTarget? Map(TSource? source, TTarget? target, Graph? graph)
        {
            if (source is null)
                return default;

            if (!collectValues && target is null)
            {
                if (!targetTypeDetail.HasCreator)
                    throw new InvalidOperationException($"Cannot create instance of {targetTypeDetail.Type.Name}");
                target = targetTypeDetail.Creator!()!;
            }

            Dictionary<string, object?>? collectedValues;
            if (collectValues)
                collectedValues = RentCollectedValues();
            else
                collectedValues = null;

            foreach (var member in members)
            {
                if (graph != null && !graph.HasMember(member.TargetMember.Name))
                    continue;
                var childGraph = graph?.GetChildGraph(member.TargetMember.Name);

                if (collectValues)
                    member.ConverterSetCollectedValues.MapFromParent(source, collectedValues!, childGraph);
                else
                    member.Converter.MapFromParent(source, target!, childGraph);
            }

            if (collectValues)
            {
                var args = new object?[parameterConstructor!.Parameters.Count];
                for (var i = 0; i < args.Length; i++)
                {
#if NETSTANDARD2_0
                    if (collectedValues!.TryGetValue(parameterConstructor.ParameterDetails[i].Name!, out var parameter))
                    {
                        collectedValues.Remove(parameterConstructor.ParameterDetails[i].Name!);
                        args[i] = parameter;
                    }
#else
                    if (collectedValues!.Remove(parameterConstructor.Parameters[i].Name!, out var parameter))
                        args[i] = parameter;
#endif
                }

                if (targetTypeDetail.Type.IsValueType)
                    target = (TTarget?)parameterConstructor.CreatorBoxed(args);
                else
                    target = parameterConstructor.Creator(args);

                foreach (var remaining in collectedValues!)
                {
                    if (membersByName!.TryGetValue(remaining.Key, out var member))
                    {
                        member.Converter.CollectedValuesSetter(target!, remaining.Value);
                    }
                }

                ReturnCollectedValues(collectedValues!);
            }

            return target;
        }
    }
}