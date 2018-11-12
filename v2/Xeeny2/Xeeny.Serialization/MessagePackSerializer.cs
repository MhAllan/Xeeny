using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xeeny.Serialization
{
    public class MessagePackSerializer : SerializerBase
    {
        public override object Deserialize(Type type, Stream stream)
        {
            return SerializationContext.Default.GetSerializer(type).Unpack(stream);
        }

        public override void Serialize(Stream stream, object obj)
        {
            SerializationContext.Default.GetSerializer(obj.GetType()).Pack(stream, obj);
        }
    }
}
