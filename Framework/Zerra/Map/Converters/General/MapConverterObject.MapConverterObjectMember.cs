// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Map.Converters;
using Zerra.Reflection;

namespace Zerra.Map
{
    partial class MapConverterObject<TSource, TTarget>
    {
        private sealed class MapConverterObjectMember
        {
            private readonly string memberKey;

            public readonly MemberDetail SourceMember;
            public readonly MemberDetail TargetMember;

            public MapConverterObjectMember(TypeDetail sourceTypeDetail, MemberDetail sourceMember, TypeDetail targetTypeDetail, MemberDetail targetMember)
            {
                this.memberKey = $"{sourceTypeDetail.Type.FullName}.{sourceMember.Name} to {targetTypeDetail.Type.FullName}.{targetMember.Name}";
                this.SourceMember = sourceMember;
                this.TargetMember = targetMember;
            }

            private MapConverter? converter = null;
            public MapConverter Converter
            {
                get
                {
                    if (converter is null)
                    {
                        lock (this)
                        {
                            converter ??= MapConverterFactory.Get(SourceMember.TypeDetail, TargetMember.TypeDetail, memberKey, SourceMember.HasGetter ? SourceMember.Getter : null, TargetMember.HasGetter ? TargetMember.Getter : null, TargetMember.HasSetter ? TargetMember.Setter : null);
                        }
                    }
                    return converter;
                }
            }

            private void SetterForConverterSetValues(object parent, object? value) => ((Dictionary<string, object?>)parent).Add(TargetMember.Name.TrimStart('_'), value);

            private MapConverter? converterSetValues;
            public MapConverter ConverterSetCollectedValues
            {
                get
                {
                    if (converterSetValues is null)
                    {
                        lock (this)
                        {
                            converterSetValues ??= MapConverterFactory.Get(SourceMember.TypeDetail, TargetMember.TypeDetail, $"{memberKey}_CollectedValues", SourceMember.HasGetter ? SourceMember.Getter : null, null, SetterForConverterSetValues);
                        }
                    }
                    return converterSetValues;
                }
            }

            //helps with debug
            public override sealed string ToString()
            {
                return TargetMember.Name;
            }
        }
    }
}