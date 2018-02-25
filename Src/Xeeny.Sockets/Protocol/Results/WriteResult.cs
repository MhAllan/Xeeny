using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Xeeny.Sockets.Protocol.Results
{
    readonly struct WriteResult
    {
        readonly ArraySegment<byte> _singleMessage;
        readonly IEnumerator<ArraySegment<byte>> _messages;

        public WriteResult(IEnumerable<ArraySegment<byte>> messages)
        {
            _messages = messages.GetEnumerator();
        }

        public WriteResult(ArraySegment<byte> message)
        {
            _singleMessage = message;
            _messages = null;
        }

        public bool HasMessages => _messages == null ? true : _messages.MoveNext();

        public ArraySegment<byte> Message => _messages == null ? _singleMessage: _messages.Current;
    }
}
