using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xeeny.Serialization
{
    public interface ISerializer
    {
        byte[] Serialize(object obj);
        T Deserialize<T>(byte[] data);
        object Deserialize(Type type, byte[] data);
    }
}
