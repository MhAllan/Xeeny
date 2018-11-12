using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xeeny.Serialization
{
    public abstract class SerializerBase : ISerializer
    {
        public virtual Encoding Encoding { get; set; } = Encoding.UTF8;

        public abstract void Serialize(Stream stream, object obj);
        public abstract object Deserialize(Type type, Stream stream);

        public virtual T Deserialize<T>(Stream stream)
        {
            return (T)Deserialize(typeof(T), stream);
        }

        public virtual object Deserialize(Type type, byte[] array)
        {
            using (var ms = new MemoryStream(array))
            {
                ms.Position = 0;
                return Deserialize(type, ms);
            }
        }

        public virtual T Deserialize<T>(byte[] array)
        {
            return (T)Deserialize(typeof(T), array);
        }

        public virtual object Deserialize(Type type, string str)
        {
            var bytes = Encoding.GetBytes(str);
            return Deserialize(type, bytes);
        }

        public virtual T Deserialize<T>(string str)
        {
            return (T)Deserialize(typeof(T), str);
        }

        public virtual byte[] SerializeToArray(object obj)
        {
            using (var ms = new MemoryStream())
            {
                Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public virtual string SerializeToString(object obj)
        {
            using (var ms = new MemoryStream())
            {
                Serialize(ms, obj);
                var bytes = ms.ToArray();
                return Encoding.GetString(bytes);
            }
        }
    }
}
