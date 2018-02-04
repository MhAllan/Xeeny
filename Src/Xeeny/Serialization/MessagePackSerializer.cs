using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xeeny.Serialization
{
    class MessagePackSerializer : ISerializer
    {
        public T Deserialize<T>(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                var serializer = SerializationContext.Default.GetSerializer<T>();
                return serializer.Unpack(ms);
            }
        }

        public object Deserialize(Type type, byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                var serializer = SerializationContext.Default.GetSerializer(type);
                return serializer.Unpack(ms);
            }
        }

        public byte[] Serialize(object obj)
        {
            using (var ms = new MemoryStream())
            {
                var serializer = SerializationContext.Default.GetSerializer(obj.GetType());
                serializer.Pack(ms, obj);

                return ms.ToArray();
            }
        }
    }
}
