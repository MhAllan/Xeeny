using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Serialization
{
    class JsonSerializer : ISerializer
    {
        Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();

        public object Deserialize(Type type, byte[] data)
        {
            using(var ms = new System.IO.MemoryStream(data))
            using (var reader = new System.IO.StreamReader(ms))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return serializer.Deserialize(jsonReader, type);
            }
        }

        public T Deserialize<T>(byte[] data)
        {
            using (var ms = new System.IO.MemoryStream(data))
            using (var reader = new System.IO.StreamReader(ms))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return serializer.Deserialize<T>(jsonReader);
            }
        }

        public byte[] Serialize(object obj)
        {
            using (var ms = new System.IO.MemoryStream())
            using (var writer = new System.IO.StreamWriter(ms))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                serializer.Serialize(jsonWriter, obj);
                jsonWriter.Flush();

                return ms.ToArray();
            }
        }
    }
}
