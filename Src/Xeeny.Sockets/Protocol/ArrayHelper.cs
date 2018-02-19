using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets.Protocol
{
    static class ArrayHelper
    {
        public static void Copy(byte[] src, byte[] dest)
        {
            Array.Copy(src, 0, dest, 0, src.Length);
        }

        public static void Copy(ArraySegment<byte> src, byte[] dest)
        {
            Array.Copy(src.Array, 0, dest, 0, src.Count);
        }

        public static void CopyToIndex(byte[] src, byte[] dest, int destIndex)
        {
            Array.Copy(src, 0, dest, destIndex, src.Length);
        }

        public static void CopyToIndex(ArraySegment<byte> src, byte[] dest, int destIndex)
        {
            Array.Copy(src.Array, src.Offset, dest, destIndex, src.Count);
        }
    }
}
