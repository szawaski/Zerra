// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Test
{
    public class NewtonsoftTimeOnlyConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeOnly) || objectType == typeof(TimeOnly?);
        }

        public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (objectType == typeof(TimeOnly?))
            {
                if (reader.Value is null)
                    return null;
                return TimeOnly.Parse((string)reader.Value);
            }
            else
            {
                return TimeOnly.Parse((string)reader.Value);
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
                writer.WriteValue(((TimeOnly)value).ToString("HH:mm:ss.FFFFFFF"));
            }
        }
    }
}
