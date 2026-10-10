using System.Globalization;
using Xunit;
using Zerra.Reflection;

namespace Zerra.Test.Reflection
{
    public class TypeAnalyzerTests
    {
        #region GetTypeDetail

        [Fact]
        public void GetTypeDetail_SimpleType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer.GetTypeDetail(typeof(int));

            Assert.NotNull(result);
            Assert.Equal(typeof(int), result.Type);
        }

        [Fact]
        public void GetTypeDetail_StringType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer.GetTypeDetail(typeof(string));

            Assert.NotNull(result);
            Assert.Equal(typeof(string), result.Type);
        }

        [Fact]
        public void GetTypeDetail_ComplexType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer.GetTypeDetail(typeof(TypeAnalyzerTestModel));

            Assert.NotNull(result);
            Assert.Equal(typeof(TypeAnalyzerTestModel), result.Type);
        }

        [Fact]
        public void GetTypeDetail_CalledTwice_ReturnsSameInstance()
        {
            var result1 = TypeAnalyzer.GetTypeDetail(typeof(Guid));
            var result2 = TypeAnalyzer.GetTypeDetail(typeof(Guid));

            Assert.Same(result1, result2);
        }

        [Fact]
        public void GetTypeDetail_DifferentTypes_ReturnDifferentInstances()
        {
            var result1 = TypeAnalyzer.GetTypeDetail(typeof(int));
            var result2 = TypeAnalyzer.GetTypeDetail(typeof(string));

            Assert.NotEqual(result1.Type, result2.Type);
        }

        [Fact]
        public void GetTypeDetail_GenericType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer.GetTypeDetail(typeof(List<int>));

            Assert.NotNull(result);
            Assert.Equal(typeof(List<int>), result.Type);
        }

        [Fact]
        public void GetTypeDetail_NullableType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer.GetTypeDetail(typeof(int?));

            Assert.NotNull(result);
            Assert.Equal(typeof(int?), result.Type);
        }

        #endregion

        #region Convert<T>

        [Fact]
        public void ConvertGeneric_IntFromString_ReturnsCorrectValue()
        {
            var result = TypeAnalyzer.Convert<int>("42");

            Assert.Equal(42, result);
        }

        [Fact]
        public void ConvertGeneric_BoolFromInt_ReturnsCorrectValue()
        {
            var result = TypeAnalyzer.Convert<bool>(1);

            Assert.True(result);
        }

        [Fact]
        public void ConvertGeneric_StringFromInt_ReturnsCorrectValue()
        {
            var result = TypeAnalyzer.Convert<string>(123);

            Assert.Equal("123", result);
        }

        [Fact]
        public void ConvertGeneric_NullableInt_FromNull_ReturnsNull()
        {
            var result = TypeAnalyzer.Convert<int?>(null);

            Assert.Null(result);
        }

        [Fact]
        public void ConvertGeneric_NullableInt_FromValue_ReturnsValue()
        {
            var result = TypeAnalyzer.Convert<int?>(99);

            Assert.Equal(99, result);
        }

        [Fact]
        public void ConvertGeneric_NonCoreType_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => TypeAnalyzer.Convert<TypeAnalyzerTestModel>(new object()));
        }

        [Fact]
        public void ConvertGeneric_NullToNonNullableInt_ReturnsDefault()
        {
            var result = TypeAnalyzer.Convert<int>(null);

            Assert.Equal(0, result);
        }

        [Fact]
        public void ConvertGeneric_GuidFromString_ReturnsCorrectValue()
        {
            var guid = Guid.NewGuid();
            var result = TypeAnalyzer.Convert<Guid>(guid.ToString());

            Assert.Equal(guid, result);
        }

        [Fact]
        public void ConvertGeneric_DateTimeFromString_ReturnsCorrectValue()
        {
            var dt = new DateTime(2024, 6, 15);
            var result = TypeAnalyzer.Convert<DateTime>(dt.ToString());

            Assert.Equal(dt, result);
        }

        [Fact]
        public void ConvertGeneric_TimeSpanFromString_ReturnsCorrectValue()
        {
            var ts = new TimeSpan(1, 2, 3);
            var result = TypeAnalyzer.Convert<TimeSpan>(ts.ToString());

            Assert.Equal(ts, result);
        }

        [Fact]
        public void ConvertGeneric_IgnoresCurrentCulture()
        {
            var original = CultureInfo.CurrentCulture;
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NumberDecimalSeparator = ",";
            culture.NumberFormat.NumberGroupSeparator = ".";
            culture.NumberFormat.NegativeSign = "−";
            CultureInfo.CurrentCulture = culture;
            try
            {
                Assert.Equal(1.5, TypeAnalyzer.Convert<double>("1.5"));
                Assert.Equal(1.5f, TypeAnalyzer.Convert<float>("1.5"));
                Assert.Equal(-1.5m, TypeAnalyzer.Convert<decimal>("-1.5"));
                Assert.Equal(-42, TypeAnalyzer.Convert<int>("-42"));
                Assert.Equal("-1.5", TypeAnalyzer.Convert<string>(-1.5));
                Assert.Equal("-42", TypeAnalyzer.Convert<string>(-42));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void ConvertGeneric_DateTimeOffsetNullable_ReturnsDateTimeOffset()
        {
            var value = new DateTimeOffset(2026, 10, 6, 13, 45, 30, TimeSpan.FromHours(-5));

            Assert.Equal(value, TypeAnalyzer.Convert<DateTimeOffset?>(value.ToString("O")));
            Assert.Equal(value, TypeAnalyzer.Convert<DateTimeOffset?>(value));
        }

        [Fact]
        public void ConvertGeneric_DatesFromOtherDateTypes()
        {
            var dateTime = new DateTime(2026, 10, 6, 13, 45, 30);

#if !NETSTANDARD2_0
            Assert.Equal(new DateOnly(2026, 10, 6), TypeAnalyzer.Convert<DateOnly>(dateTime));
            Assert.Equal(new TimeOnly(13, 45, 30), TypeAnalyzer.Convert<TimeOnly>(dateTime));
            Assert.Equal(new TimeOnly(1, 2, 3), TypeAnalyzer.Convert<TimeOnly>(new TimeSpan(1, 2, 3)));
#endif
            Assert.Equal(new DateTimeOffset(dateTime), TypeAnalyzer.Convert<DateTimeOffset>(dateTime));
#if !NETSTANDARD2_0
            Assert.Equal(new DateOnly(2026, 10, 6), TypeAnalyzer.Convert<DateOnly?>("2026-10-06"));
#endif
            Assert.Equal(dateTime, TypeAnalyzer.Convert<DateTime>("2026-10-06T13:45:30"));
            Assert.Equal(dateTime, TypeAnalyzer.Convert<DateTime>(new DateTimeOffset(dateTime, TimeSpan.FromHours(-5))));
#if !NETSTANDARD2_0
            Assert.Equal(dateTime.Date, TypeAnalyzer.Convert<DateTime?>(new DateOnly(2026, 10, 6)));
#endif
        }

        #endregion

        #region Convert(object, Type)

        [Fact]
        public void ConvertByType_IntFromString_ReturnsCorrectValue()
        {
            var result = TypeAnalyzer.Convert("42", typeof(int));

            Assert.Equal(42, result);
        }

        [Fact]
        public void ConvertByType_NullToNonNullableInt_ReturnsDefault()
        {
            var result = TypeAnalyzer.Convert(null, typeof(int));

            Assert.Equal(0, result);
        }

        [Fact]
        public void ConvertByType_NullToNullableInt_ReturnsNull()
        {
            var result = TypeAnalyzer.Convert(null, typeof(int?));

            Assert.Null(result);
        }

        [Fact]
        public void ConvertByType_NonCoreType_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => TypeAnalyzer.Convert(new object(), typeof(TypeAnalyzerTestModel)));
        }

        #endregion

        #region Convert(object, CoreType)

        [Fact]
        public void ConvertByCoreType_NullToBoolean_ReturnsFalse()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.Boolean);

            Assert.Equal(false, result);
        }

        [Fact]
        public void ConvertByCoreType_NullToBooleanNullable_ReturnsNull()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.BooleanNullable);

            Assert.Null(result);
        }

        [Fact]
        public void ConvertByCoreType_NullToInt32_ReturnsZero()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.Int32);

            Assert.Equal(0, result);
        }

        [Fact]
        public void ConvertByCoreType_NullToInt32Nullable_ReturnsNull()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.Int32Nullable);

            Assert.Null(result);
        }

        [Fact]
        public void ConvertByCoreType_NullToString_ReturnsNull()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.String);

            Assert.Null(result);
        }

        [Theory]
        [InlineData(CoreType.Boolean, "true", true)]
        [InlineData(CoreType.Byte, "200", (byte)200)]
        [InlineData(CoreType.Int16, "1000", (short)1000)]
        [InlineData(CoreType.Int32, "42", 42)]
        [InlineData(CoreType.Int64, "9999", (long)9999)]
        [InlineData(CoreType.Double, "3.14", 3.14)]
        [InlineData(CoreType.String, "hello", "hello")]
        public void ConvertByCoreType_ValueFromString_ReturnsConvertedValue(CoreType coreType, object input, object expected)
        {
            var result = TypeAnalyzer.Convert(input, coreType);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ConvertByCoreType_GuidFromString_ReturnsCorrectGuid()
        {
            var guid = Guid.NewGuid();
            var result = TypeAnalyzer.Convert(guid.ToString(), CoreType.Guid);

            Assert.Equal(guid, result);
        }

        [Fact]
        public void ConvertByCoreType_NullToGuid_ReturnsEmptyGuid()
        {
            var result = TypeAnalyzer.Convert(null, CoreType.Guid);

            Assert.Equal(Guid.Empty, result);
        }

        [Fact]
        public void ConvertByCoreType_TimeSpanFromString_ReturnsCorrectTimeSpan()
        {
            var ts = new TimeSpan(2, 30, 0);
            var result = TypeAnalyzer.Convert(ts.ToString(), CoreType.TimeSpan);

            Assert.Equal(ts, result);
        }

#if !NETSTANDARD2_0
        [Fact]
        public void ConvertByCoreType_DateOnlyFromString_ReturnsCorrectDateOnly()
        {
#if !NETSTANDARD2_0
            var date = new DateOnly(2024, 1, 15);
            var result = TypeAnalyzer.Convert(date.ToString(), CoreType.DateOnly);
#endif

            Assert.Equal(date, result);
        }
#endif

#if !NETSTANDARD2_0
        [Fact]
        public void ConvertByCoreType_TimeOnlyFromString_ReturnsCorrectTimeOnly()
        {
#if !NETSTANDARD2_0
            var time = new TimeOnly(14, 30, 0);
            var result = TypeAnalyzer.Convert(time.ToString(), CoreType.TimeOnly);
#endif

            Assert.Equal(time, result);
        }
#endif

        #endregion
    }

    public class TypeAnalyzerGenericTests
    {
        #region GetTypeDetail<T>

        [Fact]
        public void GetTypeDetail_Int_ReturnsTypeDetailForInt()
        {
            var result = TypeAnalyzer<int>.GetTypeDetail();

            Assert.NotNull(result);
            Assert.Equal(typeof(int), result.Type);
        }

        [Fact]
        public void GetTypeDetail_String_ReturnsTypeDetailForString()
        {
            var result = TypeAnalyzer<string>.GetTypeDetail();

            Assert.NotNull(result);
            Assert.Equal(typeof(string), result.Type);
        }

        [Fact]
        public void GetTypeDetail_ComplexType_ReturnsTypeDetailForType()
        {
            var result = TypeAnalyzer<TypeAnalyzerTestModel>.GetTypeDetail();

            Assert.NotNull(result);
            Assert.Equal(typeof(TypeAnalyzerTestModel), result.Type);
        }

        [Fact]
        public void GetTypeDetail_CalledTwice_ReturnsSameInstance()
        {
            var result1 = TypeAnalyzer<Guid>.GetTypeDetail();
            var result2 = TypeAnalyzer<Guid>.GetTypeDetail();

            Assert.Same(result1, result2);
        }

        [Fact]
        public void GetTypeDetail_MatchesNonGenericTypeAnalyzer()
        {
            var genericResult = TypeAnalyzer<DateTime>.GetTypeDetail();
            var nonGenericResult = TypeAnalyzer.GetTypeDetail(typeof(DateTime));

            Assert.Same(genericResult, nonGenericResult);
        }

        [Fact]
        public void GetTypeDetail_NullableType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer<int?>.GetTypeDetail();

            Assert.NotNull(result);
            Assert.Equal(typeof(int?), result.Type);
        }

        [Fact]
        public void GetTypeDetail_GenericListType_ReturnsTypeDetail()
        {
            var result = TypeAnalyzer<List<string>>.GetTypeDetail();

            Assert.NotNull(result);
            Assert.Equal(typeof(List<string>), result.Type);
        }

        #endregion
    }

    public sealed class TypeAnalyzerTestModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        [Fact]
        public void Convert_NullToEveryCoreType()
        {
            foreach (var coreType in ((CoreType[])Enum.GetValues(typeof(CoreType))))
            {
                var result = TypeAnalyzer.Convert(null, coreType);
                if (coreType == CoreType.String || coreType.ToString().EndsWith("Nullable"))
                    Assert.Null(result);
                else
                    Assert.NotNull(result);
            }
        }

        [Fact]
        public void Convert_ValuesToNullableCoreTypes()
        {
            Assert.Equal(true, TypeAnalyzer.Convert("true", CoreType.BooleanNullable));
            Assert.Equal((byte)5, TypeAnalyzer.Convert("5", CoreType.ByteNullable));
            Assert.Equal((sbyte)-5, TypeAnalyzer.Convert("-5", CoreType.SByteNullable));
            Assert.Equal((ushort)5, TypeAnalyzer.Convert(5, CoreType.UInt16Nullable));
            Assert.Equal((short)-5, TypeAnalyzer.Convert(-5L, CoreType.Int16Nullable));
            Assert.Equal(5u, TypeAnalyzer.Convert("5", CoreType.UInt32Nullable));
            Assert.Equal(-5, TypeAnalyzer.Convert("-5", CoreType.Int32Nullable));
            Assert.Equal(5UL, TypeAnalyzer.Convert("5", CoreType.UInt64Nullable));
            Assert.Equal(-5L, TypeAnalyzer.Convert("-5", CoreType.Int64Nullable));
            Assert.Equal(1.5f, TypeAnalyzer.Convert("1.5", CoreType.SingleNullable));
            Assert.Equal(1.5d, TypeAnalyzer.Convert("1.5", CoreType.DoubleNullable));
            Assert.Equal(1.5m, TypeAnalyzer.Convert("1.5", CoreType.DecimalNullable));
            Assert.Equal('c', TypeAnalyzer.Convert("c", CoreType.CharNullable));
            Assert.Equal(new DateTime(2024, 1, 2), TypeAnalyzer.Convert("2024-01-02", CoreType.DateTimeNullable));
            Assert.Equal(new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero), TypeAnalyzer.Convert("2024-01-02T00:00:00+00:00", CoreType.DateTimeOffsetNullable));
            Assert.Equal(TimeSpan.FromMinutes(90), TypeAnalyzer.Convert("01:30:00", CoreType.TimeSpanNullable));
#if !NETSTANDARD2_0
            Assert.Equal(new DateOnly(2024, 1, 2), TypeAnalyzer.Convert("2024-01-02", CoreType.DateOnlyNullable));
            Assert.Equal(new TimeOnly(1, 30), TypeAnalyzer.Convert("01:30:00", CoreType.TimeOnlyNullable));
#endif
            var guid = Guid.NewGuid();
            Assert.Equal(guid, TypeAnalyzer.Convert(guid.ToString(), CoreType.GuidNullable));
            Assert.Equal(guid, TypeAnalyzer.Convert(guid, CoreType.Guid));
            Assert.Equal("5", TypeAnalyzer.Convert(5, CoreType.String));
        }

        [Fact]
        public void RuntimeTypes_InterfaceMembersAndMethods()
        {
            //types that aren't generated get their details at runtime, explicit interface implementations included
            var dictionaryDetail = typeof(Dictionary<int, string>).GetTypeDetail();
            var explicitAdd = Assert.Single(dictionaryDetail.Methods, x => x.IsExplicitFromInterface && x.Name == "System.Collections.IDictionary.Add");
            var dictionary = new Dictionary<int, string>();
            _ = explicitAdd.CallerBoxed!(dictionary, [1, "a"]);
            Assert.Equal("a", dictionary[1]);
            var explicitReadOnly = Assert.Single(dictionaryDetail.Members, x => x.IsExplicitFromInterface && x.Name.EndsWith(".IsReadOnly") && x.Name.Contains("Generic"));
            Assert.Equal(false, explicitReadOnly.GetterBoxed!(dictionary));
            Assert.Contains(dictionaryDetail.Methods, x => x.Name == "Add" && !x.IsExplicitFromInterface);

            //an interface gets the members and methods of the interfaces it extends
            var listDetail = typeof(IList<int>).GetTypeDetail();
            Assert.Contains(listDetail.Methods, x => x.Name == "Add");
            Assert.Contains(listDetail.Methods, x => x.Name == "GetEnumerator");
            Assert.Contains(listDetail.Members, x => x.Name == "Count");
            Assert.DoesNotContain(listDetail.Members, x => x.Name == "Item");

            //a span property can't be boxed but is still a member
            var readerDetail = typeof(System.Text.Json.Utf8JsonReader).GetTypeDetail();
            var valueSpan = Assert.Single(readerDetail.Members, x => x.Name == nameof(System.Text.Json.Utf8JsonReader.ValueSpan));
            Assert.Equal(typeof(ReadOnlySpan<byte>), valueSpan.Type);

            Assert.Empty(typeof(List<>).GetTypeDetail().Members);
        }


        [Fact]
        public void RuntimeTypes_PropertyGettersReturnThePropertyValue()
        {
            //Count is computed from more than its same named field, the getter must be the property's
            var dictionary = new Dictionary<int, string>() { { 1, "a" }, { 2, "b" }, { 3, "c" } };
            _ = dictionary.Remove(2);
            var count = typeof(Dictionary<int, string>).GetTypeDetail().GetMember(nameof(Dictionary<int, string>.Count));
            Assert.Equal(2, count.GetterBoxed!(dictionary));
            Assert.False(count.HasSetter);

            //an auto property still goes through its compiler backing field, so a get only one can be set
            var exception = typeof(System.Text.Json.JsonException).GetTypeDetail().GetMember(nameof(System.Text.Json.JsonException.Path));
            var instance = new System.Text.Json.JsonException();
            exception.SetterBoxed!(instance, "$.a");
            Assert.Equal("$.a", exception.GetterBoxed!(instance));
        }

        public unsafe interface IUnusualMembers
        {
            int* Pointer { get; }
            ref int Reference { get; }
#if !NETSTANDARD2_0
            protected int Hidden => 1;
#endif
        }
        public sealed unsafe class UnusualMembersModel : IUnusualMembers
        {
            private int value = 5;
            public int* PointerField;
            public int* PointerProperty { get => null; set { } }
            int* IUnusualMembers.Pointer => null;
            ref int IUnusualMembers.Reference => ref value;
            public int Value => value;
        }

        [Fact]
        public void RuntimeTypes_PointerAndRefMembers()
        {
            //pointers and refs can't be read or written through a delegate, pointer properties are left out
            var detail = typeof(UnusualMembersModel).GetTypeDetail();

            var pointerField = detail.GetMember(nameof(UnusualMembersModel.PointerField));
            Assert.Equal(typeof(int*), pointerField.Type);
            Assert.False(pointerField.HasGetter);
            Assert.Null(pointerField.GetterBoxed);
            Assert.False(pointerField.HasSetter);

            Assert.DoesNotContain(detail.Members, x => x.Name == nameof(UnusualMembersModel.PointerProperty));
            Assert.DoesNotContain(detail.Members, x => x.Name.EndsWith(".Pointer"));
            Assert.DoesNotContain(detail.Members, x => x.Name.EndsWith("Hidden"));

            var reference = Assert.Single(detail.Members, x => x.Name.EndsWith(".Reference"));
            Assert.True(reference.IsExplicitFromInterface);
            Assert.Equal(typeof(int).MakeByRefType(), reference.Type);
            Assert.False(reference.HasGetter);

            Assert.Equal(5, detail.GetMember(nameof(UnusualMembersModel.Value)).GetterBoxed!(new UnusualMembersModel()));
        }
    }
}
