// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Globalization;
using System.Text;
using Zerra.Repository.Linq;

namespace Zerra.Repository.MongoDB
{
    //reads a document of a statement's columns, named c0, c1, and so on, straight from the reply into a row, each value as its column's type,
    //without the BsonDocument and BsonValue the driver would make of it; a column the document leaves out, a value that was missing, stays null
    internal sealed class MongoDBRowSerializer : SerializerBase<object?[]>
    {
        private readonly Type[] types;
        private readonly int length;

        public MongoDBRowSerializer(LinqPlan plan)
        {
            var columns = plan.Columns;
            types = new Type[columns.Count];
            for (var i = 0; i < types.Length; i++)
            {
                var type = columns[i].Member?.Type ?? columns[i].Type;
                types[i] = Nullable.GetUnderlyingType(type) ?? type;
            }
            length = columns.Count + plan.Relations.Count;
        }

        public override object?[] Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var row = new object?[length];
            reader.ReadStartDocument();
            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                _ = reader.ReadName(ColumnNameDecoder.Instance);
                var index = ColumnNameDecoder.Index;
                if (index < 0 || index >= types.Length)
                {
                    reader.SkipValue();
                    continue;
                }
                row[index] = ReadValue(context, reader, types[index]);
            }
            reader.ReadEndDocument();
            return row;
        }

        private static object? ReadValue(BsonDeserializationContext context, IBsonReader reader, Type type)
        {
            switch (reader.CurrentBsonType)
            {
                case BsonType.Null:
                    reader.ReadNull();
                    return null;
                case BsonType.Undefined:
                    reader.ReadUndefined();
                    return null;
                case BsonType.String:
                    {
                        var text = reader.ReadString();
                        if (type == typeof(string))
                            return text;
                        if (type.IsEnum)
                            return EnumName.Parse(text, type);
                        if (type == typeof(char))
                            return text.Length == 0 ? '\0' : text[0];
                        return LinqMongoDBConverter.FromBson(new BsonString(text), type);
                    }
                case BsonType.Boolean:
                    {
                        var value = reader.ReadBoolean();
                        return type == typeof(bool) ? value : LinqMongoDBConverter.FromBson(value, type);
                    }
                case BsonType.Int32:
                    {
                        var value = reader.ReadInt32();
                        return type == typeof(int) ? value : Number(value, type);
                    }
                case BsonType.Int64:
                    {
                        var value = reader.ReadInt64();
                        if (type == typeof(long))
                            return value;
                        //a TimeSpan and a TimeOnly are stored as ticks
                        if (type == typeof(TimeSpan))
                            return new TimeSpan(value);
                        if (type == typeof(TimeOnly))
                            return new TimeOnly(value);
                        return Number(value, type);
                    }
                case BsonType.Double:
                    {
                        var value = reader.ReadDouble();
                        return type == typeof(double) ? value : Number(value, type);
                    }
                case BsonType.Decimal128:
                    {
                        var value = (decimal)reader.ReadDecimal128();
                        return type == typeof(decimal) ? value : Number(value, type);
                    }
                case BsonType.DateTime:
                    {
                        var date = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(reader.ReadDateTime());
                        if (type == typeof(DateTime))
                            return date;
                        if (type == typeof(DateTimeOffset))
                            return new DateTimeOffset(date);
                        if (type == typeof(DateOnly))
                            return DateOnly.FromDateTime(date);
                        return LinqMongoDBConverter.FromBson(new BsonDateTime(date), type);
                    }
                case BsonType.Binary:
                    {
                        var value = reader.ReadBinaryData();
                        if (type == typeof(Guid))
                            return value.ToGuid(GuidRepresentation.Standard);
                        if (type == typeof(byte[]))
                            return value.Bytes;
                        return LinqMongoDBConverter.FromBson(value, type);
                    }
                default:
                    return LinqMongoDBConverter.FromBson(BsonValueSerializer.Instance.Deserialize(context), type);
            }
        }

        //a number read as another type: a sum of ints is a long, an average a double, an enum computed from a number is the number
        private static object Number(object value, Type type)
        {
            if (type == typeof(object))
                return value;
            if (type.IsEnum)
                return Enum.ToObject(type, System.Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture));
            if (type == typeof(TimeSpan))
                return new TimeSpan(System.Convert.ToInt64(value, CultureInfo.InvariantCulture));
            if (type == typeof(TimeOnly))
                return new TimeOnly(System.Convert.ToInt64(value, CultureInfo.InvariantCulture));
            //a fraction to an integer keeps the whole part as a cast would
            if (value is double fraction && IsInteger(type))
                value = Math.Truncate(fraction);
            else if (value is decimal exact && IsInteger(type))
                value = Math.Truncate(exact);
            return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }
        private static bool IsInteger(Type type)
            => type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);

        //the index of a column from its name's bytes, without making a string of the name
        private sealed class ColumnNameDecoder : INameDecoder
        {
            public static readonly ColumnNameDecoder Instance = new();
            private static readonly string[] names = CreateNames();
            [ThreadStatic]
            public static int Index;

            private static string[] CreateNames()
            {
                var created = new string[128];
                for (var i = 0; i < created.Length; i++)
                    created[i] = "c" + i.ToString(CultureInfo.InvariantCulture);
                return created;
            }

            public string Decode(BsonStream stream, UTF8Encoding encoding)
            {
                var bytes = stream.ReadCStringBytes();
                var span = bytes.AsSpan();
                var index = -1;
                if (span.Length > 1 && span[0] == (byte)'c')
                {
                    index = 0;
                    for (var i = 1; i < span.Length; i++)
                    {
                        var digit = span[i] - (byte)'0';
                        if (digit < 0 || digit > 9 || index > 100_000)
                        {
                            index = -1;
                            break;
                        }
                        index = index * 10 + digit;
                    }
                }
                Index = index;
                return index >= 0 && index < names.Length ? names[index] : encoding.GetString(span);
            }

            public void Inform(string name) { }
        }
    }
}
