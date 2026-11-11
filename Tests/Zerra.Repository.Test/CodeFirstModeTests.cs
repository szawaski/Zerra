// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Memory;

namespace Zerra.Repository.Test
{
    public class CodeFirstModeTests
    {
        [Entity("CodeFirstMode")]
        public sealed class Model
        {
            [Identity]
            public Guid ID { get; set; }
        }

        [Fact]
        public void NoActionThrows()
        {
            var engine = new MemoryEngine();
            _ = Assert.Throws<ArgumentException>(() => CodeFirstGeneration.Generate(engine, CodeFirstMode.None, [typeof(Model)]));
            _ = Assert.Throws<ArgumentException>(() => CodeFirstGeneration.Generate(engine, CodeFirstMode.Preview, [typeof(Model)]));
        }

        [Fact]
        public void EachActionRuns()
        {
            var engine = new MemoryEngine();
            CodeFirstGeneration.Generate(engine, CodeFirstMode.CreateDataStore, [typeof(Model)]);
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Columns, [typeof(Model)]);
            CodeFirstGeneration.Generate(engine, CodeFirstMode.Constraints, [typeof(Model)]);
            CodeFirstGeneration.Generate(engine, CodeFirstMode.DropColumns, [typeof(Model)]);
            CodeFirstGeneration.Generate(engine, CodeFirstMode.DropColumns | CodeFirstMode.Preview, [typeof(Model)]);
        }
    }
}
