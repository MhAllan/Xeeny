using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xeeny.Serialization
{
    public interface ISerializer
    {
        Encoding Encoding { get; }

        void Serialize(Stream stream, object obj);
        object Deserialize(Type type, Stream stream);
        T Deserialize<T>(Stream stream);

        byte[] SerializeToArray(object obj);
        object Deserialize(Type type, byte[] array);
        T Deserialize<T>(byte[] array);

        string SerializeToString(object obj);
        object Deserialize(Type type, string str);
        T Deserialize<T>(string str);
    }
}
