using System;
using System.Collections.Generic;
using System.Text;
using Xeeny.Sockets.Protocol.Messages;

namespace Xeeny.Sockets.Protocol.Results
{
    readonly struct ReadResult
    {
        public Message Message => _message;
        public bool IsComplete => _isComplete;

        readonly Message _message;
        readonly bool _isComplete;

        public ReadResult(in Message message, bool isComplete)
        {
            _message = message;
            _isComplete = isComplete;
        }
    }
}
