using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xeeny.Serialization
{
    public class JsonSerializer : SerializerBase
    {
        Newtonsoft.Json.JsonSerializer _serializer;

        public JsonSerializer(JsonSerializerSettings settings)
        {
            _serializer = Newtonsoft.Json.JsonSerializer.Create(settings);
        }

        public override object Deserialize(Type type, Stream stream)
        {
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return _serializer.Deserialize(jsonReader, type);
            }
        }

        public override void Serialize(Stream stream, object obj)
        {
            using (var writer = new StreamWriter(stream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                _serializer.Serialize(jsonWriter, obj);

                jsonWriter.Flush();
                writer.Flush();
            }
        }
    }
}