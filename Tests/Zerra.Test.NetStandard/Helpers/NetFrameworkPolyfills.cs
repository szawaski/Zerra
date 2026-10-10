// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

//what the tests shared from Zerra.Test use that .NET Framework doesn't have

namespace Zerra.Test.Helpers
{
    //DateOnly and TimeOnly don't exist here, so there's nothing for these to convert
    public class NewtonsoftDateOnlyConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType) => false;
        public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer) => throw new NotSupportedException();
        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer) => throw new NotSupportedException();
    }

    public class NewtonsoftTimeOnlyConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType) => false;
        public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer) => throw new NotSupportedException();
        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer) => throw new NotSupportedException();
    }
}

namespace System
{
    //for the ^ and .. operators
    internal readonly struct Index
    {
        private readonly int value;

        public Index(int value, bool fromEnd = false)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            this.value = fromEnd ? ~value : value;
        }

        public static Index Start => new(0);
        public static Index End => new(0, true);
        public int Value => value < 0 ? ~value : value;
        public bool IsFromEnd => value < 0;
        public int GetOffset(int length) => IsFromEnd ? length - ~value : value;
        public static implicit operator Index(int value) => new(value);
    }

    internal readonly struct Range
    {
        public Index Start { get; }
        public Index End { get; }

        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        public static Range StartAt(Index start) => new(start, Index.End);
        public static Range EndAt(Index end) => new(Index.Start, end);
        public static Range All => new(Index.Start, Index.End);

        public (int Offset, int Length) GetOffsetAndLength(int length)
        {
            var start = Start.GetOffset(length);
            var end = End.GetOffset(length);
            if ((uint)end > (uint)length || (uint)start > (uint)end)
                throw new ArgumentOutOfRangeException(nameof(length));
            return (start, end - start);
        }
    }
}

//no namespace, so the shared tests find these without a using
internal static class NetFrameworkPolyfills
{
    public static int Read(this Stream stream, Span<byte> buffer)
    {
        var array = new byte[buffer.Length];
        var read = stream.Read(array, 0, array.Length);
        array.AsSpan(0, read).CopyTo(buffer);
        return read;
    }

    public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
    {
        var array = buffer.ToArray();
        stream.Write(array, 0, array.Length);
    }

    public static async ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var array = new byte[buffer.Length];
        var read = await stream.ReadAsync(array, 0, array.Length, cancellationToken);
        array.AsMemory(0, read).CopyTo(buffer);
        return read;
    }

    public static async ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var array = buffer.ToArray();
        await stream.WriteAsync(array, 0, array.Length, cancellationToken);
    }

    //.NET Standard has no async dispose, so this disposes
    public static ValueTask DisposeAsync(this Stream stream)
    {
        stream.Dispose();
        return default;
    }

    public static Task<string> ReadToEndAsync(this StreamReader reader, CancellationToken cancellationToken) => reader.ReadToEndAsync();

    public static async Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)) != task)
            throw new TimeoutException();
        await task;
    }

    public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source) => source.OrderBy(x => x);

    public static int GetBytes(this System.Text.Encoding encoding, string chars, Span<byte> bytes)
    {
        var array = encoding.GetBytes(chars);
        array.CopyTo(bytes);
        return array.Length;
    }
}
