using System;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Transports.Buffers
{
    static class BufferHelper
    {
        public static byte[] GetSubArray(byte[] src, int offset, int count)
        {
            var result = new byte[count];
            Buffer.BlockCopy(src, offset, result, 0, count);

            return result;
        }

        public static void Copy(byte[] src, byte[] dest)
        {
            Buffer.BlockCopy(src, 0, dest, 0, src.Length);
        }

        public static void Copy(ArraySegment<byte> src, byte[] dest)
        {
            Buffer.BlockCopy(src.Array, 0, dest, 0, src.Count);
        }

        public static void CopyToIndex(byte[] src, byte[] dest, int destIndex)
        {
            Buffer.BlockCopy(src, 0, dest, destIndex, src.Length);
        }

        public static void CopyToIndex(ArraySegment<byte> src, byte[] dest, int destIndex)
        {
            Buffer.BlockCopy(src.Array, src.Offset, dest, destIndex, src.Count);
        }
    }
}
