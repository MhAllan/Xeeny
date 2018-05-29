using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xeeny.Serialization.Abstractions
{
    public interface ISerializer
    {
        void Serialize(Stream stream, object obj);
        byte[] SerializeToArray(object obj);
        string SerializeToString(object obj);

        object Deserialize(Type type, Stream stream);
        object Deserialize(Type type, byte[] bytes);
        object Deserialize(Type type, string str);

        T Deserialize<T>(Stream stream);
        T Deserialize<T>(byte[] bytes);
        T Deserialize<T>(string str);
    }
}
