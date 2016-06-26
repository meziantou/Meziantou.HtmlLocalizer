using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace Meziantou.HtmlLocalizer
{
    internal class StringListConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var enumerable = value as IEnumerable<string>;
            if (enumerable == null)
                return;

            var s = string.Join(", ", enumerable);
            writer.WriteValue(s);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (existingValue == null)
            {
                existingValue = new List<string>();
            }

            if (reader.TokenType == JsonToken.String)
            {
                string value = (string) reader.Value;
                char[] delimiter = {','};
                string[] values = value.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);


                var collection = (ICollection<string>) existingValue;
                foreach (var s in values)
                {
                    var t = s.Trim();
                    if (string.IsNullOrEmpty(t))
                        continue;

                    if (collection.Contains(t))
                        continue;

                    collection.Add(t);
                }
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                serializer.Populate(reader, existingValue);
            }

            return existingValue;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ICollection<string>).GetTypeInfo().IsAssignableFrom(objectType);
        }
    }
}