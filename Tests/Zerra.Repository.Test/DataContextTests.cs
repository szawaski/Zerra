// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Test
{
    public class DataContextTests
    {
        private sealed class StubEngine : IDataStoreEngine
        {
            private readonly bool valid;
            private readonly Action onValidate;
            public StubEngine(bool valid, Action onValidate)
            {
                this.valid = valid;
                this.onValidate = onValidate;
            }
            public bool ValidateDataSource()
            {
                onValidate();
                return valid;
            }
            public IDataStoreGenerationPlan BuildStoreGenerationPlan(bool create, bool update, bool delete, ICollection<ModelDetail> modelDetail) => throw new NotSupportedException();
        }

        private sealed class UnavailableContext : DataContext
        {
            public static int Validations;
            protected override IDataStoreEngine? GetEngine() => new StubEngine(false, static () => Interlocked.Increment(ref Validations));
        }

        private sealed class AvailableContext : DataContext
        {
            public static int Validations;
            protected override IDataStoreEngine? GetEngine() => new StubEngine(true, static () => Interlocked.Increment(ref Validations));
        }

        private sealed class FallbackSelector : DataContextSelector
        {
            protected override IEnumerable<DataContext> LoadDataContexts() => [new UnavailableContext(), new AvailableContext()];
        }

        [Fact]
        public void TryGetEngine_ValidatesOncePerContextType()
        {
            //providers create a new context each, a selector falling back shouldn't retry the unavailable source every time
            for (var i = 0; i < 3; i++)
            {
                Assert.True(new FallbackSelector().TryGetEngine(out var engine));
                Assert.NotNull(engine);

                Assert.False(new UnavailableContext().TryGetEngine(out var unavailable));
                Assert.Null(unavailable);
            }

            Assert.Equal(1, UnavailableContext.Validations);
            //once for its own type and once for the selector type that returned its engine
            Assert.Equal(2, AvailableContext.Validations);
        }
    }
}
