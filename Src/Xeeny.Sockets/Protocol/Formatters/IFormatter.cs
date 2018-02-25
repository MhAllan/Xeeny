using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Sockets.Protocol.Messages;
using Xeeny.Sockets.Protocol.Results;

namespace Xeeny.Sockets.Protocol.Formatters
{
    interface IFormatter: IDisposable
    {
        int MinMessageSize { get; }
        int MaxMessageSize { get; }

        ReadResult ReadMessage(byte[] buffer, int count);
        WriteResult WriteMessage(Message message, byte[] buffer, int fragmentSize);
    }
}
