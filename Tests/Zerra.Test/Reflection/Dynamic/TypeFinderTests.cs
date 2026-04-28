using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection.Dynamic
{
    public class TypeFinderTests
    {
        #region GetTypeFromName

        [Fact]
        public void GetTypeFromName_AssemblyQualifiedName_ReturnsCorrectType()
        {
            var expected = typeof(int);
            var name = expected.AssemblyQualifiedName!;

            var result = TypeFinder.GetTypeFromName(name);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTypeFromName_FullName_ReturnsCorrectType()
        {
            var name = "System.Int32";

            var result = TypeFinder.GetTypeFromName(name);

            Assert.Equal(typeof(int), result);
        }

        [Fact]
        public void GetTypeFromName_StringType_ReturnsCorrectType()
        {
            var name = typeof(string).AssemblyQualifiedName!;

            var result = TypeFinder.GetTypeFromName(name);

            Assert.Equal(typeof(string), result);
        }

        [Fact]
        public void GetTypeFromName_GenericType_ReturnsCorrectType()
        {
            var expected = typeof(List<int>);
            var name = expected.AssemblyQualifiedName!;

            var result = TypeFinder.GetTypeFromName(name);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTypeFromName_NullablePrimitive_ReturnsCorrectType()
        {
            var expected = typeof(int?);
            var name = expected.AssemblyQualifiedName!;

            var result = TypeFinder.GetTypeFromName(name);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTypeFromName_NullOrWhitespace_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TypeFinder.GetTypeFromName(null!));
            Assert.Throws<ArgumentNullException>(() => TypeFinder.GetTypeFromName(""));
            Assert.Throws<ArgumentNullException>(() => TypeFinder.GetTypeFromName("   "));
        }

        [Fact]
        public void GetTypeFromName_UnknownType_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => TypeFinder.GetTypeFromName("This.Does.Not.Exist.Type"));
        }

        [Fact]
        public void GetTypeFromName_CalledTwice_ReturnsSameType()
        {
            var name = typeof(Guid).AssemblyQualifiedName!;

            var result1 = TypeFinder.GetTypeFromName(name);
            var result2 = TypeFinder.GetTypeFromName(name);

            Assert.Equal(result1, result2);
        }

        #endregion

        #region TryGetTypeFromName

        [Fact]
        public void TryGetTypeFromName_AssemblyQualifiedName_ReturnsTrueAndCorrectType()
        {
            var expected = typeof(DateTime);
            var name = expected.AssemblyQualifiedName!;

            var result = TypeFinder.TryGetTypeFromName(name, out var type);

            Assert.True(result);
            Assert.Equal(expected, type);
        }

        [Fact]
        public void TryGetTypeFromName_UnknownType_ReturnsFalse()
        {
            var result = TypeFinder.TryGetTypeFromName("This.Does.Not.Exist.Type", out var type);

            Assert.False(result);
            Assert.Null(type);
        }

        [Fact]
        public void TryGetTypeFromName_NullOrWhitespace_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TypeFinder.TryGetTypeFromName(null!, out _));
            Assert.Throws<ArgumentNullException>(() => TypeFinder.TryGetTypeFromName("", out _));
            Assert.Throws<ArgumentNullException>(() => TypeFinder.TryGetTypeFromName("   ", out _));
        }

        [Fact]
        public void TryGetTypeFromName_CalledTwice_ReturnsSameType()
        {
            var name = typeof(TimeSpan).AssemblyQualifiedName!;

            TypeFinder.TryGetTypeFromName(name, out var type1);
            TypeFinder.TryGetTypeFromName(name, out var type2);

            Assert.Equal(type1, type2);
        }

        #endregion

        #region Register

        [Fact]
        public void Register_Type_CanBeFoundByAssemblyQualifiedName()
        {
            var type = typeof(TypeFinderTestModel);
            TypeFinder.Register(type);

            var result = TypeFinder.GetTypeFromName(type.AssemblyQualifiedName!);

            Assert.Equal(type, result);
        }

        [Fact]
        public void Register_Type_CanBeFoundByFullName()
        {
            var type = typeof(TypeFinderTestModel);
            TypeFinder.Register(type);

            var result = TypeFinder.GetTypeFromName(type.FullName!);

            Assert.Equal(type, result);
        }

        [Fact]
        public void Register_CalledMultipleTimes_DoesNotDuplicate()
        {
            var type = typeof(TypeFinderTestModel);
            TypeFinder.Register(type);
            TypeFinder.Register(type);

            var result = TypeFinder.GetTypeFromName(type.AssemblyQualifiedName!);

            Assert.Equal(type, result);
        }

        #endregion
    }

    internal sealed class TypeFinderTestModel { }
}
