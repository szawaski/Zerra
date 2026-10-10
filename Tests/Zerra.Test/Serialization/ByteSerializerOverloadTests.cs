// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Serialization.Bytes;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Serialization
{
    public class ByteSerializerOverloadTests
    {
        private static readonly SimpleModel model = new() { Value1 = 42, Value2 = "Hello" };

        public ByteSerializerOverloadTests()
        {
#if DEBUG
            Zerra.Serialization.Bytes.IO.ByteReader.Testing = true;
            Zerra.Serialization.Bytes.IO.ByteWriter.Testing = true;
#endif
        }

        private static void AssertModel(object? value)
        {
            var result = Assert.IsType<SimpleModel>(value);
            Assert.Equal(42, result.Value1);
            Assert.Equal("Hello", result.Value2);
        }

        private static byte[] WriteToBytes(Action<Stream> write)
        {
            using var stream = new MemoryStream();
            write(stream);
            return stream.ToArray();
        }

        [Fact]
        public async Task Serialize()
        {
            var expected = ByteSerializer.Serialize(model);

            Assert.Equal(expected, ByteSerializer.Serialize((object)model));
            Assert.Equal(expected, ByteSerializer.Serialize(model, typeof(SimpleModel)));
            Assert.Equal(expected, WriteToBytes(x => ByteSerializer.Serialize(x, model)));
            Assert.Equal(expected, WriteToBytes(x => ByteSerializer.Serialize(x, (object)model)));
            Assert.Equal(expected, WriteToBytes(x => ByteSerializer.Serialize(x, model, typeof(SimpleModel))));

            using var stream1 = new MemoryStream();
            await ByteSerializer.SerializeAsync(stream1, model, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(expected, stream1.ToArray());
            using var stream2 = new MemoryStream();
            await ByteSerializer.SerializeAsync(stream2, (object)model, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(expected, stream2.ToArray());
            using var stream3 = new MemoryStream();
            await ByteSerializer.SerializeAsync(stream3, model, typeof(SimpleModel), cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(expected, stream3.ToArray());
        }

        [Fact]
        public async Task Deserialize()
        {
            var bytes = ByteSerializer.Serialize(model);

            AssertModel(ByteSerializer.Deserialize<SimpleModel>(bytes));
            AssertModel(ByteSerializer.Deserialize(bytes, typeof(SimpleModel)));
            AssertModel(ByteSerializer.Deserialize<SimpleModel>(new MemoryStream(bytes)));
            AssertModel(ByteSerializer.Deserialize(new MemoryStream(bytes), typeof(SimpleModel)));
            AssertModel(await ByteSerializer.DeserializeAsync<SimpleModel>(new MemoryStream(bytes), cancellationToken: TestContext.Current.CancellationToken));
            AssertModel(await ByteSerializer.DeserializeAsync(new MemoryStream(bytes), typeof(SimpleModel), cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Empty()
        {
            var token = TestContext.Current.CancellationToken;
            Assert.Null(ByteSerializer.Deserialize<SimpleModel>(Array.Empty<byte>()));
            Assert.Null(ByteSerializer.Deserialize(Array.Empty<byte>(), typeof(SimpleModel)));
            Assert.Null(ByteSerializer.Deserialize<SimpleModel>(new MemoryStream()));
            Assert.Null(ByteSerializer.Deserialize(new MemoryStream(), typeof(SimpleModel)));
            Assert.Null(await ByteSerializer.DeserializeAsync<SimpleModel>(new MemoryStream(), cancellationToken: token));
            Assert.Null(await ByteSerializer.DeserializeAsync(new MemoryStream(), typeof(SimpleModel), cancellationToken: token));
        }

        [Fact]
        public async Task Truncated_Throws()
        {
            var bytes = ByteSerializer.Serialize(model).AsSpan()[..^2].ToArray();
            var token = TestContext.Current.CancellationToken;

            _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize<SimpleModel>(bytes));
            _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize(bytes, typeof(SimpleModel)));
            _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize<SimpleModel>(new MemoryStream(bytes)));
            _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize(new MemoryStream(bytes), typeof(SimpleModel)));
            _ = await Assert.ThrowsAsync<EndOfStreamException>(() => ByteSerializer.DeserializeAsync<SimpleModel>(new MemoryStream(bytes), cancellationToken: token));
            _ = await Assert.ThrowsAsync<EndOfStreamException>(() => ByteSerializer.DeserializeAsync(new MemoryStream(bytes), typeof(SimpleModel), cancellationToken: token));
        }

        [Fact]
        public async Task NullArguments_Throw()
        {
            var bytes = ByteSerializer.Serialize(model);
            var token = TestContext.Current.CancellationToken;

            _ = Assert.Throws<ArgumentNullException>(() => ByteSerializer.Deserialize(bytes, null!));
            _ = Assert.Throws<ArgumentNullException>(() => ByteSerializer.Deserialize(new MemoryStream(bytes), null!));
            _ = Assert.Throws<ArgumentNullException>(() => ByteSerializer.Deserialize<SimpleModel>((Stream)null!));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => ByteSerializer.DeserializeAsync(new MemoryStream(bytes), null!, cancellationToken: token));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => ByteSerializer.DeserializeAsync<SimpleModel>(null!, cancellationToken: token));

            _ = Assert.Throws<ArgumentNullException>(() => ByteSerializer.Serialize(model, (Type)null!));
            _ = Assert.Throws<ArgumentNullException>(() => ByteSerializer.Serialize(new MemoryStream(), model, (Type)null!));
            _ = Assert.Throws<ArgumentNullException>(() => ByteSerializer.Serialize<SimpleModel>(null!, model));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => ByteSerializer.SerializeAsync(new MemoryStream(), model, (Type)null!, cancellationToken: token));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => ByteSerializer.SerializeAsync<SimpleModel>(null!, model, cancellationToken: token));
            _ = Assert.Throws<ArgumentNullException>(() => ByteSerializer.Serialize(null!, (object)model));
            _ = Assert.Throws<ArgumentNullException>(() => ByteSerializer.Serialize(null!, (object)model, typeof(SimpleModel)));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => ByteSerializer.SerializeAsync(null!, (object)model, cancellationToken: token));
            _ = await Assert.ThrowsAsync<ArgumentNullException>(() => ByteSerializer.SerializeAsync(null!, (object)model, typeof(SimpleModel), cancellationToken: token));
        }

        [Fact]
        public async Task TrailingBytes_Throw()
        {
            var token = TestContext.Current.CancellationToken;
            var small = ByteSerializer.Serialize(model).Concat(new byte[] { 1, 2, 3 }).ToArray();
            //past the stream buffer, so the extra bytes come in a later read
            var large = ByteSerializer.Serialize(new string('x', 40_000)).Concat(new byte[] { 1 }).ToArray();

            _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize<SimpleModel>(small));
            _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize(small, typeof(SimpleModel)));
            foreach (var (bytes, type) in new[] { (small, typeof(SimpleModel)), (large, typeof(string)) })
            {
                _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize(new MemoryStream(bytes), type));
                _ = await Assert.ThrowsAsync<EndOfStreamException>(() => ByteSerializer.DeserializeAsync(new MemoryStream(bytes), type, cancellationToken: token));
            }
            _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize<SimpleModel>(new MemoryStream(small)));
            _ = await Assert.ThrowsAsync<EndOfStreamException>(() => ByteSerializer.DeserializeAsync<SimpleModel>(new MemoryStream(small), cancellationToken: token));
            _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize<string>(new MemoryStream(large)));
            _ = await Assert.ThrowsAsync<EndOfStreamException>(() => ByteSerializer.DeserializeAsync<string>(new MemoryStream(large), cancellationToken: token));
        }

        [Fact]
        public async Task NullObject_WritesNothingToStream()
        {
            var token = TestContext.Current.CancellationToken;
            using var stream = new MemoryStream();

            ByteSerializer.Serialize<SimpleModel>(stream, null);
            ByteSerializer.Serialize(stream, (object?)null);
            ByteSerializer.Serialize(stream, null, typeof(SimpleModel));
            await ByteSerializer.SerializeAsync<SimpleModel>(stream, null, cancellationToken: token);
            await ByteSerializer.SerializeAsync(stream, (object?)null, cancellationToken: token);
            await ByteSerializer.SerializeAsync(stream, null, typeof(SimpleModel), cancellationToken: token);

            Assert.Equal(0, stream.Length);
        }

        [Theory]
        [InlineData(typeof(IBasicModel))]
        [InlineData(typeof(object))]
        public async Task UseTypes_RootAsBaseType(Type rootType)
        {
            var options = new ByteSerializerOptions() { UseTypes = true };
            var token = TestContext.Current.CancellationToken;

            var bytes = ByteSerializer.Serialize((object)model, rootType, options);
            Assert.Equal(bytes, WriteToBytes(x => ByteSerializer.Serialize(x, (object)model, rootType, options)));
            using (var stream = new MemoryStream())
            {
                await ByteSerializer.SerializeAsync(stream, (object)model, rootType, options, token);
                Assert.Equal(bytes, stream.ToArray());
            }

            AssertModel(ByteSerializer.Deserialize(bytes, rootType, options));
            AssertModel(ByteSerializer.Deserialize(new MemoryStream(bytes), rootType, options));
            AssertModel(await ByteSerializer.DeserializeAsync(new MemoryStream(bytes), rootType, options, token));

            var interfaceBytes = ByteSerializer.Serialize<IBasicModel>(model, options);
            AssertModel(ByteSerializer.Deserialize<IBasicModel>(interfaceBytes, options));
            AssertModel(ByteSerializer.Deserialize<IBasicModel>(new MemoryStream(interfaceBytes), options));
            AssertModel(await ByteSerializer.DeserializeAsync<IBasicModel>(new MemoryStream(interfaceBytes), options, token));

            Assert.Null(ByteSerializer.Deserialize(ByteSerializer.Serialize((object?)null, rootType, options), rootType, options));
        }

        [Fact]
        public void Depth_PastTheLimit_Throws()
        {
            var deep = JsonSerializerOverloadTests.DepthModel.Create(40);
            _ = Assert.Throws<StackOverflowException>(() => ByteSerializer.Serialize(deep));
            _ = Assert.Throws<StackOverflowException>(() => ByteSerializer.Serialize((object)deep, typeof(JsonSerializerOverloadTests.DepthModel)));
        }

        [Fact]
        public void RootDeclaredAsInterface_ReadsIntoAnImplementation()
        {
            var bytes = ByteSerializer.Serialize(model);
            var result = Assert.IsAssignableFrom<JsonSerializerOverloadTests.ISimpleValues>(ByteSerializer.Deserialize(bytes, typeof(JsonSerializerOverloadTests.ISimpleValues)));
            Assert.Equal(42, result.Value1);
            Assert.Equal("Hello", result.Value2);
            Assert.Equal(42, ByteSerializer.Deserialize<JsonSerializerOverloadTests.ISimpleValues>(bytes)!.Value1);
        }

        public sealed class SimpleValues : JsonSerializerOverloadTests.ISimpleValues
        {
            public int Value1 { get; set; }
            public string? Value2 { get; set; }
        }
        public sealed class SimpleValuesWithExtra : JsonSerializerOverloadTests.ISimpleValues
        {
            public int Extra { get; set; }
            public int Value1 { get; set; }
            public string? Value2 { get; set; }
        }
        public sealed class DeclaredInterfaceModel
        {
            public JsonSerializerOverloadTests.ISimpleValues? Single { get; set; }
            public List<JsonSerializerOverloadTests.ISimpleValues?>? Items { get; set; }
            public JsonSerializerOverloadTests.ISimpleValues?[]? Array { get; set; }
            public int After { get; set; }
        }

        [Fact]
        public void DeclaredAsInterface_WithoutTypes_RoundTrips()
        {
            foreach (var value in new JsonSerializerOverloadTests.ISimpleValues[] { new SimpleValues() { Value1 = 42, Value2 = "Hello" }, new SimpleValuesWithExtra() { Extra = 9, Value1 = 42, Value2 = "Hello" } })
            {
                var model = new DeclaredInterfaceModel() { Single = value, Items = [value, null], Array = [null, value], After = 7 };
                foreach (var options in new[] { new ByteSerializerOptions(), new ByteSerializerOptions() { IndexType = ByteSerializerIndexType.MemberNames } })
                {
                    var named = ByteSerializer.Deserialize<DeclaredInterfaceModel>(ByteSerializer.Serialize(model, options), options)!;
                    Assert.Equal(7, named.After);
                    AssertValues(named.Single);
                    AssertValues(named.Items![0]);
                    Assert.Null(named.Items[1]);
                    AssertValues(named.Array![1]);
                }
                var result = ByteSerializer.Deserialize<DeclaredInterfaceModel>(ByteSerializer.Serialize(model))!;
                Assert.Equal(7, result.After);
                AssertValues(result.Single);
                Assert.Equal(2, result.Items!.Count);
                AssertValues(result.Items[0]);
                Assert.Null(result.Items[1]);
                Assert.Null(result.Array![0]);
                AssertValues(result.Array[1]);

                AssertValues(ByteSerializer.Deserialize<JsonSerializerOverloadTests.ISimpleValues>(ByteSerializer.Serialize(value)));
                AssertValues(ByteSerializer.Deserialize<JsonSerializerOverloadTests.ISimpleValues>(ByteSerializer.Serialize<JsonSerializerOverloadTests.ISimpleValues>(value)));
                AssertValues(ByteSerializer.Deserialize(ByteSerializer.Serialize((object)value, typeof(JsonSerializerOverloadTests.ISimpleValues)), typeof(JsonSerializerOverloadTests.ISimpleValues)));
            }

            //get only and set only members of the interface
            List<ITestInterface> list = [new TestInterfaceImplemented() { Property1 = 5, Property2 = 6, Property3 = 7 }];
            var listResult = Assert.Single(ByteSerializer.Deserialize<List<ITestInterface>>(ByteSerializer.Serialize(list))!);
            Assert.Equal(5, listResult.Property1);
            Assert.Equal(6, listResult.Property2);

            static void AssertValues(object? value)
            {
                var values = Assert.IsAssignableFrom<JsonSerializerOverloadTests.ISimpleValues>(value);
                Assert.Equal(42, values.Value1);
                Assert.Equal("Hello", values.Value2);
            }
        }

        [Fact]
        public async Task Stream_ValueEndingAtTheBufferEnd()
        {
            //the stream is read in 8KB blocks, around that size the value can end exactly where a block does
            var token = TestContext.Current.CancellationToken;
            for (var size = 8170; size < 8200; size++)
            {
                var bytes = ByteSerializer.Serialize(new byte[size]);
                var withTrailing = bytes.Concat(new byte[] { 1 }).ToArray();

                Assert.Equal(size, ByteSerializer.Deserialize<byte[]>(new MemoryStream(bytes))!.Length);
                Assert.Equal(size, ((byte[])ByteSerializer.Deserialize(new MemoryStream(bytes), typeof(byte[]))!).Length);
                Assert.Equal(size, (await ByteSerializer.DeserializeAsync<byte[]>(new MemoryStream(bytes), cancellationToken: token))!.Length);
                Assert.Equal(size, ((byte[])(await ByteSerializer.DeserializeAsync(new MemoryStream(bytes), typeof(byte[]), cancellationToken: token))!).Length);

                _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize<byte[]>(new MemoryStream(withTrailing)));
                _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize(new MemoryStream(withTrailing), typeof(byte[])));
                _ = await Assert.ThrowsAsync<EndOfStreamException>(() => ByteSerializer.DeserializeAsync<byte[]>(new MemoryStream(withTrailing), cancellationToken: token));
                _ = await Assert.ThrowsAsync<EndOfStreamException>(() => ByteSerializer.DeserializeAsync(new MemoryStream(withTrailing), typeof(byte[]), cancellationToken: token));
            }

            //a value needing more than what's left of the stream
            var truncated = ByteSerializer.Serialize(new byte[20000]).AsSpan(0, 10000).ToArray();
            _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize<byte[]>(new MemoryStream(truncated)));
            _ = Assert.Throws<EndOfStreamException>(() => ByteSerializer.Deserialize(new MemoryStream(truncated), typeof(byte[])));
            _ = await Assert.ThrowsAsync<EndOfStreamException>(() => ByteSerializer.DeserializeAsync<byte[]>(new MemoryStream(truncated), cancellationToken: token));
            _ = await Assert.ThrowsAsync<EndOfStreamException>(() => ByteSerializer.DeserializeAsync(new MemoryStream(truncated), typeof(byte[]), cancellationToken: token));
        }

        public sealed class PaddedSetsModel
        {
            public byte[]? Pad { get; set; }
            public HashSet<bool>? Bools { get; set; }
            public HashSet<bool?>? NullableBools { get; set; }
            public HashSet<byte>? Bytes { get; set; }
            public HashSet<sbyte>? SBytes { get; set; }
            public HashSet<byte?>? NullableBytes { get; set; }
            public HashSet<sbyte?>? NullableSBytes { get; set; }
        }

        [Fact]
        public async Task Stream_OneByteSetsAcrossTheBufferEnd()
        {
            var token = TestContext.Current.CancellationToken;
            for (var size = 8150; size < 8200; size++)
            {
                var model = new PaddedSetsModel()
                {
                    Pad = new byte[size],
                    Bools = [true, false],
                    NullableBools = [true, null, false],
                    Bytes = [1, 2, 3, 4, 5, 6, 7, 8],
                    SBytes = [-1, 0, 1, 2, 3, 4, 5, 6],
                    NullableBytes = [1, null, 3, 4, 5, 6, 7, 8],
                    NullableSBytes = [-1, null, 1, 2, 3, 4, 5, 6],
                };
                var bytes = ByteSerializer.Serialize(model);
                foreach (var result in new[] { ByteSerializer.Deserialize<PaddedSetsModel>(new MemoryStream(bytes))!, (await ByteSerializer.DeserializeAsync<PaddedSetsModel>(new MemoryStream(bytes), cancellationToken: token))! })
                {
                    Assert.Equal(size, result.Pad!.Length);
                    Assert.Equal(model.Bools, result.Bools);
                    Assert.Equal(model.NullableBools, result.NullableBools);
                    Assert.Equal(model.Bytes, result.Bytes);
                    Assert.Equal(model.SBytes, result.SBytes);
                    Assert.Equal(model.NullableBytes, result.NullableBytes);
                    Assert.Equal(model.NullableSBytes, result.NullableSBytes);
                }
            }
        }
    }
}
