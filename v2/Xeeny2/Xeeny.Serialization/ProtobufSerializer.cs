using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xeeny.Serialization
{
    public class ProtobufSerializer : SerializerBase
    {
        public override object Deserialize(Type type, Stream stream)
        {
            return ProtoBuf.Serializer.Deserialize(type, stream);
        }

        public override void Serialize(Stream stream, object obj)
        {
            ProtoBuf.Serializer.Serialize(stream, obj);
        }
    }
}
