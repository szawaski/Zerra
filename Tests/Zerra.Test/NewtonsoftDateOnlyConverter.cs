// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Test
{
    public class NewtonsoftDateOnlyConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateOnly) || objectType == typeof(DateOnly?);
        }

        public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (objectType == typeof(DateOnly?))
            {
                if (reader.Value is null)
                    return null;
                return DateOnly.Parse((string)reader.Value);
            }
            else
            {
                return DateOnly.Parse((string)reader.Value);
            }
        }

        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(((DateOnly)value).ToString("yyyy-MM-dd"));
            }
        }
    }
}
