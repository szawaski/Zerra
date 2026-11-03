// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// The memory a model holds while a stream gathers a chunk of them: the fixed size of its columns, plus the text and bytes it has, so a chunk is as many models as fit its budget.
    /// </summary>
    internal sealed class LinqStreamSize
    {
        private const long objectBytes = 24;

        private readonly ModelDetail model;
        private readonly long fixedBytes;
        private readonly ModelMemberDetail[] sized;
        private LinqStreamSize?[]? related;

        public LinqStreamSize(ModelDetail model)
        {
            this.model = model;
            var fixedBytes = objectBytes;
            var sized = new List<ModelMemberDetail>();
            foreach (var member in model.Members)
            {
                if (member.IsRelated)
                    continue;
                if (member.Type == typeof(string) || member.Type == typeof(byte[]))
                {
                    sized.Add(member);
                    continue;
                }
                fixedBytes += member.EnumType is not null ? 8 : member.ActualCoreType switch
                {
                    CoreType.Boolean or CoreType.Byte or CoreType.SByte => 1,
                    CoreType.Int16 or CoreType.UInt16 or CoreType.Char => 2,
                    CoreType.Int32 or CoreType.UInt32 or CoreType.Single or CoreType.DateOnly => 4,
                    CoreType.Decimal or CoreType.Guid or CoreType.DateTimeOffset => 16,
                    _ => 8,
                };
            }
            this.fixedBytes = fixedBytes;
            this.sized = sized.ToArray();
        }

        /// <summary>The bytes of a model's own columns.</summary>
        public long Columns(object model)
        {
            var bytes = fixedBytes;
            foreach (var member in sized)
            {
                switch (member.GetterBoxed(model))
                {
                    case string text:
                        bytes += objectBytes + text.Length * 2;
                        break;
                    case byte[] data:
                        bytes += objectBytes + data.Length;
                        break;
                }
            }
            return bytes;
        }

        /// <summary>The bytes of a model's columns and of the related models it has loaded, one level down.</summary>
        public long WithRelations(object model) => Columns(model) + Relations(model);

        /// <summary>The bytes of the related models a model has loaded, one level down.</summary>
        public long Relations(object model)
        {
            var bytes = 0L;
            var members = this.model.RelatedMembers;
            for (var i = 0; i < members.Count; i++)
            {
                var member = members[i];
                var value = member.GetterBoxed(model);
                if (value is null)
                    continue;
                var size = Related(i);
                if (member.IsEnumerable)
                {
                    foreach (var item in (IEnumerable)value)
                    {
                        if (item is not null)
                            bytes += size.Columns(item);
                    }
                }
                else
                {
                    bytes += size.Columns(value);
                }
            }
            return bytes;
        }

        /// <summary>The bytes of a model's columns before any is read, its text and bytes at their declared length, none without one.</summary>
        public long Estimate()
        {
            var bytes = fixedBytes;
            foreach (var member in sized)
                bytes += objectBytes + (member.DataSourcePrecisionLength ?? 0) * (member.Type == typeof(string) ? 2 : 1);
            return bytes;
        }

        /// <summary>The bytes of one row of each relation a graph reads, before any is loaded.</summary>
        public long EstimateRelations(Graph? graph)
        {
            if (graph is null)
                return 0;
            var bytes = 0L;
            var members = model.RelatedMembers;
            for (var i = 0; i < members.Count; i++)
            {
                if (graph.HasMemberExplicitly(members[i].Name))
                    bytes += Related(i).Estimate();
            }
            return bytes;
        }

        private LinqStreamSize Related(int index)
        {
            related ??= new LinqStreamSize?[model.RelatedMembers.Count];
            return related[index] ??= new LinqStreamSize(ModelAnalyzer.GetModel(model.RelatedMembers[index].ActualType));
        }
    }
}
